using BepInEx;
using HarmonyLib;
using UnityEngine;
using SideLoader;
using System.Collections.Generic;
//using TinyHelper;
using System;
using System.Collections;
using DelayedDamage;

namespace WhispererClass
{
    public class DamageManager : MonoBehaviour
    {

        public static DamageManager Instance;

        //public float delayedDamage = 0f;

        public float delayedTick = 0f;
        public int tickCounter = 0;
        public float regenPurTimer = 15f; //time to grant a purification stack

        public int baseDelayID = 0101010110; //1
        public int breakDelayID = 0101010111; //2A
        public int breakSpeedID = 0101010112; //2B
        public int ignorePainID = 0101010113; //3
        public int ignorePlusID = 0101010114; //4A
        public int passiveLastID = 0101010115; //4B
        public int halfDamageID = 0101010116; //5
        public int purShieldID = 0101010117; //6A
        public int purCleanseID = 0101010118; //6B
        public int applyExternalMadnessID = 0101010119; // 7A
        public int applyInternalMadnessID = 0101010120; // 7B
        public int increasehpID = 0101010121; //8A
        public int absorbStaminaID = 0101010122; //8B
        public int drainMadnessID = 0101010123; //9A
        public int expellMadnessID = 0101010124; //9B
        public int secretEffect1ID = 0101010131; //secret1

        public float delayPerStack = 0.15f; //each stack of insanity delays 15%
        public int regenInsanity = 0;
        public bool Regenerating = false;
        public Character player;
        public int maxInsanity = 0;
        public float tickStaminaMult = 2; //x2 stamina per dmg tick

        public int allMadnessStacks = 0;
        public float drainPerStackMult = 2; //2maxHP per madness stack
        public float expellPerStackDiv = 20f;
        public float expellRange = 20f;
        private float purifyShieldPot = 0f;
        public float purShieldMult = 10; //1 delay to 1 shield
        //public float purShieldDamager = 0f;
        public float purifyStrenght = 0.3f; //default pre breakthrough
        public float purifyStrenghtPlus = 0.5f; //default pre breakthrough
        public float purHealMult = 30f; //heal per debuff removed

        //Madness Spells Variables
        public float lostHP = 0;
        public float HPtoHeal = 0;

        //control k control d
        internal void Awake()
        {
            Instance = this;
            DelayedDamage.DelayedDamage.GetDamageToDelay += GetDelayedDamage;
            DelayedDamage.DelayedDamage.OnDelayedDamageTaken += StaminaRegenOnDelay;

            SL.OnGameplayResumedAfterLoading += Fixer;
        }

        private void Fixer()
        {
            player = CharacterManager.Instance.GetFirstLocalCharacter();
            Debug.Log("STARTED FIXING");
            //StartCoroutine(PurificationFix());
        }

