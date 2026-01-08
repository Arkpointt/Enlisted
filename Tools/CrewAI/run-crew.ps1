# CrewAI launcher with proper Windows UTF-8 encoding
# Fixes: [CrewAIEventsBus] Sync handler error 'charmap' codec errors

# Set Windows console to UTF-8
chcp 65001 | Out-Null

# Set PowerShell encoding
$OutputEncoding = [Console]::OutputEncoding = [Text.Encoding]::UTF8

# Set Python encoding
$env:PYTHONIOENCODING = "utf-8"
$env:PYTHONUTF8 = "1"

# Activate venv and run enlisted-crew with all arguments
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir

if (Test-Path ".\.venv\Scripts\Activate.ps1") {
    . ".\.venv\Scripts\Activate.ps1"
}

# Pass all arguments to enlisted-crew
if ($args.Count -gt 0) {
    enlisted-crew @args
} else {
    Write-Host "Usage: .\run-crew.ps1 <command> [options]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  plan -f <name> -d <description>    Design a feature"
    Write-Host "  hunt-bug -d <desc> -e <codes>      Find and fix bugs"
    Write-Host "  implement -p <plan.md>             Build from plan"
    Write-Host "  validate                           Pre-commit check"
    Write-Host "  stats [-c crew] [--costs]          View statistics"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\run-crew.ps1 plan -f 'my-feature' -d 'Add something cool'"
    Write-Host "  .\run-crew.ps1 validate"
}

Pop-Location
