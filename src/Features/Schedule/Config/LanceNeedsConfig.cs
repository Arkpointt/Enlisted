using System;
using System.Collections.Generic;
using Enlisted.Features.Schedule.Models;
using Newtonsoft.Json;

namespace Enlisted.Features.Schedule.Config
{
    /// <summary>
    /// Configuration for the Lance Needs system.
    /// Defines degradation rates, recovery rates, and thresholds.
    /// </summary>
    public class LanceNeedsConfig
    {
        /// <summary>Base daily degradation rates per need (stored as string keys for JSON)</summary>
        private Dictionary<string, int> _baseDegradationRatesRaw;

        /// <summary>Base daily degradation rates per need (before modifiers)</summary>
        public Dictionary<LanceNeed, int> BaseDegradationRates { get; private set; }
        
        /// <summary>Degradation multiplier during combat (1.0 = normal, 2.0 = double)</summary>
        public float CombatDegradationMultiplier { get; set; }
        
        /// <summary>Degradation multiplier during siege (1.0 = normal, 1.5 = 50% more)</summary>
        public float SiegeDegradationMultiplier { get; set; }
        
        /// <summary>Degradation multiplier during travel (1.0 = normal, 0.8 = 20% less)</summary>
        public float TravelDegradationMultiplier { get; set; }
        
        /// <summary>Low critical threshold (below this shows warnings, typically 30%)</summary>
        public int CriticalThresholdLow { get; set; }
        
        /// <summary>High critical threshold (below this triggers urgent alerts, typically 20%)</summary>
        public int CriticalThresholdHigh { get; set; }
        
        /// <summary>Whether to enable need degradation (for testing)</summary>
        public bool EnableDegradation { get; set; }
        
        /// <summary>Whether to show debug info for needs</summary>
        public bool DebugMode { get; set; }

        public LanceNeedsConfig()
        {
            BaseDegradationRates = new Dictionary<LanceNeed, int>
            {
                { LanceNeed.Readiness, 2 },
                { LanceNeed.Equipment, 3 },
                { LanceNeed.Morale, 1 },
                { LanceNeed.Rest, 4 },
                { LanceNeed.Supplies, 5 }
            };
            
            CombatDegradationMultiplier = 2.0f;
            SiegeDegradationMultiplier = 1.5f;
            TravelDegradationMultiplier = 0.8f;
            CriticalThresholdLow = 30;
            CriticalThresholdHigh = 20;
            EnableDegradation = true;
            DebugMode = false;
        }

        /// <summary>
        /// Set base degradation rates from string keys (used during JSON loading).
        /// </summary>
        public void SetBaseDegradationRatesFromStrings(Dictionary<string, int> rates)
        {
            _baseDegradationRatesRaw = rates;
            BaseDegradationRates = new Dictionary<LanceNeed, int>();

            foreach (var kvp in rates)
            {
                if (Enum.TryParse<LanceNeed>(kvp.Key, out var need))
                {
                    BaseDegradationRates[need] = kvp.Value;
                }
            }
        }
    }
}

