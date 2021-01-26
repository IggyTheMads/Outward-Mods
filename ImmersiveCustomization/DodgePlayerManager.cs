using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using SideLoader;

namespace ImmersiveLinks
{
    public class DodgePlayerManager : MonoBehaviour
    {
        public static DodgePlayerManager Instance;

        private Dictionary<string, float> PlayerLastHitTimes = new Dictionary<string, float>();

        private ImmersiveCustoms main;

        internal void Awake()
        {
            Instance = this;
            main = GetComponent<ImmersiveCustoms>();

            Debug.Log("Immersive Customs: Dodge Manager Loaded");
        }

        [HarmonyPatch(typeof(Character), "HasHit")]
        public class Character_HasHit
        {
            [HarmonyPrefix]
            public static bool Prefix(Character __instance, Weapon _weapon, float _damage, Vector3 _hitDir, Vector3 _hitPoint, float _angle, bool _blocked, Character _target, float _knockback, int _attackID = -999)
            {
                var self = __instance;

                if (Instance.PlayerLastHitTimes.ContainsKey(self.UID))
                {
                    Instance.PlayerLastHitTimes[self.UID] = Time.time;
                }
                else
                {
                    Instance.PlayerLastHitTimes.Add(self.UID, Time.time);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "DodgeInput", new Type[] { typeof(Vector3) })]
        public class Character_DodgeInput
        {
            [HarmonyPrefix]
            public static bool Prefix(Character __instance, Vector3 _direction)
            {
                var self = __instance;

                // only use this hook for local players. return orig everything else
                if (self.IsAI || !self.IsPhotonPlayerLocal)
                {
                    return true;
                }

                //hard coded default dodge cost
                float staminaCost = 6;


                //passive requierement for below to take effect
                int animCancelPassiveID = 010101013;

                // if dodge cancelling is NOT enabled, just do a normal dodge check.
                if (!self.Inventory.SkillKnowledge.IsItemLearned(animCancelPassiveID))
                {
                    if (At.GetField(self, "m_currentlyChargingAttack") is bool m_currentlyChargingAttack
                       && At.GetField(self, "m_preparingToSleep") is bool m_preparingToSleep
                       && At.GetField(self, "m_nextIsLocomotion") is bool m_nextIsLocomotion
                       && At.GetField(self, "m_dodgeAllowedInAction") is int m_dodgeAllowedInAction)
                    {
                        if (self.Stats.MovementSpeed > 0f
                            && !m_preparingToSleep
                            && (!self.LocomotionAction || m_currentlyChargingAttack)
                            && (m_nextIsLocomotion || m_dodgeAllowedInAction > 0))
                        {
                            if (!self.Dodging)
                            {
                                Instance.SendDodge(self, staminaCost, _direction);
                            }
                            return false;
                        }
                    }
                }
                else // cancelling enabled. check if we should allow the dodge
                {
                    //Hard coded value
                    float Dodge_DelayAfterHit = 0.5f;
                    float Dodge_DelayAfterStagger = 0.8f;
                    float Dodge_DelayAfterKD = 2f;


                    if (Instance.PlayerLastHitTimes.ContainsKey(self.UID)
                        && Time.time - Instance.PlayerLastHitTimes[self.UID] < Dodge_DelayAfterHit)
                    {
                        //  Debug.Log("Player has hit within the last few seconds. Dodge not allowed!");
                        return false;
                    }

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
                        Instance.SendDodge(self, staminaCost, _direction);
                    }

                    // send a fix to force m_dodging to false after a short delay.
                    // this is a fix for if the player dodges while airborne, the game wont reset their m_dodging to true when they land.
                    Instance.StartCoroutine(Instance.DodgeLateFix(self));
                }

                return false;
            }
        }

        private IEnumerator DodgeLateFix(Character character)
        {
            yield return new WaitForSeconds(0.25f);

            while (!character.NextIsLocomotion)
            {
                yield return null;
            }

            At.SetField(character, "m_dodging", false);
        }

        private void SendDodge(Character self, float staminaCost, Vector3 _direction)
        {
            float f = (float)At.GetField(self.Stats, "m_stamina");

            if (f >= staminaCost)
            {
                //At.SetValue(f - staminaCost, typeof(CharacterStats), self.Stats, "m_stamina");
                self.Stats.UseStamina(TagSourceManager.Dodge, staminaCost);

                At.SetField(self, "m_dodgeAllowedInAction", 0);

                if (self.CharacterCamera && self.CharacterCamera.InZoomMode)
                {
                    self.SetZoomMode(false);
                }

                self.ForceCancel(false, true);
                self.ResetCastType();

                self.photonView.RPC("SendDodgeTriggerTrivial", PhotonTargets.All, new object[] { _direction });

                At.Invoke(self, "ActionPerformed", false);

                self.Invoke("ResetDodgeTrigger", 0.1f);
            }
        }

        [HarmonyPatch(typeof(Character), "AttackInput")]
        public class Character_AttackInput
        {
            public static bool Prefix(Character __instance, int _type, int _id = 0)
            {
                var self = __instance;

                //HARD CODED attacks can cancel blocking
                bool Attack_Cancels_Blocking = true;

                if (self.IsLocalPlayer && Attack_Cancels_Blocking && !self.IsAI && self.Blocking)
                {
                    Instance.StartCoroutine(Instance.StopBlockingCoroutine(self));
                    At.Invoke(self, "StopBlocking");
                    At.SetField(self, "m_blockDesired", false);
                }


                return true;
            }
        }

        private IEnumerator StopBlockingCoroutine(Character character)
        {
            yield return new WaitForSeconds(0.05f); // 50ms wait (1 or 2 frames)

            At.Invoke(character, "StopBlocking");
            At.SetField(character, "m_blockDesired", false);
        }
    }
}
