using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GimGim.Dogs {
    /// <summary>
    /// Delegate for dog creation strategies.
    /// </summary>
    /// <param name="profiles">Available dog profiles to choose from</param>
    /// <param name="existingProfileIds">Profile IDs of dogs already owned (for duplicate checking)</param>
    /// <returns>The selected DogProfile, or null if none available</returns>
    public delegate DogProfile DogCreationStrategy(List<DogProfile> profiles, HashSet<int> existingProfileIds);
    
    /// <summary>
    /// Static factory for creating Dog instances using various strategies.
    /// Handles ID generation and timestamp assignment.
    /// </summary>
    public static class DogFactory {
        #region Creation Methods

        /// <summary>
        /// Creates a new dog using the specified strategy
        /// </summary>
        /// <param name="strategy">The strategy to use for selecting a profile</param>
        /// <param name="profiles">Available dog profiles</param>
        /// <param name="existingProfileIds">Profile IDs already owned (for duplicate checking)</param>
        /// <param name="persistence">Persistence data for ID generation</param>
        /// <param name="defaultName">Optional name, uses profile's dogName if null</param>
        /// <returns>A new Dog instance, or null if strategy returned no profile</returns>
        public static Dog CreateDog(
            DogCreationStrategy strategy,
            List<DogProfile> profiles,
            HashSet<int> existingProfileIds,
            DogPersistentData persistence,
            string defaultName = null
        ) {
            if (profiles == null || profiles.Count == 0) {
                Debug.LogWarning("[DogFactory] No profiles provided");
                return null;
            }

            DogProfile selectedProfile = strategy(profiles, existingProfileIds);
            
            if (selectedProfile == null) {
                Debug.LogWarning("[DogFactory] Strategy returned no profile");
                return null;
            }

            return CreateDogFromProfile(selectedProfile, persistence, defaultName);
        }

        /// <summary>
        /// Creates a dog directly from a specific profile
        /// </summary>
        /// <param name="profile">The profile to use</param>
        /// <param name="persistence">Persistence data for ID generation</param>
        /// <param name="name">Optional name, uses profile's dogName if null</param>
        /// <returns>A new Dog instance</returns>
        public static Dog CreateDogFromProfile(DogProfile profile, DogPersistentData persistence, string name = null) {
            Dog dog = new Dog {
                instanceId = persistence.GenerateInstanceId(),
                profileId = profile.profileId,
                name = name ?? profile.dogName,
                creationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                stepsWalked = 0
            };

            Debug.Log($"[DogFactory] Created dog: {dog.name} (ID: {dog.instanceId}, Profile: {profile.dogName})");
            return dog;
        }

        #endregion

        #region Built-in Strategies

        /// <summary>
        /// Creates a strategy that selects a random profile from the list
        /// </summary>
        /// <param name="allowDuplicates">If false, excludes profiles already owned</param>
        /// <returns>A random selection strategy</returns>
        public static DogCreationStrategy RandomStrategy(bool allowDuplicates = true) {
            return (profiles, existingProfileIds) => {
                List<DogProfile> availableProfiles = allowDuplicates 
                    ? profiles 
                    : profiles.Where(p => !existingProfileIds.Contains(p.profileId)).ToList();

                if (availableProfiles.Count == 0) {
                    Debug.LogWarning("[DogFactory] No available profiles for random selection");
                    return null;
                }

                int index = Random.Range(0, availableProfiles.Count);
                return availableProfiles[index];
            };
        }

        /// <summary>
        /// Creates a strategy that selects a profile using weighted random based on rarity values
        /// </summary>
        /// <param name="allowDuplicates">If false, excludes profiles already owned</param>
        /// <returns>A weighted random selection strategy</returns>
        public static DogCreationStrategy WeightedRandomStrategy(bool allowDuplicates = true) {
            return (profiles, existingProfileIds) => {
                List<DogProfile> availableProfiles = allowDuplicates 
                    ? profiles 
                    : profiles.Where(p => !existingProfileIds.Contains(p.profileId)).ToList();

                if (availableProfiles.Count == 0) {
                    Debug.LogWarning("[DogFactory] No available profiles for weighted selection");
                    return null;
                }

                int totalWeight = availableProfiles.Sum(p => Mathf.Max(1, p.rarity));
                int randomValue = Random.Range(0, totalWeight);
                int cumulativeWeight = 0;

                foreach (DogProfile profile in availableProfiles) {
                    cumulativeWeight += Mathf.Max(1, profile.rarity);
                    
                    if (randomValue < cumulativeWeight) {
                        return profile;
                    }
                }

                return availableProfiles[^1];
            };
        }

        /// <summary>
        /// Creates a strategy that selects a specific profile by ID
        /// </summary>
        /// <param name="profileId">The profile ID to select</param>
        /// <returns>A specific profile selection strategy</returns>
        public static DogCreationStrategy ByProfileIdStrategy(int profileId) {
            return (profiles, existingProfileIds) => {
                return profiles.FirstOrDefault(p => p.profileId == profileId);
            };
        }

        /// <summary>
        /// Creates a strategy that selects a specific profile by name
        /// </summary>
        /// <param name="dogName">The dog name to match</param>
        /// <returns>A specific profile selection strategy</returns>
        public static DogCreationStrategy ByNameStrategy(string dogName) {
            return (profiles, existingProfileIds) => {
                return profiles.FirstOrDefault(p => 
                    string.Equals(p.dogName, dogName, StringComparison.OrdinalIgnoreCase));
            };
        }

        #endregion
    }
}