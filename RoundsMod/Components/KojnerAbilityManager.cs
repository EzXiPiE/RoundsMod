using ModdingUtils.Extensions;
using Photon.Pun;
using RoundsMod.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace RoundsMod.Components
{
    public class KojnerAbilityManager : MonoBehaviour
    {
        private class AbilitySlot
        {
            public KeyCode Key;
            public string KeyLabel;
            public IKojnerAbility Ability;
        }

        private readonly List<AbilitySlot> slots = new List<AbilitySlot>
        {
            new AbilitySlot { Key = KeyCode.Q, KeyLabel = "Q" },
            new AbilitySlot { Key = KeyCode.E, KeyLabel = "E" },
            new AbilitySlot { Key = KeyCode.F, KeyLabel = "F" },
            new AbilitySlot { Key = KeyCode.V, KeyLabel = "V" }
        };

        private PhotonView view;
        private CharacterData data;
        private GUIStyle hudStyle;

        public bool HasFreeSlot => slots.Exists(s => s.Ability == null);

        private void Awake()
        {
            view = GetComponent<PhotonView>();
            data = GetComponent<CharacterData>();
        }

        public bool TryBindAbility(IKojnerAbility ability)
        {
            if (ability == null) return false;
            if (slots.Exists(s => s.Ability == ability)) return true;

            var free = slots.Find(s => s.Ability == null);
            if (free == null) return false;

            free.Ability = ability;
            if (!HasFreeSlot && data != null && data.stats != null)
            {
                var blacklisted = data.stats.GetAdditionalData().blacklistedCategories;
                if (!blacklisted.Contains(Cards.KojnerActiveCard.ActiveCategory))
                    blacklisted.Add(Cards.KojnerActiveCard.ActiveCategory);
            }

            return true;
        }

        private void Update()
        {
            if (data == null || data.dead) return;
            if (view != null && !view.IsMine) return;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Ability != null && Input.GetKeyDown(slots[i].Key))
                    slots[i].Ability.TryActivateAbility();
            }
        }

        private void OnGUI()
        {
            if (data == null || data.dead) return;
            if (view != null && !view.IsMine) return;

            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.box) { fontSize = 14 };
                hudStyle.normal.textColor = Color.white;
            }

            float yOffset = 20f;
            for (int i = 0; i < slots.Count; i++)
            {
                var ab = slots[i].Ability;
                if (ab == null) continue;

                float cd = ab.CooldownRemaining;
                string label = cd > 0.05f
                    ? $"[{slots[i].KeyLabel}] {ab.AbilityName} ({cd:F1}s)"
                    : $"[{slots[i].KeyLabel}] {ab.AbilityName}";

                GUI.color = cd > 0.05f ? new Color(1f, 0.5f, 0.5f, 0.85f) : new Color(0.5f, 1f, 0.6f, 0.95f);
                GUI.Box(new Rect(20f, yOffset, 235f, 34f), label, hudStyle);
                yOffset += 38f;
            }
            GUI.color = Color.white;
        }
    }
}
