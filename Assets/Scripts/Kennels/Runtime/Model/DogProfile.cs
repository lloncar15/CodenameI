using System;

namespace GimGim.Kennels {
    [Serializable]
    public class DogProfile {
        public int profileId;
        public string name;
        public string description;
        public string heightRange;
        public string weightRange;
    }
}
