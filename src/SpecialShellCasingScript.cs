using Receiver2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMODUnity;

namespace Receiver2ModdingKit
{
    public class SpecialShellCasingScript : ShellCasingScript
    {
        [EventRef]
        public string sound_shell_casing_impact_hard;

        [EventRef]
        public string sound_bullet_fall_hard;

        [EventRef]
        public string sound_shell_casing_impact_soft;
    }
}
