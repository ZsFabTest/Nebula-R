using Nebula.Roles.Neutral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Virial.Assignable;

namespace Nebula.Roles;

internal static class RemakeInit
{
    internal static class Citations 
    {
        static public Citation NebulaOnTheShipRLTS = new Citation("NebulaOnTheShipRLTS", null, new RawTextComponent("Nebula-R-LTS"), "https://github.com/ZsFabTest/Nebula-R-LTS");
        static public Citation TownOfUsR = new Citation("TownOfUsR", SpriteLoader.FromResource("Nebula.Resources.Remake.TownOfUsR.png", 100f), new RawTextComponent("Town Of Us-Remake"), "https://github.com/eDonnes124/Town-Of-Us-R");
        public static Citation ExtremeRoles = new Citation("ExtremeRoles", SpriteLoader.FromResource("Nebula.Resources.Remake.ExtremeRoles.png", 100f), new ColorTextComponent(new(0x00 / 255f, 0x00 / 255f, 0x00 / 255f), new RawTextComponent("Extreme Roles")), "https://github.com/yukieiji/ExtremeRoles");
        public static Citation TownofHostY = new Citation("TownofHostY", null, new ColorTextComponent(new(0xFE / 255f, 0xFF / 255f, 0x04F / 255f), new RawTextComponent("Town Of Host Y")), "https://github.com/Yumenopai/TownOfHost_Y");
    }
    internal static class PlayerStatus
    {
        public static TranslatableTag Judged = new("state.judged");
        public static TranslatableTag Misjudged = new("state.misjudged");
        public static TranslatableTag Exploded = new("state.exploded");
    }

    internal static class GameEnd
    {
        static public CustomEndCondition SpectreWin = new(192, "spectre", Neutral.Spectre.MyRole.UnityColor, 64);
        static public CustomEndCondition YandereWin = new(193, "yandere", Neutral.Yandere.MyRole.UnityColor, 64);
    }

    /*
    [HarmonyPatch(typeof(Virial.Game.Player), nameof(Virial.Game.Player.CanKill))]
    public static class JackalKillSchrodingersCatPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, Virial.Game.Player __instance, [HarmonyArgument(0)]Virial.Game.Player player)
        {
            if (__instance.Role.Role.Team == Jackal.MyTeam && 
                player.Role.Role == SchrodingersCat.MyRoles[3])
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
    */
}
