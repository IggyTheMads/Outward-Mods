using System;
using BepInEx;
using HarmonyLib;
using SideLoader;
using System.Reflection;
using UnityEngine;

namespace ImmersiveLinks
{
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance;
        Character player;
        public int offHandSpeedID = 010101016;

        // Harmony grabs Outward functions
        internal void Awake()
        {
            Instance = this;
            
            // Have Harmony patch Outward's functionality
            var harmony = new HarmonyLib.Harmony("com.iggy.immersivecustoms");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Debug.Log("Immersive Customs: Stats Manager Loaded");
            SL.OnGameplayResumedAfterLoading += Fixer;

        }

        void Start()
        {
            
        }

        private void Fixer()
        {
            player = CharacterManager.Instance.GetFirstLocalCharacter();
        }

        void Update()
        {
            if(player != null)
            {
                //checks if right hander is 2Handed weapon
                
                if (player.Inventory.SkillKnowledge.IsItemLearned(offHandSpeedID))
                {
                    var is2Handed = player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.Axe_2H) || player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.FistW_2H) || player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.Halberd_2H) || player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.Mace_2H) || player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.Spear_2H) || player.Inventory.HasWeaponTypeEquipped(EquipmentSlot.EquipmentSlotIDs.RightHand, Weapon.WeaponType.Sword_2H);
                    if ((player.LeftHandEquipment == null && !is2Handed) && !player.StatusEffectMngr.HasStatusEffect("FlowStat"))
                    {
                        player.StatusEffectMngr.AddStatusEffect("FlowStat");
                    }
                    else if ((player.LeftHandEquipment != null || is2Handed) && player.StatusEffectMngr.HasStatusEffect("FlowStat"))
                    {
                        player.StatusEffectMngr.CleanseStatusEffect("FlowStat");
                    }
                }
            }
        }
    }
}
