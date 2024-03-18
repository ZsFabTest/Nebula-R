﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;

namespace Nebula.Roles;

public abstract class ModifierInstance : AssignableInstance, RuntimeModifier
{
    public override IAssignableBase AssignableBase => Role;
    public abstract AbstractModifier Role { get; }
    DefinedModifier RuntimeModifier.Modifier => Role;
    Virial.Game.Player RuntimeAssignable.MyPlayer => MyPlayer;
    

    public ModifierInstance(PlayerModInfo player) : base(player)
    {
    }

    /// <summary>
    /// クルーメイトタスクを持っていた場合、目に見えるように無効化する
    /// </summary>
    public virtual bool InvalidateCrewmateTask => false;

    /// <summary>
    /// クルーメイトタスクを持っていたとしても、クルーメイトタスクの総数に計上されない場合はtrue
    /// </summary>
    public virtual bool MyCrewmateTaskIsIgnored => false;

    public virtual string? IntroText => null;

}
