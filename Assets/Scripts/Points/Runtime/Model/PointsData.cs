using System;

namespace GimGim.Points {
    /// <summary>
    /// Data container for points in the game.
    /// </summary>
    [Serializable]
    public class PointsData {
        public int currentPoints;
        public int allTimePoints;
    }
}