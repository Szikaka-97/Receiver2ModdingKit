using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit.CustomSounds
{
    public class CustomBulletImpactSounds
    {
        public static string GetShellCasingImpactHardSoundEvent(Transform transform)
        {
            if (!transform.TryGetComponent<SpecialShellCasingScript>(out SpecialShellCasingScript shellCasingScript))
            {
                return "event:/bullets/shell_casing_impact_hard";
            }
            return shellCasingScript.sound_shell_casing_impact_hard;
        }

        public static string GetBulletFallHardSoundEvent(Transform transform)
        {
            if (!transform.TryGetComponent<SpecialShellCasingScript>(out SpecialShellCasingScript shellCasingScript))
            {
                return "event:/bullets/bullet_fall_hard";
            }
            return shellCasingScript.sound_bullet_fall_hard;
        }

        public static string GetShellCasingImpactSoftSoundEvent(Transform transform)
        {
            if (!transform.TryGetComponent<SpecialShellCasingScript>(out SpecialShellCasingScript shellCasingScript))
            {
                return "event:/bullets/shell_casing_impact_soft";
            }
            return shellCasingScript.sound_shell_casing_impact_soft;
        }
    }
}
