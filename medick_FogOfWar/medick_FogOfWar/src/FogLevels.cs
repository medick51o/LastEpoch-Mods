namespace medick_FogOfWar
{
    // Enum names and values are frozen — they round-trip through the v1.x
    // cfg file (UserData/medick_The_fogOFwar.cfg) by name.
    public enum MapVisionLevel
    {
        BLIND   = 0,   // minimap hidden, nothing reveals
        HARD    = 1,   // minimap visible, nothing reveals
        LIMITED = 2,   // 69% of the zone default
        NORMAL  = 3,   // game default — mod sits idle
        SCOUT   = 4,   // 3x the zone default
        ORACLE  = 5,   // 600 flat — full zone, no FPS tax
    }

    internal static class FogLevels
    {
        // Observed LE default when no zone capture exists yet.
        public const float FallbackDefaultRadius = 150f;
        public const float OracleRadius          = 600f;

        public static float RadiusFor(MapVisionLevel level, float zoneDefault)
        {
            float def = zoneDefault > 0f ? zoneDefault : FallbackDefaultRadius;
            return level switch
            {
                MapVisionLevel.BLIND   => 0f,
                MapVisionLevel.HARD    => 0f,
                MapVisionLevel.LIMITED => def * 0.69f,
                MapVisionLevel.NORMAL  => def,
                MapVisionLevel.SCOUT   => def * 3f,
                MapVisionLevel.ORACLE  => OracleRadius,
                _                      => def,
            };
        }

        public static bool HidesMinimap(MapVisionLevel level) =>
            level == MapVisionLevel.BLIND;
    }
}
