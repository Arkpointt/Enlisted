# CrewAI Memory Storage

**Location:** `Tools/CrewAI/memory/` (workspace-relative)

This folder contains the AI's learned memory and is **tracked in Git** for cross-platform intelligence sharing.

## Why In Git?

When you work on both Windows and Linux, you want the AI to remember what it learned on either platform:

- ✅ **AI learns once** - Training persists across platforms
- ✅ **Shared intelligence** - Windows and Linux use same memory
- ✅ **No relearning** - AI doesn't start from scratch on each platform
- ✅ **Project-specific** - Memory is tied to this workspace

## Contents

### ChromaDB Vector Storage
- **`short_term_memory/`** - Session context and recent interactions
- **`long_term_memory/`** - Persistent patterns learned over time  
- **`entities/`** - Entity relationships and semantic connections

### SQLite Database
- **`long_term_memory_storage.db`** - Long-term memory metadata

## Size

Typically grows to **50-200 MB** over time:
- Initial: ~10 MB (empty ChromaDB indexes)
- After 10 runs: ~50 MB
- After 100 runs: ~100-200 MB
- Growth slows as patterns stabilize

## Clearing Memory

To reset the AI's memory (after major refactors):

```powershell
# From Tools/CrewAI/
.\reset-memory.ps1        # Clear all memory
.\reset-memory.ps1 -Long  # Clear only long-term memory
```

This is useful when:
- Major architectural changes
- Renamed systems/files
- Deprecated features
- AI references outdated patterns

## Cross-Platform

**Windows:** `Tools/CrewAI/memory/`  
**Linux:** `Tools/CrewAI/memory/`

Same path on both platforms - syncs via Git!

## Performance

Memory improves AI performance over time:
- Faster responses (cached patterns)
- Better context awareness (learned relationships)
- Fewer hallucinations (remembered facts)
- Consistent behavior (stable learnings)

The AI gets smarter with each use! 🧠
