using System;

namespace Enlisted.Features.Retinue.Data
{
    /// <summary>
    /// Represents a soldier who has distinguished themselves through survival and combat prowess.
    /// Named veterans emerge from anonymous retinue soldiers after surviving multiple battles.
    /// Their deaths trigger memorial events, creating emotional attachment to individual soldiers.
    /// </summary>
    [Serializable]
    public class NamedVeteran
    {
        /// <summary>
        /// Unique identifier for this veteran. Generated at creation time using timestamp + random suffix.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The veteran's name, pulled from their faction's culture name list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Number of battles this soldier has survived since joining the retinue.
        /// Veterans emerge after surviving 3+ battles.
        /// </summary>
        public int BattlesSurvived { get; set; }

        /// <summary>
        /// Total confirmed kills across all battles. Tracked for flavor text and memorial events.
        /// </summary>
        public int Kills { get; set; }

        /// <summary>
        /// Whether the veteran is currently wounded and recovering.
        /// Wounded veterans cannot participate in the next battle.
        /// </summary>
        public bool IsWounded { get; set; }

        /// <summary>
        /// A distinctive character trait that defines this veteran's personality.
        /// Possible values: "Brave", "Lucky", "Sharp-Eyed", "Steady", "Iron Will"
        /// </summary>
        public string Trait { get; set; }

        /// <summary>
        /// Campaign time when this veteran emerged from the ranks.
        /// </summary>
        public float EmergenceTimeInDays { get; set; }

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public NamedVeteran()
        {
            Id = string.Empty;
            Name = string.Empty;
            BattlesSurvived = 0;
            Kills = 0;
            IsWounded = false;
            Trait = string.Empty;
            EmergenceTimeInDays = 0f;
        }

        /// <summary>
        /// Creates a new named veteran with the given name and trait.
        /// </summary>
        /// <param name="name">The veteran's name from their culture's name list</param>
        /// <param name="trait">The veteran's distinguishing character trait</param>
        /// <param name="currentTimeInDays">Current campaign time for recording emergence</param>
        public NamedVeteran(string name, string trait, float currentTimeInDays)
        {
            // Generate unique ID using timestamp and random suffix to avoid collisions
            Id = $"vet_{DateTime.UtcNow.Ticks}_{new Random().Next(1000, 9999)}";
            Name = name ?? "Unknown Soldier";
            Trait = trait ?? "Steady";
            BattlesSurvived = 3; // They emerged after 3 battles, so start at 3
            Kills = 0;
            IsWounded = false;
            EmergenceTimeInDays = currentTimeInDays;
        }

        /// <summary>
        /// Records that this veteran survived another battle. Increments the battle counter.
        /// </summary>
        public void RecordBattleSurvival()
        {
            BattlesSurvived++;
        }

        /// <summary>
        /// Records kills from a battle. Adds to the running total.
        /// </summary>
        /// <param name="killCount">Number of confirmed kills from this battle</param>
        public void RecordKills(int killCount)
        {
            if (killCount > 0)
            {
                Kills += killCount;
            }
        }

        /// <summary>
        /// Gets a description of this veteran suitable for UI display.
        /// Example: "Bjorn the Brave - 7 battles, 12 kills"
        /// </summary>
        public string GetDescription()
        {
            var traitSuffix = !string.IsNullOrEmpty(Trait) ? $" the {Trait}" : "";
            return $"{Name}{traitSuffix} - {BattlesSurvived} battles, {Kills} kills";
        }

        public override string ToString()
        {
            return $"Veteran[{Name}, Trait={Trait}, Battles={BattlesSurvived}, Kills={Kills}, Wounded={IsWounded}]";
        }
    }
}

