using System;
using System.IO;
using UnityEngine;

namespace Tidebreak
{
    [Serializable] public class CaptainLog
    {
        public int catalogVersion=3;
        public bool[] speciesSeen=new bool[119], speciesGold=new bool[119];
        public float[] speciesWeight=new float[119];
        public int[] speciesKills=new int[119];
        public int voyages, victories, krakens, bestStage, totalKills;
        public bool[] discovered = new bool[8];
        public bool[] goldDiscovered = new bool[8];
        public float[] heaviest = new float[8];
        public float volume = .65f, sensitivity = 1;
        public bool shake = true, easy;
    }
    public static class SaveStore
    {
        public static string DirectoryOverride;
        public static string DirectoryPath { get { return DirectoryOverride ?? Application.persistentDataPath; } }
        static string PathFor(string name) { return Path.Combine(DirectoryPath, name + ".json"); }
        public static string LastError { get; private set; }
        public static T Read<T>(string name) where T : class
        {
            foreach(var suffix in new [] { "", ".bak" })
            {
                try { var p=PathFor(name)+suffix; if(File.Exists(p)) return JsonUtility.FromJson<T>(File.ReadAllText(p)); }
                catch(Exception e) { LastError=e.Message; Debug.LogWarning("Save recovery: " + e.GetType().Name); }
            }
            return null;
        }
        public static void Write(string name, object data)
        {
            try {
                Directory.CreateDirectory(DirectoryPath);
                var path = PathFor(name); var temp=path+".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data,true));
                if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
                LastError=null;
            } catch(Exception e) { LastError=e.Message; Debug.LogWarning("Save unavailable: " + e.GetType().Name); }
        }
        public static void ClearRun()
        {
            foreach(var suffix in new [] { "", ".bak", ".tmp" })
            {
                try { var p=PathFor("voyage")+suffix; if(File.Exists(p)) File.Delete(p); }
                catch(Exception e) { LastError=e.Message; }
            }
        }
    }
}
