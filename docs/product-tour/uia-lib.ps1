#Requires -Version 5.1
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if (-not ([System.Management.Automation.PSTypeName]"HmaWinShot").Type) {
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
}

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

function Save-Shot($window, [string]$fileName, [string]$dir) {
    Start-Sleep -Milliseconds 500
    $hwnd = [IntPtr]$window.Current.NativeWindowHandle
    $path = Join-Path $dir $fileName
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
    Write-Host ("click button: {0}" -f $btn.Current.Name)
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
