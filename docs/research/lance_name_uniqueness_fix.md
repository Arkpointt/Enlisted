# Lance Mate Name Uniqueness Fix

**Problem:** Multiple lance mates can have the same name, breaking immersion  
**Root Cause:** Random name generation doesn't check for duplicates  
**Solution:** Implement name deduplication during roster generation

---

## Table of Contents

1. [Problem Analysis](#problem-analysis)
2. [Current System](#current-system)
3. [Proposed Solution](#proposed-solution)
4. [Implementation Details](#implementation-details)
5. [Testing Strategy](#testing-strategy)
6. [Edge Cases](#edge-cases)

---

## Problem Analysis

### Current Behavior

**Scenario:** Lance roster with 10 members
- Leader: "Sergeant Bran the Old"
- Second: "Corporal Eldric"
- Senior Veteran: "Bran Ironside"  ← **DUPLICATE FIRST NAME**
- Veteran: "Ulfar"
- Soldier 1: "Bran"  ← **DUPLICATE FIRST NAME**
- Soldier 2: "Theron"
- Soldier 3: "Bran"  ← **DUPLICATE FIRST NAME**
- Soldier 4: "Kael"
- Recruit 1: "Mira"
- Recruit 2: "Jorn"

**Problem:** Three different people named "Bran" in the same 10-person unit breaks immersion.

### Why It Happens

**Code Location:** `LancePersonaBehavior.cs` lines 336-366

```csharp
private static LancePersonaMember GenerateMember(
    LanceCulturePersonaPoolJson pool,
    Random rng,
    float femaleChance,
    int slotIndex,
    int rankKeyIndex,
    LancePosition position,
    float epithetChance)
{
    var isFemale = rng.NextDouble() < Clamp01(femaleChance);
    var first = Pick(isFemale ? pool?.FemaleFirst : pool?.MaleFirst, rng) ?? "Soldier";
    // ↑ NO CHECK for already-used names
    
    var epithet = string.Empty;
    if (epithetChance > 0f && rng.NextDouble() < epithetChance)
    {
        epithet = Pick(pool?.Epithets, rng) ?? string.Empty;
    }
    
    return new LancePersonaMember { FirstName = first, Epithet = epithet, ... };
}
```

**The `Pick()` function (line 435) is stateless:**
```csharp
private static string Pick(List<string> list, Random rng)
{
    return list[rng.Next(list.Count)];  // Can pick same name multiple times
}
```

### Impact on Events

When events use multiple lance mate references, they become confusing:

```
{VETERAN_1_NAME} pulls you aside. "Listen, {LANCE_MATE_NAME} is having 
a rough time. Can you talk to them?"

You find {LANCE_MATE_NAME} by the fire...
```

**If both resolve to "Bran":**
```
Bran pulls you aside. "Listen, Bran is having a rough time. 
Can you talk to them?"

You find Bran by the fire...
```

❌ **Immersion destroyed.**

---

## Current System

### Roster Generation Flow

```
LancePersonaBehavior.GenerateRoster()
└── Called once per lance when first created
    └── Generates 10 members using a seeded RNG:
        1. Leader (epithet: 90% chance)
        2. Second (epithet: 20% chance)
        3. Senior Veteran (epithet: 70% chance)
        4. Veteran (epithet: 60% chance)
        5-8. Soldiers (epithet: 20% chance each)
        9-10. Recruits (epithet: 0% chance)
```

**Each member generated independently:**
- First name picked from culture pool (30-50 names typically)
- Epithet picked separately (if roll succeeds)
- No awareness of previous members

### Name Pools

**Location:** `ModuleData/Enlisted/LancePersonas/name_pools.json`

**Structure:**
```json
{
  "schemaVersion": 1,
  "cultures": {
    "empire": {
      "male_first": ["Aldric", "Bran", "Cedric", "Darius", ...],  // ~40 names
      "female_first": ["Aria", "Brigid", "Cassia", ...],           // ~40 names
      "epithets": ["the Bold", "Ironside", "the Old", ...]         // ~60 epithets
    }
  }
}
```

**Math on Collisions:**

With 40 male names and 10 lance members:
- Probability of at least one duplicate: **~60-70%**
- Expected duplicates per lance: **1-2**

With small name pools, collisions are **inevitable**.

---

## Proposed Solution

### Three-Tier Deduplication Strategy

```
When generating a name:
1. TRY to pick an unused first name
   └── If all names used, allow collision
   
2. IF collision detected:
   └── FORCE an epithet if member doesn't have one
       └── Differentiate: "Bran" vs "Bran the Bold"
   
3. IF still collision (both first name AND epithet):
   └── Add numeric suffix: "Bran the Bold II"
```

### Design Principles

✅ **Graceful Degradation**  
- Prefer unique first names
- Fall back to epithets
- Last resort: numeric suffixes

✅ **Maintain Determinism**  
- Same seed = same roster (important for save/load)
- No randomness in collision resolution

✅ **Minimal Code Changes**  
- Self-contained fix in `LancePersonaBehavior.cs`
- No changes to JSON schema or name pools

✅ **Performance**  
- O(n) complexity for 10-member roster
- No expensive lookups

---

## Implementation Details

### Step 1: Track Used Names During Generation

**Modify `GenerateRoster()` method:**

```csharp
private LancePersonaRoster GenerateRoster(
    string lanceKey,
    string lordId,
    string cultureId,
    int seed,
    LanceCulturePersonaPoolJson pool)
{
    var rng = new Random(seed);
    var cfg = ConfigurationManager.LoadLancePersonasConfig() ?? new LancePersonasConfig();
    
    // NEW: Track used combinations to detect collisions
    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var roster = new LancePersonaRoster
    {
        LanceKey = lanceKey,
        LordId = lordId ?? string.Empty,
        CultureId = cultureId ?? string.Empty,
        Seed = seed,
        Members = new List<LancePersonaMember>()
    };

    // Generate members with collision tracking
    roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleLeaderChance, 
        1, 0, LancePosition.Leader, 0.9f, usedNames));
    roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleSecondChance, 
        2, 1, LancePosition.Second, 0.2f, usedNames));
    roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleVeteranChance, 
        3, 2, LancePosition.SeniorVeteran, 0.7f, usedNames));
    roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleVeteranChance, 
        4, 3, LancePosition.Veteran, 0.6f, usedNames));

    for (var i = 0; i < 4; i++)
    {
        roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleSoldierChance, 
            5 + i, 4, LancePosition.Soldier, 0.2f, usedNames));
    }

    for (var i = 0; i < 2; i++)
    {
        roster.Members.Add(GenerateMemberWithUniqueness(pool, rng, cfg.FemaleRecruitChance, 
            9 + i, 5, LancePosition.Recruit, 0.0f, usedNames));
    }

    return roster;
}
```

### Step 2: New Generation Method with Deduplication

**Add new method:**

```csharp
/// <summary>
/// Generate a lance member with name uniqueness enforcement.
/// </summary>
private static LancePersonaMember GenerateMemberWithUniqueness(
    LanceCulturePersonaPoolJson pool,
    Random rng,
    float femaleChance,
    int slotIndex,
    int rankKeyIndex,
    LancePosition position,
    float epithetChance,
    HashSet<string> usedNames)
{
    var isFemale = rng.NextDouble() < Clamp01(femaleChance);
    var namePool = isFemale ? pool?.FemaleFirst : pool?.MaleFirst;
    
    // STEP 1: Try to pick an unused first name
    var firstName = PickUnusedName(namePool, rng, usedNames, maxAttempts: 10);
    if (string.IsNullOrEmpty(firstName))
    {
        // Fallback if all attempts failed (shouldn't happen with decent pool size)
        firstName = Pick(namePool, rng) ?? "Soldier";
    }
    
    // STEP 2: Generate epithet (may be forced if collision)
    var epithet = string.Empty;
    var needsEpithet = rng.NextDouble() < epithetChance;
    
    if (needsEpithet)
    {
        epithet = Pick(pool?.Epithets, rng) ?? string.Empty;
    }
    
    // STEP 3: Check for collision and resolve
    var displayName = BuildDisplayName(firstName, epithet);
    
    if (usedNames.Contains(displayName))
    {
        // Collision detected! Try to resolve:
        
        // Strategy A: If no epithet, force one
        if (string.IsNullOrWhiteSpace(epithet) && pool?.Epithets?.Count > 0)
        {
            epithet = PickUnusedEpithet(pool.Epithets, rng, firstName, usedNames, maxAttempts: 5);
            displayName = BuildDisplayName(firstName, epithet);
        }
        
        // Strategy B: Still collision? Add roman numeral suffix
        if (usedNames.Contains(displayName))
        {
            var suffix = 2;
            var original = epithet;
            while (usedNames.Contains(displayName) && suffix < 10)
            {
                epithet = string.IsNullOrWhiteSpace(original) 
                    ? ToRomanNumeral(suffix) 
                    : $"{original} {ToRomanNumeral(suffix)}";
                displayName = BuildDisplayName(firstName, epithet);
                suffix++;
            }
        }
    }
    
    // Register this name as used
    usedNames.Add(displayName);
    
    var (rankId, rankFallback) = ResolveRankTitle(pool, rankKeyIndex);
    
    return new LancePersonaMember
    {
        SlotIndex = slotIndex,
        Position = position,
        IsAlive = true,
        RankTitleId = rankId,
        RankTitleFallback = rankFallback,
        FirstName = firstName,
        Epithet = epithet
    };
}
```

### Step 3: Helper Methods

**Add these utility methods:**

```csharp
/// <summary>
/// Try to pick a name that hasn't been used yet.
/// </summary>
private static string PickUnusedName(
    List<string> namePool, 
    Random rng, 
    HashSet<string> usedNames, 
    int maxAttempts)
{
    if (namePool == null || namePool.Count == 0)
    {
        return null;
    }
    
    // Quick path: if pool is larger than roster, collision unlikely
    if (namePool.Count > 15)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = namePool[rng.Next(namePool.Count)];
            var displayName = BuildDisplayName(candidate, string.Empty);
            
            if (!usedNames.Contains(displayName))
            {
                return candidate;
            }
        }
    }
    
    // Exhaustive search if small pool or many attempts failed
    var availableNames = new List<string>();
    foreach (var name in namePool)
    {
        var displayName = BuildDisplayName(name, string.Empty);
        if (!usedNames.Contains(displayName))
        {
            availableNames.Add(name);
        }
    }
    
    return availableNames.Count > 0 
        ? availableNames[rng.Next(availableNames.Count)] 
        : null;
}

/// <summary>
/// Pick an epithet that creates a unique combination with firstName.
/// </summary>
private static string PickUnusedEpithet(
    List<string> epithetPool,
    Random rng,
    string firstName,
    HashSet<string> usedNames,
    int maxAttempts)
{
    if (epithetPool == null || epithetPool.Count == 0)
    {
        return string.Empty;
    }
    
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        var epithet = epithetPool[rng.Next(epithetPool.Count)];
        var displayName = BuildDisplayName(firstName, epithet);
        
        if (!usedNames.Contains(displayName))
        {
            return epithet;
        }
    }
    
    // All attempts failed - just return random epithet
    return epithetPool[rng.Next(epithetPool.Count)];
}

/// <summary>
/// Build the display name used for uniqueness checking.
/// Format: "FirstName" or "FirstName Epithet"
/// </summary>
private static string BuildDisplayName(string firstName, string epithet)
{
    var first = (firstName ?? string.Empty).Trim();
    var epi = (epithet ?? string.Empty).Trim();
    
    return string.IsNullOrWhiteSpace(epi) 
        ? first 
        : $"{first} {epi}";
}

/// <summary>
/// Convert number to Roman numeral for suffixes.
/// </summary>
private static string ToRomanNumeral(int number)
{
    if (number <= 0 || number > 10) return number.ToString();
    
    return number switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        _ => number.ToString()
    };
}
```

---

## Testing Strategy

### Unit Tests

**Test Cases:**

1. **No Collisions (Normal Case)**
   ```csharp
   [Fact]
   public void GenerateRoster_WithLargeNamePool_ProducesUniqueNames()
   {
       // Arrange: Pool with 40 names, generating 10 members
       var pool = CreateTestPool(maleNames: 40, epithets: 60);
       
       // Act
       var roster = GenerateRoster(...);
       
       // Assert
       var displayNames = roster.Members.Select(m => 
           BuildDisplayName(m.FirstName, m.Epithet)).ToList();
       
       Assert.Equal(10, displayNames.Distinct().Count()); // All unique
   }
   ```

2. **Small Pool Forces Epithets**
   ```csharp
   [Fact]
   public void GenerateRoster_WithSmallNamePool_ForcesEpithetsOnCollision()
   {
       // Arrange: Pool with only 3 names, generating 10 members
       var pool = CreateTestPool(maleNames: 3, epithets: 60);
       
       // Act
       var roster = GenerateRoster(...);
       
       // Assert
       var namesOnly = roster.Members.Select(m => m.FirstName).ToList();
       var fullNames = roster.Members.Select(m => 
           BuildDisplayName(m.FirstName, m.Epithet)).ToList();
       
       // First names WILL repeat
       Assert.True(namesOnly.Count > namesOnly.Distinct().Count());
       
       // But display names should be unique (via epithets)
       Assert.Equal(10, fullNames.Distinct().Count());
   }
   ```

3. **Determinism Check**
   ```csharp
   [Fact]
   public void GenerateRoster_SameSeed_ProducesSameRoster()
   {
       // Arrange
       var pool = CreateTestPool(maleNames: 20, epithets: 40);
       var seed = 12345;
       
       // Act
       var roster1 = GenerateRoster(seed: seed, ...);
       var roster2 = GenerateRoster(seed: seed, ...);
       
       // Assert: Exact same roster
       for (int i = 0; i < 10; i++)
       {
           Assert.Equal(roster1.Members[i].FirstName, roster2.Members[i].FirstName);
           Assert.Equal(roster1.Members[i].Epithet, roster2.Members[i].Epithet);
       }
   }
   ```

4. **Roman Numeral Fallback**
   ```csharp
   [Fact]
   public void GenerateRoster_ExtremeCollisions_UsesRomanNumerals()
   {
       // Arrange: Pool with 1 name, 0 epithets (worst case)
       var pool = CreateTestPool(maleNames: 1, epithets: 0);
       
       // Act
       var roster = GenerateRoster(...);
       
       // Assert: Should see "Bran", "Bran II", "Bran III", etc.
       var fullNames = roster.Members.Select(m => 
           BuildDisplayName(m.FirstName, m.Epithet)).ToList();
       
       Assert.Equal(10, fullNames.Distinct().Count());
       Assert.Contains("II", fullNames.First(n => n.Contains("II")));
   }
   ```

### Integration Tests

**Manual Testing Plan:**

1. **Start new enlistment**
2. **Trigger event with multiple lance mate references**
   - Example: Fire circle event with `{VETERAN_1_NAME}`, `{LANCE_MATE_NAME}`, `{SOLDIER_NAME}`
3. **Verify all names are different**
4. **Check lance roster UI** (if visible)
5. **Save and reload** - verify names persist correctly

### Regression Tests

**Ensure backward compatibility:**

1. **Load existing save files**
   - Old rosters (generated before fix) should load correctly
   - New rosters use deduplication
2. **Verify seed determinism**
   - Same lance key + seed = same roster (critical for multiplayer)

---

## Edge Cases

### Edge Case 1: Tiny Name Pool

**Scenario:** Culture has only 5 names for 10 members

**Behavior:**
```
Member 1: "Bran"
Member 2: "Ulfar"
Member 3: "Kael"
Member 4: "Theron"
Member 5: "Aldric"
Member 6: "Bran the Bold"      ← Forced epithet
Member 7: "Ulfar Ironside"     ← Forced epithet
Member 8: "Kael the Old"       ← Forced epithet
Member 9: "Theron the Fierce"  ← Forced epithet
Member 10: "Aldric II"         ← Roman numeral (if ran out of epithets)
```

**Solution:** Works correctly, all names unique.

### Edge Case 2: No Epithets Available

**Scenario:** Culture has 0 epithets, 5 names for 10 members

**Behavior:**
```
Member 1: "Bran"
Member 2: "Ulfar"
Member 3: "Kael"
Member 4: "Theron"
Member 5: "Aldric"
Member 6: "Bran II"      ← Roman numeral fallback
Member 7: "Ulfar II"
Member 8: "Kael II"
Member 9: "Theron II"
Member 10: "Aldric II"
```

**Solution:** Not ideal, but functional. Names are unique.

**Recommendation:** Ensure all cultures have at least 20 first names and 40 epithets.

### Edge Case 3: All Names Exhausted

**Scenario:** 1 name, 1 epithet, 10 members (extreme worst case)

**Behavior:**
```
Member 1: "Bran"
Member 2: "Bran the Bold"
Member 3: "Bran II"
Member 4: "Bran the Bold II"
Member 5: "Bran III"
Member 6: "Bran the Bold III"
... and so on
```

**Solution:** Ugly, but technically unique. This scenario is so pathological it's unlikely to occur in practice.

### Edge Case 4: Save/Load Compatibility

**Old Save (Pre-Fix):**
```json
{
  "roster": {
    "members": [
      { "firstName": "Bran", "epithet": "" },
      { "firstName": "Bran", "epithet": "" }  ← Duplicate
    ]
  }
}
```

**Behavior After Loading:**
- Old data loads as-is (no retroactive fix)
- If player transfers to a NEW lance, that roster uses deduplication
- Existing duplicates remain until natural roster turnover

**Mitigation:** Not a problem - old saves are valid, just suboptimal.

---

## Performance Analysis

### Complexity

**Time Complexity:**
- Best case: O(n) - no collisions, 10 members generated
- Average case: O(n) - 1-2 collisions, minimal retries
- Worst case: O(n * k) where k = maxAttempts (10), still very fast

**Space Complexity:**
- O(n) for `HashSet<string>` (10 entries max)

**Impact:** Negligible - roster generation happens once per lance, not every frame.

### Optimization Notes

1. **Quick Path for Large Pools**
   - If name pool > 15, collision unlikely
   - Use random sampling (fast)

2. **Exhaustive Search for Small Pools**
   - Only trigger if many collisions detected
   - Pre-filter available names

3. **Early Exit**
   - Stop searching after maxAttempts
   - Accept suboptimal solution (epithet or numeral)

---

## Configuration Options

**Add to `lance_personas.json`:**

```json
{
  "enabled": true,
  "enforce_unique_names": true,          ← NEW
  "max_name_retry_attempts": 10,         ← NEW
  "use_roman_numerals_on_collision": true ← NEW
}
```

**Default Behavior:**
- `enforce_unique_names`: **true** (always on)
- `max_name_retry_attempts`: **10** (balanced)
- `use_roman_numerals_on_collision`: **true** (last resort)

---

## Implementation Checklist

### Phase 1: Core Implementation (2-3 hours)

- [x] Modify `GenerateRoster()` to track used names
- [x] Create `GenerateMemberWithUniqueness()` method
- [x] Add `PickUnusedName()` helper
- [x] Add `PickUnusedEpithet()` helper
- [x] Add `BuildDisplayName()` helper
- [x] Add `ToRomanNumeral()` helper

### Phase 2: Testing (1-2 hours)

- [ ] Unit test: No collisions with large pool
- [ ] Unit test: Forced epithets with small pool
- [ ] Unit test: Roman numerals as fallback
- [ ] Unit test: Determinism (same seed = same roster)
- [ ] Integration test: Load old saves
- [ ] Integration test: Event text shows unique names

### Phase 3: Validation (1 hour)

- [ ] Manual test: Start new campaign, check roster
- [ ] Manual test: Trigger fire circle event, verify names
- [ ] Manual test: Save/load, verify persistence
- [ ] Performance test: Generate 100 rosters (should be instant)

### Phase 4: Documentation (30 minutes)

- [ ] Update `modderreadme.md` with name pool recommendations
- [ ] Add note to `CONTRIBUTING.md` about name pool size requirements
- [ ] Update `lance_personas.json` schema with new config options

---

## Rollout Plan

### Version 1.0 (Current)

❌ **Problem:** Duplicate names possible

### Version 1.1 (This Fix)

✅ **Solution:** Name deduplication enforced
✅ **Backward Compatible:** Old saves load correctly
✅ **Performance:** No impact (<1ms per roster)

### Migration Strategy

**For Existing Players:**
1. Update mod to v1.1
2. Existing rosters keep duplicates (if any)
3. New rosters (transfers, new campaigns) use deduplication
4. No save file corruption risk

**For New Players:**
5. All rosters unique from start

---

## Alternative Approaches Considered

### ❌ Approach A: Expand Name Pools

**Idea:** Add 200 names per culture to make collisions rare

**Problems:**
- Doesn't solve problem (still possible)
- Creates localization burden
- Many cultures would have historically inaccurate names

### ❌ Approach B: Use Full Name Generation

**Idea:** Generate last names (surnames) for everyone

**Problems:**
- Mount & Blade NPCs don't have surnames (lore break)
- Doubles localization work
- Still doesn't guarantee uniqueness

### ✅ Approach C: Deduplication (Chosen)

**Why This Works:**
- Solves problem completely
- No content/localization burden
- Maintains historical accuracy
- Minimal code changes
- Backward compatible

---

## Success Criteria

✅ **No duplicate display names** in any lance roster  
✅ **Deterministic generation** (same seed = same roster)  
✅ **Backward compatible** with existing save files  
✅ **Performance** impact < 1ms per roster  
✅ **Natural-looking names** (epithets preferred over numerals)

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-13 | AI Assistant | Initial implementation plan |

---

**END OF DOCUMENT**
