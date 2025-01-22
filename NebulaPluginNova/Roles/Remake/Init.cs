using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;

namespace Nebula.Roles;

internal static class RemakeInit
{
    internal static class Citations 
    {
        static public Citation NebulaOnTheShipRLTS = new Citation("NebulaOnTheShipRLTS", null, new RawTextComponent("Nebula-R-LTS"), "https://github.com/ZsFabTest/Nebula-R-LTS");
        static public Citation TownOfUsR = new Citation("TownOfUsR", SpriteLoader.FromResource("Nebula.Resources.Remake.TownOfUsR.png", 100f), new RawTextComponent("Town Of Us-Remake"), "https://github.com/eDonnes124/Town-Of-Us-R");
    }
    internal static class PlayerStatus
    {
        public static TranslatableTag Judged = new("state.judged");
        public static TranslatableTag Misjudged = new("state.misjudged");
        public static TranslatableTag Exploded = new("state.exploded");
    }
}
