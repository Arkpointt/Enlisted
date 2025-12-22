# Enlisted Content Index

Master list of all narrative content. Reference the [Event Catalog](event-catalog-by-system.md) for mechanics and schemas.

**Skill Check Column Key:**
- **Bold skill** = Required check, affects outcome
- Skill (action) = Optional check, unlocks better option
- Skill X+ = Minimum skill threshold required
- — = No skill check

---

## Summary

| Category | Count | Description |
|----------|-------|-------------|
| **Orders** | 17 | Military directives from chain of command (6 T1-T3, 6 T4-T6, 5 T7-T9) |
| **Decisions** | 31 | Player-initiated choices from Camp Hub with costs and risks |
| **Events** | 68 | Context-triggered situations (14 escalation + 5 crisis + 49 role/universal) |
| **Map Incidents** | 45 | Triggered by map actions (battle, siege, settlement) |
| **Total** | **172** | Full content catalog (includes training chain) |

---

## Index

| Section | Contents |
|---------|----------|
| [Orders](#orders-17-total) | T1-T3 Basic, T4-T6 Specialist, T7-T9 Leadership |
| [Decisions](#decisions-30-total) | Self-Care, Training, Social, Economic, Career, Info, Equipment, Risk |
| [Events](#events-63-total) | Escalation (Scrutiny, Discipline, Medical), Role (Scout, Medic, Engineer, Officer, Operative, NCO), Universal |
| [Map Incidents](#map-incidents-42-total) | LeavingBattle, DuringSiege, EnteringTown, EnteringVillage, LeavingSettlement, WaitingInSettlement |
| [Coverage Summary](#content-coverage-summary) | Counts and next steps |

---

## Orders (17 total)

### T1-T3: Basic Soldier

| ID | Name | Premise | Systems |
|----|------|---------|---------|
| `order_guard_duty` | Guard Duty | Stand watch through the night. | Athletics, Officer Rep, Readiness |
| `order_camp_patrol` | Camp Patrol | Walk the perimeter, keep eyes open. | Athletics, Scouting, Officer Rep, Soldier Rep |
| `order_firewood` | Firewood Collection | The camp needs fuel. Grab an axe. | Athletics, Soldier Rep, Morale |
| `order_equipment_check` | Equipment Inspection | Help the quartermaster check gear. | Crafting, Officer Rep, Equipment |
| `order_muster` | Muster Inspection | Stand for inspection before the lord. | Officer Rep, Lord Rep |
| `order_sentry` | Sentry Post | Watch the road. Report anything strange. | Athletics, Perception, Readiness |

### T4-T6: Specialist

| ID | Name | Skill Req | Premise | Systems |
|----|------|-----------|---------|---------|
| `order_scout_route` | Scout the Route | Scouting 40 | Ride ahead, map the terrain, spot threats. | Scouting, Athletics, Tactics, Lord Rep, Gold |
| `order_treat_wounded` | Treat the Wounded | Medicine 40 | The surgeon needs hands. Help with the injured. | Medicine, Athletics, Officer Rep, Soldier Rep, Morale |
| `order_repair_equipment` | Equipment Repair | Crafting 50 | Fix what's broken before the next fight. | Crafting, Engineering, Equipment |
| `order_forage` | Forage Supplies | Scouting 30 | Find food and useful material in the countryside. | Scouting, Athletics, Supplies |
| `order_lead_patrol` | Lead a Patrol | Tactics 50 | Take three men, sweep the eastern woods. | Tactics, Leadership, Scouting, Lord Rep, Gold |
| `order_inspect_defenses` | Inspect Defenses | Engineering 40 | Check the walls, find weaknesses before they do. | Engineering, Tactics, Lord Rep, Readiness |

### T7-T9: Leadership

| ID | Name | Skill Req | Premise | Systems |
|----|------|-----------|---------|---------|
| `order_command_squad` | Command a Squad | Leadership 80, Tactics 70 | You're responsible for twenty men today. | Leadership, Tactics, All Reps, Gold, Renown |
| `order_strategic_planning` | Strategic Planning | Tactics 100 | The lord wants your assessment of the enemy position. | Tactics, Leadership, Lord Rep, Gold, Renown |
| `order_coordinate_supply` | Coordinate Supply | Steward 80 | Organize the baggage train. Keep us fed. | Steward, Leadership, Lord Rep, Supplies, Gold |
| `order_interrogate` | Interrogate Prisoner | Charm 60 | We took a scout. Find out what he knows. | Charm, Roguery, Lord Rep, Gold |
| `order_inspect_readiness` | Inspect Company Readiness | Leadership 100 | Walk the camp. Tell me if we're ready for battle. | Leadership, Tactics, All Reps, Readiness, Morale |

### Order Failure Consequences

Some orders have severe failure penalties beyond reputation loss:

| Order | On Critical Failure |
|-------|---------------------|
| `order_scout_route` | Ambush: **Troop Loss 1-2**, player HP loss |
| `order_lead_patrol` | Patrol lost: **Troop Loss 2-4**, Morale crash |
| `order_forage` | Spoiled food brought back: **Food Loss**, Medical Risk |
| `order_command_squad` | Squad routed: **Troop Loss 3-6**, Lord Rep crash |
| `order_coordinate_supply` | Supply theft: **Food Loss**, Scrutiny on player |

---

## Decisions (30 total)

### Self-Care (3)

| ID | Name | Premise | Systems |
|----|------|---------|---------|
| `dec_rest` | Rest | Find a quiet spot and close your eyes. | Rest |
| `dec_rest_extended` | Extended Rest | You need real sleep, not just a nap. | Rest |
| `dec_seek_treatment` | Seek Treatment | The surgeon's tent. Time to deal with this properly. | Gold, Medical Risk, HP |

### Training (6)

| ID | Name | Premise | Skill Check | Base Risk | Outcomes |
|----|------|---------|-------------|-----------|----------|
| `dec_weapon_drill` | Weapon Drill | Practice forms with the training posts. | Combat | 5% injury | +XP. High skill: bonus XP |
| `dec_spar` | Sparring Match | Find someone willing to trade blows. | Combat, Athletics | 25% injury | Win/Lose/Injury. Skill 90+: "Impressive" |
| `dec_endurance` | Endurance Training | Run the camp perimeter until your lungs burn. | Athletics | 10% injury | +XP. Athletics 60+: reduced injury |
| `dec_study_tactics` | Study Tactics | Borrow a map, think about formations. | Tactics | 0% | +XP. Tactics 60+: bonus insight |
| `dec_practice_medicine` | Practice Medicine | Offer to help the surgeon with simple cases. | Medicine | 0% | +XP. Medicine 40+: required |
| `dec_train_troops` | Train the Men | Take recruits through drills. | Leadership | 20% injury | **Troop XP**. Leadership 80+: no deaths |

### Social (6)

| ID | Name | Premise | Injury Risk | Systems |
|----|------|---------|-------------|---------|
| `dec_join_men` | Join the Men | Sit by their fire, share stories. | 5% | Soldier Rep, Scrutiny |
| `dec_join_drinking` | Join the Drinking | Someone's opened a cask. Join in. | 15% | Soldier Rep, Scrutiny, Medical Risk |
| `dec_seek_officers` | Seek the Officers | Linger near the command tent, look useful. | 0% | Officer Rep |
| `dec_keep_to_self` | Keep to Yourself | Sometimes solitude is worth more than company. | 0% | Rest |
| `dec_write_letter` | Write a Letter Home | Ink, paper, and memories of another life. | 0% | Flavor, callback chance |
| `dec_confront_rival` | Confront Your Rival | This has gone on long enough. Time to settle it. | 40% | Rep, Discipline, HP |

### Economic (5)

| ID | Name | Premise | Injury Risk | Systems |
|----|------|---------|-------------|---------|
| `dec_gamble_low` | Gamble (Low Stakes) | A few coins on dice. Nothing serious. | 5% | Gold, Scrutiny |
| `dec_gamble_high` | Gamble (High Stakes) | Real money. Real risk. | 15% | Gold, Scrutiny, HP |
| `dec_side_work` | Side Work | Someone in camp needs labor. Pays decent. | 10% | Gold, Rest, HP |
| `dec_shady_deal` | Shady Deal | A man knows a man who has something to sell. | 20% | Gold, Scrutiny, HP |
| `dec_visit_market` | Visit the Market | Browse what the town has to offer. | 0% | Gold (spend) |

### Career (3)

| ID | Name | Premise | Systems |
|----|------|---------|---------|
| `dec_request_audience` | Request an Audience | Ask to speak with the lord directly. | Lord Rep |
| `dec_volunteer_duty` | Volunteer for Duty | Tell the sergeant you want extra work. | Officer Rep |
| `dec_request_leave` | Request Leave | Ask permission to visit town on your own time. | Officer Rep |

### Information (3)

| ID | Name | Premise | Injury Risk | Systems |
|----|------|---------|-------------|---------|
| `dec_listen_rumors` | Listen to Rumors | Keep your ears open around camp. | 0% | World info |
| `dec_scout_area` | Scout the Surroundings | Take a walk beyond the camp perimeter. | 15% | Scouting XP, HP |
| `dec_check_supplies` | Check Supply Situation | Ask the quartermaster how we're doing. | 0% | Company Needs info |

### Equipment (2)

| ID | Name | Premise | Systems |
|----|------|---------|---------|
| `dec_maintain_gear` | Maintain Your Gear | Oil, polish, sharpen. Keep it battle-ready. | Equipment |
| `dec_visit_quartermaster` | Visit the Quartermaster | See what's available, maybe make a request. | Gold |

### Risk-Taking (3)

| ID | Name | Premise | Injury Risk | Systems |
|----|------|---------|-------------|---------|
| `dec_dangerous_wager` | Accept a Dangerous Wager | Someone's bet you can't do something stupid. | 50% | Gold, HP, Wound |
| `dec_prove_courage` | Prove Your Courage | Do something reckless to earn respect. | 35% | Valor, Rep, HP |
| `dec_challenge` | Challenge Someone | Call them out. Settle it properly. | 60% | Rep, Valor, HP |

---

## Events (69 total)

### Escalation: Scrutiny (5)

| ID | Threshold | Name | Premise | Options |
|----|-----------|------|---------|---------|
| `evt_scrutiny_2` | 2 | Watchful Eyes | Someone's paying attention to you lately. | Lay low / Act normal / Deflect suspicion |
| `evt_scrutiny_4` | 4 | Questions Asked | An officer pulls you aside. Casual questions, but pointed. | Answer honestly / Lie / Evade |
| `evt_scrutiny_6` | 6 | Under Investigation | They're asking around about you. This is formal now. | Cooperate / Obstruct / Find a scapegoat |
| `evt_scrutiny_8` | 8 | Evidence Found | Someone found something. A witness, a stolen item, a note. | Confess / Deny / Flee |
| `evt_scrutiny_10` | 10 | The Arrest | Soldiers approach with purpose. "Come with us." | Surrender / Fight / Bargain |

### Escalation: Discipline (5)

| ID | Threshold | Name | Premise | Options |
|----|-----------|------|---------|---------|
| `evt_disc_2` | 2 | Verbal Warning | The sergeant pulls you aside. "Don't make this a habit." | Accept / Argue / Shrug it off |
| `evt_disc_4` | 4 | Formal Reprimand | Your name's on a list now. The captain knows. | Apologize / Stay silent / Blame others |
| `evt_disc_6` | 6 | Restricted to Camp | No leaving the perimeter. You're being watched. | Comply / Sneak out anyway / Appeal |
| `evt_disc_8` | 8 | Disciplinary Hearing | Stand before the captain. Explain yourself. | Honest defense / Eloquent excuse / Accept punishment |
| `evt_disc_10` | 10 | Court Martial | This is a trial. The lord himself presides. | Plead mercy / Defend your actions / Demand trial by combat |

### Escalation: Medical (4)

| ID | Threshold | Name | Premise | Options |
|----|-----------|------|---------|---------|
| `evt_med_2` | 2 | Feeling Unwell | Something's not right. Fatigue, aches, fever starting. | Rest / Push through / See the surgeon |
| `evt_med_3` | 3 | Illness Takes Hold | You're properly sick now. Can't hide it. | Bed rest / Light duties / Keep working |
| `evt_med_4` | 4 | Serious Condition | The surgeon looks concerned. "You need to stop." | Follow orders / Refuse treatment / Self-medicate |
| `evt_med_5` | 5 | Critical | This could kill you. The surgeon is blunt about it. | Intensive care / Risky treatment / Accept fate |

### Muster Events (6)

Triggered during the 12-day muster cycle. See `quartermaster-dialogue-implementation.md` for dialogue.

| ID | Trigger | Name | Skill Check | Outcome |
|----|---------|------|-------------|---------|
| `evt_muster_ration_issued` | Muster + T1-T4 | Ration Issue | — | +1 Food (quality by QM Rep) |
| `evt_muster_ration_denied` | Muster + Supply <50% | No Ration | — | Player keeps old ration, warning |
| `evt_baggage_clear` | Muster + 30% chance | Baggage Clear | — | Pass inspection |
| `evt_baggage_lookaway` | Contraband + QM 65+ | QM Looks Away | — | No penalty (favor used) |
| `evt_baggage_bribe` | Contraband + QM 35-65 | Bribe Opportunity | **Charm** (negotiate) | Pay 50-75% value or confiscate |
| `evt_baggage_confiscate` | Contraband + QM <35 | Confiscation | — | Item lost, fine, +Scrutiny |

### Party Crisis Events (Rare)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_desertion_wave` | Desertion | Men are slipping away in the night. Morale has broken. | Stop them / Let them go / Report | **Troop Loss 3-8**, Morale, Discipline |
| `evt_food_theft` | Stolen Rations | Someone took the company's food stores. | Hunt thief / Ration / Forage | **Food Loss**, Supplies, Scrutiny |
| `evt_ambush_patrol` | Patrol Ambushed | The patrol you sent didn't all come back. | Search / Mourn / Avenge | **Troop Loss 1-4**, Morale, Soldier Rep |
| `evt_disease_outbreak` | Camp Fever | Fever sweeps the camp. The surgeon is overwhelmed. | Quarantine / Pray / Move camp | **Troop Loss 2-6**, **Troop Wounded**, Medical Risk |
| `evt_supply_spoilage` | Rotten Supplies | The stores are bad. Worms, mold, sickness waiting. | Destroy / Use carefully / Blame quartermaster | **Food Loss**, Supplies, Medical Risk |

### Role: Scout (6)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `evt_scout_tracks` | Strange Tracks | Tracks that don't match our patrol routes. | **Scouting** (identify) | Report / Investigate / Set ambush | Scouting XP, Scouting 60+: identify enemy type |
| `evt_scout_enemy_camp` | Enemy Campfire | Smoke on the horizon. | **Scouting** (approach) | Scout closer / Report / Ignore | Scouting 90+: detailed intel without risk |
| `evt_scout_shortcut` | The Shortcut | Faster route, risky terrain. | **Scouting** (navigate) | Take risk / Play safe / Suggest | Scouting 60+: safe shortcut |
| `evt_scout_lost_patrol` | The Lost Patrol | Three men didn't return. | **Scouting** (track) | Search / Report / Wait | Scouting 40+: find them alive |
| `evt_scout_ambush_site` | Perfect Ambush Ground | Ideal terrain. Theirs or ours? | **Tactics** (assess) | Set ambush / Avoid / Report | Tactics 60+: successful ambush |
| `evt_scout_deserter` | The Deserter | Company man hiding in the woods. | Charm (persuade) | Turn in / Let go / Help escape | Honor, Charm 40+: talk him back |

### Role: Medic (6)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `evt_med_triage` | Triage | Too many wounded, not enough hands. | **Medicine** (prioritize) | Officers / Worst wounds / Random | Med 60+: save more lives |
| `evt_med_infection` | The Infection | Wound turning bad. Amputation? | **Medicine** (decide) | Amputate / Save limb / Let him decide | Med 80+: save limb successfully |
| `evt_med_shortage` | Medicine Shortage | Running low on bandages and herbs. | Scouting (find) | Ration / Find alternatives / Steal | Scouting 40+: find herbs |
| `evt_med_dying_man` | The Dying Man | He's not going to make it. | **Medicine** (comfort) | Stay / Give peace / Move on | Med 90+: miracle save possible |
| `evt_med_malingerer` | The Malingerer | Wounds don't match complaints. | **Medicine** (diagnose) | Call out / Let slide / Private warning | Med 40+: certain diagnosis |
| `evt_med_plague_fear` | Plague Whispers | Men whispering about sickness. Panic coming. | **Medicine** (investigate) | Investigate / Calm / Quarantine | Med 60+: identify real vs fear |

### Role: Engineer (5)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_eng_weak_wall` | The Weak Wall | This section of fortification won't hold. | Report / Fix it yourself / Exploit it later | Engineering, Officer Rep |
| `evt_eng_siege_work` | Siege Assignment | They want you supervising construction of siege equipment. | Accept proudly / Delegate / Refuse | Engineering, Leadership, Officer Rep |
| `evt_eng_sabotage` | Sabotage Opportunity | You could weaken their defenses from within. Risky. | Do it / Too dangerous / Report the chance | Engineering, Scrutiny, Lord Rep |
| `evt_eng_bridge` | The Bridge Problem | We need to cross, but the bridge looks unsafe. | Test it / Find another way / Strengthen it first | Engineering, Tactics |
| `evt_eng_supply_cache` | Hidden Cache | You notice the quartermaster hiding supplies. Personal stash? | Report / Blackmail / Ignore | Honor, Scrutiny, Officer Rep |

### Role: Officer (6)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_off_insubordination` | Insubordination | A soldier refuses a direct order. Others are watching. | Punish publicly / Private discipline / Let it go | Leadership, Discipline, Soldier Rep, Officer Rep |
| `evt_off_competing_orders` | Conflicting Orders | The captain said one thing, the lord implies another. | Follow captain / Follow lord / Find middle ground | Leadership, Lord Rep, Officer Rep |
| `evt_off_promotion_rival` | Rival for Promotion | Another soldier is being considered for your advancement. | Compete fairly / Undermine them / Support them | Honor, Officer Rep, Lord Rep |
| `evt_off_morale_crisis` | Morale Collapse | The men are losing heart. Something needs to be done. | Inspiring speech / Extra rations / Punish complainers | Leadership, Morale, Soldier Rep |
| `evt_off_desertion_plot` | Desertion Plot | You overhear men planning to leave tonight. | Report them / Confront privately / Join them | Honor, Discipline, Soldier Rep |
| `evt_off_lords_favor` | The Lord's Favor | The lord singles you out for praise. Others notice. | Accept humbly / Accept proudly / Deflect to others | Lord Rep, Officer Rep, Soldier Rep |

### Role: Operative (5)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_op_black_market` | Black Market Contact | A man offers goods that shouldn't be available. | Buy / Report / Become a regular | Gold, Scrutiny, Roguery |
| `evt_op_bribe` | The Bribe | Someone wants you to look the other way. Good money. | Take it / Refuse / Take it and report | Gold, Honor, Scrutiny |
| `evt_op_information` | Valuable Information | You learn something important. Selling it would be profitable. | Sell / Report / Keep for leverage | Gold, Scrutiny, Lord Rep |
| `evt_op_frame` | Frame Job | You could make someone else take the blame for your problems. | Do it / Too risky / Find another way | Scrutiny, Discipline, Honor |
| `evt_op_double_deal` | Playing Both Sides | Two factions want your loyalty. Both are paying. | Pick one / String both along / Expose them | Gold, Scrutiny, multiple Reps |

### Role: NCO (5 + Training Chain)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_nco_new_recruit` | The New Recruit | Fresh from his village, terrified. Your problem now. | Tough love / Gentle guidance / Assign a mentor | Leadership, Soldier Rep |
| `evt_nco_squad_dispute` | Squad Dispute | Two of your men are at each other's throats. | Let them fight / Mediate / Punish both | Leadership, Discipline, Soldier Rep |
| `evt_nco_equipment_theft` | Missing Equipment | Gear is disappearing. Someone in your squad is responsible. | Investigate / Collective punishment / Cover it up | Scrutiny, Discipline, Soldier Rep |
| `evt_nco_veteran_advice` | The Old Soldier | A veteran offers advice. It contradicts your orders. | Listen / Dismiss / Report | Leadership, Officer Rep, Soldier Rep |
| `evt_nco_initiative` | Taking Initiative | Opportunity to act without orders. Could be great or disastrous. | Act boldly / Wait for orders / Consult quietly | Leadership, Officer Rep, Lord Rep |

### NCO Training Chain (T4-T6 only)

Player can train the lord's T1-T3 troops, giving them XP. Risk of injury to player or trainees.

**Initiation (Decision)**

| ID | Name | Cost | Gate | Premise |
|----|------|------|------|---------|
| `dec_train_troops` | Train the Men | -10 Rest, 3 hrs | Tier 4+, Leadership 40+ | Take a squad of recruits through drills. They'll learn. You'll sweat. |

**Training Outcomes (Weighted Random)**

| ID | Chance | Name | Outcome | Systems |
|----|--------|------|---------|---------|
| `evt_train_success` | 40% | Good Progress | The men improve. You feel like a proper sergeant. | **Troop XP +15**, Leadership XP, Officer Rep +5 |
| `evt_train_excellent` | 15% | Exceptional Session | Something clicks. They're actually getting it. | **Troop XP +30**, Leadership XP, Officer Rep +8, Soldier Rep +5 |
| `evt_train_mediocre` | 25% | Slow Going | They're trying. Results are mixed. | **Troop XP +8**, Leadership XP |
| `evt_train_accident_minor` | 10% | Training Accident | A recruit takes a bad fall. Bruised, not broken. | Troop XP +5, **1 Troop Wounded** |
| `evt_train_accident_player` | 5% | You Go Down | Demonstrating a move, you twist something wrong. | **Player -15 HP**, Leadership XP, Soldier Rep +3 (respect) |
| `evt_train_accident_serious` | 4% | Serious Injury | Blood. Someone's hurt badly. Your fault or not, it happened. | **1 Troop Wounded**, Officer Rep -5, Soldier Rep -3 |
| `evt_train_death` | 1% | Training Death | A practice blade, a bad angle, a life ended. | **1 Troop Loss**, Officer Rep -10, Discipline +2, Morale -5 |

**Chain Events (Triggered by Outcomes)**

| ID | Trigger | Name | Premise | Options |
|----|---------|------|---------|---------|
| `evt_train_officer_notice` | After 3+ successes | Captain's Attention | "I've seen what you're doing with those recruits." | Accept praise / Deflect / Request more responsibility |
| `evt_train_resentment` | After accident | Resentful Trainee | One of the men you hurt is holding a grudge. | Apologize / Ignore / Confront |
| `evt_train_protege` | After 5+ successes | Your Protégé | One recruit follows you like a shadow. Eager to learn. | Mentor him / Push him away / Recommend to officers |
| `evt_train_investigation` | After death | The Investigation | The captain wants answers about what happened. | Truth / Blame equipment / Blame the recruit |

**Skill Modifiers**

| Player Skill | Effect |
|--------------|--------|
| Leadership 60+ | +10% excellent, -3% accident |
| Leadership 80+ | +15% excellent, -5% accident, death → serious |
| Medicine 40+ | Serious → Minor on successful check |
| Athletics 60+ | -2% player injury |

### Universal (8)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `evt_camp_theft` | Theft in Camp | Someone stole from you. You have suspicions. | Accuse / Investigate / Let it go | Soldier Rep, Scrutiny, Discipline |
| `evt_camp_brawl` | The Brawl | Fists are flying. You could intervene or stay clear. | Break it up / Join in / Watch | Soldier Rep, HP, Discipline |
| `evt_camp_rumor` | Rumors About You | People are talking. Some of it's true. | Confront / Ignore / Start counter-rumors | Rep tracks, Discipline |
| `evt_camp_favor` | A Soldier's Favor | Someone asks you to cover for them. Risk, but builds trust. | Help / Refuse / Demand payment | Soldier Rep, Scrutiny |
| `evt_camp_letter` | Letter from Home | News from your old life. Could be good or bad. | Read privately / Share / Burn it unread | Flavor, Morale |
| `evt_camp_confession` | The Confession | Someone tells you something they shouldn't have. | Keep secret / Report / Use it | Honor, Scrutiny, multiple Reps |
| `evt_camp_gamble_invite` | Gambling Invitation | "We've got a game going. Good money if you're skilled." | Join / Decline / Watch and learn | Gold, Soldier Rep, Scrutiny |
| `evt_camp_promotion_offer` | Advancement Opportunity | The captain asks if you're ready for more responsibility. | Accept / Decline / Ask for time | Officer Rep, Lord Rep |

### Food & Supply Events (8)

Events triggered by personal food inventory or company supply status. See `player-food-ration-system.md` for full details.

**Loss Events** (Trigger: Player has 2+ personal food items)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `evt_food_spoilage` | Food Spoilage | Summer heat ruined your personal stores. | — | Accept loss | **-1 Personal Food** |
| `evt_food_rats` | Rats in Camp | Vermin got into your supplies. | **Cunning** (hunt) | Accept / Hunt rats / Set traps | -1 Food, Cunning 50+: no loss |
| `evt_food_theft` | Missing Provisions | A lancemate took from your pack. | **Charm** (confront) | Confront / Let slide / Report | -1 Food, Soldier Rep, Discipline |
| `evt_food_battle_damage` | Battle Damage | Your pack was hit. Provisions scattered. | — | Accept loss | -1 to -2 Food (based on intensity) |
| `evt_food_refugee` | Desperate Refugee | A starving family begs for food. | — | Give / Refuse / Sell | -1 Food, Honor, Mercy, Charm |
| `evt_food_checkpoint` | Checkpoint Shakedown | Guards demand tribute. They eye your provisions. | **Roguery** (bluff) | Pay gold / Give food / Refuse | -30g or -1 Food, Roguery 40+: no loss |

**Shortage Events** (Trigger: Player has <3 days food)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `evt_food_starving` | Starving | No food. Your stomach aches. | Various | Buy / Forage / Beg QM / Endure | -10 HP, -5 Morale, or buy food |
| `evt_food_low_warning` | Running Low | Only 2 days of food left. | — | (Info only) | Warning message |

**Supply Crisis Events** (Trigger: Company Supply <30%)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `evt_supply_critical` | Starvation in Camp | The company is out of food. Desperate. | **Scouting** (forage) | Emergency forage / Raid village / Cut rations / Desert | +10% or -30 relation, Morale, Scrutiny |
| `evt_supply_catastrophic` | Supply Collapse | 10% supply. Men are deserting. | — | (Automatic effects) | 1d6 desertions/day, +5 Scrutiny/day |

---

## Map Incidents (45 total)

### LeavingBattle (11)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `mi_loot_decision` | Dead Man's Purse | Gold on a corpse. Officers aren't looking. | Roguery (hide) | Take / Leave / Report | Scrutiny, Gold, Honor |
| `mi_wounded_enemy` | The Wounded Enemy | He's not dead yet. He's looking at you. | Medicine (save) | Kill / Spare / Call for aid | Mercy, Valor, Soldier Rep |
| `mi_first_kill` | The First Kill | If this was your first, it's hitting you now. | — | Process / Push down / Seek company | Valor, Morale |
| `mi_officer_notice` | The Captain's Eye | "You fought well today." Public recognition. | Charm (deflect) | Accept humbly / Proudly / Deflect | Officer Rep, Soldier Rep |
| `mi_comrade_down` | Fallen Comrade | Someone you knew is dead. Burial detail needs help. | — | Help / Pay respects / Move on | Soldier Rep, Morale |
| `mi_battlefield_find` | Battlefield Discovery | Something valuable in the mud. | Scouting (assess) | Keep / Turn in / Sell later | Scrutiny, Gold, Officer Rep |
| `mi_triage_call` | Call for Medics | Wounded are screaming. | **Medicine 40+** | Help (save lives) / Carry / Move on | Medicine XP, Soldier Rep, Mercy |
| `mi_enemy_intel` | Enemy Documents | Papers on a dead officer. | **Scouting 30+** | Report / Read first (intel) / Keep quiet | Scouting XP, Lord Rep, Scrutiny |
| `mi_battle_trophy` | Battle Trophy | You could take something to remember this. | — | Take / Leave / Give to comrade | Soldier Rep, Valor |
| `mi_survivors_choice` | The Survivors | Enemy wounded who surrendered. | Leadership (control) | Protect / Ignore / Ensure silence | Mercy, Honor, Discipline |
| `mi_ambush_losses` | Ambush! | Enemy survivors rallied. Men went down. | **Tactics** (rally) | Rally / Retreat / Hold | **Troop Loss 1-3**, HP, Morale |

### DuringSiege (10)

| ID | Name | Premise | Skill Check | Options | Systems |
|----|------|---------|-------------|---------|---------|
| `mi_siege_water` | Water Rations | Wells are low. Men are thirsty. | Scouting (find source) | Share / Hoard / Find source | Soldier Rep, Supplies, Mercy |
| `mi_siege_boredom` | Siege Tedium | Weeks of waiting. Men getting restless. | Leadership (organize) | Organize games / Discipline / Let be | Morale, Discipline |
| `mi_siege_deserter` | Watching the Walls | Someone's going over tonight. | Charm (confront) | Report / Confront / Help | Honor, Discipline, Soldier Rep |
| `mi_siege_sickness` | Camp Fever | Sickness spreading. Latrines? Water? | **Medicine** (investigate) | Investigate / Quarantine / Ignore | Medicine XP, Medical Risk, Morale |
| `mi_siege_assault_prep` | Before the Assault | Tomorrow, the walls. Tonight, prayers. | Leadership (rally) | Prepare / Rally men / Drink | Morale, Valor |
| `mi_siege_opportunity` | Wall Weakness | Weak point in their defenses. | **Engineering 40+** | Report (intel) / Exploit / Keep secret | Engineering XP, Lord Rep, Scrutiny |
| `mi_siege_gambling` | Siege Gambling | Stakes getting higher. | Roguery (win) | Join / Break up / Report | Gold, Discipline, Scrutiny |
| `mi_siege_supply_theft` | Missing Supplies | Food disappearing. Someone's hoarding. | Scouting (investigate) | Investigate / Report / Get cut | Scrutiny, Supplies, Discipline |
| `mi_siege_disease` | The Plague | Men dying in their tents. | **Medicine** (save lives) | Quarantine / Pray / Flee | **Troop Loss 2-5**, Med 60+: reduce loss |
| `mi_siege_spoiled_food` | Spoiled Rations | Half the stores are ruined. | Steward (salvage) | Ration / Forage / Inform officers | **Food Loss**, Steward 40+: reduce loss |

### EnteringTown (8)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `mi_town_tavern` | The Tavern | Off-duty. A drink, a bed, maybe trouble. | Drink moderately / Drink heavily / Stay sober | Soldier Rep, Scrutiny, Gold |
| `mi_town_merchant` | Merchant's Offer | A trader has goods at good prices. Maybe too good. | Buy / Haggle / Walk away | Gold, Trade |
| `mi_town_old_friend` | Familiar Face | Someone from your past is here. This could be good or bad. | Approach / Avoid / Observe first | Flavor, callback |
| `mi_town_message` | Message from Home | A letter waiting at the inn. Addressed to you. | Read now / Read later / Ignore | Flavor, Morale |
| `mi_town_criminal` | Criminal Contact | Someone in the shadows knows your face. They have an offer. | Listen / Refuse / Report | Scrutiny, Gold, Roguery |
| `mi_town_recruitment` | Recruitment Pitch | A merchant company is hiring. Better pay, they say. | Consider / Refuse firmly / Hear them out | Lord Rep, Gold |
| `mi_town_brawl` | Tavern Trouble | Local men don't like soldiers. Words become fists. | Fight / Defuse / Flee | HP, Scrutiny, Soldier Rep |
| `mi_town_information` | Town Gossip | The locals are talking about the war. Some of it's useful. | Listen carefully / Join conversation / Spread disinformation | Scouting, Charm |

### EnteringVillage (6)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `mi_village_gratitude` | Village Welcome | They're grateful for protection. Food, drink, smiles. | Accept graciously / Decline / Take advantage | Soldier Rep, Morale |
| `mi_village_resentment` | Cold Reception | These people don't want soldiers here. Understandable. | Be polite / Demand respect / Leave quickly | Charm, Soldier Rep |
| `mi_village_sick` | The Sick Child | A villager begs for help. Their child is fevered. | Help (Medicine) / Give supplies / Can't help | Medicine, Mercy, Supplies |
| `mi_village_rumor` | Village Rumors | Old women talk. They've seen troops moving to the east. | Listen / Dismiss / Investigate | Scouting |
| `mi_village_theft` | Caught Stealing | A soldier is taking from villagers. You see it happen. | Intervene / Report / Ignore | Honor, Discipline, Soldier Rep |
| `mi_village_recruit` | Eager Youth | A young villager wants to join the company. | Encourage / Discourage / Let him decide | Soldier Rep |

### LeavingSettlement (6)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `mi_leave_hangover` | Morning After | Too much last night. Your head is pounding. | Push through / Rest first / Seek remedy | Rest, Medical Risk |
| `mi_leave_stolen` | Something's Missing | Your coin purse is lighter than it should be. | Accept loss / Investigate / Accuse | Gold, Discipline |
| `mi_leave_farewell` | The Farewell | Someone you met doesn't want you to go. | Linger / Quick goodbye / Silent departure | Flavor, callback |
| `mi_leave_intel` | Road Ahead | Information about what's between here and there. | Pay for details / Scout yourself / Trust luck | Gold, Scouting |
| `mi_leave_stowaway` | The Stowaway | Someone's trying to leave town with the company. | Turn them in / Help hide / Ignore | Scrutiny, Mercy |
| `mi_leave_purchase` | Last-Minute Purchase | A merchant catches you before you leave. Good deal. | Buy / Haggle / Walk on | Gold |

### WaitingInSettlement (4)

| ID | Name | Premise | Options | Systems |
|----|------|---------|---------|---------|
| `mi_wait_opportunity` | Passing Opportunity | A chance to make money, learn something, or cause trouble. | Take it / Pass / Investigate first | Various |
| `mi_wait_encounter` | Chance Encounter | Someone interesting crosses your path. | Engage / Observe / Avoid | Flavor, callback |
| `mi_wait_trouble_brewing` | Trouble Brewing | You sense something about to go wrong. | Prevent / Prepare / Ignore | Various |
| `mi_wait_boredom` | Garrison Tedium | Another day of nothing. The men are getting antsy. | Find activity / Maintain discipline / Let it be | Morale, Discipline |

---

## Content Coverage Summary

### By Category

| Category | Count | JSON File | Status |
|----------|-------|-----------|--------|
| Orders | 17 | `orders_catalog.json` | Indexed |
| Decisions | 30 | `decisions_catalog.json` | Indexed |
| Events - Escalation | 14 | `events_escalation.json` | Indexed |
| Events - Crisis | 5 | `events_crisis.json` | Indexed |
| Events - Role | 41 | `events_role_*.json` | Indexed |
| Events - Universal | 8 | `events_camp_life.json` | Indexed |
| Events - Food/Supply | 10 | `events_food_supply.json` | Indexed |
| Events - Muster | 6 | `events_muster.json` | Indexed |
| Map Incidents | 45 | `incidents_*.json` | Indexed |
| **Total** | **176** | — | — |

### By Trigger

| Trigger | Count | When |
|---------|-------|------|
| System-assigned | 17 | Every 3-5 days |
| Player-initiated | 30 | Camp Hub menu |
| State threshold | 14 | Escalation crossed |
| Context/role match | 49 | Daily check |
| LeavingBattle | 10 | After field battle |
| DuringSiege | 8 | Hourly while besieging |
| EnteringTown | 8 | Opening town menu |
| EnteringVillage | 6 | Opening village menu |
| LeavingSettlement | 6 | Selecting leave |
| WaitingInSettlement | 4 | Hourly while waiting |

### Systems Touched

| System | Content Pieces |
|--------|----------------|
| Soldier Rep | 70 |
| Officer Rep | 47 |
| Lord Rep | 34 |
| Gold | 32 |
| Scrutiny | 28 |
| Discipline | 20 |
| HP/Injury | 24 |
| Skills (various) | 95 |
| **Troop Loss** | 10 |
| **Food Loss** | 12 |
| **QM Rep** | 8 |

### Next Steps

1. Convert this index to JSON files per category
2. Create XML strings in `enlisted_strings.xml`
3. Implement event/incident selection in code
4. Playtest frequency and balance

