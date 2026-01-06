-- Enlisted Mod - Project Knowledge Database Schema
-- Location: C:\Dev\SQLite3\enlisted_knowledge.db
--
-- This database provides structured, queryable knowledge for CrewAI agents.
-- All values extracted from actual codebase (verified January 2026).

-- ============================================================
-- 1. CONTENT REGISTRY - Track all events/decisions/orders
-- ============================================================

CREATE TABLE IF NOT EXISTS content_items (
    id TEXT PRIMARY KEY,                    -- e.g., "equipment_quality_inspection"
    type TEXT NOT NULL,                     -- "event", "decision", "order"
    category TEXT,                          -- See valid categories below
    severity TEXT,                          -- See valid severities below
    file_path TEXT NOT NULL,                -- "ModuleData/Enlisted/Events/xyz.json"
    title TEXT,                             -- Human-readable title
    description TEXT,                       -- Brief description
    tier_variant BOOLEAN DEFAULT 0,         -- 1 if has T1/T2/T3 variants
    min_tier INTEGER,                       -- Minimum tier requirement
    max_tier INTEGER,                       -- Maximum tier requirement
    date_created TEXT,                      -- ISO timestamp
    date_modified TEXT,                     -- ISO timestamp
    implemented_by TEXT,                    -- "crewai", "human", "warp"
    status TEXT DEFAULT 'active'            -- "active", "deprecated", "planned"
);

CREATE INDEX idx_content_type ON content_items(type);
CREATE INDEX idx_content_category ON content_items(category);
CREATE INDEX idx_content_status ON content_items(status);

-- ============================================================
-- 2. TIER DEFINITIONS - Player progression (FROM progression_config.json)
-- ============================================================

CREATE TABLE IF NOT EXISTS tier_definitions (
    tier INTEGER PRIMARY KEY,               -- 1-9
    rank_name TEXT NOT NULL,                -- Default rank name
    xp_required INTEGER NOT NULL,           -- Cumulative XP to ENTER this tier
    duration TEXT,                          -- Expected time in tier
    track TEXT NOT NULL,                    -- "enlisted", "officer", "commander"
    description TEXT,                       -- What this tier represents
    key_features TEXT                       -- Features unlocked (comma-separated)
);

-- ============================================================
-- 3. BALANCE VALUES - Core game balance from config files
-- ============================================================

CREATE TABLE IF NOT EXISTS balance_values (
    key TEXT PRIMARY KEY,                   -- e.g., "daily_base_xp"
    value REAL NOT NULL,                    -- Numeric value
    unit TEXT,                              -- "xp", "gold", "days", "%", etc.
    category TEXT,                          -- "tier", "economy", "morale", "supply", "xp"
    config_file TEXT,                       -- Source config file
    description TEXT,                       -- What this value controls
    last_updated TEXT                       -- ISO timestamp
);

-- ============================================================
-- 4. ERROR CATALOG - Known error codes and solutions
-- ============================================================

CREATE TABLE IF NOT EXISTS error_catalog (
    error_code TEXT PRIMARY KEY,            -- e.g., "E-MUSTER-001"
    category TEXT NOT NULL,                 -- MUSTER, UI, ENLIST, SAVELOAD, etc.
    severity TEXT DEFAULT 'error',          -- "warning", "error", "critical"
    description TEXT NOT NULL,              -- What this error means
    common_causes TEXT,                     -- Common root causes
    solution TEXT,                          -- How to fix
    source_file TEXT,                       -- Where this error is thrown
    date_added TEXT                         -- ISO timestamp
);

CREATE INDEX idx_error_category ON error_catalog(category);

-- ============================================================
-- 5. SYSTEM DEPENDENCIES - What depends on what
-- ============================================================

