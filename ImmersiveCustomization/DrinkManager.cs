using System;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.Collections;

namespace ImmersiveLinks
{
    public class DrinkManager : MonoBehaviour
    {
        public static DrinkManager Instance;
        private static MethodInfo methodGetCastSheathRequired = null;
        public DodgePlayerManager dodgeManager;
        public bool healRedux = false;
        public bool instaPotFixer = false;

        private int instaPotID = 010101018;
        private int potHoTID = 010101019;
        Coroutine HPco = null;
        Coroutine STAMco = null;
        Coroutine MANAco = null;

        // Harmony grabs Outward functions
        internal void Awake()
        {
            Instance = this;

            // Initialize Reflection Method
            methodGetCastSheathRequired = typeof(Item).GetMethod("GetCastSheathRequired", AccessTools.all);

            // Have Harmony patch Outward's functionality
            var harmony = new HarmonyLib.Harmony("com.iggy.immersivecustoms");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Debug.Log("Immersive Customs: Drink Manager Loaded");
        }

        //Harmony gets types of casts?
        [HarmonyPatch(typeof(Item), "StartEffectsCast")]
        class Item_StartEffectsCast
        {
            [HarmonyPrefix]
            public static bool Prefix(Item __instance, Character _targetChar)
            {
                // Variable setup
                bool isDrink = __instance.IsDrink;
                var self = _targetChar;
                int drinkWhileMovingID = 010101017;


                // Check if item is drinkable
                if (isDrink)
                {
                    // Check if passive is unlocked
                    if (self.Inventory.SkillKnowledge.IsItemLearned(drinkWhileMovingID)) //Make Potions drinkable while moving but nerf impact res
                    {
                        // Variables Setup
                        int sheatheRequired = (int)methodGetCastSheathRequired.Invoke(__instance, new object[0]);
                        Character.SpellCastModifier modifier = Character.SpellCastModifier.Mobile;
                        Character.SpellCastType castType = __instance.ActivateEffectAnimType;

                        // Fix broken animation
                        if (castType == Character.SpellCastType.DrinkWater)
                        { castType = Character.SpellCastType.Potion; }

                        // Use animation
                        _targetChar.CastSpell(castType, __instance.gameObject, modifier, sheatheRequired, 0.6f);

                        _targetChar.StatusEffectMngr.AddStatusEffect("DrinkingNerf"); //created in SL.xml

                        // false to ignore original method after new drink method
                        return false;
                    }
                    else if (self.Inventory.SkillKnowledge.IsItemLearned(Instance.potHoTID)) //Make potions heal over time and drinkable while moving
                    {
                        if (Instance.healRedux == false)
                        {
                            Instance.healRedux = true;
                            // Variables setup
                            int sheatheRequired = (int)methodGetCastSheathRequired.Invoke(__instance, new object[0]);
                            Character.SpellCastModifier modifier = Character.SpellCastModifier.Mobile;
                            Character.SpellCastType castType = __instance.ActivateEffectAnimType;

                            // Fix broken animation
                            if (castType == Character.SpellCastType.DrinkWater)
                            { castType = Character.SpellCastType.Potion; }

                            // Use animation
                            _targetChar.CastSpell(castType, __instance.gameObject, modifier, sheatheRequired, 0.7f); //anim

                            Instance.Invoke("ResetHealRedux", 2f); //reset indicator
                            return false;
                        }
                        else if (self.Inventory.SkillKnowledge.IsItemLearned(Instance.instaPotID)) //Make potions instant but heal for 60% less and have a cooldown.
                        {
                            if (Instance.healRedux == false)
                            {
                                Instance.healRedux = true;
                                _targetChar.CastSpell(Character.SpellCastType.NONE, __instance.gameObject, Character.SpellCastModifier.NONE, -1, 1f);
                                Instance.Invoke("ResetHealRedux", 10f);
                                return false;
                            }
                            else { return false; }
                        }
                        else { return false; }
                    }
                    else { return true; }//true to allow vanilla drink
                }
                else
                { return true; }
            }
        }