        //In case its needed
        [HarmonyPatch(typeof(CharacterStats), "ReceiveDamage")]
        public class Character_ReceiveDamage
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterStats __instance, ref float _damage, ref Character ___m_character)
            {
                if (___m_character.StatusEffectMngr.HasStatusEffect("PurifyShield"))
                {
                    if(_damage < Instance.purifyShieldPot)
                    {
                        Instance.purifyShieldPot -= _damage;
                        _damage = 0f;
                        Debug.Log("Remaining Shield: " + Instance.purifyShieldPot);
                    }
                    else
                    {
                        _damage -= Instance.purifyShieldPot;
                        Instance.player.StatusEffectMngr.RemoveStatusWithIdentifierName("PurifyShield");
                        _damage = _damage/2;
                        Debug.Log("Shield broke:" + _damage);
                    }
                }
            }
        }

        #region DamageDelays & stam on tick
        private float GetDelayedDamage(Character character, Character dealer, DamageList damageList, float knockBack)
        {
            float _damage = damageList.TotalDamage;

            float delayedDamage = 0;
            if (/*knockBack > 0 && */_damage >= 1) //to avoiding triggering itself
            {
                
                if (character.Inventory.SkillKnowledge.IsItemLearned(baseDelayID))
                {
                    if (!character.StatusEffectMngr.HasStatusEffect("PurifyShield"))
                    {
                        if (character.Inventory.SkillKnowledge.IsItemLearned(breakDelayID))
                        {
                            //delay if using ignore pain (greed)
                            if (character.StatusEffectMngr.HasStatusEffect("IgnorePain") && character.Inventory.SkillKnowledge.IsItemLearned(ignorePlusID))
                            {
                                Debug.Log("EXTRA PLUS DELAY");
                                delayedDamage += (_damage * 1.0f);
                            }
                            else if (character.StatusEffectMngr.HasStatusEffect("IgnorePain"))
                            {
                                Debug.Log("EXTRA DELAY");
                                delayedDamage += (_damage * ((delayPerStack * 3) + 0.3f)); //testing
                            }
                            else //delay normaly
                            {
                                Debug.Log("NORMAL DELAY");
                                delayedDamage += (_damage * (delayPerStack * 3));
                            }
                        }
                        else if (character.Inventory.SkillKnowledge.IsItemLearned(breakSpeedID))
                        {
                            //delay if using ignore pain (greed)
                            if (character.StatusEffectMngr.HasStatusEffect("IgnorePain") && character.Inventory.SkillKnowledge.IsItemLearned(ignorePlusID))
                            {
                                Debug.Log("EXTRA PLUS DELAY");
                                delayedDamage += (_damage * ((character.StatusEffectMngr.GetStatusLevel("Insanity") * delayPerStack)) + 0.6f);
                            }
                            else if (character.StatusEffectMngr.HasStatusEffect("IgnorePain"))
                            {
                                Debug.Log("EXTRA DELAY");
                                delayedDamage += (_damage * (0.3f + (character.StatusEffectMngr.GetStatusLevel("Insanity") * delayPerStack)) + 0.3f);
                            }
                            else //delay normaly
                            {
                                Debug.Log("NORMAL DELAY");
                                delayedDamage += (_damage * (character.StatusEffectMngr.GetStatusLevel("Insanity") * delayPerStack));
                            }
                        }
                        else
                        {
                            if(character.StatusEffectMngr.HasStatusEffect("Insanity"))
                            {
                                delayedDamage += (_damage * delayPerStack);
                            }
                        }
                    }
                }
            }
            return delayedDamage;
        }

        public void StaminaRegenOnDelay(Character receiver, Character dealer, float damage)
        {
            if (receiver.Inventory.SkillKnowledge.IsItemLearned(absorbStaminaID)) //if any question mark is null, return false
            {
                receiver.Stats.AffectStamina(Mathf.Abs(damage * tickStaminaMult));
            }
        }
        #endregion

        #region GainCharges
        void Update()
        {
            if (player != null)
            {
                //generate purify charges passively
                if (player.Inventory.SkillKnowledge.IsItemLearned(Instance.baseDelayID))
                {
                    //probably not optimal for performance
                    if (player.Inventory.SkillKnowledge.IsItemLearned(Instance.breakDelayID)) { maxInsanity = 3; }
                    else if (player.Inventory.SkillKnowledge.IsItemLearned(Instance.breakSpeedID)) { maxInsanity = 2; }
                    else { maxInsanity = 1; }

                    if (regenInsanity < maxInsanity && Regenerating == false)
                    {
                        Debug.Log("GAINING CHARGES");
                        StartCoroutine(RegenPurificationStack());
                    }

                    //fix errors with charges
                    if (regenInsanity > player.StatusEffectMngr.GetStatusLevel("Insanity") && regenInsanity > 0)
                    {
                        player.StatusEffectMngr.AddStatusEffect("Insanity");
                    }
                    else if (regenInsanity < player.StatusEffectMngr.GetStatusLevel("Insanity") && player.StatusEffectMngr.HasStatusEffect("Insanity"))
                    {
                        regenInsanity += 1;
                    }
                    if(player.StatusEffectMngr.GetStatusLevel("InsanitySpeed") != player.StatusEffectMngr.GetStatusLevel("Insanity") && player.Inventory.SkillKnowledge.IsItemLearned(Instance.breakSpeedID))
                    {
                        if(player.StatusEffectMngr.GetStatusLevel("InsanitySpeed") > player.StatusEffectMngr.GetStatusLevel("Insanity"))
                        {
                            StatusEffect insanityStatus = player.StatusEffectMngr.GetStatusEffectOfName("InsanitySpeed");
                            player.StatusEffectMngr.ReduceStatusLevel(insanityStatus, 1);
                        }
                        else if(player.StatusEffectMngr.GetStatusLevel("InsanitySpeed") < player.StatusEffectMngr.GetStatusLevel("Insanity"))
                        {
                            player.StatusEffectMngr.AddStatusEffect("InsanitySpeed");
                        }

                    }
                }
            }
        }

        public IEnumerator RegenPurificationStack()
        {
            while (regenInsanity < maxInsanity)
            {
                Regenerating = true;
                yield return new WaitForSeconds(regenPurTimer);

                //player.StatusEffectMngr.AddStatusEffect("Insanity"); //moved to fixer
                regenInsanity += 1;
                Debug.Log("Generated Purification" + regenInsanity);
            }
            Regenerating = false;
        }
        #endregion

        /*#region Improved Pain Dodge Replacer
        [HarmonyPatch(typeof(Character), "DodgeInput", new Type[] { typeof(Vector3) })]
        public class Character_DodgeInput
        {
            [HarmonyPrefix]
            public static bool Prefix(Character __instance, Vector3 _direction)
            {
                if (__instance.Inventory.SkillKnowledge.IsItemLearned(Instance.ignorePlusID))
                {
                    if (Instance.regenInsanity > 0)
                    {
                        var self = __instance;

                        float Dodge_DelayAfterStagger = 0.8f;
                        float Dodge_DelayAfterKD = 2f;

                        Character.HurtType hurtType = (Character.HurtType)At.GetField(self, "m_hurtType");

                        // manual fix (game sometimes does not reset HurtType to NONE when animation ends.
                        float timeout = Dodge_DelayAfterStagger;
                        if (hurtType == Character.HurtType.Knockdown)
                        {
                            timeout = Dodge_DelayAfterKD;
                        }

                        if ((float)At.GetField(self, "m_timeOfLastStabilityHit") is float lasthit && Time.time - lasthit > timeout)
                        {
                            hurtType = Character.HurtType.NONE;
                            At.SetField(self, "m_hurtType", hurtType);
                        }

                        // if we're not currently dodging or staggered, force an animation cancel dodge (provided we have enough stamina).
                        if (!self.Dodging && hurtType == Character.HurtType.NONE)
                        {
                            self.ForceCancel(true, true);
                            //SEND ABILITY
                            __instance.ForceCastSpell(Character.SpellCastType.Bubble, __instance.gameObject, Character.SpellCastModifier.Mobile, -1, 0f);

                            StatusEffect insanityStatus = Instance.player.StatusEffectMngr.GetStatusEffectOfName("Insanity");
                            Instance.player.StatusEffectMngr.AddStatusEffect("ImprovedPain");
                            Instance.player.StatusEffectMngr.ReduceStatusLevel(insanityStatus, 1);
                            Instance.regenInsanity -= 1;

                            Instance.Invoke("TrueAnim", 0.01f);

                        }
                    }
                    else
                    {
                        Instance.player.ForceCancel(true, true);
                        __instance.ForceCastSpell(Character.SpellCastType.Bubble, __instance.gameObject, Character.SpellCastModifier.Mobile, -1, 0f);
                        Instance.Invoke("CancelDodge", 0.01f);
                    }
                    return false;
                }
                else
                {
                    return true;
                }

            }
        }

        void TrueAnim() //after canceling dodge animation, use the desired one
        {
            player.ForceCancel(false, true);

            //SEND ABILITY
            player.ForceCastSpell(Character.SpellCastType.Bubble, player.gameObject, Character.SpellCastModifier.Immobilized, -1, 1f);
        }

        void CancelDodge() //after canceling dodge animation, use the desired one
        {
            player.ForceCancel(false, true);

            //SEND KNOCK
            Instance.player.AutoKnock(false, new Vector3(0, 0, 0));
        }
        #endregion*/

        #region SKILLS effects (purify, ignorepain, drain, expell)
        [HarmonyPatch(typeof(Item), "Use", new Type[] { typeof(Character) })]
        public class Item_Usage
        {
            [HarmonyPrefix]
            public static void Prefix(Item __instance, Character _character)
            {
                StatusEffect insanityStatus = _character.StatusEffectMngr.GetStatusEffectOfName("Insanity");
                
                if (__instance.ItemID == Instance.halfDamageID)
                {
                    float preStagger = 0;
                    float postStagger = 0;

                    float purifyAmount = Instance.purifyStrenght;
                    ReduceInsanity(_character, insanityStatus);
                    if(_character.Inventory.SkillKnowledge.IsItemLearned(Instance.purCleanseID))
                    {
                        purifyAmount = Instance.purifyStrenghtPlus;
                        var effectCounter = _character.StatusEffectMngr.Statuses.Count;
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Elemental Vulnerability");
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Curse");
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Poisoned");
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Poisoned +");
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Food Poisoned");
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("Food Poisoned +");
                        if (_character.StatusEffectMngr.Statuses.Count < effectCounter)
                        {
                            _character.StatusEffectMngr.AddStatusEffect("Health Recovery 5");
                            _character.Stats.AffectHealth((effectCounter - _character.StatusEffectMngr.Statuses.Count) * Instance.purHealMult);
                        }
                    }
                    foreach (var status in _character.StatusEffectMngr.Statuses)
                    {
                        if (status.IdentifierName == "Delayed Damage")
                        {
                            preStagger += status.EffectPotency;
                            status.SetPotency(status.EffectPotency * (1f - purifyAmount));
                            postStagger += status.EffectPotency;
                            //Debug.Log("Prestagger:" + preStagger); //for debug
                            //Debug.Log("Post:" + postStagger); //for debug
                        }
                    }
                    if (_character.Inventory.SkillKnowledge.IsItemLearned(Instance.purShieldID))
                    {
                        Instance.purifyShieldPot = (preStagger - postStagger) * Instance.purShieldMult;
                        _character.StatusEffectMngr.RemoveStatusWithIdentifierName("PurifyShield");
                        _character.StatusEffectMngr.AddStatusEffect("PurifyShield");
                        //Instance.purifyShield = _character.StatusEffectMngr.GetStatusEffectOfName("PurifyShield");
                        //Instance.purifyShield.SetPotency(Instance.purifyShieldPot);
                        Debug.Log("ShieldCreated For: " + Instance.purifyShieldPot);
                    }

                }
                else if (__instance.ItemID == Instance.ignorePainID)
                {
                    ReduceInsanity(_character, insanityStatus);
                    //Debug.Log("IGNORING PAIN" + Instance.regenInsanity);
                }
                else if (__instance.ItemID == Instance.drainMadnessID) //DRAIN MADNESS
                {
                    ReduceInsanity(_character, insanityStatus);
                    if (_character.Inventory.SkillKnowledge.IsItemLearned(Instance.applyExternalMadnessID))
                    {
                        var enemies = FindObjectsOfType<Character>();
                        for (int i = 0; i < enemies.Length; i++)
                        {
                            if (enemies[i].StatusEffectMngr.HasStatusEffect("ExternalMadness"))
                            {
                                Instance.allMadnessStacks += enemies[i].StatusEffectMngr.GetStatusLevel("ExternalMadness");
                                enemies[i].StatusEffectMngr.CleanseStatusEffect("ExternalMadness");
                            }
                        }
                    }
                    else if (_character.Inventory.SkillKnowledge.IsItemLearned(Instance.applyInternalMadnessID) && _character.StatusEffectMngr.HasStatusEffect("InternalMadness"))
                    {
                                Instance.allMadnessStacks += _character.StatusEffectMngr.GetStatusLevel("InternalMadness");
                                _character.StatusEffectMngr.CleanseStatusEffect("InternalMadness");
                    }
                    Instance.lostHP = (Instance.player.Stats.ActiveMaxHealth - Instance.player.Stats.CurrentHealth);
                    Instance.HPtoHeal = Instance.allMadnessStacks * (Instance.player.Stats.MaxHealth / 100) * Instance.drainPerStackMult;
                    Instance.Invoke("DrainMadness", 1f);
                }




                else if (__instance.ItemID == Instance.expellMadnessID)
                {
                    ReduceInsanity(_character, insanityStatus);
                    var enemies = FindObjectsOfType<Character>();
                    Instance.StartCoroutine(Instance.ExpellMadness(enemies, _character));
                }
            }

            private static void ReduceInsanity(Character _character, StatusEffect insanityStatus)
            {
                _character.StatusEffectMngr.ReduceStatusLevel(insanityStatus, 1);
                Instance.regenInsanity -= 1;
            }
        }

        public void DrainMadness()
        {
            if (HPtoHeal > lostHP) //restore burnt hp if full or near
            {
                float burntRestore = HPtoHeal - lostHP;
                Instance.player.Stats.RestoreBurntHealth(burntRestore, false);
                //Debug.Log("Restored " + burntRestore);
            }
            Instance.player.Stats.AffectHealth(HPtoHeal);
            Debug.Log("HPtoHeal: " + HPtoHeal);
            Instance.allMadnessStacks = 0;
        }

        public IEnumerator ExpellMadness(Character[] enemies, Character player)
        {
            yield return new WaitForSeconds(1.4f);

            float totalStagger = 0;
            foreach (var status in player.StatusEffectMngr.Statuses)
            {
                if (status.IdentifierName == "Delayed Damage")
                {
                    totalStagger += status.EffectPotency;

                }
            }
            Debug.Log("Total Stagger:" + totalStagger);

            //for (int i = 0; i < enemies.Length; i++) //for enemey madness. NEED TO ADD FOR PERSONAL MADNESS
            foreach(var enemy in enemies)
            {
                float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
                if(distance < expellRange && enemy != player)
                {
                    if (player.Inventory.SkillKnowledge.IsItemLearned(applyExternalMadnessID))
                    {
                        float damageToDo = totalStagger * (Mathf.Pow(enemy.StatusEffectMngr.GetStatusLevel("ExternalMadness"), 2)/expellPerStackDiv); //stacks to 8
                        enemy.Stats.ReceiveDamage(damageToDo);
                        enemy.StatusEffectMngr.CleanseStatusEffect("ExternalMadness");
                        Debug.Log("Damaged enemy for:" + (damageToDo));
                    }
                    else if (player.Inventory.SkillKnowledge.IsItemLearned(applyInternalMadnessID) && player.StatusEffectMngr.HasStatusEffect("InternalMadness")) 
                    {
                        float damageToDo = totalStagger * (Mathf.Pow(player.StatusEffectMngr.GetStatusLevel("InternalMadness"), 2)/expellPerStackDiv); //stacks to 5
                        enemy.Stats.ReceiveDamage(damageToDo);
                        Debug.Log("Damaged enemy for:" + (damageToDo));
                    }
                }
            }
            player.StatusEffectMngr.CleanseStatusEffect("InternalMadness");
        }
        #endregion

        #region AI APPLY MADNESS
        [HarmonyPatch(typeof(Character), "VitalityHit")]
        public class Character_VitalityHit
        {
            [HarmonyPrefix]
            public static void Prefix(Character __instance, Character _dealerChar, float _damage, Vector3 _hitVector)
            {
                if (__instance.IsAI && Instance.player == _dealerChar)
                {
                    if (Instance.player.Inventory.SkillKnowledge.IsItemLearned(Instance.applyExternalMadnessID))
                    {
                        __instance.StatusEffectMngr.AddStatusEffect("ExternalMadness");
                    }
                    else if (Instance.player.Inventory.SkillKnowledge.IsItemLearned(Instance.applyInternalMadnessID))
                    {
                        Instance.player.StatusEffectMngr.AddStatusEffect("InternalMadness");
                    }
                }
            }
        }
        #endregion

        [HarmonyPatch(typeof(CharacterStats), "RestoreBurntHealth")]
        public class Character_RestoreBurntHealth
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterStats __instance, ref float _value, bool _ratioFromMax, ref Character ___m_character)
            {
                if (___m_character.IsLocalPlayer)
                {
                    // double burnt hp restore
                    if (___m_character.Inventory.SkillKnowledge.IsItemLearned(Instance.secretEffect1ID))
                    {
                        _value += _value;
                    }
                }

            }
        }
    }
}