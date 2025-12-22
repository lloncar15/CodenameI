using System;
using System.Collections.Generic;
using GimGim.Persistence;

namespace GimGim.Dogs {
    /// <summary>
    /// Serializable data container for dog ownership data
    /// </summary>
    [Serializable]
    public class DogsData {
        /// <summary>
        /// Counter for generating unique dog instance IDs
        /// </summary>
        public int nextInstanceId = 1;
        
        /// <summary>
        /// Instance ID of the currently active dog, -1 if none
        /// </summary>
        public int activeDogId = -1;
        
        /// <summary>
        /// All dogs owned by the player (including active dog)
        /// </summary>
        public List<Dog> kennel = new();
    }
    
    /// <summary>
    /// Persistent data class for dog ownership.
    /// Handles serialization and provides typed access to dog data.
    /// </summary>
    [Serializable]
    public class DogPersistentData : PersistentDataBase<DogsData> {
        #region Properties

        /// <summary>
        /// The next available instance ID for new dogs
        /// </summary>
        public int NextInstanceId => Data.nextInstanceId;

        /// <summary>
        /// Instance ID of the currently active dog, -1 if none
        /// </summary>
        public int ActiveDogId {
            get => Data.activeDogId;
            set {
                Data.activeDogId = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// All dogs owned by the player
        /// </summary>
        public List<Dog> Kennel => Data.kennel;

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates and returns a new unique instance ID
        /// </summary>
        /// <returns>A unique instance ID</returns>
        public int GenerateInstanceId() {
            int id = Data.nextInstanceId;
            Data.nextInstanceId++;
            MarkDirty();
            return id;
        }

        /// <summary>
        /// Adds a dog to the kennel
        /// </summary>
        /// <param name="dog">The dog to add</param>
        public void AddDog(Dog dog) {
            Data.kennel.Add(dog);
            MarkDirty();
        }

        /// <summary>
        /// Removes a dog from the kennel by instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID of the dog to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveDog(int instanceId) {
            int index = Data.kennel.FindIndex(d => d.instanceId == instanceId);
            
            if (index < 0) 
                return false;
            
            Data.kennel.RemoveAt(index);
            MarkDirty();
            return true;
        }

        /// <summary>
        /// Finds a dog by instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID to search for</param>
        /// <returns>The dog, or null if not found</returns>
        public Dog GetDogById(int instanceId) {
            return Data.kennel.Find(d => d.instanceId == instanceId);
        }

        #endregion
    }
}