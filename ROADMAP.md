# Enlisted Mod - Roadmap

**Current Version:** v0.6.0  
**Last Updated:** December 15, 2025

---

##  v0.6.1 - Kingdom-Style Camp Management Screen (In Progress)

**Release:** Q1 2026

Replacing the overlay-based Camp Bulletin with a full-screen **Kingdom-style Camp Management** interface. Five tabs organize all camp features:

- **Squad** - Your squad roster, member status, NCO info
- **Orders** - View/manage daily schedule (tier-based authority)
- **Activities** - Camp activities grid (rest, train, patrol, etc.)
- **Reports** - News feed, dispatches, bulletin
- **Party** - Other squads in lord's party (T5+ only, situational awareness)

This matches native Bannerlord UI patterns (like Kingdom Screen) and sets the foundation for all future camp features.

**Note:** You're serving *in* the lord's party, not commanding it. Authority to manage schedule is earned through progression.

---

##  v0.7.0 - AI Daily Schedule

**Release:** Q2 2026 (~2 months)

Leadership assigns your squad duties based on the current situation. Different activities happen at different times of day (dawn drills, afternoon patrols, night watch). The **Orders tab** in Camp Management shows your daily schedule.

**Authority Model:**
- **Schedule controlled by:** Lord/AI by default
- **Changes daily based on:** Lord's objectives, lance needs, tactical situation
- **Player authority:** Earned through rank progression and promotion to Lance Leader

**Schedule Authority by Tier/Role:**
- **T1-T2:** View only - follow assigned orders
- **T3-T4:** Can request schedule changes (approval roll based on lord relation)
- **T5-T6 (not Squad Leader):** Squad Second/Senior NCO - guided management (AI recommends, player decides)
- **Squad Leader (T5-T6):** Full control over squad schedule (requires Squad Leader promotion)

Once you're promoted to **Squad Leader** (can happen at T5 or T6 when a vacancy exists), you set the schedule. Until then, leadership decides and you serve.

**Note:** Squad Leader is a **role/promotion**, not automatic with tier. Requires vacancy and meeting promotion requirements.

No more aimless downtime. Your service has purpose.

**Example (Orders Tab):**
```
Today's Schedule (Day 7 of 12)
┌─────────────┬────────────────────────┐
│ Time Block  │ Assigned Activity      │
├─────────────┼────────────────────────┤
│ Dawn        │ Morning drill          │ ▶ Active
│ Morning     │ Guard duty             │
│ Afternoon   │ Free time              │
│ Evening     │ Lance briefing         │
│ Dusk        │ Rest                   │
│ Night       │ Rest                   │
└─────────────┴────────────────────────┘

[Select a time block to see details and modify (T5+)]
```

---

##  v0.8.0 - Squad Life Simulation

**Release:** Q3 2026 (~3 months)

Your squad members get injured, die, and ask you for help. When Harald twists his ankle on patrol, he asks you to cover his guard duty. Help him and earn favor. Refuse and he remembers. When Bjorn dies in battle, you attend his memorial.

Promotions happen for real reasons: officer killed, officer transferred, army expanded, battlefield commission. No more random rank-ups.

Your squad feels like actual people who need each other.

---

##  v0.9.0 - Persistent NCO Leaders

**Release:** Q4 2026 (~2.5 months)

Your NCO remembers recent battles, who died, and what you did. Three personality types react differently:

- **Grizzled Veteran:** "Saw you left Harald hanging. Don't expect favors back."
- **Noble Officer:** "Harald spoke to me. A soldier helps his brothers."
- **Ambitious Climber:** "You turned down Harald? Noted."

When your NCO dies, you get a new one with a different personality. Start fresh.

---

##  Version 1.0.0 - Party Squad Activity Simulation (Optional Feature)

**Status:** Planned for after core systems complete  
**Release:** Q1 2027 (estimated 2 months development)

**Note:** This feature is for **T5+ officers only** - you need rank to see what other squads are doing.

### What You'll Get:

#### Party-Wide Awareness (T5+ Only):
- **Other Squads in Lord's Party** - See 8-15 peer squads serving alongside yours:
  - "3rd Squad (Ironsides Warband) - On patrol duty"
  - "5th Squad (Hawk Company) - Training at camp"
  - "7th Squad (Shield Brothers) - Recovering from injuries"
- **Squad Activity Reports** - Camp bulletin shows what peer squads are doing:
  - "3rd Squad returned from successful patrol - captured 3 bandits"
  - "5th Squad training went well - readiness improved"
  - "7th Squad ambushed on foraging run - 2 wounded, 1 killed"
