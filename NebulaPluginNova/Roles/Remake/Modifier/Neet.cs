using Virial;
using Virial.Assignable;
using Nebula.Compat;

namespace Nebula.Roles.Modifier;

public class Neet : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private Neet() : base("neet", "nt", new(125, 125, 125), allocateToNeutral: false, allocateToImpostor: false) { }
    Citation? HasCitation.Citaion => RemakeInit.Citations.ExtremeRoles;
    static public Neet MyRole = new Neet();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        void RuntimeAssignable.OnActivated() { }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += "≠".Color(MyRole.RoleColor.ToUnityColor());
        }
        bool RuntimeModifier.InvalidateCrewmateTask => true;
        bool RuntimeModifier.MyCrewmateTaskIsIgnored => true;

    }
}

