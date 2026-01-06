"""
Execution monitoring for Enlisted CrewAI workflows.

Provides real-time visibility into crew execution for performance optimization
and debugging.
"""

import json
import sqlite3
from datetime import datetime
from pathlib import Path
from typing import Optional

from crewai.events import (
    BaseEventListener,
    CrewKickoffStartedEvent,
    CrewKickoffCompletedEvent,
    AgentExecutionCompletedEvent,
    TaskExecutionCompletedEvent,
    ToolExecutionEvent,
)


class EnlistedExecutionMonitor(BaseEventListener):
    """
    Monitor CrewAI execution for performance insights and debugging.
    
    Tracks:
    - Execution time per crew/agent/task
    - Token usage (if available)
    - Tool calls and success rates
    - Errors and warnings
    - Overall workflow efficiency
    
    Logs to both console and SQLite database for analysis.
    """
    
    def __init__(self, db_path: Optional[str] = None):
        """
        Initialize the execution monitor.
        
        Args:
            db_path: Path to SQLite database. Defaults to enlisted_knowledge.db
        """
        super().__init__()
        
        if db_path is None:
            project_root = Path(__file__).parent.parent.parent.parent.parent
            db_path = str(project_root / "Tools" / "CrewAI" / "enlisted_knowledge.db")
        
        self.db_path = db_path
        self._init_monitoring_tables()
        
        # Track current execution
        self.current_crew_id: Optional[int] = None
        self.crew_start_time: Optional[datetime] = None
    
    def _init_monitoring_tables(self):
        """Create monitoring tables if they don't exist."""
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.cursor()
            
            # Crew execution log
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS crew_executions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    crew_name TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    completed_at TEXT,
                    duration_seconds REAL,
                    status TEXT,
                    output_length INTEGER,
                    notes TEXT
                )
            """)
            
            # Agent execution log
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS agent_executions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    crew_execution_id INTEGER,
                    agent_role TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    completed_at TEXT,
                    duration_seconds REAL,
                    output_length INTEGER,
                    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
                )
            """)
            
            # Task execution log
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS task_executions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    crew_execution_id INTEGER,
                    task_description TEXT,
                    started_at TEXT NOT NULL,
                    completed_at TEXT,
                    duration_seconds REAL,
                    output_length INTEGER,
                    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
                )
            """)
            
            # Tool usage log
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS tool_usages (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    crew_execution_id INTEGER,
                    tool_name TEXT NOT NULL,
                    executed_at TEXT NOT NULL,
                    duration_seconds REAL,
                    success BOOLEAN,
                    error_message TEXT,
                    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
                )
            """)
            
            conn.commit()
    
    def setup_listeners(self, crewai_event_bus):
        """Register event handlers for monitoring."""
        
        @crewai_event_bus.on(CrewKickoffStartedEvent)
        def on_crew_started(source, event):
            """Handle crew execution start."""
            self.crew_start_time = datetime.now()
            
            print("\n" + "=" * 70)
            print(f"[CREW STARTED] {event.crew_name}")
            print(f"[TIME] {self.crew_start_time.strftime('%Y-%m-%d %H:%M:%S')}")
            print("=" * 70 + "\n")
            
            # Log to database
            with sqlite3.connect(self.db_path) as conn:
                cursor = conn.cursor()
                cursor.execute("""
                    INSERT INTO crew_executions (crew_name, started_at, status)
                    VALUES (?, ?, 'running')
                """, (event.crew_name, self.crew_start_time.isoformat()))
                self.current_crew_id = cursor.lastrowid
                conn.commit()
        
        @crewai_event_bus.on(CrewKickoffCompletedEvent)
        def on_crew_completed(source, event):
            """Handle crew execution completion."""
            end_time = datetime.now()
            duration = (end_time - self.crew_start_time).total_seconds() if self.crew_start_time else 0
            
            output_length = len(str(event.output)) if hasattr(event, 'output') else 0
            
            print("\n" + "=" * 70)
            print(f"[CREW COMPLETED] {event.crew_name}")
            print(f"[DURATION] {duration:.2f} seconds ({duration / 60:.1f} minutes)")
            print(f"[OUTPUT] {output_length} characters")
            print("=" * 70 + "\n")
            
            # Update database
            if self.current_crew_id:
                with sqlite3.connect(self.db_path) as conn:
                    cursor = conn.cursor()
                    cursor.execute("""
                        UPDATE crew_executions
                        SET completed_at = ?, duration_seconds = ?, status = 'completed',
                            output_length = ?
                        WHERE id = ?
                    """, (end_time.isoformat(), duration, output_length, self.current_crew_id))
                    conn.commit()
        
        @crewai_event_bus.on(AgentExecutionCompletedEvent)
        def on_agent_completed(source, event):
            """Handle agent execution completion."""
            agent_name = event.agent.role if hasattr(event.agent, 'role') else 'Unknown Agent'
            
            # Try to get execution time if available
            duration = getattr(event, 'execution_time', None)
            if duration:
                print(f"  [AGENT] {agent_name} completed in {duration:.2f}s")
            else:
                print(f"  [AGENT] {agent_name} completed")
            
            # Log to database
            if self.current_crew_id and duration:
                with sqlite3.connect(self.db_path) as conn:
                    cursor = conn.cursor()
                    cursor.execute("""
                        INSERT INTO agent_executions 
                        (crew_execution_id, agent_role, started_at, completed_at, duration_seconds)
                        VALUES (?, ?, ?, ?, ?)
                    """, (
                        self.current_crew_id,
                        agent_name,
                        (datetime.now() - duration).isoformat() if duration else datetime.now().isoformat(),
                        datetime.now().isoformat(),
                        duration
                    ))
                    conn.commit()
        
        @crewai_event_bus.on(TaskExecutionCompletedEvent)
        def on_task_completed(source, event):
            """Handle task execution completion."""
            task_desc = event.task.description[:50] if hasattr(event.task, 'description') else 'Unknown Task'
            
            # Try to get execution time if available
            duration = getattr(event, 'execution_time', None)
            if duration:
                print(f"    [TASK] {task_desc}... ({duration:.2f}s)")
            else:
                print(f"    [TASK] {task_desc}...")
            
            # Log to database
            if self.current_crew_id:
                with sqlite3.connect(self.db_path) as conn:
                    cursor = conn.cursor()
                    cursor.execute("""
                        INSERT INTO task_executions 
                        (crew_execution_id, task_description, started_at, completed_at, duration_seconds)
                        VALUES (?, ?, ?, ?, ?)
                    """, (
                        self.current_crew_id,
                        task_desc,
                        (datetime.now() - duration).isoformat() if duration else datetime.now().isoformat(),
                        datetime.now().isoformat(),
                        duration
                    ))
                    conn.commit()
        
        @crewai_event_bus.on(ToolExecutionEvent)
        def on_tool_used(source, event):
            """Handle tool execution."""
            tool_name = event.tool_name if hasattr(event, 'tool_name') else 'Unknown Tool'
            success = getattr(event, 'success', True)
            error = getattr(event, 'error', None)
            
            if success:
                print(f"      [TOOL] {tool_name} - OK")
            else:
                print(f"      [TOOL] {tool_name} - FAILED: {error}")
            
            # Log to database
            if self.current_crew_id:
                with sqlite3.connect(self.db_path) as conn:
                    cursor = conn.cursor()
                    cursor.execute("""
                        INSERT INTO tool_usages 
                        (crew_execution_id, tool_name, executed_at, success, error_message)
                        VALUES (?, ?, ?, ?, ?)
                    """, (
                        self.current_crew_id,
                        tool_name,
                        datetime.now().isoformat(),
                        success,
                        str(error) if error else None
                    ))
                    conn.commit()


