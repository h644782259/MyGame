namespace Emberfall
{
    public static class ConcentratedVenomRules
    {
        public static float DirectCoefficient(int rank) { return rank <= 1 ? 2.4f : rank == 2 ? 3.6f : 4.8f; }
        public const float Radius = .14f;
        public const float SteeringSeconds = .18f;
        public const float SteeringDegrees = 18f;
        public const float DegreesPerSecond = 70f;
    }
}
