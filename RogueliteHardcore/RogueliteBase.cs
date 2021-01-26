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
    [BepInPlugin(GUID, NAME, VERSION)]

    public class RogueliteBase : BaseUnityPlugin
    {
        const string GUID = "com.iggy.roguelike";
        const string NAME = "Roguelike Hardcore";
        const string VERSION = "1.0";

        public static RogueliteBase Instance;

        internal void Awake()
        {
            Instance = this;


            Debug.Log("Whisperer Class awake");

            var _obj = this.gameObject;

            _obj.AddComponent<DeathManager>();

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        internal void Start()
        {
            Debug.Log("Roguelike Hardcore Loaded");
        }
    }
}
