# Windows-only end-to-end check for image paste and on-demand preview.
# Run: pwsh -NoProfile -Sta -File scripts/validate-image-workflow.ps1
param()

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    throw 'This validation requires an STA PowerShell process. Run pwsh -Sta -File scripts/validate-image-workflow.ps1.'
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ImageWorkflowMouse {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }
}
'@

$repo = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $repo '.artifacts\image-workflow-validation'
$appOutput = Join-Path $artifactRoot 'app'
$dataDir = Join-Path $artifactRoot 'data'
Remove-Item $artifactRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item $appOutput -ItemType Directory -Force | Out-Null
New-Item $dataDir -ItemType Directory -Force | Out-Null

& dotnet build (Join-Path $repo 'src\StickyNotes.App\StickyNotes.App.csproj') -c Release --no-restore "-p:OutputPath=$appOutput"
if ($LASTEXITCODE -ne 0) { throw 'Image workflow build failed.' }
$exe = Join-Path $appOutput 'StickyNotes.App.exe'

function Start-IsolatedApp {
    $start = [Diagnostics.ProcessStartInfo]::new($exe)
    $start.UseShellExecute = $false
    $start.Environment['WIN_STICKY_NOTES_DATA_DIR'] = $dataDir
    [Diagnostics.Process]::Start($start)
}

function Find-Elements([Diagnostics.Process]$Process) {
    $condition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::ProcessIdProperty,
        $Process.Id)
    [Windows.Automation.AutomationElement]::RootElement.FindAll(
        [Windows.Automation.TreeScope]::Descendants,
        $condition)
}

function Wait-ForNoteWindow([Diagnostics.Process]$Process) {
    $deadline = [DateTime]::UtcNow.AddSeconds(12)
    while ([DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 200
        $note = Find-Elements $Process |
            Where-Object { $_.Current.ControlType -eq [Windows.Automation.ControlType]::Window -and $_.Current.Name -eq 'Note' } |
            Select-Object -First 1
        if ($note) { return $note }
    }
    throw 'Note Window did not appear.'
}

# Drive the real Ctrl+V path with a generated clipboard bitmap.
$bitmap = [Drawing.Bitmap]::new(96, 64)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([Drawing.Color]::CornflowerBlue)
$graphics.FillEllipse([Drawing.Brushes]::Gold, 18, 10, 45, 45)
$graphics.Dispose()
[Windows.Forms.Clipboard]::SetImage($bitmap)
$bitmap.Dispose()

$process = Start-IsolatedApp
try {
    $noteWindow = Wait-ForNoteWindow $process
    $noteWindow.SetFocus()
    Start-Sleep -Milliseconds 250
    [Windows.Forms.SendKeys]::SendWait('^v')
    Start-Sleep -Seconds 2

    if ($process.HasExited) { throw "Application exited after image paste (exit code $($process.ExitCode))." }
    $attachments = @(Get-ChildItem (Join-Path $dataDir 'attachments') -Filter *.png -Recurse -ErrorAction SilentlyContinue)
    if ($attachments.Count -ne 1) { throw "Expected one imported PNG; found $($attachments.Count)." }
    $notesPath = Join-Path $dataDir 'notes.json'
    $notesText = Get-Content $notesPath -Raw
    if ($notesText -notmatch '!\[image\]\(attachments/') { throw 'Markdown image reference was not saved.' }
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}

# Reopen the same note at minimum width and verify the popup remains fixed-size and closes externally.
$notes = @(Get-Content $notesPath -Raw | ConvertFrom-Json)
$notes[0].Width = 280
$notes | ConvertTo-Json -Depth 10 -AsArray | Set-Content $notesPath -Encoding utf8

$process = Start-IsolatedApp
try {
    $noteWindow = Wait-ForNoteWindow $process
    $elements = Find-Elements $process
    $icon = $elements |
        Where-Object {
            $_.Current.ControlType -eq [Windows.Automation.ControlType]::Button -and
            $_.Current.Name -eq '▧' -and
            $_.Current.BoundingRectangle.Width -lt 30
        } | Select-Object -First 1
    if (-not $icon) { throw 'Image preview icon was not rendered before the Markdown link.' }

    $invoke = $icon.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Start-Sleep -Seconds 1
    if ($process.HasExited) { throw 'Application exited while opening image preview.' }

    $popup = Find-Elements $process |
        Where-Object {
            $_.Current.ControlType -eq [Windows.Automation.ControlType]::Window -and
            [Math]::Abs($_.Current.BoundingRectangle.Width - 420) -lt 2 -and
            [Math]::Abs($_.Current.BoundingRectangle.Height - 320) -lt 2
        } | Select-Object -First 1
    if (-not $popup) { throw 'Fixed 420 × 320 preview did not appear.' }

    $noteRect = $noteWindow.Current.BoundingRectangle
    $popupRect = $popup.Current.BoundingRectangle
    $centerDelta = [Math]::Abs(($noteRect.Left + $noteRect.Width / 2) - ($popupRect.Left + $popupRect.Width / 2))
    if ($centerDelta -gt 3) { throw "Preview was not centered on the narrow note (delta $centerDelta px)." }
    if ($popupRect.Width -le $noteRect.Width) { throw 'Preview incorrectly shrank to the narrow note width.' }

    [ImageWorkflowMouse]::Click([int]($noteRect.Left + 10), [int]($noteRect.Top + 10))
    Start-Sleep -Milliseconds 500
    $popupStillOpen = Find-Elements $process |
        Where-Object {
            $_.Current.ControlType -eq [Windows.Automation.ControlType]::Window -and
            [Math]::Abs($_.Current.BoundingRectangle.Width - 420) -lt 2
        } | Select-Object -First 1
    if ($popupStillOpen) { throw 'Clicking outside the preview did not close it.' }

    Write-Output "PASS: image paste survived, Markdown and attachment were saved, the narrow-note preview stayed 420 × 320, and an outside click closed it."
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}