- **Meaningful Cover Requests** - Other squads ask YOUR squad for help:
  - Their squad is exhausted from repeated patrols
  - They're undermanned due to injuries
  - **Your choice matters:** Help prevents them from taking casualties

#### Optional Realistic Attrition (Configurable):
This feature can be DISABLED entirely if you prefer arcade gameplay. When enabled:

- **Routine Operations Have Consequences:**
  - Patrols can encounter bandits -> small casualties (1-5 troops)
  - Foraging runs can get ambushed -> wounded troops (recover in 7 days)
  - Scout missions can get spotted -> brief combat
  - Guard duty accidents, disease, desertion
- **Your Army Weakens Over Time** - Armies lose troops to routine operations, not just battles
- **Strategic Depth** - Can't send squads on dangerous missions constantly
- **Fully Configurable:**
  - **Casual Mode:** Disabled - pure storytelling, no casualties
  - **Normal Mode:** Light attrition (5-8% monthly) - player army only
  - **Realistic Mode:** Historical attrition (10-15% monthly) - both armies
  - **Hardcore Mode:** Heavy attrition (15-25% monthly) - challenging campaign management

### Why It Matters:
Your army feels like a real military organization with multiple units operating simultaneously. You see the bigger picture beyond just your squad. Adds immersion and optional strategic challenge.

**Example Bulletin Entries:**
```
[Camp Bulletin - Today's Activity]

3rd Squad (The Ironsides): On patrol north of camp
- Status: 2 hours out, expected return at dusk

5th Squad (Hawk Company): Training with recruits
- Status: Building readiness after last battle

7th Squad (Shield Brothers): Requests cover for foraging duty
- They're at 40% readiness, risk of casualties if sent alone
- [Accept: Your squad goes, -6 fatigue, prevents 7th Squad casualties]
- [Decline: 7th Squad goes anyway, may take losses]
```

---

##  Release Timeline Summary

| Version | Feature | Timeline | Development Time |
|---------|---------|----------|------------------|
| **v0.6.1** | Camp Bulletin Polish | Q1 2026 | 1-2 weeks |
| **v0.7.0** | AI Daily Schedule | Q2 2026 | ~8 weeks |
| **v0.8.0** | Squad Life Simulation | Q3 2026 | ~12 weeks |
| **v0.9.0** | Persistent NCO Leaders | Q4 2026 | ~10 weeks |
| **v1.0.0** | Army Squad Activity | Q1 2027 | ~8 weeks |

**Total estimated development:** ~6-9 months for full feature set

---

##  What's Already in the Mod (v0.6.0)

Don't forget what you already have access to:

[x] **Core Military Service** - Enlist, ranks, formations, pay system  
[x] **Squad System** - Join culture-specific military units  
[x] **Duties System** - Formation-based job assignments  
[x] **Fatigue & Camp Activities** - Stamina management and camp interaction  
[x] **Pay Tension System** - Financial crisis gameplay with moral choices  
[x] **Companion Management** - Battle participation toggle  
[x] **Commander's Retinue** - Personal troops at T7-T9  
[x] **Formation Training** - Daily skill XP  
[x] **Squad Life Events** - Random military life events  
[x] **Camp Life Simulation** - Dynamic camp conditions  
[x] **Quartermaster System** - Equipment, provisions, personality  
[x] **Story Packs** - Modular event content  
[x] **Full Localization** - Translation support

---

##  Frequently Asked Questions

**Q: Will these features be free updates?**  
A: Yes! All planned features are free updates to the existing mod.

**Q: Can I keep my save when updating?**  
A: We aim for save compatibility, but major updates (0.7.0+) may require new campaigns. We'll announce this clearly before release.

**Q: What if I don't want the attrition system in v1.0.0?**  
A: It's completely optional and can be disabled in config. Default setting will be "Normal" (light attrition, player only).

**Q: Will this work with other mods?**  
A: We design for compatibility. Most features are self-contained and shouldn't conflict with overhaul mods, economy mods, or combat mods.

**Q: Can I suggest features?**  
A: Absolutely! Join our discussions and share your ideas. We're building this for the community.

**Q: What about AI lords having enlisted soldiers?**  
A: That's a separate feature we've designed but it's low priority. Focus is on making YOUR experience amazing first.

---

##  Stay Updated

- **Development Updates:** Watch this file for status changes
- **Testing Opportunities:** We'll announce beta testing for major releases
- **Bug Reports:** Use GitHub Issues or our community channels
- **Discussions:** Share feedback on features before they're built!

---

**Thank you for serving with Enlisted! Your feedback shapes our development priorities.**

*Remember: Timelines are estimates. Quality over speed - we'll take the time needed to make each feature great.*