def get_execution_stats(db_path: Optional[str] = None, crew_name: Optional[str] = None) -> dict:
    """
    Get execution statistics from monitoring database.
    
    Args:
        db_path: Path to database. Defaults to enlisted_knowledge.db
        crew_name: Filter by crew name. If None, returns stats for all crews.
    
    Returns:
        Dictionary with execution statistics
    """
    if db_path is None:
        project_root = Path(__file__).parent.parent.parent.parent.parent
        db_path = str(project_root / "Tools" / "CrewAI" / "enlisted_knowledge.db")
    
    with sqlite3.connect(db_path) as conn:
        cursor = conn.cursor()
        
        # Get crew execution stats
        if crew_name:
            cursor.execute("""
                SELECT 
                    COUNT(*) as total_runs,
                    AVG(duration_seconds) as avg_duration,
                    MIN(duration_seconds) as min_duration,
                    MAX(duration_seconds) as max_duration
                FROM crew_executions
                WHERE crew_name = ? AND status = 'completed'
            """, (crew_name,))
        else:
            cursor.execute("""
                SELECT 
                    crew_name,
                    COUNT(*) as total_runs,
                    AVG(duration_seconds) as avg_duration,
                    MIN(duration_seconds) as min_duration,
                    MAX(duration_seconds) as max_duration
                FROM crew_executions
                WHERE status = 'completed'
                GROUP BY crew_name
            """)
        
        results = cursor.fetchall()
        
        if crew_name:
            if results and results[0]:
                return {
                    'crew_name': crew_name,
                    'total_runs': results[0][0],
                    'avg_duration_seconds': round(results[0][1], 2) if results[0][1] else 0,
                    'min_duration_seconds': round(results[0][2], 2) if results[0][2] else 0,
                    'max_duration_seconds': round(results[0][3], 2) if results[0][3] else 0,
                }
            return {'crew_name': crew_name, 'total_runs': 0}
        else:
            return {
                row[0]: {
                    'total_runs': row[1],
                    'avg_duration_seconds': round(row[2], 2) if row[2] else 0,
                    'min_duration_seconds': round(row[3], 2) if row[3] else 0,
                    'max_duration_seconds': round(row[4], 2) if row[4] else 0,
                }
                for row in results
            }


