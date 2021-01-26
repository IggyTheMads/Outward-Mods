using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using SideLoader;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace RogueliteHardcore
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class RogueliteBase : BaseUnityPlugin
    {
        const string GUID = "com.iggy.roguelite";
        const string NAME = "Roguelite Hardcore";
        const string VERSION = "1.0";

        public static RogueliteBase Instance;
        public static RogueliteSaveData History;
        public static string ModFolder = Environment.CurrentDirectory + @"\BepInEx\plugins\HardcoreRoguelike\";
        public static string SaveLocation = "SavedValues.xml";

        public static XmlSerializer serial = new XmlSerializer(typeof(RogueliteSaveData));

        internal void Awake()
        {
            Instance = this;

            var _obj = this.gameObject;
            _obj.AddComponent<DeathManager>();

            if(Directory.Exists(ModFolder + SaveLocation))
            {
                using(FileStream fileStream = File.OpenRead(ModFolder + SaveLocation))
                {
                    try
                    {
                        History = serial.Deserialize(fileStream) as RogueliteSaveData;
                    } catch (Exception)
                    {
                        Logger.Log(LogLevel.Error, "Couldn't Load SavedValues.xml");
                    }
                }
            }

            if (History == null)
                History = new RogueliteSaveData();

            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            Logger.Log(LogLevel.Message, "Roguelite Started...");
        }
    }

    public class RogueliteSaveData
    {
        public List<RogueliteEqmtData> TombStones;
        public int HighScore;

        public RogueliteSaveData()
        {
            TombStones = new List<RogueliteEqmtData>();
            HighScore = 0;
        }
    }

    public class RogueliteEqmtData
    {
        List<int> eqmtIDs;
        string Name;
    }
}
