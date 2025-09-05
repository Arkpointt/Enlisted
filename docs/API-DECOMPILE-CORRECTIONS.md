# API Decompile Corrections & Lessons Learned

**Critical Reference Document for Future Development**  
**Generated**: 2025-01-28 during Phase 2A Enhanced Menu Implementation  

## ⚠️ **CRITICAL RULE: Use TaleWorlds Decompiled APIs ONLY**

**NEVER** use outdated mod documentation or assumed API signatures. **ALWAYS** verify method signatures using:
```
📁 Source Location: C:\Dev\Enlisted\DECOMPILE\TaleWorlds.CampaignSystem\
```

## 🔧 **API Corrections Made During Phase 2A**

### **1. AddWaitGameMenu Signature Correction**

**❌ WRONG (from outdated docs)**:
```csharp
starter.AddWaitGameMenu("menu_id", 
    new TextObject("Title").ToString(),
    GetMenuText,          // ❌ Wrong delegate type
    OnMenuInit,           // ❌ Wrong signature  
    null,
    GameOverlays.MenuOverlayType.None,  // ❌ Wrong parameter order
    GameMenu.MenuFlags.None);           // ❌ Missing parameters
```

**✅ CORRECT (from TaleWorlds.CampaignSystem\CampaignBehaviors\HideoutCampaignBehavior.cs:81)**:
```csharp
starter.AddWaitGameMenu("enlisted_status", 
    "Enlisted Status\n{ENLISTED_STATUS_TEXT}",
    new OnInitDelegate(OnEnlistedStatusInit),      // ✅ Correct delegate wrapper
    new OnConditionDelegate(OnEnlistedStatusCondition), // ✅ Proper condition delegate
    null, // No consequence
    null, // No tick handler 
    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
    GameOverlays.MenuOverlayType.None,
    1f, // Target hours
    GameMenu.MenuFlags.None,
    null);
```

### **2. Dialog Registration Pattern Correction**

**❌ WRONG (lord-initiated)**:
```csharp
// ❌ Lord offers to discuss military service
starter.AddDialogLine("enlisted_main_entry",
    "lord_talk_speak_diplomacy_2",
    "enlisted_main_options", 
    "I wish to discuss military service.",  // ❌ Wrong - lord speaking
    IsEnlistedDialogAvailable, null, 120, null);
```

**✅ CORRECT (player-initiated)**:
```csharp
// ✅ Player initiates military service discussion
starter.AddPlayerLine("enlisted_diplomatic_entry",
    "lord_talk_speak_diplomacy_2",
    "enlisted_main_hub",
    "I wish to discuss military service.",  // ✅ Correct - player speaking
    IsValidLordForMilitaryService, null, 110);

// ✅ Then lord responds
starter.AddDialogLine("enlisted_main_hub_response",
    "enlisted_main_hub",
    "enlisted_service_options",
    "What military matters do you wish to discuss?", // ✅ Correct - lord responding
    null, null, 110);
```

### **3. Menu Behavior Correction**

**❌ WRONG (breaks SAS pattern)**:
```csharp
private void OnReturnToDutiesSelected(MenuCallbackArgs args)
{
    GameMenu.ExitToLast();  // ❌ Exits menu, pauses game
}
```

**✅ CORRECT (proper SAS pattern)**:
```csharp
// ✅ No "return to duties" button needed
// Being in enlisted menu IS doing duties
// Menu stays active while following lord
```

### **4. Input System Simplification**

**❌ WRONG (overcomplicated state checking)**:
```csharp
if (GameStateManager.Current?.ActiveState?.GetType()?.Name != "MapState")  // ❌ API doesn't exist
    return false;
```

**✅ CORRECT (simple, reliable checks)**:
```csharp
if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)  // ✅ Works
    return false;
```

## 📋 **Verified Working API Patterns**

### **Menu Text Management**
```csharp
// ✅ CORRECT: Use MBTextManager for dynamic content
MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", statusText);

// ❌ WRONG: Return string from delegate (wrong return type)
private string GetEnlistedStatusMenuText(MenuCallbackArgs args)  // Wrong!
```

### **Notification System**
```csharp
// ✅ CORRECT: Use InformationManager.DisplayMessage
InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

// ❌ WRONG: AddQuickInformation doesn't exist in current version
InformationManager.AddQuickInformation(message, 0, character, sound);  // Wrong API!
```

### **Menu Option Registration**
```csharp
// ✅ CORRECT: Simple condition/consequence delegates
starter.AddGameMenuOption("enlisted_status", "enlisted_field_medical",
    "Request field medical treatment",
    IsFieldMedicalAvailable,        // ✅ Simple bool method
    OnFieldMedicalSelected,         // ✅ Simple void method
    false, 1);
```

## 🔧 **Field Name Verification Protocol**

**When adding new methods to existing behaviors:**

1. **Read the actual file** to check field names
2. **Use grep/search** to find existing field references  
3. **Match exact names** - don't assume naming patterns
4. **Test compilation** immediately after adding methods

**Example**: EnlistedDutiesBehavior.cs field names:
- ✅ `_activeDuties` (not `_activeDutyIds`)
- ✅ `_playerFormation` (not `_playerFormationType`)  
- ✅ `_config.Duties` (not `_duties`)

## 📚 **Reference Sources for Future Development**

### **Primary API Reference**
```
📁 C:\Dev\Enlisted\DECOMPILE\TaleWorlds.CampaignSystem\TaleWorlds.CampaignSystem\
├── CampaignGameStarter.cs           # Menu registration methods
├── GameMenus\GameMenu.cs           # Menu enums and delegates
├── CampaignBehaviors\*.cs          # Working implementation examples
└── Actions\*.cs                    # Game action APIs
```

### **Working Implementation Examples**
- **HideoutCampaignBehavior.cs**: AddWaitGameMenu usage
- **VillageHostileActionCampaignBehavior.cs**: Complex menu condition patterns
- **PlayerArmyWaitBehavior.cs**: Army-related menu implementations

### **Documentation Priority**
1. **Actual TaleWorlds decompiled code** (PRIMARY)
2. **Working examples from current project** (SECONDARY)  
3. **Official Bannerlord modding docs** (TERTIARY)
4. **Outdated mod source code** (❌ AVOID)

## 🎯 **Critical Success Factors**

### **What Made This Phase Successful**
1. **Real API Verification**: Used actual decompiled TaleWorlds code
2. **Iterative Problem Solving**: Fixed issues one by one with proper testing
3. **Preserved Working Components**: Didn't break the working dialog entry point
4. **Logical Design**: Removed unnecessary menu options that didn't make sense

### **What Could Have Gone Better**  
1. **Initial API Research**: Should have verified signatures before implementation
2. **Conservative Changes**: Should have tested each change individually
3. **Working Pattern Preservation**: Should have studied working patterns more carefully

## 🏆 **Achievement Summary**

**✅ Professional military interface implemented using 100% verified APIs**  
**✅ Comprehensive real-time status display with all military service information**
**✅ Keyboard shortcuts and proper input handling**  
**✅ Proper SAS behavior patterns maintained**
**✅ Foundation established for remaining Phase 2B troop selection implementation**

---

**This document serves as the authoritative reference for API usage and implementation patterns for all future development on the Enlisted project.**
