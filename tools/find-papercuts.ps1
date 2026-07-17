param(
    [string]$Path = (Get-Item .).FullName,
    [string]$PapercutsFile = "$env:USERPROFILE\.pi\papercuts.md"
)

if (!(Test-Path $PapercutsFile)) {
    Write-Warning "Papercuts file not found at $PapercutsFile"
    exit 1
}

$content = Get-Content $PapercutsFile -Raw
$entries = $content -split '(?=^## )' -split '(?=\r?\n## )'

$normalizedTarget = $Path.Replace('\', '/').TrimEnd('/').ToLowerInvariant()

foreach ($entry in $entries) {
    if ($entry -match '\*\*Path:\*\*\s*`([^`]+)`') {
        $entryPath = $Matches[1].Replace('\', '/').TrimEnd('/').ToLowerInvariant()
        if ($entryPath -eq $normalizedTarget) {
            Write-Output $entry.Trim()
            Write-Output ""
            Write-Output "---"
            Write-Output ""
        }
    }
}
