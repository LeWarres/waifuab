using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Standalone flight challenge. Story keeps chapter 2 (space) pending until the flight is won.
public class Ascenso : MonoBehaviour
{
    enum Phase { Intro, Flying, Lost, Arrived }
    public float duration = 55f;
    class Debris { public Transform body; public Renderer renderer; public float speed, spin, drift; }
    readonly List<Debris> debris = new();
    readonly List<Material> materials = new();
    readonly List<Mesh> meshes = new();
    readonly GameObject[] obstacleModels = new GameObject[4];
    Phase phase;
    Camera view;
    Transform train, exhaust;
    Renderer[] trainRenderers;
    Vector2 trainHalf;
    AscentBackdrop backdrop;
    UnityEngine.UI.Text status, altitude, dialogue, speaker, actionText;
    Slanted dialogPanel, progressFill, flash;
    Canvas hud;
    readonly Slanted[] collisionPips = new Slanted[3];
    InputAction move;
    float elapsed, spawnIn = .8f, immunity, flashTime, dialogueAge;
    int hits, lastConfirm = -1;
    int lastPattern = -1;
    float nextBarrier = 5f;
    bool paused, leaving;
    UnityEngine.Rendering.Volume look;
    AscentAudio flightAudio;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        view = Camera.main;
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        BuildWorld();
        BuildUI();
        flightAudio = GetComponent<AscentAudio>() ?? gameObject.AddComponent<AscentAudio>();
        ShowDialog("ascent.intro", "ascent.begin");
        RefreshStatus();
    }

    void BuildWorld()
    {
        string[] models = { "HullWreckage", "PressureCanister", "OrbitalSatellite", "DamagedBooster" };
        for (int i = 0; i < models.Length; i++) obstacleModels[i] = Resources.Load<GameObject>("Ascent/Prefabs/" + models[i]);
        look = new GameObject("FlightExposure").AddComponent<UnityEngine.Rendering.Volume>();
        look.isGlobal = true;
        look.priority = 10f;
        var exposure = look.profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);
        exposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed;
        exposure.fixedExposure.value = 13.7f;
        look.profile.Add<UnityEngine.Rendering.HighDefinition.Tonemapping>(true).mode.value = UnityEngine.Rendering.HighDefinition.TonemappingMode.Neutral;
        var bloom = look.profile.Add<UnityEngine.Rendering.HighDefinition.Bloom>(true);
        bloom.intensity.value = .6f;
        bloom.scatter.value = .7f;
        bloom.threshold.value = 1f;
        // The same bright, vivid grade as the ride (TrainSim.Grade), in space's colours.
        var grade = look.profile.Add<UnityEngine.Rendering.HighDefinition.ColorAdjustments>(true);
        grade.saturation.value = 14f;
        grade.contrast.value = 6f;
        grade.postExposure.value = .25f;
        var toning = look.profile.Add<UnityEngine.Rendering.HighDefinition.SplitToning>(true);
        Color.RGBToHSV(UiKit.Biome("space").main, out float hue, out _, out _);
        toning.shadows.value = Color.HSVToRGB(.58f, .45f, .5f);
        toning.highlights.value = Color.HSVToRGB(hue, .12f, .5f);
        toning.balance.value = 10f;
        UiKit.SetBiome("space"); // and its UI

        train = new GameObject("FlyingLocomotive").transform;
        var model = Locomotives.Create(train, Locomotives.Selected);
        // Models point along -X. Tilt the roof toward the camera, then point the nose upward.
        model.localRotation = Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(-56f, -12f, 0f);
        Bounds bounds = BoundsOf(model);
        model.localScale *= 4.2f / bounds.size.y;
        bounds = BoundsOf(model);
        model.position -= bounds.center;
        trainHalf = new Vector2(bounds.size.x * .36f, 1.65f);
        train.position = new Vector3(0f, -3.7f, 0f);
        var drive = Instantiate(Resources.Load<GameObject>("Ascent/Prefabs/FlightDrive"), train).transform;
        drive.localPosition = new Vector3(0f, -1.9f, 0f);
        drive.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        foreach (var renderer in drive.GetComponentsInChildren<Renderer>()) TrainSim.Toon(renderer);
        exhaust = new GameObject("IonExhaust").transform;
        exhaust.SetParent(train, false);
        exhaust.localPosition = new Vector3(0f, -2.25f, 0f);
        var outer = Material(new Color(.18f, .65f, 1f) * 1.6f);
        var core = Material(new Color(.85f, .96f, 1f) * 1.5f);
        for (int side = -1; side <= 1; side += 2)
        {
            Flame(exhaust, new Vector3(side * .45f, 0f, 0f), .22f, 1.6f, outer);
            Flame(exhaust, new Vector3(side * .45f, -.02f, -.035f), .11f, 1.05f, core);
        }
        trainRenderers = train.GetComponentsInChildren<Renderer>();

        var canvas = new GameObject("AscentSky", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = view;
        canvas.planeDistance = 60f;
        canvas.sortingOrder = -20;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        backdrop = new GameObject("CloudsAndStars", typeof(RectTransform), typeof(CanvasRenderer), typeof(AscentBackdrop)).GetComponent<AscentBackdrop>();
        backdrop.transform.SetParent(canvas.transform, false);
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        backdrop.raycastTarget = false;
        backdrop.Refresh(0f, 0f);
    }

    Material Material(Color color)
    {
        var mat = TrainSim.Mat(color, false);
        materials.Add(mat);
        return mat;
    }

    static Bounds BoundsOf(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        var result = renderers[0].bounds;
        foreach (var renderer in renderers) result.Encapsulate(renderer.bounds);
        return result;
    }

    void Flame(Transform parent, Vector3 at, float radius, float length, Material material)
    {
        const int sides = 12, rings = 4;
        var vertices = new Vector3[sides * rings];
        var triangles = new int[(rings - 1) * sides * 6];
        for (int ring = 0; ring < rings; ring++)
        for (int i = 0; i < sides; i++)
        {
            float t = ring / (float)(rings - 1), angle = i * Mathf.PI * 2f / sides;
            float r = radius * (ring == 1 ? 1.1f : 1f - t);
            vertices[ring * sides + i] = new Vector3(Mathf.Cos(angle) * r, -t * length, Mathf.Sin(angle) * r);
            if (ring == rings - 1) continue;
            int a = ring * sides + i, b = ring * sides + (i + 1) % sides, k = (ring * sides + i) * 6;
            triangles[k] = a; triangles[k + 1] = b; triangles[k + 2] = a + sides;
            triangles[k + 3] = b; triangles[k + 4] = b + sides; triangles[k + 5] = a + sides;
        }
        var mesh = new Mesh { name = "TaperedIonPlume", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        meshes.Add(mesh);
        var plume = new GameObject("IonPlume", typeof(MeshFilter), typeof(MeshRenderer));
        plume.transform.SetParent(parent, false);
        plume.transform.localPosition = at;
        plume.GetComponent<MeshFilter>().sharedMesh = mesh;
        plume.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    void BuildUI()
    {
        hud = UiKit.NewCanvas("AscentHUD", 20);
        var glass = new Color(.06f, .09f, .19f, .72f);
        var left = UiKit.Group(hud.transform, new Vector2(0f, 1f), new Vector2(30f, -25f));
        var hitCard = UiKit.Panel(left, new Vector2(235f, -58f), new Vector2(470f, 116f), glass, glass, .06f);
        UiKit.Persona(hitCard);
        UiKit.Paint(UiKit.Panel(hitCard.transform, new Vector2(-227f, 0f), new Vector2(5f, 108f), Color.white, Color.white, 0f), UiKit.Ink.Main);
        UiKit.Picture(hitCard.transform, "wagon", new Vector2(-190f, 18f), 32f);
        status = Label(hitCard.transform, 25, new Vector2(24f, 22f), new Vector2(360f, 40f));
        status.alignment = TextAnchor.MiddleLeft;
        for (int i = 0; i < collisionPips.Length; i++)
            collisionPips[i] = UiKit.Panel(hitCard.transform, new Vector2(-140f + i * 110f, -25f), new Vector2(95f, 9f), UiKit.Mint, UiKit.Mint, 0f);
        var right = UiKit.Group(hud.transform, new Vector2(1f, 1f), new Vector2(-30f, -25f));
        var travelCard = UiKit.Panel(right, new Vector2(-185f, -58f), new Vector2(370f, 116f), glass, glass, .06f);
        UiKit.Persona(travelCard);
        altitude = Label(travelCard.transform, 25, new Vector2(0f, 20f), new Vector2(330f, 40f));
        UiKit.Panel(travelCard.transform, new Vector2(0f, -23f), new Vector2(300f, 7f), UiKit.Slate, UiKit.Slate, 0f);
        progressFill = UiKit.Paint(UiKit.Panel(travelCard.transform, new Vector2(-150f, -23f), new Vector2(0f, 7f), Color.white, Color.white, 0f), UiKit.Ink.Main);
        progressFill.rectTransform.pivot = new Vector2(0f, .5f);
        var bottom = UiKit.Group(hud.transform, new Vector2(.5f, 0f), new Vector2(0f, 31f));
        UiKit.Persona(UiKit.Panel(bottom, Vector2.zero, new Vector2(1070f, 44f), glass, glass, .04f));
        Label(bottom, 20, Vector2.zero, new Vector2(1020f, 38f)).text = L10n.T("ascent.controls");
        flash = UiKit.Panel(hud.transform, Vector2.zero, Vector2.zero, Color.clear, Color.clear, 0f);
        flash.rectTransform.anchorMin = Vector2.zero;
        flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        var dialogueGlass = new Color(.06f, .09f, .19f, .94f);
        dialogPanel = UiKit.Persona(UiKit.Panel(hud.transform, new Vector2(0f, -330f), new Vector2(1420f, 270f), dialogueGlass, dialogueGlass, .035f), 6f, 14f);
        UiKit.Paint(UiKit.Panel(dialogPanel.transform, new Vector2(0f, -128f), new Vector2(1380f, 6f), Color.white, Color.white, 0f), UiKit.Ink.Main).sprite = null;
        var nameTag = UiKit.Paint(UiKit.Panel(dialogPanel.transform, new Vector2(-480f, 135f), new Vector2(380f, 60f), Color.white, Color.white, .25f), UiKit.Ink.Main);
        nameTag.sprite = null;
        nameTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        speaker = Label(nameTag.transform, 36, new Vector2(0f, 2f), new Vector2(360f, 56f));
        speaker.font = UiKit.Display;
        speaker.color = Color.white;
        dialogue = Label(dialogPanel.transform, 28, new Vector2(0f, 16f), new Vector2(1290f, 100f));
        var action = UiKit.Paint(UiKit.Panel(dialogPanel.transform, new Vector2(0f, -86f), new Vector2(360f, 56f), Color.white, Color.white, .25f), UiKit.Ink.Main);
        action.sprite = null;
        action.raycastTarget = true;
        var clicks = action.gameObject.AddComponent<Clicks>();
        clicks.click = _ => Confirm();
        clicks.hover = _ => Sound.Play("click");
        actionText = Label(action.transform, 30, Vector2.zero, new Vector2(330f, 50f));
        actionText.font = UiKit.Display;
    }

    static UnityEngine.UI.Text Label(Transform parent, int size, Vector2 at, Vector2 box)
    {
        var label = UiKit.Label(parent, UiKit.Body, size, Color.white, TextAnchor.MiddleCenter, at, box, true);
        label.fontStyle = FontStyle.Normal;
        return label;
    }

    void ShowDialog(string key, string button)
    {
        dialogPanel.gameObject.SetActive(true);
        dialogueAge = 0f;
        speaker.text = paused ? L10n.T("ascent.pause") : phase == Phase.Lost ? L10n.T("ascent.failed") : L10n.T(Locomotives.HeroNameKey(Locomotives.Selected));
        dialogue.text = L10n.T(key);
        dialogue.color = phase == Phase.Lost ? UiKit.Rose : Color.white;
        actionText.text = L10n.T(button);
    }

    void Confirm()
    {
        if (leaving || lastConfirm == Time.frameCount || dialogueAge < .3f) return;
        lastConfirm = Time.frameCount;
        Sound.Play("confirm");
        if (paused) { SetPaused(false); return; }
        if (phase == Phase.Intro)
        {
            phase = Phase.Flying;
            dialogPanel.gameObject.SetActive(false);
            Sound.Play("whistle");
        }
        else if (phase == Phase.Lost)
        {
            leaving = true;
            if (Story.Active && Story.AscentPending)
            {
                Story.RetryAscent();
                Wipe.Load("historiaDialogos");
            }
            else Wipe.Load("ascenso");
        }
        else if (phase == Phase.Arrived)
        {
            leaving = true;
            if (Story.Active && Story.AscentPending)
            {
                Story.PassAscent();
                Wipe.Load("historiaDialogos");
            }
            else Wipe.Load("sandScenario");
        }
    }

    void SetPaused(bool value)
    {
        paused = value;
        flightAudio.Pause(value);
        Sound.Play("Pause");
        if (paused) ShowDialog("ascent.pause.text", "ascent.resume");
        else dialogPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        flightAudio.Flight(phase == Phase.Flying && !paused && !leaving, elapsed / Mathf.Max(1f, duration));
        if (leaving) return;
        if (!paused && Story.TakeTestAdvance())
        {
            if (phase == Phase.Arrived) { dialogueAge = 1f; Confirm(); }
            else
            {
                phase = Phase.Arrived;
                elapsed = duration;
                flightAudio.Flight(false, 1f);
                backdrop.Refresh(1f, 0f);
                foreach (var renderer in trainRenderers) renderer.enabled = true;
                flash.color = flash.bottom = Color.clear;
                ShowDialog("ascent.arrived", "quiz.ok");
            }
            return;
        }
        float dt = Time.unscaledDeltaTime;
        dialogueAge += dt;
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        bool cancel = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
        bool pause = (pad != null && pad.startButton.wasPressedThisFrame) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
        if (phase == Phase.Flying && (pause || (paused && cancel))) { SetPaused(!paused); return; }
        bool accept = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if ((phase != Phase.Flying || paused) && accept) { Confirm(); return; }
        if (phase != Phase.Flying || paused) return;
        elapsed += dt;
        immunity = Mathf.Max(0f, immunity - dt);
        flashTime = Mathf.Max(0f, flashTime - dt);
        flash.color = flash.bottom = new Color(1f, .15f, .2f, flashTime * .8f);
        foreach (var renderer in trainRenderers) renderer.enabled = immunity <= 0f || Mathf.FloorToInt(immunity * 12f) % 2 == 0;
        Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        flightAudio.Flight(true, elapsed / Mathf.Max(1f, duration), input.x);
        Vector3 position = train.position + (Vector3)(input * (12.5f * dt));
        float edge = Mathf.Min(14f, view.orthographicSize * view.aspect - trainHalf.x - .6f);
        position.x = Mathf.Clamp(position.x, -edge, edge);
        position.y = Mathf.Clamp(position.y, -6f, 1f);
        train.position = position;
        // Small bank gives steering feedback while the nose continues pointing upward.
        train.rotation = Quaternion.Euler(0f, input.x * -9f, input.x * -6f);
        exhaust.localScale = new Vector3(1f, 1f + .12f * Mathf.Sin(elapsed * 32f), 1f);
        backdrop.Refresh(elapsed / Mathf.Max(1f, duration), dt * 1.4f);
        progressFill.rectTransform.sizeDelta = new Vector2(300f * Mathf.Clamp01(elapsed / duration), 7f);
        spawnIn -= dt;
        if (spawnIn <= 0f && elapsed < duration - 5f)
        {
            spawnIn = SpawnWave(edge);
        }
        for (int i = debris.Count - 1; i >= 0; i--)
        {
            var piece = debris[i];
            Vector3 before = piece.body.position;
            piece.body.position += new Vector3(piece.drift, -piece.speed, 0f) * dt;
            piece.body.Rotate(0f, 0f, piece.spin * dt);
            Vector3 after = piece.body.position;
            Vector3 half = piece.renderer.bounds.extents * .78f;
            // Swept vertical interval avoids missed impacts on slow frames; z is decorative.
            bool collision = Mathf.Abs(after.x - train.position.x) < half.x + trainHalf.x &&
                after.y - half.y <= train.position.y + trainHalf.y && before.y + half.y >= train.position.y - trainHalf.y;
            if (collision && immunity <= 0f)
            {
                Destroy(piece.body.gameObject);
                debris.RemoveAt(i);
                Hit();
                if (phase != Phase.Flying) break;
            }
            else
            {
                float distance = after.x - train.position.x;
                if (before.y > train.position.y && after.y <= train.position.y && Mathf.Abs(distance) < 5.5f)
                    flightAudio.NearMiss(distance);
                if (after.y < -13f) { Destroy(piece.body.gameObject); debris.RemoveAt(i); }
            }
        }
        RefreshStatus();
        if (phase == Phase.Flying && elapsed >= duration)
        {
            phase = Phase.Arrived;
            foreach (var renderer in trainRenderers) renderer.enabled = true;
            flash.color = flash.bottom = Color.clear;
            ShowDialog("ascent.arrived", "quiz.ok");
        }
    }

    void Hit()
    {
        hits++;
        immunity = 1.1f;
        flashTime = .35f;
        flightAudio.Hit();
        if (hits < 3) return;
        phase = Phase.Lost;
        ShowDialog("ascent.lost", "quiz.ok");
    }

    void RefreshStatus()
    {
        status.text = L10n.T("ascent.hits", hits);
        status.color = hits >= 2 ? UiKit.Rose : UiKit.Ice;
        for (int i = 0; i < collisionPips.Length; i++) collisionPips[i].color = collisionPips[i].bottom = i < hits ? UiKit.Rose : UiKit.Mint;
        altitude.text = L10n.T("ascent.altitude", Mathf.RoundToInt(Mathf.Clamp01(elapsed / duration) * 100f));
    }

    float SpawnWave(float edge)
    {
        float difficulty = Mathf.Clamp01(elapsed / duration);
        float speed = Mathf.Lerp(14f, 22f, difficulty);
        const int lanes = 7;
        float laneWidth = edge * 2f / lanes;
        int pattern;
        do pattern = Random.Range(0, elapsed >= nextBarrier ? 4 : 3);
        while (pattern == lastPattern);
        lastPattern = pattern;
        if (pattern == 3)
        {
            // A partial formation leaves TWO two-lane passages, instead of one compulsory hole.
            bool mirror = Random.value < .5f;
            foreach (int original in new[] { 0, 1, 4 })
            {
                int lane = mirror ? lanes - 1 - original : original;
                Spawn(-edge + (lane + .5f) * laneWidth, speed, 0f, laneWidth * .78f, true);
            }
            nextBarrier = elapsed + Random.Range(6f, 9f);
            // Let this formation pass before launching the next threat.
            return 1.4f;
        }
        if (pattern == 2)
        {
            // An occasional aimed piece asks for a dodge without filling the rest of the screen.
            float target = Mathf.Clamp(train.position.x + Random.Range(-.5f, .5f), -edge, edge);
            float start = Mathf.Clamp(target + Random.Range(-3.5f, 3.5f), -edge, edge);
            float fallTime = (12f - train.position.y) / speed;
            Spawn(start, speed, (target - start) / fallTime);
            return Mathf.Lerp(1f, .8f, difficulty);
        }
        int first = Random.Range(0, lanes);
        float drift = pattern == 1 ? (Random.value < .5f ? -2f : 2f) : 0f;
        for (int i = 0; i < 3; i++)
        {
            int lane = (first + i * 2) % lanes;
            Spawn(-edge + (lane + .5f) * laneWidth, speed * Random.Range(.9f, 1.05f), drift);
        }
        return Mathf.Lerp(.85f, .6f, difficulty) * Random.Range(.9f, 1.15f);
    }

    void Spawn(float x, float speed, float drift, float wallWidth = 0f, bool wall = false)
    {
        int kind = wall ? Random.Range(2, 4) : Random.Range(0, obstacleModels.Length);
        var root = new GameObject("Falling_" + obstacleModels[kind].name).transform;
        var model = Instantiate(obstacleModels[kind], root).transform;
        model.localRotation = Quaternion.Euler(-16f, -18f, kind == 2 ? -7f : Random.Range(-15f, 15f));
        foreach (var renderer in model.GetComponentsInChildren<Renderer>()) TrainSim.Toon(renderer);
        Bounds bounds = BoundsOf(model);
        float width = wall ? wallWidth : kind == 2 ? 4f : kind == 3 ? 3.2f : 2.1f;
        model.localScale *= width / Mathf.Max(.01f, bounds.size.x);
        bounds = BoundsOf(model);
        model.position -= bounds.center;
        root.position = new Vector3(x, 12f, 0f);
        debris.Add(new Debris {
            body = root, renderer = model.GetComponentInChildren<Renderer>(),
            speed = speed, drift = drift,
            spin = wall ? 0f : kind == 2 ? Random.Range(-8f, 8f) : Random.Range(-35f, 35f)
        });
    }

    void OnDisable() { if (flightAudio) flightAudio.Flight(false, 0f); }

    void OnDestroy()
    {
        foreach (var mat in materials) if (mat) Destroy(mat);
        foreach (var mesh in meshes) if (mesh) Destroy(mesh);
        if (look && look.profile) Destroy(look.profile);
    }
}