CREATE TABLE IF NOT EXISTS system_dependencies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    system_name TEXT NOT NULL,              -- e.g., "EnlistmentBehavior"
    system_type TEXT,                       -- "Behavior", "Manager", "Generator"
    depends_on TEXT NOT NULL,               -- e.g., "Hero.MainHero"
    dependency_type TEXT,                   -- "required", "optional", "weak"
    description TEXT,                       -- How it depends
    file_path TEXT                          -- Source file location
);

CREATE INDEX idx_system_name ON system_dependencies(system_name);
CREATE INDEX idx_depends_on ON system_dependencies(depends_on);

-- ============================================================
-- 6. CORE SYSTEMS REGISTRY - All major systems
-- ============================================================

CREATE TABLE IF NOT EXISTS core_systems (
    name TEXT PRIMARY KEY,                  -- e.g., "EnlistmentBehavior"
    full_name TEXT,                         -- Full class name with namespace
    type TEXT,                              -- "Behavior", "Manager", "Generator", "Static"
    file_path TEXT,                         -- Source file path
    description TEXT,                       -- What this system does
    key_responsibilities TEXT               -- Comma-separated responsibilities
);

-- ============================================================
-- 7. IMPLEMENTATION HISTORY - Track what was built when
-- ============================================================

CREATE TABLE IF NOT EXISTS implementation_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    feature_name TEXT NOT NULL,
    plan_file TEXT,                         -- Path to planning doc
    date_implemented TEXT,                  -- ISO timestamp
    implemented_by TEXT,                    -- "crewai", "human", "warp"
    files_added TEXT,                       -- JSON array of file paths
    files_modified TEXT,                    -- JSON array of file paths
    content_ids TEXT,                       -- JSON array of event/decision IDs
    commit_hash TEXT,                       -- Git commit SHA
    notes TEXT                              -- Any important notes
);

CREATE INDEX idx_impl_date ON implementation_history(date_implemented);
CREATE INDEX idx_impl_by ON implementation_history(implemented_by);

-- ============================================================
-- 8. API PATTERNS - Bannerlord API usage patterns
-- ============================================================

CREATE TABLE IF NOT EXISTS api_patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    api_name TEXT NOT NULL,                 -- e.g., "GiveGoldAction.ApplyByHeroGained"
    category TEXT,                          -- "economy", "reputation", "party", etc.
    usage_pattern TEXT,                     -- Code example
    description TEXT,                       -- What it does
    common_mistakes TEXT,                   -- What to avoid
    source TEXT                             -- "decompile", "docs", "discovered"
);

CREATE INDEX idx_api_category ON api_patterns(category);

-- ============================================================
-- 9. VALID CATEGORIES AND SEVERITIES
-- ============================================================

CREATE TABLE IF NOT EXISTS valid_categories (
    category TEXT PRIMARY KEY,
    description TEXT,
    used_in TEXT                            -- "events", "orders", "decisions", "all"
);

CREATE TABLE IF NOT EXISTS valid_severities (
    severity TEXT PRIMARY KEY,
    description TEXT,
    used_in TEXT                            -- "events", "injuries", "simulation", "all"
);

-- ============================================================
-- SEED DATA - Verified from actual codebase
-- ============================================================

-- Tier definitions (from ModuleData/Enlisted/Config/progression_config.json)
INSERT OR IGNORE INTO tier_definitions (tier, rank_name, xp_required, duration, track, description, key_features) VALUES
(1, 'Follower', 0, '1-2 weeks', 'enlisted', 'Raw recruit, learning the ropes', 'Basic equipment, following orders'),
(2, 'Recruit', 800, '2-4 weeks', 'enlisted', 'Proven soldier, gaining trust', 'Formation selection unlocked'),
(3, 'Free Sword', 3000, '1-2 months', 'enlisted', 'Skilled fighter, reliable in battle', 'T3 equipment access'),
(4, 'Veteran', 6000, '2-3 months', 'enlisted', 'Battle-hardened warrior', 'T4 equipment access'),
(5, 'Blade', 11000, '3-4 months', 'officer', 'Elite soldier, NCO potential', 'Officer track begins, T5 equipment'),
(6, 'Chosen', 19000, '4-6 months', 'officer', 'Trusted leader, company influence', 'T6 equipment access'),
(7, 'Captain', 30000, '6+ months', 'commander', 'Combat leader, strategic decisions', 'Commander track begins'),
(8, 'Commander', 45000, 'Officer track', 'commander', 'High-ranking officer', 'Major command responsibilities'),
(9, 'Marshal', 65000, 'Endgame', 'commander', 'Legendary status', 'End-game content');

