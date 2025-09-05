# Enlisted Menu System - Complete Interface Design

**Based on SAS/Freelancer analysis and user requirements**

## 🎮 **Menu Access & Navigation**

### **Primary Access Method**
- **Campaign Map + Enlisted**: Press 'N' key opens enlisted menu (replaces clan menu)
- **Dialog Access**: Talk to lord → "I'd like to check my service status"
- **Auto-Open**: During promotions and significant events

### **Menu Hierarchy**
```
Campaign Map
    ↓ Press 'N' key (when enlisted)
┌─ enlisted_status (MAIN HUB) ─────────┐
│  • Field medical treatment          │
│  • Speak with lord → CONVERSATION   │
│  • Visit quartermaster → Switch     │  
│  • View service record → Switch     │
│  • Request retirement (365+ days)   │
│  • Return to duties → Close         │
└──────────────────────────────────────┘
    ↓                           ↓
┌─ enlisted_equipment ─┐  ┌─ enlisted_record ─┐
│  • Multiple troops   │  │  • XP breakdown   │
│  • Formation costs   │  │  • Battle history │ 
│  • Personal restore  │  │  • Relationships  │
│  • Back to main      │  │  • Back to main   │
└──────────────────────┘  └───────────────────┘
```

## 🎖️ **Main Menu: enlisted_status**

### **Status Display**
```
═══════════════════════════════════════
         🎖️ ENLISTED STATUS 🎖️
═══════════════════════════════════════

Lord: [Sir Derthert] (Empire)
Rank: Sergeant (Tier 4)  
Formation: Legionary (Infantry)
Service Duration: 47 days (318 days to retirement)

Current Duties:
  • Field Medic (Medicine +25 XP/day) 🏥
  • Runner (Athletics +15 XP/day) 🏃

Next Promotion: 890/1500 XP
Daily Wage: 145 🪙

Army Status: Following [Derthert's Army] (12 parties, 1,847 troops)
Current Objective: Besieging Zeonica

═══════════════════════════════════════
```

### **Menu Options**
1. **Request field medical treatment**
   - Available: Anywhere when enlisted
   - Cooldown: 5 days (standard), 2 days (Field Medic)
   - Healing: 80% missing HP (100% for Field Medics)
   - Cost: FREE (military medical support)

2. **Speak with lord about duties**  
   - Opens conversation system for duty assignment changes
   - Shows available duties based on tier + formation
   - Allows multiple duty assignments within slot limits

3. **Visit the quartermaster**
   - Switches to equipment management menu
   - Shows multiple troop choices per tier with costs
   - Formation switching with realistic pricing

4. **View detailed service record**
   - Switches to progress tracking menu
   - XP breakdown, battle history, relationships

5. **Request retirement** (if eligible)
   - Available after 365 days service
   - Equipment choice system
   - Veteran benefits application

6. **Return to duties**
   - Closes menu system
   - Returns to following lord

## 🛡️ **Equipment Menu: enlisted_equipment**

### **Multiple Troop Choices Display**
```
═══════════════════════════════════════
      🛡️ QUARTERMASTER SUPPLIES 🛡️  
═══════════════════════════════════════
Your Gold: 1,250🪙 | Current: Imperial Legionary T4

Available Equipment (Tier 4 & Below):

┌─ INFANTRY TROOPS ─────────────────────┐
│ ● Imperial Legionary T4    (300🪙) ✓  │
│ ○ Imperial Veteran T3      (225🪙) ✓  │
│ ○ Imperial Guard T3        (240🪙) ✓  │
└───────────────────────────────────────┘

┌─ RANGED TROOPS ───────────────────────┐
│ ○ Imperial Archer T4       (390🪙) ✓  │
│ ○ Imperial Crossbow T4     (405🪙) ✓  │ 
│ ○ Imperial Skirmisher T3   (293🪙) ✓  │
└───────────────────────────────────────┘

┌─ CAVALRY TROOPS ──────────────────────┐
│ ○ Imperial Equite T4       (600🪙) ✓  │
│ ○ Imperial Heavy Cav T4    (650🪙) ✓  │
│ ○ Imperial Cavalry T3      (450🪙) ✓  │
└───────────────────────────────────────┘

┌─ ELITE TROOPS ────────────────────────┐
│ ○ Imperial Cataphract T4   (900🪙) ✓  │
│ ○ Elite Horse Archer T4  (1,170🪙) ✓  │
│ ○ Bucellarii Elite T5    (1,350🪙) ❌  │ (Tier 5 required)
└───────────────────────────────────────┘

═══════════════════════════════════════
[ Apply selected equipment ]
[ Restore personal equipment (FREE) ]
[ Back to enlisted status ]
═══════════════════════════════════════
```

### **Equipment Selection Logic**
```csharp
// Get all available troops for culture and tier
var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
var availableChoices = allCharacters.Where(c => 
    c.Culture == playerCulture && 
    c.Tier <= playerTier &&
    c.Tier > 0 &&
    !c.IsHero).ToList();

// Calculate costs with formation multipliers
var cost = baseCost * formationMultiplier * cultureModifier * eliteMultiplier;
```

