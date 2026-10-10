using System.Collections.Generic;
using UnityEngine;

// The game's screen settings, kept between runs: window mode, resolution, a cap on the frame rate and
// vertical sync. The title screen's settings page changes them (TrainSim's settings menu); each change
// applies and saves at once. Written like Sound.cs, so the two settings pages read the same.
public static class Display
{
    // The three window modes, in the order the setting cycles them (a key per name to show).
    public static readonly FullScreenMode[] Modes =
    {
        FullScreenMode.FullScreenWindow,    // borderless, fills the screen
        FullScreenMode.ExclusiveFullScreen, // the monitor's own resolution changes
        FullScreenMode.Windowed,
    };
    public static readonly string[] ModeKeys = { "display.borderless", "display.fullscreen", "display.windowed" };

    // Frame-rate caps; -1 is no cap (shown as "unlimited").
    public static readonly int[] Fps = { 30, 60, 90, 120, 144, 240, -1 };

    static Resolution[] resolutions;

    // The display's resolutions, each size once, smallest first.
    public static Resolution[] Resolutions => resolutions != null ? resolutions : resolutions = Build();

    public static int ModeIndex { get; private set; } = PlayerPrefs.GetInt("display.mode", -1);
    public static int ResIndex { get; private set; } = PlayerPrefs.GetInt("display.res", -1);
    public static int FpsIndex { get; private set; } = PlayerPrefs.GetInt("display.fps", 1);
    public static bool VSync { get; private set; } = PlayerPrefs.GetInt("display.vsync", 1) == 1;

    public static int Width => Resolutions[Mathf.Clamp(ResIndex, 0, Resolutions.Length - 1)].width;
    public static int Height => Resolutions[Mathf.Clamp(ResIndex, 0, Resolutions.Length - 1)].height;
    public static string FpsLabel => Fps[FpsIndex] < 0 ? L10n.T("display.fps.unlimited") : Fps[FpsIndex].ToString();

    static Display()
    {
        // No saved mode yet: keep whatever the game started with (the editor or the build's own window).
        if (ModeIndex < 0) ModeIndex = Mathf.Max(0, System.Array.IndexOf(Modes, Screen.fullScreenMode));
        ModeIndex = Mathf.Clamp(ModeIndex, 0, Modes.Length - 1);
        if (ResIndex < 0 || ResIndex >= Resolutions.Length) ResIndex = Current();
        FpsIndex = Mathf.Clamp(FpsIndex, 0, Fps.Length - 1);
    }

    static int Current()
    {
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].width == Screen.width && Resolutions[i].height == Screen.height) return i;
        return Resolutions.Length - 1; // the biggest, if the current size is not on the list
    }

    static Resolution[] Build()
    {
        var list = new List<Resolution>();
        foreach (Resolution r in Screen.resolutions)
            if (!list.Exists(x => x.width == r.width && x.height == r.height)) list.Add(r);
        if (list.Count == 0) list.Add(Screen.currentResolution);
        list.Sort((a, b) => a.width != b.width ? a.width.CompareTo(b.width) : a.height.CompareTo(b.height));
        return list.ToArray();
    }

    // A step is +1 or -1; past either end comes round to the other, like the sound volumes.
    public static void StepMode(int step) { ModeIndex = Wrap(ModeIndex + step, Modes.Length); Save(); }
    public static void StepResolution(int step) { ResIndex = Wrap(ResIndex + step, Resolutions.Length); Save(); }
    public static void StepFps(int step) { FpsIndex = Wrap(FpsIndex + step, Fps.Length); Save(); }
    public static void ToggleVSync() { VSync = !VSync; Save(); }

    static int Wrap(int at, int count) => (at % count + count) % count;

    static void Save()
    {
        PlayerPrefs.SetInt("display.mode", ModeIndex);
        PlayerPrefs.SetInt("display.res", ResIndex);
        PlayerPrefs.SetInt("display.fps", FpsIndex);
        PlayerPrefs.SetInt("display.vsync", VSync ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    // Puts the settings on the screen. In the editor the resolution part is left to the Game view.
    public static void Apply()
    {
        Resolution r = Resolutions[Mathf.Clamp(ResIndex, 0, Resolutions.Length - 1)];
        Screen.SetResolution(r.width, r.height, Modes[ModeIndex]);
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : Fps[FpsIndex]; // ignored while v-sync is on
    }
}
