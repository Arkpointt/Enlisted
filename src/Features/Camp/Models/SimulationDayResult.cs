using System.Collections.Generic;

namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// Encapsulates the results of a single daily simulation tick.
    /// Used to pass data to the news system and orchestrator.
    /// </summary>
    public class SimulationDayResult
    {
        // Roster changes (sick, recovered, died, wounded, deserted)
        public List<RosterChange> RosterChanges { get; } = new List<RosterChange>();

        // Deaths specifically (always shown in news)
        public List<RosterChange> Deaths { get; } = new List<RosterChange>();

        // Random incidents that occurred
        public List<CampIncident> Incidents { get; } = new List<CampIncident>();

        // Threshold crossings (morale dropped to 'critical', etc.)
        public List<PulseEvent> PulseEvents { get; } = new List<PulseEvent>();

        // Crisis events to queue with the orchestrator
        public List<string> TriggeredCrises { get; } = new List<string>();

        // Net changes to company needs (for diagnostics)
        public Dictionary<string, int> NeedChanges { get; } = new Dictionary<string, int>();

        // Count of news items generated
        public int TotalNewsItems => RosterChanges.Count + Deaths.Count + Incidents.Count + PulseEvents.Count;
    }

    /// <summary>
    /// Represents a change in the company roster.
    /// </summary>
    public class RosterChange
    {
        public string ChangeType { get; set; }  // "sick", "recovered", "died", "wounded", "deserted", "missing"
        public string NewsText { get; set; }
        public int Count { get; set; }
        public string Severity { get; set; }

        public static RosterChange Sick(int count, string text) =>
            new RosterChange { ChangeType = "sick", Count = count, NewsText = text, Severity = "minor" };

        public static RosterChange Recovered(int count, string text) =>
            new RosterChange { ChangeType = "recovered", Count = count, NewsText = text, Severity = "flavor" };

        public static RosterChange Death(string text) =>
            new RosterChange { ChangeType = "died", Count = 1, NewsText = text, Severity = "critical" };

        public static RosterChange Wounded(int count, string text) =>
            new RosterChange { ChangeType = "wounded", Count = count, NewsText = text, Severity = "minor" };

        public static RosterChange Deserted(int count, string text) =>
            new RosterChange { ChangeType = "deserted", Count = count, NewsText = text, Severity = "notable" };

        public static RosterChange Missing(string text) =>
            new RosterChange { ChangeType = "missing", Count = 1, NewsText = text, Severity = "minor" };
    }

    /// <summary>
    /// Represents a company need threshold crossing.
    /// </summary>
    public class PulseEvent
    {
        public string Need { get; set; }       // "Morale", "Supplies", "Rest", etc.
        public string Direction { get; set; }  // "up" or "down"
        public string NewLevel { get; set; }   // "Critical", "Low", "Fair", "Good"
        public string NewsText { get; set; }
        public string Severity { get; set; }   // "notable", "critical"
    }
}
