# CrewAI Flow Testing Script
# Run this after implementation to validate all three flows

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CrewAI Flow Testing Suite" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Change to CrewAI directory
Set-Location "C:\Dev\Enlisted\Enlisted\Tools\CrewAI"

# Activate virtual environment
Write-Host "[1/4] Activating virtual environment..." -ForegroundColor Yellow
& .\.venv\Scripts\Activate.ps1

# Verify crewai installation
Write-Host "[2/4] Verifying CrewAI installation..." -ForegroundColor Yellow
$crewaiVersion = python -c "import crewai; print(crewai.__version__)" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: CrewAI not found. Run 'pip install crewai' first." -ForegroundColor Red
    exit 1
}
Write-Host "   CrewAI version: $crewaiVersion" -ForegroundColor Green

# Check for OPENAI_API_KEY
Write-Host "[3/4] Checking environment variables..." -ForegroundColor Yellow
if (-not $env:OPENAI_API_KEY) {
    Write-Host "ERROR: OPENAI_API_KEY not set." -ForegroundColor Red
    Write-Host "   Set it with: `$env:OPENAI_API_KEY = 'your-key-here'" -ForegroundColor Yellow
    exit 1
}
Write-Host "   OPENAI_API_KEY: Set" -ForegroundColor Green

Write-Host ""
Write-Host "[4/4] Running Flow Tests..." -ForegroundColor Yellow
Write-Host ""

# Test parameters
$iterations = 3
$model = "gpt-5"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "TEST 1: PlanningFlow" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Description: Tests feature design workflow" -ForegroundColor Gray
Write-Host "Steps: research -> advise -> design -> validate -> auto-fix" -ForegroundColor Gray
Write-Host ""
Write-Host "Running: crewai test -n $iterations -m $model" -ForegroundColor Yellow
Write-Host ""

# Note: crewai test requires the crew to be defined in a specific way
# For flows, we'll test them individually via our CLI
enlisted-crew plan -f "Test Feature" -d "A simple test feature to validate the planning workflow"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "TEST 2: ImplementationFlow" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Description: Tests implementation workflow with smart partial handling" -ForegroundColor Gray
Write-Host "Steps: verify_existing -> route -> implement -> validate -> document" -ForegroundColor Gray
Write-Host ""
Write-Host "Note: ImplementationFlow requires a plan file. Skipping for now." -ForegroundColor Yellow
Write-Host "   To test manually: enlisted-crew implement -p 'path/to/plan.md'" -ForegroundColor Gray
Write-Host ""

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "TEST 3: BugHuntingFlow" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Description: Tests bug investigation and fixing workflow" -ForegroundColor Gray
Write-Host "Steps: investigate -> route_severity -> analyze -> fix -> validate" -ForegroundColor Gray
Write-Host ""
Write-Host "Running: enlisted-crew hunt-bug -d 'Test bug description'" -ForegroundColor Yellow
Write-Host ""

enlisted-crew hunt-bug -d "The supply pressure events are not triggering correctly during daily simulation"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   Testing Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Results:" -ForegroundColor Yellow
Write-Host "   - Check generated plan files in docs/CrewAI_Plans/" -ForegroundColor Gray
Write-Host "   - Review terminal output for performance metrics" -ForegroundColor Gray
Write-Host "   - Validate state persistence files were created" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Review generated outputs for quality and accuracy" -ForegroundColor Gray
Write-Host "   2. Verify database was updated (check enlisted_knowledge.db)" -ForegroundColor Gray
Write-Host "   3. Confirm documentation was synchronized" -ForegroundColor Gray
Write-Host "   4. Run git diff to see file changes" -ForegroundColor Gray
Write-Host ""
