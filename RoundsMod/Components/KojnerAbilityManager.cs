using Photon.Pun;
using RoundsMod.Utils;
using UnityEngine;

namespace RoundsMod.Components
{
    public class KojnerAbilityManager : MonoBehaviour
    {
        private IKojnerAbility slotE;
        private PhotonView view;
        private CharacterData data;
        private GUIStyle hudStyle;

        public bool HasSlotE => slotE != null;

        private void Awake()
        {
            view = GetComponent<PhotonView>();
            data = GetComponent<CharacterData>();
        }

        public bool TryBindSlotE(IKojnerAbility ability)
        {
            if (slotE != null) return false;
            slotE = ability;
            return true;
        }

        private void Update()
        {
            if (slotE == null || data == null || data.dead) return;
            if (view != null && !view.IsMine) return;

            if (Input.GetKeyDown(KeyCode.E))
                slotE.TryActivateAbility();
        }

        private void OnGUI()
        {
            if (slotE == null || data == null || data.dead) return;
            if (view != null && !view.IsMine) return;

            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.box) { fontSize = 15 };
                hudStyle.normal.textColor = Color.white;
            }

            float cd = slotE.CooldownRemaining;
            string label = cd > 0.05f ? $"[E] {slotE.AbilityName} ({cd:F1}s)" : $"[E] {slotE.AbilityName}";
            GUI.color = cd > 0.05f ? new Color(1f, 0.5f, 0.5f, 0.85f) : new Color(0.5f, 1f, 0.6f, 0.95f);
            GUI.Box(new Rect(20, Screen.height - 65, 230, 38), label, hudStyle);
            GUI.color = Color.white;
        }
    }
}
