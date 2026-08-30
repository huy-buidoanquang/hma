#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ASCII-only matching: PowerShell 5.1 misreads UTF-8 without BOM.

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root "src\Hma.Desktop.Wpf\Hma.Desktop.Wpf.csproj"))) {
    $root = "f:\projects\hma"
}
$outDir = Join-Path $root "docs\product-tour\images"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class HmaWinShot {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public static void Capture(IntPtr hwnd, string path) {
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        RECT r;
        GetWindowRect(hwnd, out r);
        int w = Math.Max(1, r.Right - r.Left);
        int h = Math.Max(1, r.Bottom - r.Top);
        using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp)) {
            IntPtr hdc = g.GetHdc();
            PrintWindow(hwnd, hdc, 2);
            g.ReleaseHdc(hdc);
            bmp.Save(path, ImageFormat.Png);
        }
    }
    public static int Width(IntPtr hwnd) {
        RECT r;
        GetWindowRect(hwnd, out r);
        return r.Right - r.Left;
    }
}
"@ -ReferencedAssemblies System.Drawing

function Get-UiaList($collection) {
    $list = @()
    if ($null -eq $collection) { return $list }
    for ($i = 0; $i -lt $collection.Count; $i++) {
        $list += $collection.Item($i)
    }
    return $list
}

function Get-HmaProcess {
    return Get-Process -Name "Hma.Desktop.Wpf" -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Wait-HmaWindow([int]$minWidth, [int]$timeoutSec) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $proc = Get-HmaProcess
        if ($null -ne $proc) {
            $hwnd = $proc.MainWindowHandle
            if ($null -ne $hwnd -and [int64]$hwnd -ne 0) {
                $width = [HmaWinShot]::Width([IntPtr]$hwnd)
                if ($width -ge $minWidth) {
                    $el = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$hwnd)
                    Write-Host ("found window w={0} title={1}" -f $width, $el.Current.Name)
                    return $el
                }
            }
        }
        Start-Sleep -Milliseconds 400
    }
    $proc = Get-HmaProcess
    $info = "none"
    if ($null -ne $proc) {
        $info = ("pid={0} handle={1} title={2}" -f $proc.Id, $proc.MainWindowHandle, $proc.MainWindowTitle)
    }
    throw ("timeout waiting for HMA window minWidth={0}. {1}" -f $minWidth, $info)
}

function Save-Shot($window, [string]$fileName) {
    Start-Sleep -Milliseconds 500
    $hwnd = [IntPtr]$window.Current.NativeWindowHandle
    $path = Join-Path $outDir $fileName
    [HmaWinShot]::Capture($hwnd, $path)
    Write-Host "saved $fileName"
}

function Find-Descendant($parent, $controlType, [string]$name = $null) {
    $typeCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $controlType)
    if ([string]::IsNullOrEmpty($name)) {
        return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $typeCond)
    }
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $and = New-Object System.Windows.Automation.AndCondition($typeCond, $nameCond)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $and)
}

function Set-EditValue($edit, [string]$value) {
    $edit.SetFocus()
    Start-Sleep -Milliseconds 150
    try {
        $vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        $vp.SetValue($value)
    } catch {
        [System.Windows.Forms.SendKeys]::SendWait("^a")
        Start-Sleep -Milliseconds 80
        [System.Windows.Forms.SendKeys]::SendWait($value)
    }
}

function Invoke-ButtonByName($window, [string]$name) {
    $btn = Find-Descendant $window ([System.Windows.Automation.ControlType]::Button) $name
    if ($null -eq $btn) { throw "button not found: $name" }
    $inv = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $inv.Invoke()
}

function Invoke-FirstButton($window) {
    $btn = Find-Descendant $window ([System.Windows.Automation.ControlType]::Button)
    if ($null -eq $btn) { throw "no button on window" }
    Write-Host ("click button: {0}" -f $btn.Current.Name)
    $inv = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $inv.Invoke()
}

function Select-NavIndex($window, [int]$index) {
    $list = Find-Descendant $window ([System.Windows.Automation.ControlType]::List)
    if ($null -eq $list) { throw "nav list not found" }
    $itemCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    $items = Get-UiaList ($list.FindAll([System.Windows.Automation.TreeScope]::Children, $itemCond))
    if ($items.Count -eq 0) {
        $items = Get-UiaList ($list.FindAll([System.Windows.Automation.TreeScope]::Descendants, $itemCond))
    }
    if ($index -ge $items.Count) { throw ("nav index {0} >= {1}" -f $index, $items.Count) }
    $item = $items[$index]
    Write-Host ("nav [{0}] {1}" -f $index, $item.Current.Name)
    $sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $sel.Select()
    Start-Sleep -Milliseconds 1400
}

