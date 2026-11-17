using HarmonyLib;
using RoundsMod.Components;
using UnityEngine;

namespace RoundsMod.Patches
{
    [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.TakeDamage))]
    public static class HealthHandlerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(HealthHandler __instance, Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool lethal, bool ignoreBlock)
        {
            if (__instance == null) return true;
            var shield = __instance.GetComponent<SatelliteShieldMono>();
            if (shield != null && shield.TryInterceptDamage())
            {
                return false;
            }
            return true;
        }
    }
}
