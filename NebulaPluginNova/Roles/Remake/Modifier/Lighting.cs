using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Nebula.Compat;

namespace Nebula.Roles.Modifier;

public class lighting : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private lighting() : base("lighting", "lig", new(233, 225, 194), [LightAreaOption]) { }
    Citation? HasCitation.Citaion => RemakeInit.Citations.TownofHostY;

    static private FloatConfiguration LightAreaOption = NebulaAPI.Configurations.Configuration("options.role.lighting.lightarea", (1f, 3f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio);
    static public lighting MyRole = new lighting();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        void RuntimeAssignable.OnActivated() { }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " ‸".Color(MyRole.RoleColor.ToUnityColor());
        }
        [Local]
        public void EditLightRange(LightRangeUpdateEvent ev)
        {
            ev.LightRange *= LightAreaOption;
        }
    }
}

