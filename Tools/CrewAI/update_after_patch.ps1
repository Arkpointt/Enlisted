# Update CrewAI After Bannerlord Patch
# Run this after decompiling a new Bannerlord version

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Enlisted CrewAI - Post-Patch Update" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Check decompile folder exists (checks multiple locations)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)

# Check environment variable first
$decompilePath = $env:BANNERLORD_DECOMPILE_PATH
if (-not $decompilePath) {
    # Try sibling folder (standard location: C:\Dev\Enlisted\Decompile)
    $siblingDecompile = Join-Path (Split-Path -Parent $projectRoot) "Decompile"
    if (Test-Path $siblingDecompile) {
        $decompilePath = $siblingDecompile
    } else {
        # Fall back to workspace
        $workspaceDecompile = Join-Path $projectRoot "Decompile"
        if (Test-Path $workspaceDecompile) {
            $decompilePath = $workspaceDecompile
        } else {
            $decompilePath = "C:\Dev\Enlisted\Decompile"
        }
    }
}

if (-not (Test-Path $decompilePath)) {
    Write-Host "[ERROR] Decompile folder not found at $decompilePath" -ForegroundColor Red
    Write-Host "        Decompile the new Bannerlord version to C:\Dev\Enlisted\Decompile first!" -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/3] Decompile folder found at $decompilePath" -ForegroundColor Green

# Step 2: Rebuild MCP Index
Write-Host "`n[2/3] Rebuilding MCP Index..." -ForegroundColor Yellow
Push-Location mcp_servers
try {
    & python build_index.py
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      [ERROR] Index build failed" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "      [OK] MCP Index rebuilt" -ForegroundColor Green
} finally {
    Pop-Location
}

# Step 3: Run Tests
Write-Host "`n[3/3] Running tests..." -ForegroundColor Yellow
& python test_all.py

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "[DONE] Post-patch update complete!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green
} else {
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "[WARN] Tests had failures - review above" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
}