## 📈 **Service Record Menu: enlisted_record**

```
═══════════════════════════════════════
         📈 SERVICE RECORD 📈
═══════════════════════════════════════

Experience Breakdown:
  Daily Service: 1,175 XP
  Battle Participation: 420 XP
  Duty Performance: 280 XP  
  TOTAL: 1,875 XP

Relationships:
  Lord Derthert: 67 (+Friendly)
  Empire Faction: 156 (+Respected)

Battle History:
  Battles Fought: 12
  Sieges Participated: 3
  Victories: 9

Formation Progress:
  Infantry Specialization: Expert
  Available Duties: 8/12 unlocked
  Equipment Access: T4/T7

Service Details:
  Enlistment Date: Summer, Year 1084
  Service Duration: 47 days
  Retirement Eligible: 🔒 318 days remaining
  
═══════════════════════════════════════
[ Back to enlisted status ]
═══════════════════════════════════════
```

## 🏥 **Medical Treatment System**

### **Simplified Requirements** (no settlement detection)
```csharp
private bool CanUseFieldMedicalTreatment()
{
    var timeSinceLastTreatment = CampaignTime.Now.ElapsedDaysUntilNow - _lastTreatmentTime;
    
    var cooldown = DutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") == true ?
        2f :  // Field Medic: Every 2 days
        5f;   // Standard: Every 5 days
    
    return timeSinceLastTreatment >= cooldown;
}
```

### **Healing Implementation**
- **Substantial healing**: 80% of missing health (100% for Field Medics)
- **Minimum guarantee**: 20 HP always healed
- **Fast treatment**: 1 hour time advance
- **No cost**: Military medical support included in service

## 💰 **Equipment Pricing System**

### **Formation-Based Multipliers**
- **Infantry**: 1.0× (base cost - cheapest)
- **Archer**: 1.3× (+30% for ranged weapons)
- **Cavalry**: 2.0× (+100% for horse equipment)
- **Horse Archer**: 2.5× (+150% for horse + ranged premium)

### **Culture Economic Modifiers**
- **Khuzait**: 0.8× (steppe economy - cheapest)
- **Battania**: 0.8× (tribal economy)
- **Aserai**: 0.9× (desert economy)
- **Sturgia**: 0.9× (nordic economy)
- **Empire**: 1.0× (base economy)
- **Vlandia**: 1.2× (wealthy western kingdoms - most expensive)

### **Elite Troop Surcharge**
- **Standard Troops**: Base price
- **Elite Variants**: +50% premium (e.g., Cataphracts, Champions)

## 🎯 **Implementation APIs (All Verified)**

### **Menu System**
```csharp
CampaignGameStarter.AddGameMenu(menuId, text, onInit, overlay, flags, null);
CampaignGameStarter.AddGameMenuOption(menuId, optionId, text, condition, consequence);
GameMenu.SwitchToMenu(menuId);
GameMenu.ExitToLast();
```

### **Healing System**
```csharp
Hero.MainHero.Heal(healAmount, addXp);
Hero.MainHero.MaxHitPoints { get; }
Hero.MainHero.HitPoints { get; }
```

### **Equipment Management**
```csharp
MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipment);
CharacterObject.Culture, .Tier, .IsRanged, .IsMounted
```

### **Economic Actions**
```csharp
GiveGoldAction.ApplyBetweenCharacters(from, to, amount, showNotification);
```

## 🏆 **Enhanced Features vs. Original SAS**

### **✅ Improvements Added**
1. **Horse Archer Formation**: 4th formation type with elite specializations
2. **Multiple Equipment Choices**: 3-6 options per tier vs. 1 in SAS
3. **Realistic Pricing**: JSON-based economics vs. free switching
4. **Enhanced Menu System**: Complete military interface vs. basic equipment selector
5. **Simplified Healing**: Available anywhere vs. complex settlement logic
6. **Configuration-Driven**: JSON files vs. hardcoded values
7. **Formation Selection**: Interactive choice vs. automatic-only

### **❌ Original SAS Limitations Fixed**
- SAS never had class selection upon promotions - **WE ADDED THIS**
- SAS had only basic equipment switching - **WE ENHANCED WITH MULTIPLE CHOICES**
- SAS used 37+ Harmony patches - **WE USE ONLY 4-5 TARGETED PATCHES**
- SAS was hardcoded - **WE USE JSON CONFIGURATION**

## 📋 **Ready for Implementation**

**All conversation insights have been integrated into the implementation plan.**
**APIs verified, configuration files created, menu system designed.**
**Ready to begin Phase 1A development with complete specification.**

---

**This enhanced military service system delivers professional-grade gameplay with realistic military economics, comprehensive choice systems, and superior user experience compared to the original ServeAsSoldier mod.**
