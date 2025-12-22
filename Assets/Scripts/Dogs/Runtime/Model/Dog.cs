using System;
using UnityEngine;

namespace GimGim.Dogs {
    /// <summary>
    /// Represents an individual dog instance owned by the player.
    /// References a DogProfile for breed data and tracks instance-specific stats.
    /// </summary>
    [Serializable]
    public class Dog {
        /// <summary>
        /// Unique identifier for this dog instance
        /// </summary>
        public int instanceId;
        
        /// <summary>
        /// Reference to the breed profile via ID. Use GetProfile() to retrieve full data.
        /// </summary>
        public int profileId;
        
        /// <summary>
        /// Player-assigned nickname for this dog
        /// </summary>
        public string name;
        
        /// <summary>
        /// Timestamp when this dog was created (Unix milliseconds)
        /// </summary>
        public long creationTimestamp;
        
        /// <summary>
        /// Total steps walked while this dog was active
        /// </summary>
        public int stepsWalked;

        #region Helpers

        
        /// <summary>
        /// Retrieves the DogProfile ScriptableObject for this dog's breed
        /// </summary>
        /// <returns>The DogProfile, or null if not found</returns>
        public DogProfile GetProfile() {
            return DogProfileController.Instance?.GetDogProfileById(profileId);
        }

        /// <summary>
        /// Calculates the number of days since this dog was created
        /// </summary>
        /// <returns>Number of days owned, minimum 1</returns>
        public int GetDaysOwned() {
            DateTime creationDate = DateTimeOffset.FromUnixTimeMilliseconds(creationTimestamp).LocalDateTime;
            int days = (DateTime.Now - creationDate).Days;
            return Math.Max(1, days);
        }

        #endregion
    }
}