using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MountainShrineConfig
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class MountainShrineConfigPlugin : BaseUnityPlugin
    {
        public const string
            MODNAME = "MountainShrineConfig",
            AUTHOR = "owl777",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.0";

        static ConfigEntry<int> minBossShrineCount;
        static ConfigEntry<int> maxBossShrineCount;
        static SceneInfo currentScene;
        static int bossShrineCardSelectedCount;
        static bool addedBossCardToOriginalDeck;

        static readonly string[] validStages = { "golemplains", "blackbeach", "goolake", "foggyswamp", "frozenwall", "wispgraveyard", "dampcavesimple", "shipgraveyard", "skymeadow", "rootjungle" };
        static readonly string[] bossCardStages = { "golemplains", "blackbeach", "goolake", "foggyswamp", "dampcavesimple", "shipgraveyard", "skymeadow", "rootjungle" };
        static readonly WeightedSelection<DirectorCard> bossDeck = new WeightedSelection<DirectorCard>(capacity: 1);
        static readonly Dictionary<string, int> bossShrineWeights = new Dictionary<string, int>() 
        {
            { "frozenwall", 2 },
            { "wispgraveyard", 10 },
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake()
        {
            Log.Init(Logger);
            InitializeConfigEntries();
            InitializeBossDeck();
            On.RoR2.SceneDirector.SelectCard += OnSelectCard;
            On.RoR2.ClassicStageInfo.Awake += OnStageChange;
        }

        private void InitializeConfigEntries() 
        {
            minBossShrineCount = Config.Bind<int>(
                section: "Quantity",
                key: "Minimum",
                defaultValue: 0,
                description: "Minimum amount of mountain shrines per stage."
            );
            maxBossShrineCount = Config.Bind<int>(
                section: "Quantity",
                key: "Maximum",
                defaultValue: 3,
                description: "Maximum amount of mountain shrines per stage."
            );
        }

        private void InitializeBossDeck()
        {
            bossDeck.AddChoice(NewBossChoiceInfo());
        }

        private WeightedSelection<DirectorCard>.ChoiceInfo NewBossChoiceInfo(int selectionWeight = 1)
        {
            var bossCard = Resources.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscShrineBoss");
            var bossChoiceInfo = new WeightedSelection<DirectorCard>.ChoiceInfo();
            var bossChoiceDirectorCard = new DirectorCard
            {
                spawnCard = bossCard,
                selectionWeight = selectionWeight,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                allowAmbushSpawn = true,
                preventOverhead = false,
                minimumStageCompletions = 0
            };
            bossChoiceInfo.value = bossChoiceDirectorCard;
            return bossChoiceInfo;
        }
        
        private DirectorCard OnSelectCard(On.RoR2.SceneDirector.orig_SelectCard orig, SceneDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
        {
            if (!IsValidStage() || !CanAffordBossShrine(maxCost) || !ShouldSpawnBossShrine())
            {
                Log.LogInfo("Using original deck.");
                if (ShouldRemoveBossCardFromDeck()) RemoveBossCard(deck);
                else if (ShouldAddBossCardToDeck()) AddBossCard(deck);
                var selectedCard = orig(self, deck, maxCost);
                if (selectedCard.spawnCard.name.ToLower().Equals("iscshrineboss")) bossShrineCardSelectedCount++;
                return selectedCard;
            }
            bossShrineCardSelectedCount++;
            Log.LogInfo($"Using boss deck ({bossShrineCardSelectedCount}/{minBossShrineCount.Value}).");
            return orig(self, bossDeck, maxCost);
        } 

        private void OnStageChange(On.RoR2.ClassicStageInfo.orig_Awake orig, ClassicStageInfo self) 
        {
            bossShrineCardSelectedCount = 0;
            addedBossCardToOriginalDeck = false;
            currentScene = self.GetComponent<SceneInfo>();
            orig(self);
        }

        private void AddBossCard(WeightedSelection<DirectorCard> deck)
        {
            var selectionWeight = bossShrineWeights[currentScene.sceneDef.baseSceneName];
            deck.AddChoice(NewBossChoiceInfo(selectionWeight: selectionWeight));
            addedBossCardToOriginalDeck = true;
            Log.LogInfo($"Added boss card to original deck with selection weight of {selectionWeight}");
        }

        private bool ShouldAddBossCardToDeck() => !ShouldRemoveBossCardFromDeck() && !bossCardStages.Contains(currentScene.sceneDef.baseSceneName) && !addedBossCardToOriginalDeck;

        private void RemoveBossCard(WeightedSelection<DirectorCard> deck)
        {
            for (var i = 0; i < deck.Count; i++)
            {
                if (deck.GetChoice(i).value.spawnCard.name.ToLower().Equals("iscshrineboss"))
                {
                    deck.RemoveChoice(i);
                    Log.LogInfo("Boss card removed from original deck.");
                    break;
                }
            }
        }

        private bool ShouldRemoveBossCardFromDeck() => bossShrineCardSelectedCount == maxBossShrineCount.Value;

        private bool ShouldSpawnBossShrine() => bossShrineCardSelectedCount < minBossShrineCount.Value;

        private bool CanAffordBossShrine(int maxCost) => bossDeck.GetChoice(0).value.cost < maxCost;

        private bool IsValidStage() => validStages.Contains(currentScene.sceneDef.baseSceneName);
    }
}
