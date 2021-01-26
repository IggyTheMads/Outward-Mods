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

            if (History == null)
            {
                History = new RogueliteSaveData();
                Save();
            }


            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            Logger.Log(LogLevel.Message, "Roguelite Started...");
        }

        public static void Load()
        {
            if (Directory.Exists(ModFolder + SaveLocation))
                using (FileStream fileStream = File.OpenRead(ModFolder + SaveLocation))
                    try
                    {
                        History = serial.Deserialize(fileStream) as RogueliteSaveData;
                    }
                    catch (Exception)
                    {
                        Instance.Logger.Log(LogLevel.Error, "Couldn't Load SavedValues.xml");
                    }
        }

        public static void Save()
        {
            Directory.CreateDirectory(ModFolder);
            string path = ModFolder + SaveLocation;
            if (File.Exists(path))
                File.Delete(path);

            using (FileStream fileStream = File.Create(path))
                serial.Serialize(fileStream, History);
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
        public List<int> EqmtIDs;
        public string Name;

        // TODO: Add tombstone to death location
        //       MAKE SURE YOU CAN WALK THROUGH IT
        //          and probably see through it
        //       OR make it invisible during combat and fade in after combat ends
        public AreaManager.AreaEnum DeathScene;
        public Vector3 Position;
    }
}