function Select-FirstGridRow($window) {
    $grid = Find-Descendant $window ([System.Windows.Automation.ControlType]::DataGrid)
    if ($null -eq $grid) { return }
    $rowCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataItem)
    $row = $grid.FindFirst([System.Windows.Automation.TreeScope]::Children, $rowCond)
    if ($null -eq $row) {
        $row = $grid.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $rowCond)
    }
    if ($null -eq $row) { return }
    try {
        $row.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Start-Sleep -Milliseconds 300
    } catch {}
}

Get-Process -Name "Hma.Desktop.Wpf" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$exe = Join-Path $root "src\Hma.Desktop.Wpf\bin\Debug\net10.0-windows\Hma.Desktop.Wpf.exe"
if (-not (Test-Path $exe)) { throw "exe not found: $exe" }
$wd = Split-Path $exe
Write-Host "starting $exe"
$proc = Start-Process -FilePath $exe -WorkingDirectory $wd -PassThru

try {
    $login = Wait-HmaWindow 400 90
    Save-Shot $login "00-login.png"

    $idCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "PasswordBox")
    $pwd = $login.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $idCond)

    $edits = Get-UiaList ($login.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Edit))))
    if ($edits.Count -lt 1) { throw "login field not found" }

    $userEdit = $edits[0]
    if ($null -ne $pwd -and $edits.Count -ge 2) {
        foreach ($e in $edits) {
            if ($e.Current.NativeWindowHandle -ne $pwd.Current.NativeWindowHandle) {
                $userEdit = $e
                break
            }
        }
    }
    Set-EditValue $userEdit "admin"
    Start-Sleep -Milliseconds 200

    if ($null -ne $pwd) {
        $pwd.SetFocus()
        Start-Sleep -Milliseconds 150
        [System.Windows.Forms.SendKeys]::SendWait("admin123")
    } else {
        [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
        Start-Sleep -Milliseconds 150
        [System.Windows.Forms.SendKeys]::SendWait("admin123")
    }
    Start-Sleep -Milliseconds 250
    Invoke-FirstButton $login
    Start-Sleep -Seconds 2

    $main = Wait-HmaWindow 1000 60
    $hwnd = [IntPtr]$main.Current.NativeWindowHandle
    [HmaWinShot]::SetWindowPos($hwnd, [IntPtr]::Zero, 40, 40, 1440, 900, 0x0040)
    Start-Sleep -Milliseconds 800
    $main = Wait-HmaWindow 1000 10

    $screens = @(
        @{ File = "01-dashboard.png"; Index = 0 },
        @{ File = "02-customers.png"; Index = 1 },
        @{ File = "03-partners.png"; Index = 2 },
        @{ File = "04-drivers.png"; Index = 3 },
        @{ File = "05-vehicles.png"; Index = 4 },
        @{ File = "06-employees.png"; Index = 5 },
        @{ File = "07-departments.png"; Index = 6 },
        @{ File = "08-job-titles.png"; Index = 7 },
        @{ File = "09-cities.png"; Index = 8 },
        @{ File = "10-price-lists.png"; Index = 9 },
        @{ File = "11-dispatch.png"; Index = 10 },
        @{ File = "12-lookup.png"; Index = 11 },
        @{ File = "13-reconcile.png"; Index = 12 },
        @{ File = "14-statements.png"; Index = 13 },
        @{ File = "15-reports.png"; Index = 14 },
        @{ File = "16-settings.png"; Index = 15 },
        @{ File = "17-users.png"; Index = 16 }
    )

    foreach ($s in $screens) {
        Select-NavIndex $main $s.Index
        Save-Shot $main $s.File
    }

    try {
        Select-NavIndex $main 10
        Select-FirstGridRow $main
        Invoke-ButtonByName $main "Xem"
        Start-Sleep -Seconds 2
        Save-Shot $main "18-dispatch-detail.png"
    } catch {
        Write-Host ("skip dispatch detail: {0}" -f $_.Exception.Message)
    }

    try {
        Select-NavIndex $main 1
        Select-FirstGridRow $main
        Invoke-ButtonByName $main "Xem"
        Start-Sleep -Seconds 1
        Save-Shot $main "19-customer-editor.png"
    } catch {
        Write-Host ("skip customer editor: {0}" -f $_.Exception.Message)
    }

    try {
        Select-NavIndex $main 3
        Select-FirstGridRow $main
        Invoke-ButtonByName $main "Xem"
        Start-Sleep -Seconds 1
        Save-Shot $main "20-driver-history.png"
    } catch {
        Write-Host ("skip driver history: {0}" -f $_.Exception.Message)
    }

    Write-Host "DONE"
}
finally {
    Get-Process -Name "Hma.Desktop.Wpf" -ErrorAction SilentlyContinue | Stop-Process -Force
    if ($null -ne $proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
