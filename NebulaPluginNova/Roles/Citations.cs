using Virial.Assignable;

namespace Nebula.Roles;

public static class Citations
{
    static public Citation AmongUs { get; private set; } = new("amongUs", SpriteLoader.FromResource("Nebula.Resources.Citations.AmongUs.png", 100f), new ColorTextComponent(new(1f,1f,1f), new RawTextComponent("Among Us")), "https://www.innersloth.com/games/among-us/");
    static public Citation TheOtherRoles { get; private set; } = new("theOtherRoles", SpriteLoader.FromResource("Nebula.Resources.Citations.TheOtherRoles.png", 100f), new ColorTextComponent(new(0xFF / 255f, 0x35 / 255f, 0x1F / 255f), new RawTextComponent("The Other Roles")), "https://github.com/TheOtherRolesAU/TheOtherRoles");
    static public Citation TheOtherRolesGM { get; private set; } = new("theOtherRolesGM", null, new ColorTextComponent(new(0xFF / 255f, 0x35 / 255f, 0x1F / 255f), new RawTextComponent("The Other Roles: GM Edition")), "https://github.com/yukinogatari/TheOtherRoles-GM");
    static public Citation TheOtherRolesGMH { get; private set; } = new("theOtherRolesGMH", null, new ColorTextComponent(new(0xFF / 255f, 0x35 / 255f, 0x1F / 255f), new RawTextComponent("The Other Roles: GM-Haoming Edition")), null);
    static public Citation TownOfImpostors { get; private set; } = new("townOfImpostors", null, new ColorTextComponent(new(255f / 255f, 165f / 255f, 0f / 255f), new RawTextComponent("Town Of Impostors")), "https://github.com/Town-of-Impostors/TownOfImpostors");
    static public Citation SuperNewRoles { get; private set; } = new("superNewRoles", SpriteLoader.FromResource("Nebula.Resources.Citations.SuperNewRoles.png", 100f), new RawTextComponent(""), "https://github.com/SuperNewRoles/SuperNewRoles");
    static public Citation NebulaOnTheShip { get; private set; } = new("nebulaOnTheShip", SpriteLoader.FromResource("Nebula.Resources.Citations.NebulaOnTheShip.png", 100f), new RawTextComponent(""), "https://github.com/Dolly1016/Nebula");
    static public Citation NebulaOnTheShip_Old { get; private set; } = new("nebulaOnTheShip", SpriteLoader.FromResource("Nebula.Resources.Citations.NebulaOnTheShip_Old.png", 100f), new RawTextComponent(""), "https://github.com/Dolly1016/Nebula-OLD-");
    static public Citation Nebula_Remake_LongTimeSupport { get; private set; } = new("nebulaRemakeLTS", null, new RawTextComponent("Nebula-R-LTS"), "https://github.com/ZsFabTest/Nebula-R-LTS");
    static public Citation NebulaOnTheShip_Remake { get; private set; } = new("nebulaOnTheShipRemake", SpriteLoader.FromResource("Nebula.Resources.Citations.NebulaOnTheShip_Remake.png", 100f), new RawTextComponent(""), "https://github.com/ZsFabTest/Nebula-R");
    static public Citation TownOfHostY { get; private set; } = new("townOfHostY", null, new ColorTextComponent(new(1f, 1f, 13f / 255f), new RawTextComponent("TownOfHost_Y")), "https://github.com/Yumenopai/TownOfHost_Y");
    static public Citation TownOfHost { get; private set; } = new("townOfHost", null, new ColorTextComponent(new(0f, 254f / 255f, 254f / 255f), new RawTextComponent("TownOfHost")), "https://github.com/tukasa0001/TownOfHost");
    static public Citation Project_Lotus { get; private set; } = new("projectLotus", null, new ColorTextComponent(new(198f / 255f, 128f / 255f, 66f / 255f), new RawTextComponent("Project Lotus")), "https://github.com/ImaMapleTree/Lotus");
    static public Citation ExtremeRoles { get; private set; } = new("extremeRoles", null, new ColorTextComponent(Color.white, new RawTextComponent("Extreme Roles")), "https://github.com/yukieiji/ExtremeRoles");
}
