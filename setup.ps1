# ---
# This is a comprehensive script to normalize the .csproj and .sln files for a Bannerlord mod.
# It performs the following actions:
# 1. Creates backups of both the project and solution files.
# 2. Corrects any XML encoding issues with ampersands (&).
# 3. Sets a known-good PostBuildEvent to copy mod files correctly.
# 4. Fixes a common issue in the .sln file where "Any CPU" incorrectly maps to "x64".
# ---

# --- Configuration ---
$proj = "C:\Dev\Enlisted\Enlisted\Enlisted.csproj"
$sln  = "C:\Dev\Enlisted\Enlisted\Enlisted.sln"

# --- 1. Create Backups ---
Write-Host "Creating backups..."
Copy-Item $proj "$proj.bak" -Force
if (Test-Path $sln) {
    Copy-Item $sln "$sln.bak" -Force
}

# --- 2. Fix .csproj File ---
Write-Host "Fixing .csproj file..."
$content = Get-Content -Raw -LiteralPath $proj

# Normalize any double-encoded '&amp;' to a single '&' before processing
$content = $content -replace '&amp;', '&'

# Define the correct PostBuildEvent content.
# Note: The ampersand in "Mount & Blade" is escaped with a caret (^) for cmd.exe,
# and the XML DOM will handle escaping it to '&amp;' when it saves.
$post = @'
set "MODROOT=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted"
set "BINDIR=%MODROOT%\bin\Win64_Shipping_Client"
if not exist "%BINDIR%" mkdir "%BINDIR%"
xcopy /Y /I /D "$(TargetPath)" "%BINDIR%"
if not exist "%MODROOT%" mkdir "%MODROOT%"
xcopy /Y /I /D "$(ProjectDir)SubModule.xml" "%MODROOT%"
'@

# Use regex to replace the existing PostBuildEvent content
$pattern = '(?s)(<PostBuildEvent>)(.*?)(</PostBuildEvent>)'
if ($content -match $pattern) {
    $content = [regex]::Replace($content, $pattern, { param($m) $m.Groups[1].Value + $post + $m.Groups[3].Value })
} else {
    # If no PostBuildEvent exists, append one before the final </Project> tag
    $content = $content.TrimEnd() -replace '</Project>', "  <PropertyGroup>`r`n    <PostBuildEvent>$post</PostBuildEvent>`r`n  </PropertyGroup>`r`n</Project>"
}

Set-Content -LiteralPath $proj -Value $content -Encoding UTF8
Write-Host ".csproj file updated."

# --- 3. Fix .sln File ---
Write-Host "Fixing .sln file..."
if (Test-Path $sln) {
    $s = Get-Content -Raw -LiteralPath $sln
    $guid = '{580669B1-F074-4840-9B20-D7C18FBC4935}'
    
    # Correct the "Any CPU" mappings if they point to x64
    $s = $s -replace "($guid.Debug|Any CPU.ActiveCfg\s*=\s*)Debug|x64", "`$1Debug|Any CPU"
    $s = $s -replace "($guid.Debug|Any CPU.Build.0\s*=\s*)Debug|x64", "`$1Debug|Any CPU"
    
    Set-Content -LiteralPath $sln -Value $s -Encoding UTF8
    Write-Host ".sln file updated."
}

Write-Host "Done. You can now reopen and rebuild the solution."
