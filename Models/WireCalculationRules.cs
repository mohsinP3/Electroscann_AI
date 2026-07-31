namespace Electroscann_ai.Models
{
    public static class WireCalculationRules
    {
        public const double LightWatts = 60;
        public const double FanWatts = 80;
        public const double SocketWatts = 150;
        public const double AcUnitWatts = 2000;
        public const double HeavyLoadWatts = 3000;
        public const double StandardVoltage = 220.0;

        public static (string WireSize, string BreakerRating, string SafetyMessage) EvaluateLoad(double load)
        {
            if (load <= 1500)
                return ("14 AWG", "15 A", "Standard residential wiring sufficient. Ensure proper grounding.");
            if (load <= 2500)
                return ("12 AWG", "20 A", "12 AWG copper recommended. Suitable for general purpose circuits.");
            if (load <= 3800)
                return ("10 AWG", "30 A", "Higher load detected. 10 AWG wire with 30A breaker recommended.");
            if (load <= 5500)
                return ("8 AWG", "40 A", "Heavy load. Consider dedicated circuits for AC and appliances.");
            return ("6 AWG", "50 A", "High power demand. Consult a licensed electrician.");
        }
    }
}
