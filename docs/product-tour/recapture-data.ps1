#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = "f:\projects\hma"
$outDir = Join-Path $root "docs\product-tour\images"
. (Join-Path $PSScriptRoot "uia-lib.ps1")

Get-Process -Name "Hma.Desktop.Wpf" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$exe = Join-Path $root "src\Hma.Desktop.Wpf\bin\Debug\net10.0-windows\Hma.Desktop.Wpf.exe"
$proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru

try {
    $login = Wait-HmaWindow 400 90
    $idCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "PasswordBox")
    $pwd = $login.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $idCond)
    $edits = Get-UiaList ($login.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Edit))))
    Set-EditValue $edits[0] "admin"
    if ($null -ne $pwd) { $pwd.SetFocus(); Start-Sleep -Milliseconds 120; [System.Windows.Forms.SendKeys]::SendWait("admin123") }
    Invoke-FirstButton $login
    Start-Sleep -Seconds 2
    $main = Wait-HmaWindow 1000 60
    $hwnd = [IntPtr]$main.Current.NativeWindowHandle
    [HmaWinShot]::SetWindowPos($hwnd, [IntPtr]::Zero, 40, 40, 1440, 900, 0x0040)
    Start-Sleep -Milliseconds 700
    $main = Wait-HmaWindow 1000 10

    $tim = "T" + [char]0x00EC + "m"
    $tongHop = "T" + [char]0x1ED5 + "ng h" + [char]0x1EE3 + "p"
    $taiBaoCao = "T" + [char]0x1EA3 + "i b" + [char]0x00E1 + "o c" + [char]0x00E1 + "o"

    function Dump-Buttons($window) {
        $col = $window.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Button)))
        $btns = Get-UiaList $col
        for ($i = 0; $i -lt $btns.Count; $i++) {
            Write-Host ("  btn[{0}] '{1}'" -f $i, $btns[$i].Current.Name)
        }
    }

    # Lookup: search by plate fragment
    try {
        Select-NavIndex $main 11
        $edit = Find-Descendant $main ([System.Windows.Automation.ControlType]::Edit)
        if ($null -ne $edit) { Set-EditValue $edit "29A" }
        Invoke-ButtonByName $main $tim
        Start-Sleep -Seconds 2
        Save-Shot $main "12-lookup.png" $outDir
    } catch {
        Write-Host ("lookup fail: {0}" -f $_.Exception.Message)
        Dump-Buttons $main
    }

    # Monthly statement: first customer + generate
    try {
        Select-NavIndex $main 13
        Start-Sleep -Milliseconds 800
        $combo = Find-Descendant $main ([System.Windows.Automation.ControlType]::ComboBox)
        if ($null -ne $combo) {
            try {
                $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
                Start-Sleep -Milliseconds 400
            } catch {}
            $itemCond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::ListItem)
            $items = Get-UiaList ($combo.FindAll([System.Windows.Automation.TreeScope]::Descendants, $itemCond))
            if ($items.Count -gt 0) {
                try {
                    $items[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
                } catch {}
            }
            Start-Sleep -Milliseconds 300
        }
        Invoke-ButtonByName $main $tongHop
        Start-Sleep -Seconds 3
        Save-Shot $main "14-statements.png" $outDir
    } catch {
        Write-Host ("statement fail: {0}" -f $_.Exception.Message)
        Dump-Buttons $main
    }

    # Reports: load period
    try {
        Select-NavIndex $main 14
        Invoke-ButtonByName $main $taiBaoCao
        Start-Sleep -Seconds 2
        Save-Shot $main "15-reports.png" $outDir
    } catch {
        Write-Host ("report fail: {0}" -f $_.Exception.Message)
        Dump-Buttons $main
    }

    Write-Host "RECAPTURE DONE"
}
finally {
    Get-Process -Name "Hma.Desktop.Wpf" -ErrorAction SilentlyContinue | Stop-Process -Force
    if ($null -ne $proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