        [HarmonyPatch(typeof(CharacterStats), "AffectHealth")]
        public class CharacterStats_AffectHealth
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterStats __instance, ref float _quantity, ref Character ___m_character)
            {
                if (___m_character.Inventory.SkillKnowledge.IsItemLearned(Instance.potHoTID) && Instance.healRedux == true && _quantity > 10) //HEAL OVER TIME
                {
                    if (Instance.HPco != null)
                    {
                        Instance.StopCoroutine(Instance.HPco);
                    }
                    Instance.healRedux = false;
                    Instance.HPco = Instance.StartCoroutine(Instance.HPPotionOverTime(10, _quantity, ___m_character));
                    _quantity = 0f;
                }
                else if (___m_character.Inventory.SkillKnowledge.IsItemLearned(Instance.instaPotID)) //INSTANT
                {
                    if (Instance.healRedux == true && Instance.instaPotFixer == false)
                    {
                        Instance.instaPotFixer = true;
                        Debug.Log("OG Heal:" + _quantity);
                        _quantity = _quantity * 0.4f;
                        Debug.Log("New Heal:" + _quantity);
                    }
                }
            }
        }

        void ResetHealRedux() //Reset usage
        {
            healRedux = false;
            instaPotFixer = false;
            //Debug.Log("Potion usable again");
        }

        private IEnumerator HPPotionOverTime(float timer, float _quantity, Character player) //heal over time ticks
        {
            int time = 0;
            player.StatusEffectMngr.AddStatusEffect("PotHotHP");
            while (time < timer && player.Stats.CurrentHealth > 1)
            {
                yield return new WaitForSeconds(1f);
                time += 1;
                if(player.Stats.CurrentHealth > 1)
                {
                    player.Stats.AffectHealth(_quantity / timer);
                    //Debug.Log("HoT for:" + (_quantity / timer));
                }
            }
        }

        [HarmonyPatch(typeof(CharacterStats), "AffectStamina")]
        public class CharacterStats_AffectStamina
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterStats __instance, ref float _quantity, ref Character ___m_character)
            {
                if (___m_character.Inventory.SkillKnowledge.IsItemLearned(Instance.potHoTID) && Instance.healRedux == true && _quantity > 10) //STAM OVER TIME
                {
                    if (Instance.STAMco != null)
                    {
                        Instance.StopCoroutine(Instance.STAMco);
                    }
                    Instance.healRedux = false;
                    Instance.STAMco = Instance.StartCoroutine(Instance.StamPotionOverTime(10, _quantity, ___m_character));
                    _quantity = 0f;
                }
            }
        }

        private IEnumerator StamPotionOverTime(float timer, float _quantity, Character player) //stam over time ticks
        {
            int time = 0;
            player.StatusEffectMngr.AddStatusEffect("PotHotStam");
            while (time < timer && player.Stats.CurrentHealth > 1)
            {
                yield return new WaitForSeconds(1f);
                time += 1;
                if (player.Stats.CurrentHealth > 1)
                {
                    player.Stats.AffectStamina(_quantity / timer);
                    //Debug.Log("Stam for:" + (_quantity / timer));
                }
            }
            //Instance.healRedux = false;
        }

        [HarmonyPatch(typeof(CharacterStats), "RestaureMana")]
        public class CharacterStats_RestaureMana
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterStats __instance, Tag[] _tags, ref float _amount, ref Character ___m_character)
            {
                if (___m_character.Inventory.SkillKnowledge.IsItemLearned(Instance.potHoTID) && Instance.healRedux == true && _amount > 10) //MANA OVER TIME
                {
                    if (Instance.MANAco != null)
                    {
                        Instance.StopCoroutine(Instance.MANAco);
                    }
                    Instance.healRedux = false;
                    Instance.MANAco = Instance.StartCoroutine(Instance.ManaPotionOverTime(10, _amount, _tags, ___m_character));
                    _amount = 0f;
                }
            }
        }

        private IEnumerator ManaPotionOverTime(float timer, float _amount, Tag[] _tags, Character player) //Mana over time ticks
        {
            int time = 0;
            player.StatusEffectMngr.AddStatusEffect("PotHotMana");
            while (time < timer && player.Stats.CurrentHealth > 1)
            {
                yield return new WaitForSeconds(1f);
                time += 1;
                if (player.Stats.CurrentHealth > 1)
                {
                    player.Stats.RestaureMana(_tags, _amount / timer);
                    //Debug.Log("Mana for:" + (_amount / timer));
                }
            }
            //Instance.healRedux = false;
        }
    }
}