def print_execution_report(crew_name: Optional[str] = None):
    """Print a formatted execution statistics report."""
    stats = get_execution_stats(crew_name=crew_name)
    
    print("\n" + "=" * 70)
    print("CREWAI EXECUTION STATISTICS")
    print("=" * 70 + "\n")
    
    if isinstance(stats, dict) and 'crew_name' in stats:
        # Single crew stats
        print(f"Crew: {stats['crew_name']}")
        print(f"Total Runs: {stats['total_runs']}")
        if stats['total_runs'] > 0:
            print(f"Average Duration: {stats['avg_duration_seconds']:.2f}s ({stats['avg_duration_seconds']/60:.1f}m)")
            print(f"Min Duration: {stats['min_duration_seconds']:.2f}s")
            print(f"Max Duration: {stats['max_duration_seconds']:.2f}s")
    else:
        # Multiple crews
        for crew_name, crew_stats in stats.items():
            print(f"\nCrew: {crew_name}")
            print(f"  Total Runs: {crew_stats['total_runs']}")
            if crew_stats['total_runs'] > 0:
                print(f"  Avg Duration: {crew_stats['avg_duration_seconds']:.2f}s ({crew_stats['avg_duration_seconds']/60:.1f}m)")
                print(f"  Min Duration: {crew_stats['min_duration_seconds']:.2f}s")
                print(f"  Max Duration: {crew_stats['max_duration_seconds']:.2f}s")
    
    print("\n" + "=" * 70 + "\n")


# Global monitoring instance - initialized once
_MONITOR_INSTANCE: Optional[EnlistedExecutionMonitor] = None


def enable_monitoring(db_path: Optional[str] = None):
    """
    Enable execution monitoring for all CrewAI workflows.
    
    Call this once at startup to begin tracking execution metrics.
    
    Args:
        db_path: Path to SQLite database. Defaults to enlisted_knowledge.db
    """
    global _MONITOR_INSTANCE
    
    if _MONITOR_INSTANCE is None:
        _MONITOR_INSTANCE = EnlistedExecutionMonitor(db_path=db_path)
        print("[MONITORING] Execution monitoring enabled")
    
    return _MONITOR_INSTANCE
