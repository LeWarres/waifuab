using System.Collections.Generic;
using System.IO;
using UnityEngine;

// The game's save, one JSON file in the player's data folder: the locomotives unlocked, and the story under way.
// The story is saved biome by biome: which chapter comes next, with the locomotive, and what each chapter won hands
// on (the upgrade kept, the upgrade carried at random, the truck's wagon). A chapter always starts from nothing;
// lost, it is played again from its start; won, the next one is saved as the one to continue. A new story wipes
// it all. Endless mode is free play and saves nothing.
public static class SaveGame
{
    [System.Serializable]
    class Data
    {
        public List<int> unlocked = new();
        public bool story;                    // a story is under way
        public int locomotive, chapter, reached;
        public List<string> keep = new();     // per chapter won: the upgrade chosen to keep (a Story token)
        public List<string> passive = new();  // per chapter won: the upgrade carried at random, "<id>*<level>"
        public List<int> truck = new();       // per chapter won: the level of the truck's wagon won in it, 0 none
    }

    static Data data;
    static string File => Path.Combine(Application.persistentDataPath, "save.json");

    static Data D
    {
        get
        {
            if (data != null) return data;
            try { data = System.IO.File.Exists(File) ? JsonUtility.FromJson<Data>(System.IO.File.ReadAllText(File)) : null; }
            catch (System.Exception e) { Debug.LogWarning("Save unreadable, starting a new one: " + e.Message); }
            if (data == null)
            {
                // Every locomotive is open for now: unlocking them is still to be decided (Unlock is there for it).
                data = new Data();
                for (int i = 0; i < Locomotives.Models.Length; i++) data.unlocked.Add(i);
            }
            return data;
        }
    }

    static void Write()
    {
        try { System.IO.File.WriteAllText(File, JsonUtility.ToJson(D, true)); }
        catch (System.Exception e) { Debug.LogWarning("Could not save: " + e.Message); }
    }

    public static bool Unlocked(int locomotive) => D.unlocked.Contains(locomotive);
    public static void Unlock(int locomotive)
    {
        if (D.unlocked.Contains(locomotive)) return;
        D.unlocked.Add(locomotive);
        Write();
    }

    public static bool HasStory => D.story;
    public static int Locomotive => D.locomotive;
    public static int Chapter => D.chapter;   // the one to continue with
    public static int Reached => D.reached;   // chapters open on the menu's list

    // A new story with this locomotive: whatever was saved before is gone.
    public static void NewStory(int locomotive)
    {
        D.story = true;
        D.locomotive = locomotive;
        D.chapter = D.reached = 0;
        D.keep.Clear();
        D.passive.Clear();
        D.truck.Clear();
        Write();
    }

    // A chapter won: what it hands on, and the next one is the one to continue with.
    public static void Won(int chapter, string keep, string passive, int truck, int reachedCap)
    {
        while (D.keep.Count <= chapter) { D.keep.Add(""); D.passive.Add(""); D.truck.Add(0); }
        D.keep[chapter] = keep ?? "";
        D.passive[chapter] = passive ?? "";
        D.truck[chapter] = truck;
        D.chapter = chapter + 1;
        D.reached = Mathf.Min(Mathf.Max(D.reached, chapter + 1), reachedCap);
        Write();
    }

    // A chapter lost: it stays the one to continue with, from its start.
    public static void Lost(int chapter)
    {
        if (D.chapter == chapter) return; // already so: nothing to write
        D.chapter = chapter;
        Write();
    }

    public static string Keep(int chapter) => chapter < D.keep.Count ? D.keep[chapter] : "";
    public static string Passive(int chapter) => chapter < D.passive.Count ? D.passive[chapter] : "";
    public static int Truck(int chapter) => chapter < D.truck.Count ? D.truck[chapter] : 0;
}
