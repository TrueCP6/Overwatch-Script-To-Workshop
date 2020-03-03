using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby
{
    public class Ruleset
    {
        public HeroesRoot Heroes { get; set; } = new HeroesRoot();

        public static void GenerateSchema()
        {
            // Initialize the root.
            RootSchema root = new RootSchema().InitDefinitions().InitProperties();
            root.Schema = "http://json-schema.org/schema";

            // Get the hero settings.
            RootSchema heroesRoot = new RootSchema("Hero settings.").InitProperties();
            root.Properties.Add("Heroes", heroesRoot);
            root.Definitions.Add("HeroList", GetHeroList());

            heroesRoot.Properties.Add("General", GetHeroListReference("The list of hero settings that affects both teams."));
            heroesRoot.Properties.Add("Team 1", GetHeroListReference("The list of hero settings that affects team 1."));
            heroesRoot.Properties.Add("Team 2", GetHeroListReference("The list of hero settings that affects team 2."));
            
            // Get the result.
            string result = JsonConvert.SerializeObject(root, new JsonSerializerSettings() {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            Console.WriteLine(result);
        }

        private static RootSchema GetHeroListReference(string description)
        {
            RootSchema schema = new RootSchema(description);
            schema.Ref = "#/definitions/HeroList";
            return schema;
        }

        private static RootSchema GetHeroList()
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var heroSettings in LobbySettingCollection.AllHeroSettings) schema.Properties.Add(heroSettings.HeroName, heroSettings.GetSchema());
            return schema;
        }
    }

    public class HeroesRoot
    {
        [JsonProperty("General")]
        public HeroList General { get; set; }

        [JsonProperty("Team 1")]
        public HeroList Team1 { get; set; }

        [JsonProperty("Team 2")]
        public HeroList Team2 { get; set; }
    }

    public class HeroList : Dictionary<String, Dictionary<String, object>> {}

    public class LobbySettingCollection : List<LobbySetting>
    {
        // ***********
        // * GLOBALS *
        // ***********
        private static readonly LobbySetting QuickMelee = new SwitchValue("Quick Melee", true);
        private static readonly LobbySetting SpawnWithUlt = new SwitchValue("Spawn With Ultimate Ready", false);
        private static readonly LobbySetting DamageDealt = new RangeValue("Damage Dealt", 10, 500);
        private static readonly LobbySetting DamageRecieved = new RangeValue("Damage Recieved", 10, 500);
        private static readonly LobbySetting HealingDealt = new RangeValue("Healing Dealt", 10, 500);
        private static readonly LobbySetting HealingRecieved = new RangeValue("Healing Recieved", 10, 500);
        private static readonly LobbySetting JumpVerticalSpeed = new RangeValue("Jump Vertical Speed", 25, 800);
        private static readonly LobbySetting MovementGravity = new RangeValue("Movement Gravity", 25, 400);
        private static readonly LobbySetting MovementSpeed = new RangeValue("Movement Speed", 50, 300);
        private static readonly LobbySetting RecieveHeadshotsOnly = new SwitchValue("Recieve Headshots Only", false);
        private static readonly LobbySetting PrimaryFire = new SwitchValue("Primary Fire", true);
        // * Generic Ammunition Info *
        private static readonly LobbySetting AmmunitionClipSizeScalar = new RangeValue("Ammunition Clip Size Scalar", 25, 500);
        private static readonly LobbySetting NoAmmunitionRequirement = new SwitchValue("No Ammunition Requirement", false);


        // ***********
        // * HEALERS *
        // ***********
        private static readonly LobbySetting Health = new RangeValue("Health", 10, 500);

        // ***************
        // * PROJECTILES *
        // ***************
        private static readonly LobbySetting ProjectileSpeed = new RangeValue("Projectile Speed", 0, 300);
        private static readonly LobbySetting ProjectileGravity = new RangeValue("Projectile Gravity", 0, 500);

        // ****************
        // * ULT DURATION *
        // ****************
        private static readonly LobbySetting UltimateDuration = new RangeValue("Ultimate Duration", 25, 500);
        private static readonly LobbySetting InfiniteDuration = new SwitchValue("Infinite Ultimate Duration", false);

        /// <summary>An array of all heroes + general and their settings.</summary>
        public static readonly LobbySettingCollection[] AllHeroSettings = new LobbySettingCollection[] {
            new LobbySettingCollection("General").AddUlt(null, true).AddProjectile(true).AddHealer(),
            new LobbySettingCollection("Ana").AddUlt("Nano Boost").AddProjectile(false).AddHealer().AddAbility("Biotic Grenade").AddAbility("Sleep Dart"),
            new LobbySettingCollection("Ashe").AddUlt("B.O.B.", true).AddProjectile(true).AddAbility("Coach Gun").AddAbility("Dynamite"),
            new LobbySettingCollection("Baptiste").AddUlt("Amplification Matrix", true).AddProjectile(false).AddHealer().AddAbility("Immortality Field").AddAbility("Regenerative Burst"),
            new LobbySettingCollection("Bastion").AddUlt("Configuration: Tank", true).AddProjectile(false).AddHealer().AddAbility("Reconfigure", hasCooldown: false).AddAbility("Self-Repair", rechargeable: true),
            new LobbySettingCollection("Brigitte").AddUlt("Rally", true).AddHealer().AddAbility("Repair Pack").AddAbility("Shield Bash", hasKnockback: true).AddAbility("Whip Shot", hasKnockback: true).RemoveAmmunition(),
            new LobbySettingCollection("D.va").AddUlt("Self-Destruct", true).AddAbility("Micro Missiles").AddAbility("Boosters", hasKnockback: true).AddAbility("Defense Matrix", rechargeable: true).RemoveAmmunition()
        };


        /// <summary>The name of the hero.</summary>
        public string HeroName { get; }

        public LobbySettingCollection(string heroName)
        {
            HeroName = heroName;
            AddGlobals();
        }
        
        public LobbySettingCollection AddGlobals()
        {
            Add(QuickMelee);
            Add(SpawnWithUlt);
            Add(DamageDealt);
            Add(DamageRecieved);
            Add(HealingRecieved);
            Add(JumpVerticalSpeed);
            Add(MovementGravity);
            Add(MovementSpeed);
            Add(RecieveHeadshotsOnly);
            Add(PrimaryFire);
            Add(AmmunitionClipSizeScalar);
            Add(NoAmmunitionRequirement);
            return this;
        }

        public LobbySettingCollection AddHealer()
        {
            Add(HealingDealt);
            return this;
        }

        public LobbySettingCollection AddProjectile(bool hasGravity)
        {
            Add(ProjectileSpeed);
            if (hasGravity) Add(ProjectileGravity);
            return this;
        }

        public LobbySettingCollection AddUlt(string name, bool hasDuration = false)
        {
            // Get the names of the settings to be added.
            string isEnabled = "Ultimate Ability";
            string generation = "Ultimate Generation";
            string passive = "Ultimate Generation - Passive";
            string combat = "Ultimate Generation - Combat";

            if (name != null)
            {
                // Add the (name) suffix if 'name' is not null.
                isEnabled += " (" + name + ")";
                generation += " (" + name + ")";
                passive += " (" + name + ")";
                combat += " (" + name + ")";
            }

            // Add the settings.
            Add(new SwitchValue(isEnabled, true));
            Add(new RangeValue(generation, 10, 500));
            Add(new RangeValue(passive, 0, 500));
            Add(new RangeValue(combat, 0, 500));

            if (hasDuration)
            {
                // Add duration info if it can be changed.
                Add(UltimateDuration);
                Add(InfiniteDuration);
            }

            return this;
        }

        public LobbySettingCollection AddAbility(string name, bool hasCooldown = true, bool hasKnockback = false, bool rechargeable = false)
        {
            Add(new SwitchValue(name, true));

            // If the ability has a cooldown, add the cooldown options.
            if (hasCooldown && !rechargeable) Add(new RangeValue(name + " Cooldown Time", 0, 500));

            // If the ability has a knockback scalar, add the knockback option.
            if (hasKnockback) Add(new RangeValue(name + " Knockback Scalar", 0, 500));

            // If the ability is rechargeable, add the max time and recharge rate.
            if (rechargeable)
            {
                Add(new RangeValue(name + " Maximum Time", 20, 500));
                Add(new RangeValue(name + " Recharge Rate", 0, 500));
            }
            return this;
        }

        public LobbySettingCollection RemoveAmmunition()
        {
            Remove(AmmunitionClipSizeScalar);
            Remove(NoAmmunitionRequirement);
            return this;
        }

        public new LobbySettingCollection Add(LobbySetting lobbySetting)
        {
            base.Add(lobbySetting);
            return this;
        }

        public RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema($"'{HeroName}' hero settings.").InitProperties();
            foreach (var value in this) schema.Properties.Add(value.Name, value.GetSchema());
            return schema;
        }
    }
}