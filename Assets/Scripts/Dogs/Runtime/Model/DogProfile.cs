using System;
using UnityEngine;

namespace GimGim.Dogs {
    [CreateAssetMenu(fileName = "Dog", menuName = "GimGim/Kennel/Dog")]
    public class DogProfile : ScriptableObject {
        [Header("Ids")]
        public int profileId;
        public string dogName;
        
        [Header("Info")]
        public string description;
        public string heightRange;
        public string weightRange;
        
        [Header("Sprites")]
        public Sprite sprite;
        [Tooltip("Offset positions for sprites shown in thumbnails across the UI.")]
        public Vector2 spriteThumbPositions;
    }
}
