﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace OverwatchParser.Elements
{
    public static class Constants
    {
        public const int RULE_NAME_MAX_LENGTH = 128;

        public static readonly Type[] EnumParameters = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<EnumParameter>() != null).ToArray();

        public static readonly string[] Strings = new string[]
        {
            // All lowercase please :)
            " ",
            "----------",
            "-> {0}",
            "!",
            "!!",
            "!!!",
            "#{0}",
            "({0})",
            "*",
            "...",
            "?",
            "??",
            "???",
            "{0} - {1}",
            "{0} - {1} - {2}",
            "{0} ->",
            "{0} -> {1}",
            "{0} != {1}",
            "{0} * {1}",
            "{0} / {1}",
            "{0} : {1} : {2}",
            "{0} {1}",
            "{0} {1} {2}",
            "{0} + {1}",
            "{0} <-",
            "{0} <- {1}",
            "{0} <->",
            "{0} <-> {1}",
            "{0} < {1}",
            "{0} <= {1}",
            "{0} = {1}",
            "{0} == {1}",
            "{0} > {1}",
            "{0} >= {1}",
            "{0} and {1}",
            "{0} m",
            "{0} m/s",
            "{0} sec",
            "{0} vs {1}",
            "{0}!",
            "{0}!!",
            "{0}!!!",
            "{0}%",
            "{0}, {1}",
            "{0}, {1}, and {2}",
            "{0}:",
            "{0}: {1}",
            "{0}: {1} and {2}",
            "{0}:{1}",
            "{0}?",
            "{0}??",
            "{0}???",
            "<- {0}",
            "<-> {0}",
            "abilities",
            "ability",
            "ability 1",
            "ability 2",
            "alert",
            "alive",
            "allies",
            "ally",
            "attack",
            "attacked",
            "attacking",
            "attempt",
            "attempts",
            "average",
            "avoid",
            "avoided",
            "avoiding",
            "backward",
            "bad",
            "ban",
            "banned",
            "banning",
            "best",
            "better",
            "boss",
            "bosses",
            "bought",
            "build",
            "building",
            "built",
            "burn",
            "burning",
            "burnt",
            "buy",
            "buying",
            "capture",
            "captured",
            "capturing",
            "caution",
            "center",
            "challenge accepted",
            "chase",
            "chased",
            "chasing",
            "checkpoint",
            "checkpoints",
            "cloud",
            "clouds",
            "come here",
            "condition",
            "congratulations",
            "connect",
            "connected",
            "connecting",
            "control point",
            "control points",
            "cooldown",
            "cooldowns",
            "corrupt",
            "corrupted",
            "corrupting",
            "credit",
            "credits",
            "critical",
            "crouch",
            "crouched",
            "crouching",
            "current",
            "current allies",
            "current ally",
            "current attempt",
            "current checkpoint",
            "current enemies",
            "current enemy",
            "current form",
            "current game",
            "current hero",
            "current heroes",
            "current hostage",
            "current hostages",
            "current level",
            "current mission",
            "current object",
            "current objective",
            "current objects",
            "current phase",
            "current player",
            "current players",
            "current round",
            "current target",
            "current targets",
            "current upgrade",
            "damage",
            "damaged",
            "damaging",
            "danger",
            "dead",
            "defeat",
            "defend",
            "defended",
            "defending",
            "deliver",
            "delivered",
            "delivering",
            "destabilize",
            "destabilized",
            "destabilizing",
            "destory",
            "destoryed",
            "destorying",
            "die",
            "disconnect",
            "disconnected",
            "disconnecting",
            "distance",
            "distances",
            "dodge",
            "dodged",
            "dodging",
            "dome",
            "domes",
            "down",
            "download",
            "downloaded",
            "downloading",
            "draw",
            "drop",
            "dropped",
            "dropping",
            "dying",
            "east"
        };
        public const string DEFAULT_STRING = " ";
    }

    public enum RuleEvent
    {
        Ongoing_Global,
        Ongoing_EachPlayer,

        Player_Earned_Elimination,
        Player_Dealt_Final_Blow,

        Player_Dealt_Damage,
        Player_Took_Damage,

        Player_Died
    }

    public enum PlayerSelector
    {
        All,
        Slot0,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot10,
        Slot11,
        // Why isn't it alphabetical? we will never know.
        Reaper,
        Tracer,
        Mercy,
        Hanzo,
        Torbjorn,
        Reinhardt,
        Pharah,
        Winston,
        Widowmaker,
        Bastion,
        Symmetra,
        Zenyatta,
        Gengi,
        Roadhog,
        Mccree,
        Junkrat,
        Zarya,
        Soldier76,
        Lucio,
        Dva,
        Mei,
        Sombra,
        Doomfist,
        Ana,
        Orisa,
        Brigitte,
        Moira,
        WreckingBall,
        Ashe,
        Baptiste
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumParameter : Attribute
    {
        public EnumParameter() {}
    }

    [EnumParameter]
    public enum Variable
    {
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z
    }

    [EnumParameter]
    public enum Operators
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    [EnumParameter]
    public enum Operation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        RaiseToPower,
        Min,
        Max,
        AppendToArray,
        RemoveFromArrayByValue,
        RemoveFromArrayByIndex
    }

    [EnumParameter]
    public enum Button
    {
        PrimaryFire,
        SecondaryFire,
        Ability1,
        Ability2,
        Ultimate,
        Interact,
        Jump,
        Crouch
    }

    [EnumParameter]
    public enum Relative
    {
        ToWorld,
        ToPlayer
    }

    [EnumParameter]
    public enum ContraryMotion
    {
        Cancel,
        Incorporate
    }

    [EnumParameter]
    public enum ChaseReevaluation
    {
        DestinationAndRate,
        None
    }

    [EnumParameter]
    public enum Status
    {
        Hacked,
        Burning,
        KnockedDown,
        Asleep,
        Frozen,
        Unkillable,
        Invincible,
        PhasedOut,
        Rooted,
        Stunned
    }

    [EnumParameter]
    public enum TeamSelector
    {
        All,
        Team1,
        Team2,
    }

    [EnumParameter]
    public enum WaitBehavior
    {
        IgnoreCondition,
        AbortWhenFalse,
        RestartWhenTrue
    }

    [EnumParameter]
    public enum Effect
    {
        Sphere,
        LightShaft,
        Orb,
        Ring,
        Cloud,
        Sparkles,
        GoodAura,
        BadAura,
        EnergySound,
        PickupSound,
        GoodAuraSound,
        BadAuraSound,
        SparklesSound,
        SmokeSound,
        DecalSound,
        BeaconSound
    }

    [EnumParameter]
    public enum Color
    {
        White,
        Yellow,
        Green,
        Purple,
        Red,
        Blue,
        Team1,
        Team2
    }

    [EnumParameter]
    public enum EffectRev
    {
        VisibleToPositionAndRadius,
        PositionAndRadius,
        VisibleTo,
        None
    }
}
