# Check Player Condition State
# This script helps diagnose why Urgent Medical Care decision is appearing
# when no sickness/injury is visible in Player Status

Write-Host "=== Enlisted Player Condition Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

$saveDir = "$env:USERPROFILE\Documents\Mount and Blade II Bannerlord\Game Saves"
Write-Host "Checking save directory: $saveDir" -ForegroundColor Yellow

if (Test-Path $saveDir) {
    $saves = Get-ChildItem $saveDir -Filter "*.sav" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
    
    Write-Host "Recent save files found:" -ForegroundColor Green
    foreach ($save in $saves) {
        Write-Host "  - $($save.Name) ($(Get-Date $save.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "To check your player condition state:" -ForegroundColor Cyan
    Write-Host "1. Look in your most recent save file for these save data keys:" -ForegroundColor White
    Write-Host "   - pc_injSeverity (0=None, 1=Minor, 2=Moderate, 3=Severe, 4=Critical)" -ForegroundColor Gray
    Write-Host "   - pc_injType (injury type name)" -ForegroundColor Gray
    Write-Host "   - pc_injDays (days remaining)" -ForegroundColor Gray
    Write-Host "   - pc_illSeverity (0=None, 1=Mild, 2=Moderate, 3=Severe, 4=Critical)" -ForegroundColor Gray
    Write-Host "   - pc_illType (illness type name)" -ForegroundColor Gray
    Write-Host "   - pc_illDays (days remaining)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. If pc_injSeverity >= 3 or pc_illSeverity >= 3, that's triggering Urgent Medical Care" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Common causes:" -ForegroundColor Cyan
    Write-Host "  - Save data corruption (severity set but days = 0)" -ForegroundColor White
    Write-Host "  - Combat injury that wasn't properly displayed" -ForegroundColor White
    Write-Host "  - Event that applied condition but status UI didn't update" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "Save directory not found at: $saveDir" -ForegroundColor Red
}

Write-Host "=== Quick Fix ===" -ForegroundColor Cyan
Write-Host "If you want to clear any hidden conditions:" -ForegroundColor White
Write-Host "1. Take the 'Get to the surgeon. Now.' option (costs 200 gold)" -ForegroundColor Yellow
Write-Host "   This will clear the severe condition and prevent the decision from reappearing" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Or wait for natural recovery (if days remaining > 0)" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== Root Cause Investigation ===" -ForegroundColor Cyan
Write-Host "The bug is likely one of these:" -ForegroundColor White
Write-Host "A. Status display not checking PlayerConditionBehavior.State" -ForegroundColor Gray
Write-Host "B. Condition was applied but message was missed/not displayed" -ForegroundColor Gray
Write-Host "C. Save data has stale condition from previous session" -ForegroundColor Gray
Write-Host ""
