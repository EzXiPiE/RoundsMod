using System;
using HarmonyLib;
using RoundsMod.Components;
using UnityEngine;

namespace RoundsMod.Patches
{
    [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.TakeDamage), new Type[]
    {
        typeof(Vector2), typeof(Vector2), typeof(Color), typeof(GameObject), typeof(Player), typeof(bool), typeof(bool)
    })]
    public static class HealthHandlerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(HealthHandler __instance)
        {
            if (__instance == null) return true;
            var shield = __instance.GetComponent<SatelliteShieldMono>();
            return shield == null || !shield.TryInterceptDamage();
        }
    }
}
