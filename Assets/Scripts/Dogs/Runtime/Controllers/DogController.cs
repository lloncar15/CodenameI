using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GimGim.Dogs {
    /// <summary>
    /// Main controller for dog ownership and management.
    /// Handles active dog, kennel, and coordinates with persistence.
    /// </summary>
    public class DogController : MonoBehaviour {
        #region Singleton

        private static DogController _instance;

        public static DogController Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<DogController>();

                    if (_instance == null) {
                        GameObject go = new GameObject("DogController");
                        _instance = go.AddComponent<DogController>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Private Fields

        private DogPersistentData _persistence;
        private Dog _activeDog;

        #endregion

        #region Properties

        /// <summary>
        /// The currently active dog, or null if none
        /// </summary>
        public Dog ActiveDog => _activeDog;

        /// <summary>
        /// All dogs in the kennel (excluding active dog)
        /// </summary>
        public IReadOnlyList<Dog> Kennel => _persistence?.Kennel
            .Where(d => d.instanceId != _persistence.ActiveDogId)
            .ToList();

        /// <summary>
        /// All owned dogs (including active dog)
        /// </summary>
        public IReadOnlyList<Dog> AllDogs => _persistence?.Kennel;

        /// <summary>
        /// Whether the player has any dogs
        /// </summary>
        public bool HasDogs => _persistence?.Kennel.Count > 0;

        /// <summary>
        /// Whether there is an active dog set
        /// </summary>
        public bool HasActiveDog => _activeDog != null;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the active dog changes
        /// </summary>
        public event Action<Dog, Dog> OnActiveDogChanged;

        /// <summary>
        /// Fired when a dog is added to the kennel
        /// </summary>
        public event Action<Dog> OnDogAddedToKennel;

        /// <summary>
        /// Fired when a dog is removed from the kennel
        /// </summary>
        public event Action<Dog> OnDogRemovedFromKennel;

        #endregion

        #region Unity Lifecycle

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _persistence = new DogPersistentData();
        }

        private void Start() {
            LoadActiveDog();
        }

        private void OnDestroy() {
            if (_instance == this) {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and adds a new dog using the specified strategy
        /// </summary>
        /// <param name="strategy">The creation strategy to use</param>
        /// <param name="profiles">Available profiles (uses DogProfileController if null)</param>
        /// <param name="name">Optional custom name</param>
        /// <returns>The created dog, or null if creation failed</returns>
        public Dog CreateAndAddDog(
            DogCreationStrategy strategy, 
            List<DogProfile> profiles = null, 
            string name = null
        ) {
            profiles ??= DogProfileController.Instance?.GetDogProfiles();
            
            if (profiles == null || profiles.Count == 0) {
                Debug.LogWarning("[DogController] No profiles available for dog creation");
                return null;
            }

            HashSet<int> existingProfileIds = new HashSet<int>(_persistence.Kennel.Select(d => d.profileId));
            
            Dog dog = DogFactory.CreateDog(strategy, profiles, existingProfileIds, _persistence, name);
            
            if (dog == null) 
                return null;

            AddDog(dog);
            return dog;
        }

        /// <summary>
        /// Adds an existing dog to the kennel
        /// </summary>
        /// <param name="dog">The dog to add</param>
        public void AddDog(Dog dog) {
            if (dog == null) {
                Debug.LogWarning("[DogController] Cannot add null dog");
                return;
            }

            _persistence.AddDog(dog);
            Debug.Log($"[DogController] Added dog to kennel: {dog.name} (ID: {dog.instanceId})");
            
            OnDogAddedToKennel?.Invoke(dog);
        }

        /// <summary>
        /// Removes a dog from the kennel by instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID of the dog to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveDog(int instanceId) {
            Dog dog = _persistence.GetDogById(instanceId);
            
            if (dog == null) {
                Debug.LogWarning($"[DogController] Dog not found with ID: {instanceId}");
                return false;
            }

            if (_activeDog?.instanceId == instanceId) {
                SetActiveDog(null);
            }

            bool removed = _persistence.RemoveDog(instanceId);
            
            if (removed) {
                Debug.Log($"[DogController] Removed dog from kennel: {dog.name} (ID: {instanceId})");
                OnDogRemovedFromKennel?.Invoke(dog);
            }

            return removed;
        }

        /// <summary>
        /// Sets the active dog by instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID of the dog to set as active</param>
        /// <returns>True if successful, false if dog not found</returns>
        public bool SetActiveDogById(int instanceId) {
            Dog dog = _persistence.GetDogById(instanceId);
            
            if (dog == null) {
                Debug.LogWarning($"[DogController] Cannot set active dog - not found with ID: {instanceId}");
                return false;
            }

            SetActiveDog(dog);
            return true;
        }

        /// <summary>
        /// Sets the active dog
        /// </summary>
        /// <param name="dog">The dog to set as active, or null to clear</param>
        public void SetActiveDog(Dog dog) {
            Dog previousDog = _activeDog;
            _activeDog = dog;
            _persistence.ActiveDogId = dog?.instanceId ?? -1;

            Debug.Log($"[DogController] Active dog changed: {previousDog?.name ?? "None"} -> {dog?.name ?? "None"}");
            
            OnActiveDogChanged?.Invoke(previousDog, dog);
        }

        /// <summary>
        /// Gets a dog by instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID to search for</param>
        /// <returns>The dog, or null if not found</returns>
        public Dog GetDogById(int instanceId) {
            return _persistence.GetDogById(instanceId);
        }

        /// <summary>
        /// Adds steps to the active dog
        /// </summary>
        /// <param name="steps">Number of steps to add</param>
        public void AddStepsToActiveDog(int steps) {
            if (_activeDog == null) {
                Debug.LogWarning("[DogController] Cannot add steps - no active dog");
                return;
            }

            if (steps <= 0) 
                return;

            _activeDog.stepsWalked += steps;
            _persistence.MarkDirty();
            
            Debug.Log($"[DogController] Added {steps} steps to {_activeDog.name}. Total: {_activeDog.stepsWalked}");
        }

        /// <summary>
        /// Forces save of dog data
        /// </summary>
        public void ForceSave() {
            _persistence.MarkDirty();
            _persistence.Save();
        }

        /// <summary>
        /// Clears all dog data (for testing/reset)
        /// </summary>
        public void ClearAllData() {
            _activeDog = null;
            _persistence.Clear();
            Debug.Log("[DogController] All dog data cleared");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the active dog from persistence on startup
        /// </summary>
        private void LoadActiveDog() {
            if (_persistence.ActiveDogId < 0) {
                _activeDog = null;
                return;
            }

            _activeDog = _persistence.GetDogById(_persistence.ActiveDogId);
            
            if (_activeDog == null) {
                Debug.LogWarning($"[DogController] Saved active dog ID {_persistence.ActiveDogId} not found, clearing");
                _persistence.ActiveDogId = -1;
            }
            else {
                Debug.Log($"[DogController] Loaded active dog: {_activeDog.name}");
            }
        }

        #endregion
    }
}