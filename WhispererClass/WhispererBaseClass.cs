using BepInEx;
using HarmonyLib;
using UnityEngine;
using SideLoader;

namespace WhispererClass
{
    [BepInPlugin(GUID, NAME, VERSION)]

    public class WhispererBaseClass : BaseUnityPlugin
    {
        const string GUID = "com.iggy.whisperer";
        const string NAME = "Whisperer Class";
        const string VERSION = "1.5";

        public static WhispererBaseClass Instance;

        internal void Awake()
        {
            Instance = this;


            Debug.Log("Whisperer Class awake");

            var _obj = this.gameObject;

            _obj.AddComponent<DamageManager>();

            

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        internal void Start()
        {
            Debug.Log("Whisperer Class Loaded");
        }
    }
}
