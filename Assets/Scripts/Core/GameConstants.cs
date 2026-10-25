using UnityEngine;

public static class GameConstants
{
    public const int LaneCount = 3;
    public const float LaneWidth = 2.0f;

    public const float TileLength = 20.0f;

    public static int CenterLane => (LaneCount - 1) / 2;

    public static float LaneToX(int laneIndex)
    {
        float center = (LaneCount - 1) / 2f; 
        return (laneIndex - center) * LaneWidth;
    }
}