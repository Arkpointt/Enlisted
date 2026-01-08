# Setup SQLite Knowledge Database
# Run from Tools/CrewAI/database/ directory

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Enlisted CrewAI - Database Setup" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$DB_FILE = "enlisted_knowledge.db"
$SCHEMA_FILE = "schema.sql"

# Check if schema exists
if (-not (Test-Path $SCHEMA_FILE)) {
    Write-Host "[ERROR] Schema file not found: $SCHEMA_FILE" -ForegroundColor Red
    Write-Host "        Make sure you're running this from Tools/CrewAI/database/" -ForegroundColor Yellow
    exit 1
}

# Check if database already exists
if (Test-Path $DB_FILE) {
    Write-Host "[1/2] Database already exists" -ForegroundColor Yellow
    $response = Read-Host "      Recreate? (y/N)"
    if ($response -ne 'y') {
        Write-Host "      [SKIP] Keeping existing database" -ForegroundColor Yellow
        exit 0
    }
    Remove-Item $DB_FILE -Force
}

Write-Host "[1/2] Creating database..." -ForegroundColor Yellow

# Try to find sqlite3.exe
$sqlite3 = $null
$searchPaths = @(
    "sqlite3.exe",  # In PATH (preferred for cross-platform)
    "C:\Dev\SQLite3\sqlite3.exe"  # Windows dev location
)

foreach ($path in $searchPaths) {
    if (Test-Path $path -ErrorAction SilentlyContinue) {
        $sqlite3 = $path
        break
    }
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $sqlite3 = $path
        break
    }
}

if (-not $sqlite3) {
    Write-Host "[ERROR] sqlite3.exe not found" -ForegroundColor Red
    Write-Host "        Download from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    Write-Host "        Or install via: winget install SQLite.SQLite" -ForegroundColor Yellow
    exit 1
}

# Create database from schema
try {
    Get-Content $SCHEMA_FILE | & $sqlite3 $DB_FILE
    Write-Host "      [OK] Database created" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to create database: $_" -ForegroundColor Red
    exit 1
}

# Verify tables
Write-Host "[2/2] Verifying tables..." -ForegroundColor Yellow
$tables = & $sqlite3 $DB_FILE ".tables"
$tableCount = ($tables -split '\s+' | Where-Object { $_ }).Count

if ($tableCount -gt 0) {
    Write-Host "      [OK] $tableCount tables created" -ForegroundColor Green
    Write-Host "`n[DONE] Database setup complete!" -ForegroundColor Green
    Write-Host "       Location: $(Get-Location)\$DB_FILE`n" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "      [ERROR] No tables found - schema may have failed" -ForegroundColor Red
    exit 1
}
