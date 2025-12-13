using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GimGim.Dogs {
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

        public List<DogProfile> profiles;

        public List<DogProfile> GetDogProfiles() {
            return profiles;
        }

        public DogProfile GetDogProfileById(int profileId) {
            return profiles.FirstOrDefault(profile => profile.profileId == profileId);
        }

        public DogProfile GetDogProfileByName(string dogName) {
            return profiles.FirstOrDefault(profile => profile.name == dogName);
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
        }

        #endregion
    }
}