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
        public Character player;
        public ItemContainer itemContainer;

        public string path = @"D:\Program Files (x86)\Steam\steamapps\common\Outward\BepInEx\plugins\HardcoreRoguelike\SavedValues.txt";

        internal void Awake()
        {
            Instance = this;

            SL.OnGameplayResumedAfterLoading += Fixer;
        }

        private void Fixer()
        {
            //Get character.
            player = CharacterManager.Instance.GetFirstLocalCharacter();
            itemContainer = FindObjectOfType<ItemContainer>();

            //TESTING SHIIT--------------------------------------------------------
            bool firstSpawn = bool.Parse(System.IO.File.ReadLines(path).Skip(12).Take(1).First());
            
            if (firstSpawn == true)
            {
                #region Read & spawn items
                string chestLine = System.IO.File.ReadLines(path).Skip(1).Take(1).First();
                string headLine = System.IO.File.ReadLines(path).Skip(3).Take(1).First();
                string feetLine = System.IO.File.ReadLines(path).Skip(5).Take(1).First();
                string rightLine = System.IO.File.ReadLines(path).Skip(7).Take(1).First();
                string leftLine = System.IO.File.ReadLines(path).Skip(9).Take(1).First();
                string backLine = System.IO.File.ReadLines(path).Skip(11).Take(1).First();

                var chestIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(chestLine);
                var headIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(headLine);
                var feetIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(feetLine);
                var rightIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(rightLine);
                var leftIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(leftLine);
                var backIDtoSpawn = ResourcesPrefabManager.Instance.GetItemPrefab(backLine);
                player.Inventory.GenerateItem(chestIDtoSpawn, 1, false);
                player.Inventory.GenerateItem(headIDtoSpawn, 1, false);
                player.Inventory.GenerateItem(feetIDtoSpawn, 1, false);
                player.Inventory.GenerateItem(rightIDtoSpawn, 1, false);
                player.Inventory.GenerateItem(leftIDtoSpawn, 1, false);
                player.Inventory.GenerateItem(backIDtoSpawn, 1, false);
                #endregion

                List<string> boolLine = System.IO.File.ReadAllLines(Instance.path).ToList<string>();
                //give the line to be inserted
                boolLine[12] = "false";
                System.IO.File.WriteAllLines(Instance.path, boolLine);
            }
        }

        [HarmonyPatch(typeof(DefeatScenariosManager), "DefeatHardcoreDeath")]
        public class Character_DefeatScenariosManager
        {
            [HarmonyPrefix]
            public static void Prefix(DefeatScenariosManager __instance)
            {
                #region GetItemIDs
                var chestSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.Chest).EquippedItemID.ToString();
                var headSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.Helmet).EquippedItemID.ToString();
                var feetSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.Foot).EquippedItemID.ToString();
                var rightSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.RightHand).EquippedItemID.ToString();
                var leftSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.LeftHand).EquippedItemID.ToString();
                var backSlotID = Instance.player.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.Back).EquippedItemID.ToString();
                #endregion

                #region WriteLines
                
                List<string> lines = System.IO.File.ReadAllLines(Instance.path).ToList<string>();
                //give the line to be inserted
                lines[1] = chestSlotID;
                lines[3] = headSlotID;
                lines[5] = feetSlotID;
                lines[7] = rightSlotID;
                lines[9] = leftSlotID;
                lines[11] = backSlotID;
                lines[12] = "true";
                System.IO.File.WriteAllLines(Instance.path, lines);
                #endregion
            }
        }
    }
}
