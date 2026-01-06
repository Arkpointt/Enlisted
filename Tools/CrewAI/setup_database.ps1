# Setup SQLite Knowledge Database for CrewAI
# This creates and initializes the enlisted_knowledge.db database

$DB_PATH = "C:\Dev\SQLite3\enlisted_knowledge.db"
$SCHEMA_PATH = "Tools\CrewAI\database_schema.sql"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Enlisted CrewAI - Knowledge Database Setup" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if SQLite3 directory exists
if (-not (Test-Path "C:\Dev\SQLite3")) {
    Write-Host "[1/4] Creating C:\Dev\SQLite3 directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path "C:\Dev\SQLite3" -Force | Out-Null
    Write-Host "      [OK] Directory created" -ForegroundColor Green
} else {
    Write-Host "[1/4] C:\Dev\SQLite3 directory exists" -ForegroundColor Green
}

# Check if database already exists
if (Test-Path $DB_PATH) {
    Write-Host "`n[2/4] Database already exists at $DB_PATH" -ForegroundColor Yellow
    $response = Read-Host "      Do you want to recreate it? (y/N)"
    if ($response -ne 'y') {
        Write-Host "      [SKIP] Keeping existing database" -ForegroundColor Yellow
        Write-Host "`n[DONE] Setup complete - using existing database`n" -ForegroundColor Green
        exit 0
    }
    Write-Host "      [OK] Will recreate database" -ForegroundColor Yellow
    Remove-Item $DB_PATH -Force
} else {
    Write-Host "`n[2/4] No existing database found" -ForegroundColor Yellow
}

# Check if schema file exists
if (-not (Test-Path $SCHEMA_PATH)) {
    Write-Host "`n[ERROR] Schema file not found: $SCHEMA_PATH" -ForegroundColor Red
    Write-Host "        Make sure you're running this from the project root`n" -ForegroundColor Red
    exit 1
}

Write-Host "`n[3/4] Creating database and loading schema..." -ForegroundColor Yellow

try {
    # Create database and load schema using sqlite3 command
    $schemaContent = Get-Content $SCHEMA_PATH -Raw
    
    # Use sqlite3.exe if available, otherwise use System.Data.SQLite
    $sqlite3Exe = Get-Command sqlite3.exe -ErrorAction SilentlyContinue
    
    if ($sqlite3Exe) {
        Write-Host "      Using sqlite3.exe command-line tool" -ForegroundColor Gray
        $schemaContent | sqlite3.exe $DB_PATH
    } else {
        Write-Host "      Using PowerShell SQLite library" -ForegroundColor Gray
        
        # Load SQLite assembly (may need to install: Install-Module -Name PSSQLite)
        try {
            Add-Type -Path "System.Data.SQLite.dll" -ErrorAction Stop
        } catch {
            Write-Host "`n[ERROR] SQLite not available. Please install sqlite3.exe or System.Data.SQLite`n" -ForegroundColor Red
            Write-Host "Option 1: Download sqlite3.exe from https://www.sqlite.org/download.html" -ForegroundColor Yellow
            Write-Host "Option 2: Install-Module -Name PSSQLite`n" -ForegroundColor Yellow
            exit 1
        }
        
        $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DB_PATH;Version=3;")
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $schemaContent
        $cmd.ExecuteNonQuery() | Out-Null
        $conn.Close()
    }
    
    Write-Host "      [OK] Database created and schema loaded" -ForegroundColor Green
    
} catch {
    Write-Host "`n[ERROR] Failed to create database: $_" -ForegroundColor Red
    Write-Host "        Try running: sqlite3 $DB_PATH < $SCHEMA_PATH`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n[4/4] Verifying database..." -ForegroundColor Yellow

# Test if database was created successfully
if (Test-Path $DB_PATH) {
    $fileSize = (Get-Item $DB_PATH).Length
    Write-Host "      [OK] Database file created ($fileSize bytes)" -ForegroundColor Green
} else {
    Write-Host "      [ERROR] Database file not found after creation" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "[SUCCESS] Knowledge database ready!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Database location: $DB_PATH" -ForegroundColor White
Write-Host "`nTables created:" -ForegroundColor White
Write-Host "  - content_items (events, decisions, orders)" -ForegroundColor Gray
Write-Host "  - balance_values (XP, gold, thresholds)" -ForegroundColor Gray
Write-Host "  - error_catalog (error codes and solutions)" -ForegroundColor Gray
Write-Host "  - system_dependencies (system relationships)" -ForegroundColor Gray
Write-Host "  - implementation_history (what was built when)" -ForegroundColor Gray
Write-Host "  - api_patterns (Bannerlord API usage)" -ForegroundColor Gray
Write-Host "  - tier_definitions (player progression)" -ForegroundColor Gray

Write-Host "`nSeed data loaded:" -ForegroundColor White
Write-Host "  - 10 tier definitions" -ForegroundColor Gray
Write-Host "  - 4 common error codes" -ForegroundColor Gray
Write-Host "  - 8 balance values" -ForegroundColor Gray
Write-Host "  - 5 system dependencies" -ForegroundColor Gray
Write-Host "  - 4 API patterns" -ForegroundColor Gray

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Update Tools/CrewAI/src/enlisted_crew/tools/__init__.py" -ForegroundColor White
Write-Host "  2. Add database tools to agent tool lists in crew.py" -ForegroundColor White
Write-Host "  3. Test: enlisted-crew plan -f 'test' -d 'test'" -ForegroundColor White
Write-Host ""
