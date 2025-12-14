using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Lances.Events;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.UI
{
    /// <summary>
    /// Modern ViewModel for Lance Life events with rich visual presentation.
    /// Powers the custom Gauntlet UI for event screens.
    /// </summary>
    public class LanceLifeEventVM : ViewModel
    {
        private readonly LanceLifeEventDefinition _event;
        private readonly EnlistmentBehavior _enlistment;
        private readonly Action _onClose;

        // Header properties
        private string _eventTitle;
        private string _categoryText;
        private string _categoryColor;
        private string _timeLocationText;

        // Scene properties
        private string _sceneImagePath;
        private bool _showCharacter;
        private string _characterNameText;
        private string _bodyProperties;
        private bool _isFemale;
        private string _equipmentCode;
        private string _charStringId;
        private int _stanceIndex;
        private Hero _eventCharacter;

        // Story text
        private string _storyText;

        // Escalation tracks
        private float _heatBarWidth;
        private float _disciplineBarWidth;
        private float _lanceRepBarWidth;
        private string _heatText;
        private string _disciplineText;
        private string _lanceRepText;
        private string _fatigueText;

        // Choices
        private MBBindingList<EventChoiceVM> _choiceOptions;

        // Control
        private bool _canClose;

        public LanceLifeEventVM(LanceLifeEventDefinition eventDef, EnlistmentBehavior enlistment, Action onClose)
        {
            _event = eventDef;
            _enlistment = enlistment;
            _onClose = onClose;

            ChoiceOptions = new MBBindingList<EventChoiceVM>();

            InitializeEvent();
        }

        private void InitializeEvent()
        {
            try
            {
                // Header setup
                EventTitle = _event.TitleFallback ?? "Lance Activity";
                CategoryText = FormatCategory(_event.Category);
                CategoryColor = GetCategoryColor(_event.Category);
                TimeLocationText = GetTimeLocationText();

                // Scene setup
                SetupSceneVisuals();

                // Story text with rich formatting
                StoryText = FormatStoryText(_event.SetupFallback);

                // Escalation tracks
                UpdateEscalationTracks();

                // Fatigue
                FatigueText = $"{_enlistment?.FatigueCurrent ?? 0} / {_enlistment?.FatigueMax ?? 24}";

                // Build choice buttons
                BuildChoices();

                // Can close if not forced choice
                CanClose = _event.Options?.Count > 1;
            }
            catch (Exception ex)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Error("LanceLifeUI", $"Failed to initialize event display: {ex.Message}", ex);
                
                // Set safe defaults
                EventTitle = "Event";
                CategoryText = "Notice";
                CategoryColor = "#FFFFFFFF";
                TimeLocationText = "";
                StoryText = _event.SetupFallback ?? "An event has occurred.";
                ShowCharacter = false;
                SceneImagePath = "SPGeneral\\MapBar\\camp";
                CanClose = true;
                
                // Rebuild choices with safe defaults
                ChoiceOptions.Clear();
                if (_event?.Options != null)
                {
                    foreach (var option in _event.Options)
                    {
                        try
                        {
                            var vm = new EventChoiceVM(option, _enlistment, ChoiceOptions.Count, OnChoiceSelected);
                            ChoiceOptions.Add(vm);
                        }
                        catch
                        {
                            // Skip problematic choices
                        }
                    }
                }
            }
        }

        private void SetupSceneVisuals()
        {
            // Determine if we show a character or scene image
            var category = _event.Category?.ToLowerInvariant() ?? "";

            if (category.Contains("duty") || category.Contains("social"))
            {
                // Show character for interpersonal events
                ShowCharacter = true;
                SetupCharacterDisplay();
            }
            else
            {
                // Show scene image for other events
                ShowCharacter = false;
                SceneImagePath = GetSceneImageForEvent();
                
                // Initialize character properties to null - CharacterTableauWidget was removed from XML
                // (caused binding errors during LoadMovie even with IsVisible=false)
                EventCharacter = null;
                CharacterNameText = "";
                BodyProperties = null;
                EquipmentCode = null;  
                CharStringId = null;
                IsFemale = false;
                StanceIndex = 0;
            }
        }

        private void SetupCharacterDisplay()
        {
            try
            {
                // #region agent log
                System.IO.File.AppendAllText(@"c:\Dev\Enlisted\Enlisted\.cursor\debug.log", Newtonsoft.Json.JsonConvert.SerializeObject(new { location = "LanceLifeEventVM.cs:154", message = "SetupCharacterDisplay START", data = new { category = _event.Category }, timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "initial", hypothesisId = "H2" }) + "\n");
                // #endregion
                
                // Get relevant character (lance leader, companion, etc.)
                var character = GetEventCharacter();
                
                if (character != null)
                {
                    // Store character reference for XML DataSource binding
                    EventCharacter = character;
                    
                    CharacterNameText = character.Name?.ToString() ?? "Unknown";
                    
                    // Safely get body properties
                    try
                    {
                        BodyProperties = character.BodyProperties.ToString();
                        if (string.IsNullOrWhiteSpace(BodyProperties))
                        {
                            BodyProperties = "";
                        }
                    }
                    catch
                    {
                        BodyProperties = "";
                    }
                    
                    IsFemale = character.IsFemale;
                    EquipmentCode = GetCharacterEquipmentCode(character);
                    CharStringId = character.StringId ?? "";
                    StanceIndex = 0; // Casual stance
                }
                else
                {
                    EventCharacter = null;
                    CharacterNameText = "Camp Scene";
                    ShowCharacter = false;
                    SceneImagePath = GetSceneImageForEvent();
                }
            }
            catch (Exception ex)
            {
                // Fallback to no character display on any error
                Enlisted.Mod.Core.Logging.ModLogger.Warn("LanceLifeUI", $"Could not display character portrait, using scene image instead: {ex.Message}");
                EventCharacter = null;
                CharacterNameText = "Camp Scene";
                ShowCharacter = false;
                SceneImagePath = GetSceneImageForEvent();
            }
        }

        private Hero GetEventCharacter()
        {
            // Determine which character to show based on event type
            var category = _event.Category?.ToLowerInvariant() ?? "";

            if (category.Contains("duty") || category.Contains("onboarding"))
            {
                // Show lance leader
                return _enlistment?.CurrentLord;
            }
            else if (category.Contains("social"))
            {
                // Show a random lance mate or companion
                var companions = _enlistment?.CurrentLord?.PartyBelongedTo?.MemberRoster?
                    .GetTroopRoster()
                    .Where(t => t.Character?.IsHero == true)
                    .Select(t => t.Character.HeroObject)
                    .Where(h => h != null && h != Hero.MainHero)
                    .ToList();

                return companions?.Any() == true 
                    ? companions[MBRandom.RandomInt(companions.Count)] 
                    : null;
            }

            return null;
        }

        private string GetCharacterEquipmentCode(Hero hero)
        {
            // Get character's equipment for display
            try
            {
                var equipment = hero.BattleEquipment ?? hero.CivilianEquipment;
                return equipment?.CalculateEquipmentCode() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetSceneImageForEvent()
        {
            // Map event categories to scene images
            var category = _event.Category?.ToLowerInvariant() ?? "";

            if (category.Contains("training"))
                return "SPGeneral\\MapBar\\training_field";
            else if (category.Contains("task"))
                return "SPGeneral\\MapBar\\camp";
            else if (category.Contains("escalation"))
                return "SPGeneral\\MapBar\\conflict";
            else
                return "SPGeneral\\MapBar\\camp";
        }

        private void BuildChoices()
        {
            // #region agent log
            System.IO.File.AppendAllText(@"c:\Dev\Enlisted\Enlisted\.cursor\debug.log", Newtonsoft.Json.JsonConvert.SerializeObject(new { location = "LanceLifeEventVM.cs:256", message = "BuildChoices START", data = new { optionCount = _event.Options?.Count ?? 0 }, timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sessionId = "debug-session", runId = "initial", hypothesisId = "H3" }) + "\n");
            // #endregion
            
            ChoiceOptions.Clear();

            if (_event.Options == null || _event.Options.Count == 0)
            {
                return;
            }

            int index = 0;
            foreach (var option in _event.Options)
            {
                var vm = new EventChoiceVM(option, _enlistment, index, OnChoiceSelected);
                ChoiceOptions.Add(vm);
                index++;
            }
        }

        private void OnChoiceSelected(EventChoiceVM choice)
        {
            // Apply choice effects
            ApplyChoiceEffects(choice.Option);

            // Show outcome popup, then close
            ShowOutcomePopup(choice.Option);
        }

        private void ApplyChoiceEffects(LanceLifeEventOptionDefinition option)
        {
            // Apply through existing effects applier
            LanceLifeEventEffectsApplier.Apply(_event, option, _enlistment);

            // Update display
            UpdateEscalationTracks();
            FatigueText = $"{_enlistment?.FatigueCurrent ?? 0} / {_enlistment?.FatigueMax ?? 24}";
        }

        private void ShowOutcomePopup(LanceLifeEventOptionDefinition option)
        {
            // Show a brief outcome notification
            var outcomeText = LanceLifeEventText.Resolve(
                option.OutcomeTextId, 
                option.OutcomeTextFallback, 
                "The situation resolves...", 
                _enlistment);

            InformationManager.ShowInquiry(
                new InquiryData(
                    "Outcome",
                    outcomeText,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: false,
                    affirmativeText: "Continue",
                    negativeText: null,
                    affirmativeAction: () => ExecuteClose(),
                    negativeAction: null),
                pauseGameActiveState: true);
        }

        private void UpdateEscalationTracks()
        {
            var escalation = EscalationManager.Instance;
            if (escalation?.IsEnabled() == true)
            {
                // Calculate bar widths (max width = 180 pixels, max value = 10)
                var heat = escalation.State?.Heat ?? 0;
                var discipline = escalation.State?.Discipline ?? 0;
                var lanceRep = escalation.State?.LanceReputation ?? 0;

                HeatBarWidth = Math.Min(180f, (heat / 10f) * 180f);
                DisciplineBarWidth = Math.Min(180f, (discipline / 10f) * 180f);
                LanceRepBarWidth = Math.Min(180f, ((lanceRep + 50f) / 100f) * 180f); // Rep ranges from -50 to +50

                HeatText = $"{heat} / 10";
                DisciplineText = $"{discipline} / 10";
                LanceRepText = $"{lanceRep:F0}";
            }
            else
            {
                HeatBarWidth = 0;
                DisciplineBarWidth = 0;
                LanceRepBarWidth = 0;
                HeatText = "N/A";
                DisciplineText = "N/A";
                LanceRepText = "N/A";
            }
        }

        private string GetCategoryColor(string category)
        {
            var cat = category?.ToLowerInvariant() ?? "";

            if (cat.Contains("onboarding"))
            {
                // Onboarding = welcoming blue/gold
                return "#4488FFFF"; // Blue
            }
            else if (cat.Contains("escalation"))
            {
                // Escalation = dramatic red (intensity based on threshold)
                var heat = EscalationManager.Instance?.State?.Heat ?? 0;
                var discipline = EscalationManager.Instance?.State?.Discipline ?? 0;
                
                if (heat >= 7 || discipline >= 7)
                    return "#DD0000FF"; // Critical - dark red
                else if (heat >= 5 || discipline >= 5)
                    return "#FF4444FF"; // Warning - red
                else
                    return "#FFAA44FF"; // Caution - orange
            }
            else if (cat.Contains("duty"))
                return "#4488FFFF"; // Blue
            else if (cat.Contains("training"))
                return "#44FF88FF"; // Green
            else if (cat.Contains("social"))
                return "#FF88FFFF"; // Purple
            else if (cat.Contains("task"))
                return "#FFAA44FF"; // Orange
            else
                return "#888888FF"; // Gray
        }

        private string FormatCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "EVENT";

            return category.ToUpperInvariant();
        }

        private string GetTimeLocationText()
        {
            var time = CampaignTime.Now;
            var timeOfDay = GetTimeOfDay(time.CurrentHourInDay);
            
            var lord = _enlistment?.CurrentLord;
            var settlement = lord?.PartyBelongedTo?.CurrentSettlement;
            var location = settlement != null ? settlement.Name.ToString() : "Camp";

            return $"{timeOfDay} • {location}";
        }

        private string GetTimeOfDay(float hour)
        {
            if (hour < 6) return "Night";
            else if (hour < 12) return "Morning";
            else if (hour < 18) return "Afternoon";
            else if (hour < 21) return "Evening";
            else return "Night";
        }

        private string FormatStoryText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            try
            {
                // Apply rich text formatting
                text = LanceLifeEventText.Resolve(null, text, "", _enlistment);

                // Add paragraph breaks for better readability
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.Replace("\n\n", "\n \n"); // Add spacing between paragraphs
                }

                return text ?? "";
            }
            catch (Exception ex)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("LanceLifeUI", $"Could not format event text, using plain text: {ex.Message}");
                // Return original text as fallback
                return text ?? "";
            }
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        // Properties for data binding
        [DataSourceProperty]
        public string EventTitle
        {
            get => _eventTitle;
            set
            {
                if (_eventTitle != value)
                {
                    _eventTitle = value;
                    OnPropertyChangedWithValue(value, nameof(EventTitle));
                }
            }
        }

        [DataSourceProperty]
        public string CategoryText
        {
            get => _categoryText;
            set
            {
                if (_categoryText != value)
                {
                    _categoryText = value;
                    OnPropertyChangedWithValue(value, nameof(CategoryText));
                }
            }
        }

        [DataSourceProperty]
        public string CategoryColor
        {
            get => _categoryColor;
            set
            {
                if (_categoryColor != value)
                {
                    _categoryColor = value;
                    OnPropertyChangedWithValue(value, nameof(CategoryColor));
                }
            }
        }

        [DataSourceProperty]
        public string TimeLocationText
        {
            get => _timeLocationText;
            set
            {
                if (_timeLocationText != value)
                {
                    _timeLocationText = value;
                    OnPropertyChangedWithValue(value, nameof(TimeLocationText));
                }
            }
        }

        [DataSourceProperty]
        public string SceneImagePath
        {
            get => _sceneImagePath;
            set
            {
                if (_sceneImagePath != value)
                {
                    _sceneImagePath = value;
                    OnPropertyChangedWithValue(value, nameof(SceneImagePath));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowCharacter
        {
            get => _showCharacter;
            set
            {
                if (_showCharacter != value)
                {
                    _showCharacter = value;
                    OnPropertyChangedWithValue(value, nameof(ShowCharacter));
                }
            }
        }

        [DataSourceProperty]
        public string CharacterNameText
        {
            get => _characterNameText;
            set
            {
                if (_characterNameText != value)
                {
                    _characterNameText = value;
                    OnPropertyChangedWithValue(value, nameof(CharacterNameText));
                }
            }
        }

        [DataSourceProperty]
        public string BodyProperties
        {
            get => _bodyProperties;
            set
            {
                if (_bodyProperties != value)
                {
                    _bodyProperties = value;
                    OnPropertyChangedWithValue(value, nameof(BodyProperties));
                }
            }
        }

        [DataSourceProperty]
        public bool IsFemale
        {
            get => _isFemale;
            set
            {
                if (_isFemale != value)
                {
                    _isFemale = value;
                    OnPropertyChangedWithValue(value, nameof(IsFemale));
                }
            }
        }

        [DataSourceProperty]
        public string EquipmentCode
        {
            get => _equipmentCode;
            set
            {
                if (_equipmentCode != value)
                {
                    _equipmentCode = value;
                    OnPropertyChangedWithValue(value, nameof(EquipmentCode));
                }
            }
        }

        [DataSourceProperty]
        public string CharStringId
        {
            get => _charStringId;
            set
            {
                if (_charStringId != value)
                {
                    _charStringId = value;
                    OnPropertyChangedWithValue(value, nameof(CharStringId));
                }
            }
        }

        [DataSourceProperty]
        public int StanceIndex
        {
            get => _stanceIndex;
            set
            {
                if (_stanceIndex != value)
                {
                    _stanceIndex = value;
                    OnPropertyChangedWithValue(value, nameof(StanceIndex));
                }
            }
        }

        [DataSourceProperty]
        public string StoryText
        {
            get => _storyText;
            set
            {
                if (_storyText != value)
                {
                    _storyText = value;
                    OnPropertyChangedWithValue(value, nameof(StoryText));
                }
            }
        }

        [DataSourceProperty]
        public float HeatBarWidth
        {
            get => _heatBarWidth;
            set
            {
                if (Math.Abs(_heatBarWidth - value) > 0.01f)
                {
                    _heatBarWidth = value;
                    OnPropertyChangedWithValue(value, nameof(HeatBarWidth));
                }
            }
        }

        [DataSourceProperty]
        public float DisciplineBarWidth
        {
            get => _disciplineBarWidth;
            set
            {
                if (Math.Abs(_disciplineBarWidth - value) > 0.01f)
                {
                    _disciplineBarWidth = value;
                    OnPropertyChangedWithValue(value, nameof(DisciplineBarWidth));
                }
            }
        }

        [DataSourceProperty]
        public float LanceRepBarWidth
        {
            get => _lanceRepBarWidth;
            set
            {
                if (Math.Abs(_lanceRepBarWidth - value) > 0.01f)
                {
                    _lanceRepBarWidth = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRepBarWidth));
                }
            }
        }

        [DataSourceProperty]
        public string HeatText
        {
            get => _heatText;
            set
            {
                if (_heatText != value)
                {
                    _heatText = value;
                    OnPropertyChangedWithValue(value, nameof(HeatText));
                }
            }
        }

        [DataSourceProperty]
        public string DisciplineText
        {
            get => _disciplineText;
            set
            {
                if (_disciplineText != value)
                {
                    _disciplineText = value;
                    OnPropertyChangedWithValue(value, nameof(DisciplineText));
                }
            }
        }

        [DataSourceProperty]
        public string LanceRepText
        {
            get => _lanceRepText;
            set
            {
                if (_lanceRepText != value)
                {
                    _lanceRepText = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRepText));
                }
            }
        }

        [DataSourceProperty]
        public string FatigueText
        {
            get => _fatigueText;
            set
            {
                if (_fatigueText != value)
                {
                    _fatigueText = value;
                    OnPropertyChangedWithValue(value, nameof(FatigueText));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EventChoiceVM> ChoiceOptions
        {
            get => _choiceOptions;
            set
            {
                if (_choiceOptions != value)
                {
                    _choiceOptions = value;
                    OnPropertyChangedWithValue(value, nameof(ChoiceOptions));
                }
            }
        }

        [DataSourceProperty]
        public bool CanClose
        {
            get => _canClose;
            set
            {
                if (_canClose != value)
                {
                    _canClose = value;
                    OnPropertyChangedWithValue(value, nameof(CanClose));
                }
            }
        }

        [DataSourceProperty]
        public Hero EventCharacter
        {
            get => _eventCharacter;
            set
            {
                if (_eventCharacter != value)
                {
                    _eventCharacter = value;
                    OnPropertyChangedWithValue(value, nameof(EventCharacter));
                }
            }
        }
    }
}
