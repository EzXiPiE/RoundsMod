using System.Collections;
using BepInEx;
using HarmonyLib;
using UnboundLib.Cards;
using RoundsMod.Cards;

namespace RoundsMod
{
    [BepInPlugin("com.username.myfirstmod", "Power Of Kojner", "1.0.0")]
    [BepInDependency("com.willis.rounds.unbound")]
    public class MyMod : BaseUnityPlugin
    {
        public static MyMod Instance { get; private set; }
        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            harmony = new Harmony("com.username.myfirstmod.harmony");
            harmony.PatchAll();
        }

        private IEnumerator Start()
        {
            CustomCard.BuildCard<KojnerPowerCard>();
            yield return null;
            CustomCard.BuildCard<BlackHoleCard>();
            yield return null;
            CustomCard.BuildCard<TimeControlCard>();
            yield return null;
            CustomCard.BuildCard<SatelliteCard>();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
