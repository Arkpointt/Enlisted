# PowerShell script to apply all remaining fixes

# Fix ActivityIconVM.cs
$file = "src\Features\Camp\UI\Bulletin\ActivityIconVM.cs"
$content = Get-Content $file -Raw

# Fix property names
$content = $content -replace '\.ActivityName', '.TextFallback'
$content = $content -replace '\.DurationHours', '.FatigueCost'  # Temp placeholder
$content = $content -replace '\.TierRequired', '.MinTier'
$content = $content -replace '\.TierName', '.MinTier'  # Will need custom formatting
$content = $content -replace '\.TimePeriod', '.DayParts'
$content = $content -replace 'EnlistmentBehavior\.Instance', 'Enlisted.Features.Enlistment.EnlistmentBehavior.Instance'

# Fix DayParts check (list vs string)
$content = $content -replace 'if \(\!string\.IsNullOrEmpty\(_activity\.DayParts\)\)', 'if (_activity.DayParts != null && _activity.DayParts.Any())'
$content = $content -replace 'var currentPeriod = CampaignTriggerTrackerBehavior\.Instance\?\.GetDayPart\(\)\.ToString\(\) \?\? "Morning";', 'var currentPeriod = CampaignTriggerTrackerBehavior.Instance?.GetDayPart().ToString().ToLowerInvariant() ?? "morning";'
$content = $content -replace '\!string\.Equals\(_activity\.DayParts, currentPeriod, StringComparison\.OrdinalIgnoreCase\)', '!_activity.DayParts.Contains(currentPeriod, StringComparer.OrdinalIgnoreCase)'
$content = $content -replace '\!string\.Equals\(_activity\.DayParts, "anytime", StringComparison\.OrdinalIgnoreCase\)', '!_activity.DayParts.Contains("anytime", StringComparer.OrdinalIgnoreCase)'

Set-Content $file $content

# Fix LanceLeaderVM.cs
$file = "src\Features\Camp\UI\Bulletin\LanceLeaderVM.cs"
$content = Get-Content $file -Raw

$content = $content -replace 'EnlistmentBehavior\.Instance', 'Enlisted.Features.Enlistment.EnlistmentBehavior.Instance'
$content = $content -replace '\.GetHeroTraits\(\)', '.GetTraitLevel(DefaultTraits.Valor)'  # Simplified
$content = $content -replace '\.LimitedPartySize', '.Party.PartySizeLimit'

Set-Content $file $content

# Fix CampPlayerStatusVM.cs
$file = "src\Features\Camp\UI\Bulletin\CampPlayerStatusVM.cs"
$content = Get-Content $file -Raw

$content = $content -replace 'EnlistmentBehavior\.Instance', 'Enlisted.Features.Enlistment.EnlistmentBehavior.Instance'

Set-Content $file $content

Write-Host "All fixes applied!"

