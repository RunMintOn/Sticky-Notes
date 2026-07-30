# Non-interactive regression check for image insertion and image resource loading.
# This script does not open app windows, move the pointer, send keys, or modify the clipboard.
# Run: pwsh -NoProfile -File scripts/validate-image-workflow.ps1
$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$output = Join-Path $repo '.artifacts\image-regression-tests'
& dotnet test (Join-Path $repo 'tests\StickyNotes.App.Tests\StickyNotes.App.Tests.csproj') `
    -c Release `
    --no-restore `
    "-p:OutputPath=$output" `
    --filter 'FullyQualifiedName~MarkdownEditorIntegrationTests' `
    --logger 'console;verbosity=minimal'
if ($LASTEXITCODE -ne 0) { throw 'Image workflow regression check failed.' }
