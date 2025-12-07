using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GimGim.Kennels {
    public class DogProfileController : MonoBehaviour {
        #region Singleton

        private static DogProfileController _instance;
        
        public static DogProfileController Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<DogProfileController>();

                    if (_instance == null) {
                        GameObject go = new GameObject("DogProfileController");
                        _instance = go.AddComponent<DogProfileController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Profiles & Getters
        
        private readonly Dictionary<int, DogProfile> _profiles = new();

        public Dictionary<int, DogProfile> GetDogProfiles() {
            return _profiles;
        }
        
        public DogProfile GetDogProfileById(int profileId) {
            return _profiles[profileId];
        }

        public DogProfile GetDogProfileByName(string dogName) {
            return _profiles.Where(kvp => kvp.Value.name == dogName).Select(kvp => kvp.Value).FirstOrDefault();
        }

        #endregion

        #region UnityLifecycle
        
        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        } 

        #endregion

        #region Initialization

        /// <summary>
        /// Reads the profiles json and populates the profiles' dictionary.
        /// </summary>
        private void Initialize() {
             
        }

        #endregion
    }
}