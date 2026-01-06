"""Enlisted CrewAI Initialization

Minimal class for project initialization:
- Sets ENLISTED_PROJECT_ROOT environment variable
- Checks MCP index staleness

All workflows are now Flow-based (see flows/ directory):
- PlanningFlow       - Design a feature
- ImplementationFlow - Build from plan
- BugHuntingFlow     - Find & fix bugs
- ValidationFlow     - Pre-commit validation
"""

import os
from pathlib import Path


class EnlistedCrew:
    """
    Enlisted CrewAI initialization.
    
    Sets up the project root environment variable and checks MCP index freshness.
    All agent workflows are now implemented as Flows in the flows/ directory.
    """
    
    def __init__(self):
        """Initialize with project root detection."""
        self.project_root = self._find_project_root()
        os.environ["ENLISTED_PROJECT_ROOT"] = str(self.project_root)
        self._check_index_staleness()
    
    def _find_project_root(self) -> Path:
        """Find the Enlisted project root directory."""
        env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
        if env_root:
            return Path(env_root)
        
        current = Path(__file__).resolve()
        for parent in current.parents:
            if (parent / "Enlisted.csproj").exists():
                return parent
        
        return Path(r"C:\Dev\Enlisted\Enlisted")
    
    def _check_index_staleness(self):
        """Check if MCP index is stale and warn user."""
        mcp_index = self.project_root / "Tools" / "CrewAI" / "mcp_servers" / "bannerlord_api_index.db"
        decompile_path = Path(r"C:\Dev\Enlisted\Decompile")
        
        if not mcp_index.exists():
            print("\n[WARN] MCP index not found. Run: cd Tools/CrewAI/mcp_servers && python build_index.py\n")
            return
        
        if not decompile_path.exists():
            return  # Can't check staleness without decompile folder
        
        # Compare modification times (sample check)
        index_mtime = mcp_index.stat().st_mtime
        sample_dirs = ["TaleWorlds.Core", "TaleWorlds.CampaignSystem"]
        
        for subdir in sample_dirs:
            check_dir = decompile_path / subdir
            if check_dir.exists():
                for cs_file in list(check_dir.rglob("*.cs"))[:5]:
                    if cs_file.stat().st_mtime > index_mtime:
                        print("\n" + "="*60)
                        print("[WARN] MCP INDEX IS STALE - Decompile folder has newer files!")
                        print("       Run: cd Tools/CrewAI && .\\update_after_patch.ps1")
                        print("="*60 + "\n")
                        return
