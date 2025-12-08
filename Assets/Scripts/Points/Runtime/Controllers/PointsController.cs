using UnityEngine;

namespace GimGim.Points {
    public class PointsController : MonoBehaviour {
        #region Singleton

        private static PointsController _instance;

        public static PointsController Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<PointsController>();

                    if (_instance == null) {
                        GameObject go = new GameObject("PointsController");
                        _instance = go.AddComponent<PointsController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion
    }
}