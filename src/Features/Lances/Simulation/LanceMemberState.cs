using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Simulation
{
    /// <summary>
    /// Health state of a lance member - determines availability for duties.
    /// Track C1: Lance Life Simulation core state tracking.
    /// </summary>
    public enum MemberHealthState
    {
        /// <summary>Fully operational, can perform all duties</summary>
        Healthy = 0,
        
        /// <summary>Can work but with reduced effectiveness (1-3 days recovery)</summary>
        MinorInjury = 1,
        
        /// <summary>Cannot perform duties, requires recovery (7-14 days)</summary>
        MajorInjury = 2,
        
        /// <summary>Bedridden, may require medical evacuation (14-30 days)</summary>
        Incapacitated = 3,
        
        /// <summary>Removed from roster permanently</summary>
        Dead = 4
    }

    /// <summary>
    /// Activity state of a lance member - what they're currently doing.
    /// </summary>
    public enum MemberActivityState
    {
        /// <summary>Performing assigned camp duties (linked to AI Camp Schedule)</summary>
        OnDuty = 0,
        
        /// <summary>Personal time, recreational activities</summary>
        OffDuty = 1,
        
        /// <summary>Recovering from injury or illness</summary>
        SickBay = 2,
        
        /// <summary>Temporary absence (personal matters)</summary>
        OnLeave = 3,
        
        /// <summary>Temporarily assigned elsewhere</summary>
        Detached = 4
    }

    /// <summary>
    /// Relationship level between player and a lance member.
    /// </summary>
    public enum MemberRelationship
    {
        /// <summary>Hostile or antagonistic (relation &lt; -30)</summary>
        Hostile = -2,
        
        /// <summary>Cold or distant (-30 to -10)</summary>
        Cold = -1,
        
        /// <summary>Neutral, professional (-10 to 10)</summary>
        Neutral = 0,
        
        /// <summary>Friendly, cooperative (10 to 30)</summary>
        Friendly = 1,
        
        /// <summary>Loyal, close bond (30+)</summary>
        Loyal = 2
    }

    /// <summary>
    /// Complete state tracking for a single lance member.
    /// Includes health, activity, relationships, and career progression.
    /// </summary>
    public class LanceMemberState
    {
        /// <summary>Unique identifier for this member (generated GUID or troop ID)</summary>
        [SaveableProperty(1)]
        public string MemberId { get; set; }

        /// <summary>Display name of the member</summary>
        [SaveableProperty(2)]
        public string Name { get; set; }

        /// <summary>Current health state</summary>
        [SaveableProperty(3)]
        public MemberHealthState HealthState { get; set; }

        /// <summary>Current activity state</summary>
        [SaveableProperty(4)]
        public MemberActivityState ActivityState { get; set; }

        /// <summary>Current rank tier (1-6 for enlisted)</summary>
        [SaveableProperty(5)]
        public int RankTier { get; set; }

        /// <summary>Days of service in the lance</summary>
        [SaveableProperty(6)]
        public int DaysInService { get; set; }

        /// <summary>Number of battles participated in</summary>
        [SaveableProperty(7)]
        public int BattlesParticipated { get; set; }

        /// <summary>Relationship score with the player (-100 to 100)</summary>
        [SaveableProperty(8)]
        public int RelationWithPlayer { get; set; }

        /// <summary>Number of favors the player owes this member</summary>
        [SaveableProperty(9)]
        public int FavorsOwed { get; set; }

        /// <summary>Number of favors this member owes the player</summary>
        [SaveableProperty(10)]
        public int FavorsOwedToPlayer { get; set; }

        /// <summary>Campaign time when current injury/illness will heal</summary>
        [SaveableProperty(11)]
        public CampaignTime RecoveryTime { get; set; }

        /// <summary>Campaign time when leave ends (if on leave)</summary>
        [SaveableProperty(12)]
        public CampaignTime LeaveEndTime { get; set; }

        /// <summary>Is this member the current lance leader?</summary>
        [SaveableProperty(13)]
        public bool IsLanceLeader { get; set; }

        /// <summary>Is this member eligible for promotion?</summary>
        [SaveableProperty(14)]
        public bool IsEligibleForPromotion { get; set; }

        /// <summary>Campaign time this member joined the lance</summary>
        [SaveableProperty(15)]
        public CampaignTime JoinDate { get; set; }

        /// <summary>Formation type: infantry, archer, cavalry, horsearcher</summary>
        [SaveableProperty(16)]
        public string FormationType { get; set; }

        /// <summary>Last duty performed by this member</summary>
        [SaveableProperty(17)]
        public string LastDutyId { get; set; }

        /// <summary>Cover requests made to player (count)</summary>
        [SaveableProperty(18)]
        public int CoverRequestsMade { get; set; }

        /// <summary>Cover requests accepted by player (count)</summary>
        [SaveableProperty(19)]
        public int CoverRequestsAccepted { get; set; }

        /// <summary>Campaign time of last cover request (to prevent spam)</summary>
        [SaveableProperty(20)]
        public CampaignTime LastCoverRequestTime { get; set; }

        /// <summary>Random seed for this member (for consistent personality traits)</summary>
        [SaveableProperty(21)]
        public int PersonalitySeed { get; set; }

        /// <summary>Cause of death if dead</summary>
        [SaveableProperty(22)]
        public string DeathCause { get; set; }

        /// <summary>Medical risk level (0-5 scale from escalation system)</summary>
        [SaveableProperty(23)]
        public int MedicalRisk { get; set; }

        public LanceMemberState()
        {
            MemberId = Guid.NewGuid().ToString();
            HealthState = MemberHealthState.Healthy;
            ActivityState = MemberActivityState.OnDuty;
            RankTier = 1;
            RelationWithPlayer = 0;
            FormationType = "infantry";
        }

        /// <summary>
        /// Create a new lance member with specified properties.
        /// </summary>
        public static LanceMemberState Create(string name, int rankTier, string formationType, bool isLeader = false)
        {
            return new LanceMemberState
            {
                MemberId = Guid.NewGuid().ToString(),
                Name = name,
                RankTier = rankTier,
                FormationType = formationType,
                IsLanceLeader = isLeader,
                JoinDate = CampaignTime.Now,
                HealthState = MemberHealthState.Healthy,
                ActivityState = MemberActivityState.OnDuty,
                PersonalitySeed = new Random().Next(int.MaxValue)
            };
        }

        /// <summary>
        /// Get the relationship category based on relation score.
        /// </summary>
        public MemberRelationship GetRelationshipLevel()
        {
            if (RelationWithPlayer >= 30) return MemberRelationship.Loyal;
            if (RelationWithPlayer >= 10) return MemberRelationship.Friendly;
            if (RelationWithPlayer >= -10) return MemberRelationship.Neutral;
            if (RelationWithPlayer >= -30) return MemberRelationship.Cold;
            return MemberRelationship.Hostile;
        }

        /// <summary>
        /// Check if this member is available for duty assignment.
        /// </summary>
        public bool IsAvailableForDuty()
        {
            if (HealthState >= MemberHealthState.MajorInjury)
                return false;
            
            if (ActivityState == MemberActivityState.SickBay ||
                ActivityState == MemberActivityState.OnLeave ||
                ActivityState == MemberActivityState.Detached)
                return false;
            
            return true;
        }

        /// <summary>
        /// Check if this member is in need of recovery.
        /// </summary>
        public bool NeedsRecovery()
        {
            return HealthState == MemberHealthState.MinorInjury ||
                   HealthState == MemberHealthState.MajorInjury ||
                   HealthState == MemberHealthState.Incapacitated;
        }

        /// <summary>
        /// Apply injury to this member.
        /// </summary>
        public void ApplyInjury(MemberHealthState injuryLevel, int recoveryDays)
        {
            HealthState = injuryLevel;
            RecoveryTime = CampaignTime.Now + CampaignTime.Days(recoveryDays);
            
            if (injuryLevel >= MemberHealthState.MajorInjury)
            {
                ActivityState = MemberActivityState.SickBay;
            }
        }

        /// <summary>
        /// Process recovery (called daily).
        /// </summary>
        public void ProcessRecovery()
        {
            if (HealthState == MemberHealthState.Dead)
                return;

            if (!NeedsRecovery())
                return;

            // Check if recovery time has passed
            if (CampaignTime.Now >= RecoveryTime)
            {
                // Recover based on current state
                switch (HealthState)
                {
                    case MemberHealthState.MinorInjury:
                        HealthState = MemberHealthState.Healthy;
                        ActivityState = MemberActivityState.OnDuty;
                        break;
                    case MemberHealthState.MajorInjury:
                        HealthState = MemberHealthState.MinorInjury;
                        RecoveryTime = CampaignTime.Now + CampaignTime.Days(3);
                        break;
                    case MemberHealthState.Incapacitated:
                        HealthState = MemberHealthState.MajorInjury;
                        RecoveryTime = CampaignTime.Now + CampaignTime.Days(7);
                        break;
                }
            }

            // Check if leave has ended
            if (ActivityState == MemberActivityState.OnLeave && CampaignTime.Now >= LeaveEndTime)
            {
                ActivityState = MemberActivityState.OnDuty;
            }
        }

        /// <summary>
        /// Mark this member as dead.
        /// </summary>
        public void MarkDead(string cause)
        {
            HealthState = MemberHealthState.Dead;
            ActivityState = MemberActivityState.Detached;
            DeathCause = cause;
        }

        /// <summary>
        /// Modify relationship with player.
        /// </summary>
        public void ModifyRelation(int amount)
        {
            RelationWithPlayer = Math.Max(-100, Math.Min(100, RelationWithPlayer + amount));
        }

        /// <summary>
        /// Grant leave to this member.
        /// </summary>
        public void GrantLeave(int days)
        {
            ActivityState = MemberActivityState.OnLeave;
            LeaveEndTime = CampaignTime.Now + CampaignTime.Days(days);
        }

        /// <summary>
        /// Increment days in service.
        /// </summary>
        public void IncrementService()
        {
            DaysInService++;
        }

        /// <summary>
        /// Record a battle participation.
        /// </summary>
        public void RecordBattle()
        {
            BattlesParticipated++;
        }
    }

    /// <summary>
    /// Save container definitions for LanceMemberState.
    /// </summary>
    public class LanceMemberStateSaveDefiner : SaveableTypeDefiner
    {
        public LanceMemberStateSaveDefiner() : base(735310) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(LanceMemberState), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<LanceMemberState>));
            ConstructContainerDefinition(typeof(Dictionary<string, LanceMemberState>));
        }
    }
}

