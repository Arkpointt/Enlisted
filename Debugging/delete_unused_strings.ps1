# Delete Unused XML Strings Script
# Removes unused localization strings from enlisted_strings.xml
# Keeps: Used strings (519), Map Incidents (mi_*), Orders (order_*), Core decisions

$ErrorActionPreference = "Stop"
$xmlFile = "C:\Dev\Enlisted\Enlisted\ModuleData\Languages\enlisted_strings.xml"
$backupFile = "C:\Dev\Enlisted\Enlisted\Debugging\enlisted_strings_backup_before_cleanup.xml"
$unusedFile = "C:\Dev\Enlisted\Enlisted\Debugging\xml_string_ids.txt"
$reportFile = "C:\Dev\Enlisted\Enlisted\Debugging\xml_audit_report.txt"

Write-Host "=== Phase 7: Delete Unused Strings ===" -ForegroundColor Cyan
Write-Host ""

# Create backup
Write-Host "Creating backup..." -ForegroundColor Yellow
Copy-Item $xmlFile $backupFile -Force
Write-Host "  Backup saved to: $backupFile" -ForegroundColor Green
Write-Host ""

# Read the report to get unused strings
Write-Host "Loading unused strings list..." -ForegroundColor Yellow
$reportContent = Get-Content $reportFile -Raw

# Extract unused strings from the report
$unusedSection = $reportContent -match "(?s)UNUSED STRINGS.*?The following (\d+) string IDs.*?^  - (.+?)(?=^USED STRINGS)"
$unusedStrings = @()

if ($reportContent -match "(?s)UNUSED STRINGS.*?\n((?:  - .+\n)+)") {
    $unusedBlock = $Matches[1]
    $unusedStrings = $unusedBlock -split "`n" | Where-Object { $_ -match "^  - (.+)$" } | ForEach-Object {
        if ($_ -match "^  - (.+)$") { $Matches[1].Trim() }
    }
}

Write-Host "  Found $($unusedStrings.Count) unused strings" -ForegroundColor White
Write-Host ""

# Define strings to KEEP even if unused (planned features)
$keepPrefixes = @(
    "mi_",           # Map incidents (Phase 6)
    "order_",        # Orders system (Phase 8)
    "dec_",          # Decisions (some used, some planned)
    "Enlisted_",     # Core system strings
    "enlisted_",     # Core system strings
    "News_",         # News system
    "qm_",           # Quartermaster
    "camp_",         # Camp menu
    "promo_",        # Promotions
    "formation_",    # Formations
    "Squad_"         # Squad system
)

# Define DELETE prefixes (old/abandoned systems)
$deletePrefixes = @(
    "brief_",        # Old briefing system
    "act_",          # Old activity system
    "ll_story_",     # Old story system
    "ll_inquiry_",   # Old inquiry system
    "hlm_",          # Old HELMS system
    "player_"        # Old player activity system
)

# Filter unused strings to delete
$stringsToDelete = @()
$stringsToKeep = @()

foreach ($stringId in $unusedStrings) {
    # Check if it starts with a keep prefix
    $shouldKeep = $false
    foreach ($prefix in $keepPrefixes) {
        if ($stringId.StartsWith($prefix)) {
            $shouldKeep = $true
            $stringsToKeep += $stringId
            break
        }
    }
    
    # Check if it starts with a delete prefix (override keep)
    if (-not $shouldKeep) {
        $shouldDelete = $false
        foreach ($prefix in $deletePrefixes) {
            if ($stringId.StartsWith($prefix)) {
                $shouldDelete = $true
                break
            }
        }
        
        # If it's an ll_evt_* string, check if it's a role event (keep NCO/soldier events)
        if ($stringId.StartsWith("ll_evt_") -and -not $shouldDelete) {
            # Keep onboarding and role-based events
            if ($stringId -match "ll_evt_(enlisted|officer|commander|nco|soldier|squad)_") {
                $shouldKeep = $true
                $stringsToKeep += $stringId
            } else {
                $shouldDelete = $true
            }
        }
        
        if ($shouldDelete) {
            $stringsToDelete += $stringId
        }
    }
}

Write-Host "Deletion Strategy:" -ForegroundColor Cyan
Write-Host "  Strings to DELETE: $($stringsToDelete.Count)" -ForegroundColor Red
Write-Host "  Strings to KEEP (planned features): $($stringsToKeep.Count)" -ForegroundColor Green
Write-Host ""

# Show categories being deleted
$deleteCategories = $stringsToDelete | ForEach-Object { ($_ -split '_')[0] } | Group-Object | Sort-Object Count -Descending
Write-Host "Categories being deleted:" -ForegroundColor Yellow
foreach ($cat in $deleteCategories | Select-Object -First 10) {
    Write-Host "  $($cat.Name): $($cat.Count) strings" -ForegroundColor Gray
}
Write-Host ""

# Read XML and delete strings
Write-Host "Processing XML file..." -ForegroundColor Yellow
$xmlContent = Get-Content $xmlFile -Raw

$deletedCount = 0
$pattern = '<string id="([^"]+)"[^>]*(?:/>|>.*?</string>)'

foreach ($stringId in $stringsToDelete) {
    # Escape special regex characters
    $escapedId = [regex]::Escape($stringId)
    $stringPattern = "<string id=`"$escapedId`"[^>]*(?:/>|>.*?</string>)"
    
    if ($xmlContent -match $stringPattern) {
        # Remove the entire line including the string
        $xmlContent = $xmlContent -replace "(?m)^.*$stringPattern.*`r?`n", ""
        $deletedCount++
        
        if ($deletedCount % 100 -eq 0) {
            Write-Host "  Deleted $deletedCount strings..." -ForegroundColor Gray
        }
    }
}

Write-Host "  Total deleted: $deletedCount strings" -ForegroundColor Green
Write-Host ""

# Save cleaned XML
Write-Host "Saving cleaned XML..." -ForegroundColor Yellow
$xmlContent | Out-File -FilePath $xmlFile -Encoding UTF8 -NoNewline
Write-Host "  Saved to: $xmlFile" -ForegroundColor Green
Write-Host ""

# Generate summary
$newSize = (Get-Item $xmlFile).Length
$oldSize = (Get-Item $backupFile).Length
$savedBytes = $oldSize - $newSize
$savedKB = [math]::Round($savedBytes / 1KB, 2)
$savedPercent = [math]::Round(($savedBytes / $oldSize) * 100, 1)

Write-Host "=== Cleanup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Strings deleted: $deletedCount" -ForegroundColor White
Write-Host "  File size reduced: $savedKB KB ($savedPercent%)" -ForegroundColor White
Write-Host "  Old size: $([math]::Round($oldSize / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host "  New size: $([math]::Round($newSize / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host ""
Write-Host "Backup location: $backupFile" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next: Run 'dotnet build' to verify changes" -ForegroundColor Cyan