-- Balance values (from progression_config.json)
INSERT OR IGNORE INTO balance_values (key, value, unit, category, config_file, description) VALUES
-- XP System
('daily_base_xp', 25, 'xp', 'xp', 'progression_config.json', 'XP gained per day of service'),
('battle_participation_xp', 25, 'xp', 'xp', 'progression_config.json', 'XP gained per battle'),
('xp_per_kill', 2, 'xp', 'xp', 'progression_config.json', 'XP gained per enemy killed'),

-- Wage System
('wage_daily_base', 10, 'gold', 'economy', 'progression_config.json', 'Base daily wage'),
('wage_tier_bonus', 5, 'gold/tier', 'economy', 'progression_config.json', 'Additional wage per tier'),
('wage_hero_level_mult', 1, 'multiplier', 'economy', 'progression_config.json', 'Hero level wage multiplier'),
('wage_xp_divisor', 200, 'divisor', 'economy', 'progression_config.json', 'XP to wage bonus divisor'),
('wage_maximum_base', 200, 'gold', 'economy', 'progression_config.json', 'Maximum base wage cap'),

-- Assignment Multipliers
('wage_mult_grunt_work', 0.8, 'multiplier', 'economy', 'progression_config.json', 'Grunt work wage multiplier'),
('wage_mult_guard_duty', 0.9, 'multiplier', 'economy', 'progression_config.json', 'Guard duty wage multiplier'),
('wage_mult_cook', 0.9, 'multiplier', 'economy', 'progression_config.json', 'Cook wage multiplier'),
('wage_mult_foraging', 1.0, 'multiplier', 'economy', 'progression_config.json', 'Foraging wage multiplier'),
('wage_mult_surgeon', 1.3, 'multiplier', 'economy', 'progression_config.json', 'Surgeon wage multiplier'),
('wage_mult_engineer', 1.4, 'multiplier', 'economy', 'progression_config.json', 'Engineer wage multiplier'),
('wage_mult_quartermaster', 1.2, 'multiplier', 'economy', 'progression_config.json', 'Quartermaster wage multiplier'),
('wage_mult_scout', 1.1, 'multiplier', 'economy', 'progression_config.json', 'Scout wage multiplier'),
('wage_mult_sergeant', 1.5, 'multiplier', 'economy', 'progression_config.json', 'Sergeant wage multiplier'),
('wage_mult_strategist', 1.6, 'multiplier', 'economy', 'progression_config.json', 'Strategist wage multiplier'),
('wage_mult_in_army', 1.2, 'multiplier', 'economy', 'progression_config.json', 'In-army bonus multiplier'),

