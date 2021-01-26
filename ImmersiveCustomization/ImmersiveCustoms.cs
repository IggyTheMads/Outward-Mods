using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace ImmersiveLinks
{
    [BepInPlugin(GUID, NAME, VERSION)]

    public class ImmersiveCustoms : BaseUnityPlugin
    {
        const string GUID = "com.iggy.immersivecustoms";
        const string NAME = "Immersive Customization";
        const string VERSION = "1.0";

        public static ImmersiveCustoms Instance;


        internal void Awake()
        {
            Instance = this;

            Debug.Log("Immersive Customization awake");

            var _obj = this.gameObject;
            _obj.AddComponent<DodgePlayerManager>();
            _obj.AddComponent<DrinkManager>();
            _obj.AddComponent<StatsManager>();

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        internal void Start()
        {
            Debug.Log("Immersive Customs Loaded");
        }
    }
}