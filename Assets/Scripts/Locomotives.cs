using UnityEngine;

// The selection survives scene loads, restarts and reopening the game.
public static class Locomotives
{
    // One entry per locomotive, in the order of the selection screen. To add one: its model (Tools/make_locos.py
    // builds them), its picture in Resources/Menu, and its two texts.
    public static readonly string[] Models = { "Models/Locomotive", "Prefabs/Locomotive_RedWhite", "Models/Locomotive_Steam", "Models/Locomotive_Dark", "Models/Locomotive_Future", "Models/Locomotive_Cat", "Models/Locomotive_Shark", "Models/Locomotive_Armor", "Models/Locomotive_Jet", "Models/Locomotive_Beast", "Models/Locomotive_Spider", "Models/Locomotive_Crochet" };
    public static readonly string[] Portraits = { "Menu/loco_classic", "Menu/loco_redwhite", "Menu/loco_steam", "Menu/loco_dark", "Menu/loco_future", "Menu/loco_cat", "Menu/loco_shark", "Menu/loco_armor", "Menu/loco_jet", "Menu/loco_beast", "Menu/loco_spider", "Menu/loco_crochet" };
    // Where each one's stack opens, in the model's own space; zero for the ones with no smoke.
    public static readonly Vector3[] Stacks = { Vector3.zero, Vector3.zero, new Vector3(-1.75f, 2.65f, 0f), new Vector3(-1.7f, 2.9f, 0f), Vector3.zero, new Vector3(-0.85f, 2.4f, 0f), Vector3.zero, Vector3.zero, Vector3.zero, new Vector3(-0.2f, 2.7f, 0f), new Vector3(-1.9f, 2.4f, 0f), Vector3.zero };
    public static readonly string[] NameKeys = { "loco.classic", "loco.redwhite", "loco.steam", "loco.dark", "loco.future", "loco.cat", "loco.shark", "loco.armor", "loco.jet", "loco.beast", "loco.spider", "loco.crochet" };
    public static readonly string[] DescriptionKeys = { "loco.classic.description", "loco.redwhite.description", "loco.steam.description", "loco.dark.description", "loco.future.description", "loco.cat.description", "loco.shark.description", "loco.armor.description", "loco.jet.description", "loco.beast.description", "loco.spider.description", "loco.crochet.description" };
    // Not earned yet: on the selection screen, preview and all, but not for taking out. Only the two diesels are open.
    public static bool Locked(int index) => index > 1;
    public static int Selected => Mathf.Clamp(PlayerPrefs.GetInt("locomotive.selected", 0), 0, Models.Length - 1);

    public static void Select(int index)
    {
        PlayerPrefs.SetInt("locomotive.selected", Mathf.Clamp(index, 0, Models.Length - 1));
        PlayerPrefs.Save();
    }

    public static Transform Create(Transform parent, int index)
    {
        index = Mathf.Clamp(index, 0, Models.Length - 1);
        var model = Resources.Load<GameObject>(Models[index]);
        if (!model) model = Resources.Load<GameObject>(Models[0]);
        Transform body = Object.Instantiate(model, parent).transform;
        body.name = "Locomotive_" + index;
        body.localScale = Vector3.one * 1.3f;
        foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>()) TrainSim.Toon(renderer);
        if (Stacks[index] != Vector3.zero)
        {
            // Steam engines smoke: a Smoke at the mouth of the stack (the model's nose is its -x).
            var smoke = new GameObject("Smoke").AddComponent<Smoke>();
            smoke.transform.SetParent(body, false);
            smoke.transform.localPosition = Stacks[index];
            if (index == 2 || index == 5) { smoke.colour = new Color(0.75f, 0.75f, 0.78f); smoke.size = 0.9f; } // the 279: lighter, thinner steam
            if (index == 9) smoke.colour = new Color(0.25f, 0.85f, 0.2f); // the iron beast breathes green
        }
        if (index == 10) body.gameObject.AddComponent<SpiderLegs>(); // the spider walks
        return body;
    }
}