-- Tier XP Thresholds (cumulative)
('tier_2_xp', 800, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 2 (Recruit)'),
('tier_3_xp', 3000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 3 (Free Sword)'),
('tier_4_xp', 6000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 4 (Veteran)'),
('tier_5_xp', 11000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 5 (Blade) - Officer track'),
('tier_6_xp', 19000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 6 (Chosen)'),
('tier_7_xp', 30000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 7 (Captain) - Commander track'),
('tier_8_xp', 45000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 8 (Commander)'),
('tier_9_xp', 65000, 'xp', 'tier', 'progression_config.json', 'XP to enter Tier 9 (Marshal)'),

-- Formation Selection
('formation_trigger_tier', 2, 'tier', 'progression', 'progression_config.json', 'Tier when formation selection unlocks'),
('formation_change_cooldown', 7, 'days', 'progression', 'progression_config.json', 'Days between formation changes'),
('formation_free_changes', 1, 'count', 'progression', 'progression_config.json', 'Free formation changes allowed');

-- Valid categories (verified from ModuleData/Enlisted/**/*.json - all content types)
INSERT OR IGNORE INTO valid_categories (category, description, used_in) VALUES
('camp_life', 'Daily camp activities and routines', 'events'),
('crisis', 'Urgent problems requiring immediate attention', 'events'),
('decision', 'Player choices with consequences', 'decisions'),
('discipline', 'Behavioral issues and corrections', 'orders'),
('discovery', 'Learning and skill development', 'events'),
('economic', 'Financial and resource management', 'events'),
('emergency_drill', 'Combat readiness training', 'orders'),
('extended_rest', 'Recovery and recuperation', 'orders'),
('foraging', 'Resource gathering activities', 'orders'),
('formation', 'Formation selection and tactics', 'orders'),
('light_duty', 'Reduced work assignments', 'orders'),
('map_incident', 'Random encounters during travel', 'events'),
('medical', 'Illness, injury, and treatment', 'events'),
('onboarding', 'New player introduction and tutorials', 'events'),
('order_event', 'Order-triggered events', 'events'),
('patrol', 'Patrol and reconnaissance missions', 'orders'),
('pay', 'Wage, payment, and compensation', 'events'),
('problems', 'General problems and complications', 'events'),
('promotion', 'Tier advancement and recognition', 'events'),
('recovery', 'Healing and rehabilitation', 'orders'),
('retinue', 'Companion and follower events', 'events'),
('social', 'Social interactions and relationships', 'events'),
('special', 'Special circumstances and unique events', 'events'),
('threshold', 'Escalation threshold triggers', 'events'),
('training', 'Skill training and practice', 'orders'),
('work', 'Work assignments and labor', 'orders');

-- Valid severities (verified from ModuleData/Enlisted/**/*.json - actually used)
INSERT OR IGNORE INTO valid_severities (severity, description, used_in) VALUES
('normal', 'Standard priority event', 'all'),
('attention', 'Requires player attention', 'all'),
('critical', 'High priority, serious consequences', 'all'),
('serious', 'Major problem or situation', 'all'),
('moderate', 'Medium severity', 'all');

-- Core systems (from src/Features)
INSERT OR IGNORE INTO core_systems (name, full_name, type, file_path, description, key_responsibilities) VALUES
('EnlistmentBehavior', 'Features.Enlistment.Behaviors.EnlistmentBehavior', 'Behavior', 'src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs', 'Core enlistment state manager', 'Tier, XP, lord tracking, save/load, grace periods'),
('ContentOrchestrator', 'Features.Content.ContentOrchestrator', 'Behavior', 'src/Features/Content/ContentOrchestrator.cs', 'Event and opportunity scheduling', 'Event triggering, cooldown management, phase awareness'),
('EscalationManager', 'Features.Escalation.EscalationManager', 'Behavior', 'src/Features/Escalation/EscalationManager.cs', 'Reputation and discipline tracking', 'Reputation, discipline, scrutiny, threshold events'),
('CompanyNeedsManager', 'Features.Company.CompanyNeedsManager', 'Static', 'src/Features/Company/CompanyNeedsManager.cs', 'Party-level needs management', 'Supply, morale, rest, readiness'),
('CompanySimulationBehavior', 'Features.Camp.CompanySimulationBehavior', 'Behavior', 'src/Features/Camp/CompanySimulationBehavior.cs', 'Daily camp life simulation', 'Daily ticks, event checks, resource consumption'),
('CampScheduleManager', 'Features.Camp.CampScheduleManager', 'Manager', 'src/Features/Camp/CampScheduleManager.cs', 'Camp routine scheduling', 'Activity slots, schedule phases'),
('CampOpportunityGenerator', 'Features.Camp.CampOpportunityGenerator', 'Behavior', 'src/Features/Camp/CampOpportunityGenerator.cs', 'Camp event generation', 'Opportunity creation, context evaluation'),
('MusterMenuHandler', 'Features.Enlistment.Behaviors.MusterMenuHandler', 'Behavior', 'src/Features/Enlistment/Behaviors/MusterMenuHandler.cs', 'Muster/payday menu system', 'Pay resolution, discharge options, promotion'),
('EnlistedMenuBehavior', 'Features.Interface.Behaviors.EnlistedMenuBehavior', 'Behavior', 'src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs', 'Main UI and menus', 'Camp hub, status, orders, decisions'),
('EnlistedDialogManager', 'Features.Conversations.Behaviors.EnlistedDialogManager', 'Behavior', 'src/Features/Conversations/Behaviors/EnlistedDialogManager.cs', 'Conversation and dialogue', 'Quartermaster, lord conversations, recruitment'),
('OrderManager', 'Features.Orders.Behaviors.OrderManager', 'Behavior', 'src/Features/Orders/Behaviors/OrderManager.cs', 'Order assignment and tracking', 'Order slots, assignment, completion'),
('OrderProgressionBehavior', 'Features.Orders.Behaviors.OrderProgressionBehavior', 'Behavior', 'src/Features/Orders/Behaviors/OrderProgressionBehavior.cs', 'Order execution and events', 'Progress tracking, order events'),
('MapIncidentManager', 'Features.Content.MapIncidentManager', 'Behavior', 'src/Features/Content/MapIncidentManager.cs', 'Map encounter management', 'Travel events, waiting events'),
('EventPacingManager', 'Features.Content.EventPacingManager', 'Behavior', 'src/Features/Content/EventPacingManager.cs', 'Event cooldown management', 'Pacing, repetition prevention'),
('PromotionBehavior', 'Features.Ranks.Behaviors.PromotionBehavior', 'Behavior', 'src/Features/Ranks/Behaviors/PromotionBehavior.cs', 'Tier advancement', 'Promotion events, requirements check'),
('EnlistedNewsBehavior', 'Features.Interface.Behaviors.EnlistedNewsBehavior', 'Behavior', 'src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs', 'News and notifications', 'Message display, priority');

-- System dependencies
INSERT OR IGNORE INTO system_dependencies (system_name, system_type, depends_on, dependency_type, description) VALUES
('EnlistmentBehavior', 'Behavior', 'Hero.MainHero', 'required', 'Core service requires player hero'),
('EnlistmentBehavior', 'Behavior', 'Campaign.Current', 'required', 'Requires active campaign'),
('ContentOrchestrator', 'Behavior', 'EnlistmentBehavior', 'required', 'Schedules based on enlistment state'),
('ContentOrchestrator', 'Behavior', 'EventPacingManager', 'required', 'Checks cooldowns before triggering'),
('EscalationManager', 'Behavior', 'EnlistmentBehavior', 'required', 'Tracks reputation for enlisted service'),
('CompanyNeedsManager', 'Static', 'MobileParty', 'required', 'Manages party-level needs'),
('CompanySimulationBehavior', 'Behavior', 'EnlistmentBehavior', 'required', 'Daily simulation during enlistment'),
('CompanySimulationBehavior', 'Behavior', 'CompanyNeedsManager', 'required', 'Updates company needs'),
('CampOpportunityGenerator', 'Behavior', 'EnlistmentBehavior', 'required', 'Generates events based on state'),
('CampOpportunityGenerator', 'Behavior', 'ContentOrchestrator', 'required', 'Registers opportunities with orchestrator'),
('MusterMenuHandler', 'Behavior', 'EnlistmentBehavior', 'required', 'Muster requires enlistment'),
('MusterMenuHandler', 'Behavior', 'EscalationManager', 'optional', 'Pay can be affected by reputation'),
('EnlistedMenuBehavior', 'Behavior', 'EnlistmentBehavior', 'required', 'UI shows enlistment state'),
('OrderManager', 'Behavior', 'EnlistmentBehavior', 'required', 'Orders require enlistment'),
('PromotionBehavior', 'Behavior', 'EnlistmentBehavior', 'required', 'Promotions based on tier/XP');

-- Error codes (verified from grep of codebase)
INSERT OR IGNORE INTO error_catalog (error_code, category, severity, description, source_file) VALUES
-- Muster errors
('E-MUSTER-001', 'MUSTER', 'error', 'Failed to register muster menus, will use legacy fallback', 'MusterMenuHandler.cs'),
('E-MUSTER-002', 'MUSTER', 'error', 'Stage transition or init failed', 'MusterMenuHandler.cs'),
('E-MUSTER-003', 'MUSTER', 'error', 'Failed to restore muster state', 'MusterMenuHandler.cs'),
('E-MUSTER-004', 'MUSTER', 'error', 'Effect application failed (pay resolution, discharge)', 'MusterMenuHandler.cs'),
('E-MUSTER-005', 'MUSTER', 'error', 'Player not enlisted or unhandled exception', 'MusterMenuHandler.cs'),
('E-MUSTER-006', 'MUSTER', 'error', 'Campaign.Current is null', 'MusterMenuHandler.cs'),
('E-MUSTER-007', 'MUSTER', 'error', 'GameMenuManager is null', 'MusterMenuHandler.cs'),
('E-MUSTER-008', 'MUSTER', 'error', 'Deferred menu activation or validation failed', 'MusterMenuHandler.cs'),
('E-MUSTER-009', 'MUSTER', 'error', 'Failed to pre-initialize text variables', 'MusterMenuHandler.cs'),
('E-MUSTER-010', 'MUSTER', 'error', 'Muster menus not registered, deferring', 'MusterMenuHandler.cs'),

-- UI errors
('E-UI-001', 'UI', 'error', 'Error handling battle menu transition', 'EnlistedMenuBehavior.cs'),
('E-UI-002', 'UI', 'error', 'Error in siege battle detection', 'EnlistedMenuBehavior.cs'),
('E-UI-003', 'UI', 'error', 'Error opening debug tools', 'EnlistedMenuBehavior.cs'),
('E-UI-004', 'UI', 'error', 'Deferred enlisted menu activation error', 'EnlistedMenuBehavior.cs'),
('E-UI-006', 'UI', 'error', 'Failed to add Return to camp options', 'EnlistedMenuBehavior.cs'),
('E-UI-007', 'UI', 'error', 'Error returning to camp', 'EnlistedMenuBehavior.cs'),
('E-UI-009', 'UI', 'error', 'OnSettlementLeftReturnToCamp error', 'EnlistedMenuBehavior.cs'),
('E-UI-010', 'UI', 'error', 'Error initializing Camp hub', 'EnlistedMenuBehavior.cs'),
('E-UI-011', 'UI', 'error', 'Error initializing enlisted status menu', 'EnlistedMenuBehavior.cs'),

-- Enlistment errors
('E-ENLIST-001', 'ENLIST', 'error', 'Error opening baggage train stash', 'EnlistmentBehavior.cs'),
('E-ENLIST-002', 'ENLIST', 'error', 'Reservist re-entry boost failed', 'EnlistmentBehavior.cs'),
('E-ENLIST-004', 'ENLIST', 'error', 'Error stashing belongings', 'EnlistmentBehavior.cs'),
('E-ENLIST-005', 'ENLIST', 'error', 'Error liquidating belongings', 'EnlistmentBehavior.cs'),
('E-ENLIST-006', 'ENLIST', 'error', 'Error smuggling item', 'EnlistmentBehavior.cs'),
('E-ENLIST-007', 'ENLIST', 'error', 'Error joining lord''s kingdom', 'EnlistmentBehavior.cs'),

-- Save/Load errors
('E-SAVELOAD-001', 'SAVELOAD', 'critical', 'Critical save/load error', 'SessionDiagnostics.cs'),
('E-SAVELOAD-002', 'SAVELOAD', 'error', 'Error serializing minor faction desertion cooldowns', 'EnlistmentBehavior.cs'),
('E-SAVELOAD-003', 'SAVELOAD', 'error', 'Error serializing issued rations', 'EnlistmentBehavior.cs'),

-- Diagnostics errors
('E-DIAG-002', 'DIAG', 'error', 'Failed to run startup diagnostics', 'ModConflictDiagnostics.cs'),
('E-DIAG-003', 'DIAG', 'error', 'Failed to refresh deferred patch diagnostics', 'ModConflictDiagnostics.cs'),
('E-DIAG-004', 'DIAG', 'error', 'Failed to log behaviors', 'ModConflictDiagnostics.cs'),

-- Quartermaster errors
('E-QM-025', 'QM', 'error', 'Quartermaster not available', 'EnlistedMenuBehavior.cs'),
('E-QM-026', 'QM', 'error', 'Failed to open provisions UI', 'EnlistedDialogManager.cs'),

-- Baggage errors
('E-BAG-001', 'BAG', 'error', 'Error processing emergency baggage access', 'EnlistedDialogManager.cs'),
('E-BAG-002', 'BAG', 'error', 'Error processing column halt request', 'EnlistedDialogManager.cs'),

-- Dialog errors
('E-DIALOG-003', 'DIALOG', 'error', 'Error cleaning up menu after discharge', 'EnlistedDialogManager.cs'),
('E-DIALOG-004', 'DIALOG', 'error', 'Failed to add retirement supplies', 'EnlistedDialogManager.cs'),

-- Camp errors
('E-CAMP-001', 'CAMP', 'error', 'Camp opportunity generation error', 'CampOpportunityGenerator.cs'),
('E-CAMP-002', 'CAMP', 'error', 'Camp context evaluation error', 'CampOpportunityGenerator.cs'),
('E-CAMP-003', 'CAMP', 'error', 'Camp event registration error', 'CampOpportunityGenerator.cs');

-- API patterns (from decompiled code and usage)
INSERT OR IGNORE INTO api_patterns (api_name, category, usage_pattern, description, common_mistakes) VALUES
('GiveGoldAction.ApplyByHeroGained', 'economy', 'GiveGoldAction.ApplyByHeroGained(Hero.MainHero, amount);', 'Give gold to player safely', 'Never modify Hero.Gold directly'),
('TextObject', 'localization', 'new TextObject("{=key}Localized text");', 'Create localized string', 'Never use string concatenation for displayed text'),
('Campaign.Current?.GetCampaignBehavior<T>()', 'core', 'var behavior = Campaign.Current?.GetCampaignBehavior<EnlistmentBehavior>();', 'Get campaign behavior safely', 'Always null-check Campaign.Current first'),
('ChangeRelationAction.ApplyPlayerRelation', 'reputation', 'ChangeRelationAction.ApplyPlayerRelation(hero, amount);', 'Change relationship with native hero', 'For mod reputation, use EscalationManager instead'),
('InformationManager.DisplayMessage', 'ui', 'InformationManager.DisplayMessage(new InformationMessage(text, color));', 'Show message to player', 'Use TextObject for localization, not raw strings'),
('Hero.MainHero', 'core', 'if (Hero.MainHero != null) { ... }', 'Access player hero', 'ALWAYS null-check before access'),
('MobileParty.MainParty', 'party', 'if (MobileParty.MainParty != null) { ... }', 'Access player party', 'ALWAYS null-check before access'),
('CampaignTime.Now', 'time', 'var today = CampaignTime.Now;', 'Get current game time', 'Use for comparisons, not raw Day values');
