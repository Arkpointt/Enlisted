"""
Execution monitoring for Enlisted CrewAI workflows.

Provides real-time visibility into crew execution for performance optimization
and debugging.
"""

import json
import os
import sqlite3
from datetime import datetime
from pathlib import Path
from typing import Optional

from crewai.events import (
    BaseEventListener,
    CrewKickoffStartedEvent,
    CrewKickoffCompletedEvent,
    TaskCompletedEvent,
    ToolUsageFinishedEvent,
)


def _get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")


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
            project_root = _get_project_root()
            db_path = str(project_root / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db")
        
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
        
        @crewai_event_bus.on(TaskCompletedEvent)
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
        
        @crewai_event_bus.on(ToolUsageFinishedEvent)
        def on_tool_used(source, event):
            """Handle tool execution."""
            tool_name = event.tool_name if hasattr(event, 'tool_name') else 'Unknown Tool'
            success = not hasattr(event, 'error') or event.error is None
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
        project_root = _get_project_root()
        db_path = str(project_root / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db")
    
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


def get_execution_history(
    db_path: Optional[str] = None,
    crew_name: Optional[str] = None,
    start_date: Optional[str] = None,
    end_date: Optional[str] = None,
    limit: Optional[int] = None
) -> list:
    """
    Get chronological execution history with time-range filtering.
    
    Args:
        db_path: Path to database. Defaults to enlisted_knowledge.db
        crew_name: Filter by crew name. If None, returns all crews.
        start_date: ISO format date string (e.g., '2026-01-01'). If None, no lower bound.
        end_date: ISO format date string. If None, no upper bound.
        limit: Maximum number of records to return. If None, returns all.
    
    Returns:
        List of execution records with timestamps and durations
    """
    if db_path is None:
        project_root = _get_project_root()
        db_path = str(project_root / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db")
    
    with sqlite3.connect(db_path) as conn:
        cursor = conn.cursor()
        
        # Build WHERE clauses
        where_clauses = ["status = 'completed'"]
        params = []
        
        if crew_name:
            where_clauses.append("crew_name = ?")
            params.append(crew_name)
        
        if start_date:
            where_clauses.append("started_at >= ?")
            params.append(start_date)
        
        if end_date:
            where_clauses.append("started_at <= ?")
            params.append(end_date)
        
        where_clause = " AND ".join(where_clauses)
        limit_clause = f"LIMIT {limit}" if limit else ""
        
        query = f"""
            SELECT 
                id,
                crew_name,
                started_at,
                completed_at,
                duration_seconds,
                output_length
            FROM crew_executions
            WHERE {where_clause}
            ORDER BY started_at DESC
            {limit_clause}
        """
        
        cursor.execute(query, params)
        results = cursor.fetchall()
        
        return [
            {
                'id': row[0],
                'crew_name': row[1],
                'started_at': row[2],
                'completed_at': row[3],
                'duration_seconds': round(row[4], 2) if row[4] else 0,
                'output_length': row[5]
            }
            for row in results
        ]


def get_performance_trends(
    db_path: Optional[str] = None,
    crew_name: Optional[str] = None,
    window_size: int = 10
) -> dict:
    """
    Analyze performance trends by comparing recent runs to historical average.
    
    Args:
        db_path: Path to database. Defaults to enlisted_knowledge.db
        crew_name: Filter by crew name. If None, analyzes all crews separately.
        window_size: Number of recent runs to compare (default: 10)
    
    Returns:
        Dictionary with trend analysis:
        - historical_avg: Average duration of all runs except recent window
        - recent_avg: Average duration of recent window
        - trend: 'improving', 'degrading', or 'stable'
        - percent_change: Percentage change (negative = faster)
    """
    if db_path is None:
        project_root = _get_project_root()
        db_path = str(project_root / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db")
    
    with sqlite3.connect(db_path) as conn:
        cursor = conn.cursor()
        
        if crew_name:
            crews = [crew_name]
        else:
            cursor.execute("SELECT DISTINCT crew_name FROM crew_executions WHERE status = 'completed'")
            crews = [row[0] for row in cursor.fetchall()]
        
        results = {}
        
        for crew in crews:
            # Get all completed runs for this crew
            cursor.execute("""
                SELECT duration_seconds
                FROM crew_executions
                WHERE crew_name = ? AND status = 'completed'
                ORDER BY started_at DESC
            """, (crew,))
            
            durations = [row[0] for row in cursor.fetchall() if row[0] is not None]
            
            if len(durations) < window_size + 5:  # Need enough data for comparison
                results[crew] = {
                    'error': 'Insufficient data for trend analysis',
                    'total_runs': len(durations),
                    'required_runs': window_size + 5
                }
                continue
            
            # Recent window vs historical
            recent = durations[:window_size]
            historical = durations[window_size:]
            
            recent_avg = sum(recent) / len(recent)
            historical_avg = sum(historical) / len(historical)
            
            percent_change = ((recent_avg - historical_avg) / historical_avg) * 100
            
            if percent_change < -5:
                trend = 'improving'
            elif percent_change > 5:
                trend = 'degrading'
            else:
                trend = 'stable'
            
            results[crew] = {
                'total_runs': len(durations),
                'window_size': window_size,
                'historical_avg_seconds': round(historical_avg, 2),
                'recent_avg_seconds': round(recent_avg, 2),
                'percent_change': round(percent_change, 2),
                'trend': trend
            }
        
        return results if not crew_name else results.get(crew_name, {})


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
        for crew_name_key, crew_stats in stats.items():
            print(f"\nCrew: {crew_name_key}")
            print(f"  Total Runs: {crew_stats['total_runs']}")
            if crew_stats['total_runs'] > 0:
                print(f"  Avg Duration: {crew_stats['avg_duration_seconds']:.2f}s ({crew_stats['avg_duration_seconds']/60:.1f}m)")
                print(f"  Min Duration: {crew_stats['min_duration_seconds']:.2f}s")
                print(f"  Max Duration: {crew_stats['max_duration_seconds']:.2f}s")
    
    print("\n" + "=" * 70 + "\n")


def print_trend_report(crew_name: Optional[str] = None, window_size: int = 10):
    """
    Print a formatted performance trend report.
    
    Args:
        crew_name: Filter by crew name. If None, shows all crews.
        window_size: Number of recent runs to compare (default: 10)
    """
    trends = get_performance_trends(crew_name=crew_name, window_size=window_size)
    
    print("\n" + "=" * 70)
    print(f"PERFORMANCE TREND ANALYSIS (Last {window_size} runs vs Historical)")
    print("=" * 70 + "\n")
    
    if 'error' in trends:
        print(f"[INFO] {trends['error']}")
        print(f"       Current runs: {trends['total_runs']}, Required: {trends['required_runs']}\n")
        print("=" * 70 + "\n")
        return
    
    if isinstance(trends, dict) and 'trend' in trends:
        # Single crew
        _print_crew_trend(crew_name, trends)
    else:
        # Multiple crews
        for crew, trend_data in trends.items():
            _print_crew_trend(crew, trend_data)
    
    print("=" * 70 + "\n")


def _print_crew_trend(crew_name: str, trend_data: dict):
    """Helper to print trend data for a single crew."""
    if 'error' in trend_data:
        print(f"Crew: {crew_name}")
        print(f"  [INFO] {trend_data['error']}")
        print(f"         Current runs: {trend_data['total_runs']}, Required: {trend_data['required_runs']}\n")
        return
    
    trend_icon = {
        'improving': '[+] IMPROVING',
        'degrading': '[-] DEGRADING',
        'stable': '[=] STABLE'
    }.get(trend_data['trend'], '[?] UNKNOWN')
    
    print(f"Crew: {crew_name}")
    print(f"  Total Runs: {trend_data['total_runs']}")
    print(f"  Historical Avg: {trend_data['historical_avg_seconds']:.2f}s ({trend_data['historical_avg_seconds']/60:.1f}m)")
    print(f"  Recent Avg: {trend_data['recent_avg_seconds']:.2f}s ({trend_data['recent_avg_seconds']/60:.1f}m)")
    print(f"  Change: {trend_data['percent_change']:+.1f}%")
    print(f"  Trend: {trend_icon}\n")


# Global monitoring instance - initialized once
_MONITOR_INSTANCE: Optional[EnlistedExecutionMonitor] = None


def print_cost_report(crew_name: Optional[str] = None):
    """
    Print a formatted cost tracking report from execution hooks.
    
    Args:
        crew_name: Filter by crew name (correlates by timestamp). If None, shows all costs.
    """
    project_root = _get_project_root()
    db_path = str(project_root / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db")
    
    with sqlite3.connect(db_path) as conn:
        cursor = conn.cursor()
        
        # Check if llm_costs table exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='llm_costs'
        """)
        if not cursor.fetchone():
            print("\n[INFO] No cost data available yet. Run a workflow to start tracking costs.\n")
            return
        
        print("\n" + "=" * 70)
        print("LLM COST TRACKING REPORT")
        print("=" * 70 + "\n")
        
        # If filtering by crew, get timestamp range
        timestamp_filter = ""
        filter_params = []
        if crew_name:
            cursor.execute("""
                SELECT MIN(started_at), MAX(completed_at)
                FROM crew_executions
                WHERE crew_name = ?
            """, (crew_name,))
            result = cursor.fetchone()
            if result and result[0]:
                timestamp_filter = "WHERE timestamp BETWEEN ? AND ?"
                filter_params = [result[0], result[1] or datetime.now().isoformat()]
                print(f"Filtered by crew: {crew_name}\n")
        
        # Total costs by model
        query = f"""
            SELECT 
                model,
                COUNT(*) as total_calls,
                SUM(input_tokens) as total_input,
                SUM(output_tokens) as total_output,
                SUM(cost_usd) as total_cost
            FROM llm_costs
            {timestamp_filter}
            GROUP BY model
            ORDER BY total_cost DESC
        """
        cursor.execute(query, filter_params)
        results = cursor.fetchall()
        
        if not results:
            print("[INFO] No cost data available for the specified filters.\n")
            print("=" * 70 + "\n")
            return
        
        print("Cost by Model:")
        print("-" * 70)
        total_cost = 0
        total_calls = 0
        total_input = 0
        total_output = 0
        
        for row in results:
            model, calls, input_tokens, output_tokens, cost = row
            total_cost += cost
            total_calls += calls
            total_input += input_tokens
            total_output += output_tokens
            
            print(f"  {model:20} | Calls: {calls:4} | In: {input_tokens:8,} | Out: {output_tokens:8,} | ${cost:7.2f}")
        
        print("-" * 70)
        print(f"  {'TOTAL':20} | Calls: {total_calls:4} | In: {total_input:8,} | Out: {total_output:8,} | ${total_cost:7.2f}")
        print()
        
        # Most expensive individual calls
        query = f"""
            SELECT timestamp, model, input_tokens, output_tokens, cost_usd
            FROM llm_costs
            {timestamp_filter}
            ORDER BY cost_usd DESC
            LIMIT 5
        """
        cursor.execute(query, filter_params)
        expensive_calls = cursor.fetchall()
        
        if expensive_calls:
            print("\nMost Expensive LLM Calls:")
            print("-" * 70)
            for row in expensive_calls:
                timestamp, model, input_tokens, output_tokens, cost = row
                # Format timestamp
                dt = datetime.fromisoformat(timestamp)
                time_str = dt.strftime('%Y-%m-%d %H:%M:%S')
                print(f"  {time_str} | {model:15} | {input_tokens:6,}in + {output_tokens:6,}out = ${cost:.4f}")
        
        # Daily cost summary (if we have multiple days of data)
        query = f"""
            SELECT 
                DATE(timestamp) as date,
                COUNT(*) as calls,
                SUM(cost_usd) as daily_cost
            FROM llm_costs
            {timestamp_filter}
            GROUP BY DATE(timestamp)
            ORDER BY date DESC
            LIMIT 7
        """
        cursor.execute(query, filter_params)
        daily_results = cursor.fetchall()
        
        if len(daily_results) > 1:
            print("\n\nDaily Cost Summary (Last 7 Days):")
            print("-" * 70)
            for row in daily_results:
                date, calls, daily_cost = row
                print(f"  {date} | Calls: {calls:4} | ${daily_cost:7.2f}")
        
        print("\n" + "=" * 70 + "\n")


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
