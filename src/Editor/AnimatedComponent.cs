using System;
using UnityEngine;

namespace Receiver2ModdingKit.Editor {
    public class AnimatedComponent : MonoBehaviour {
        [Serializable]
        public struct Keyframe {
            public float time;
            public float value;
        }

        public string path;
        public string mover_name;
        
        public Keyframe[] X_pos_keyframes;
        public Keyframe[] Y_pos_keyframes;
        public Keyframe[] Z_pos_keyframes;

        public Keyframe[] X_rot_keyframes;
        public Keyframe[] Y_rot_keyframes;
        public Keyframe[] Z_rot_keyframes;
    }
}
