# Reset CrewAI Memory
# Run this after major refactors to clear stale agent learnings
#
# Usage:
#   .\reset-memory.ps1           # Clear all memory
#   .\reset-memory.ps1 -Short    # Clear only short-term
#   .\reset-memory.ps1 -Long     # Clear only long-term  
#   .\reset-memory.ps1 -Entity   # Clear only entity memory

param(
    [switch]$Short,
    [switch]$Long,
    [switch]$Entity,
    [switch]$Help
)

if ($Help) {
    Write-Host @"

Enlisted CrewAI - Memory Reset Tool
===================================

Clears CrewAI memory to remove stale patterns after major refactors.

Usage:
  .\reset-memory.ps1           # Clear ALL memory types
  .\reset-memory.ps1 -Short    # Clear short-term memory only
  .\reset-memory.ps1 -Long     # Clear long-term memory only
  .\reset-memory.ps1 -Entity   # Clear entity memory only

When to use:
  - After renaming systems/files
  - After major architectural refactors
  - After deleting/deprecating systems
  - If agents reference outdated patterns

Memory Types:
  Short-Term: Session context (ChromaDB) - cleared between runs anyway
  Long-Term:  Learned patterns (SQLite) - persists across runs
  Entity:     Entity relationships (ChromaDB) - persists across runs

"@
    exit 0
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Enlisted CrewAI - Memory Reset" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$memoryPath = "$env:LOCALAPPDATA\CrewAI\enlisted_crew"

if (-not (Test-Path $memoryPath)) {
    Write-Host "[INFO] No memory folder found at $memoryPath" -ForegroundColor Yellow
    Write-Host "       Memory will be created fresh on next run.`n" -ForegroundColor Yellow
    exit 0
}

# Determine what to clear
$clearAll = -not ($Short -or $Long -or $Entity)

$cleared = @()

# Clear Short-Term Memory
if ($clearAll -or $Short) {
    $stmPath = Join-Path $memoryPath "short_term_memory"
    if (Test-Path $stmPath) {
        Remove-Item -Recurse -Force $stmPath
        $cleared += "short_term_memory"
        Write-Host "[OK] Cleared short-term memory" -ForegroundColor Green
    } else {
        Write-Host "[--] Short-term memory already empty" -ForegroundColor DarkGray
    }
}

# Clear Long-Term Memory
if ($clearAll -or $Long) {
    $ltmPath = Join-Path $memoryPath "long_term_memory"
    if (Test-Path $ltmPath) {
        Remove-Item -Recurse -Force $ltmPath
        $cleared += "long_term_memory"
        Write-Host "[OK] Cleared long-term memory folder" -ForegroundColor Green
    }
    
    $ltmDb = Join-Path $memoryPath "long_term_memory_storage.db"
    if (Test-Path $ltmDb) {
        Remove-Item -Force $ltmDb
        $cleared += "long_term_memory_storage.db"
        Write-Host "[OK] Cleared long-term memory database" -ForegroundColor Green
    }
    
    if (-not (Test-Path $ltmPath) -and -not (Test-Path $ltmDb)) {
        Write-Host "[--] Long-term memory already empty" -ForegroundColor DarkGray
    }
}

# Clear Entity Memory
if ($clearAll -or $Entity) {
    $entityPath = Join-Path $memoryPath "entities"
    if (Test-Path $entityPath) {
        Remove-Item -Recurse -Force $entityPath
        $cleared += "entities"
        Write-Host "[OK] Cleared entity memory" -ForegroundColor Green
    } else {
        Write-Host "[--] Entity memory already empty" -ForegroundColor DarkGray
    }
}

# Clear Contextual Memory SQL Table
if ($clearAll) {
    $dbPath = Join-Path (Get-Location) "database\enlisted_knowledge.db"
    if (Test-Path $dbPath) {
        try {
            $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$dbPath")
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "DELETE FROM contextual_memory"
            $rowsAffected = $cmd.ExecuteNonQuery()
            $conn.Close()
            if ($rowsAffected -gt 0) {
                $cleared += "contextual_memory ($rowsAffected chunks)"
                Write-Host "[OK] Cleared contextual_memory table ($rowsAffected chunks)" -ForegroundColor Green
            } else {
                Write-Host "[--] Contextual memory table already empty" -ForegroundColor DarkGray
            }
        } catch {
            Write-Host "[WARN] Could not clear contextual_memory: $_" -ForegroundColor Yellow
            Write-Host "       (SQLite provider may not be installed)" -ForegroundColor Yellow
        }
    }
}

# Summary
Write-Host ""
if ($cleared.Count -gt 0) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "[DONE] Cleared $($cleared.Count) memory component(s)" -ForegroundColor Green
    Write-Host "       Agents will learn fresh patterns on next run" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green
} else {
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "[INFO] Nothing to clear - memory was already empty" -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow
}
