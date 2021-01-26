using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using SideLoader;
using HarmonyLib;
using UnityEngine;

namespace RogueliteHardcore
{
    public class DeathManager : MonoBehaviour
    {
        public static DeathManager Instance;
        public ItemContainer itemContainer;

        internal void Awake()
        {
            Instance = this;
        }

        [HarmonyPatch(typeof(DefeatScenariosManager), "DefeatHardcoreDeath")]
        public class Character_DefeatScenariosManager
        {
            [HarmonyPrefix]
            public static void Prefix(DefeatScenariosManager __instance, AreaManager.AreaEnum ___m_fatalScene, Character ___m_character)
            {
                //Character player = CharacterManager.Instance.GetFirstLocalCharacter();
                Character player = ___m_character;
                List<int> items = new List<int>();

                foreach(EquipmentSlot slot in player.Inventory.Equipment.EquipmentSlots)
                    if (slot != null && slot.EquippedItem != null)
                        items.Add(slot.EquippedItemID);

                // Simplified Object Creation (PFM)
                RogueliteEqmtData tombstone = new RogueliteEqmtData
                {
                    EqmtIDs = items,
                    Name = player.Name,
                    DeathScene = ___m_fatalScene,
                    Position = player.transform.position
                };
                RogueliteBase.History.TombStones.Add(tombstone);
                RogueliteBase.Save();
            }
        }
    }
}
