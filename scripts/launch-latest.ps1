$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName PresentationFramework

$running = Get-Process -Name 'StickyNotes.App' -ErrorAction SilentlyContinue
if ($running) {
    [System.Windows.MessageBox]::Show(
        'Win Sticky Notes is already running. Close the old version before launching the latest version.',
        'Win Sticky Notes',
        [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]::Information) | Out-Null
    exit 0
}

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\StickyNotes.App\StickyNotes.App.csproj'
$publishDirectory = Join-Path $repository '.artifacts\desktop-app'

try {
    $output = & dotnet publish $project -c Release --nologo "-p:PublishDir=$publishDirectory\" 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw $output.Trim() }

    $application = Join-Path $publishDirectory 'StickyNotes.App.exe'
    Start-Process -FilePath $application -WorkingDirectory $publishDirectory
}
catch {
    [System.Windows.MessageBox]::Show(
        "The latest version could not be built or started.`n`n$($_.Exception.Message)",
        'Win Sticky Notes',
        [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]::Error) | Out-Null
    exit 1
}
