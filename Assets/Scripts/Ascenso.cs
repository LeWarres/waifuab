using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Standalone flight challenge. Story keeps chapter 2 (space) pending until the flight is won.
public class Ascenso : MonoBehaviour
{
    enum Phase { Intro, Flying, Lost, Arrived }
    public float duration = 55f;
    class Debris { public Transform body; public Vector2 half; public float speed, spin; }
    readonly List<Debris> debris = new();
    readonly List<Material> materials = new();
    Phase phase;
    Camera view;
    Transform train, exhaust;
    Renderer[] trainRenderers;
    Vector2 trainHalf;
    AscentBackdrop backdrop;
    UnityEngine.UI.Text status, altitude, dialogue, speaker, actionText;
    Slanted dialogPanel, progressFill, flash;
    Canvas hud;
    InputAction move;
    float elapsed, spawnIn = 1.2f, immunity, flashTime, dialogueAge;
    int hits, lastConfirm = -1;
    bool paused, leaving;
    Material metal, dark, blue, gold, rust, flame;
    UnityEngine.Rendering.Volume look;

    void Awake()
    {
        Time.timeScale = 1f;
        Display.Apply();
        view = Camera.main;
        InputSystem.actions.Enable();
        move = InputSystem.actions.FindAction("Player/Move", true);
        BuildWorld();
        BuildUI();
        ShowDialog("ascent.intro", "ascent.begin");
        RefreshStatus();
    }

    void BuildWorld()
    {
        metal = Material(new Color(.65f, .73f, .82f));
        dark = Material(new Color(.14f, .19f, .29f));
        blue = Material(new Color(.17f, .4f, .7f));
        gold = Material(new Color(.95f, .71f, .2f));
        rust = Material(new Color(.62f, .34f, .25f));
        flame = Material(new Color(.35f, .85f, 1f));
        look = new GameObject("FlightExposure").AddComponent<UnityEngine.Rendering.Volume>();
        look.isGlobal = true;
        look.priority = 10f;
        var exposure = look.profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);
        exposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed;
        exposure.fixedExposure.value = 13.7f;

        train = new GameObject("FlyingLocomotive").transform;
        var model = Locomotives.Create(train, Locomotives.Selected);
        // Models point along -X. Tilt the roof toward the camera, then point the nose upward.
        model.localRotation = Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(-24f, 0f, 0f);
        Bounds bounds = BoundsOf(model);
        model.localScale *= 3.5f / bounds.size.y;
        bounds = BoundsOf(model);
        model.position -= bounds.center;
        trainHalf = new Vector2(bounds.size.x * .36f, 1.38f);
        train.position = new Vector3(0f, -4.5f, 0f);
        trainRenderers = model.GetComponentsInChildren<Renderer>();
        exhaust = Part(train, new Vector3(0f, -2.2f, .2f), new Vector3(.35f, 1.25f, .3f), flame);
        Part(train, new Vector3(0f, -1.9f, .15f), new Vector3(.18f, .6f, .32f), metal);

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
    }

    Material Material(Color color)
    {
        var mat = TrainSim.Mat(color);
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

    static Transform Part(Transform parent, Vector3 at, Vector3 size, Material material)
        => TrainSim.Box(parent, at, size, material);

    void BuildUI()
    {
        hud = UiKit.NewCanvas("AscentHUD", 20);
        var top = UiKit.Group(hud.transform, new Vector2(.5f, 1f), new Vector2(0f, -78f));
        UiKit.Panel(top, Vector2.zero, new Vector2(1140f, 110f), UiKit.Navy, UiKit.Navy);
        status = Label(top, 28, new Vector2(-250f, 20f), new Vector2(560f, 48f));
        altitude = Label(top, 26, new Vector2(310f, 20f), new Vector2(450f, 48f));
        UiKit.Panel(top, new Vector2(0f, -26f), new Vector2(1050f, 12f), UiKit.Slate, UiKit.Slate, 0f);
        progressFill = UiKit.Panel(top, new Vector2(-525f, -26f), new Vector2(0f, 12f), UiKit.Sky, UiKit.Sky, 0f);
        progressFill.rectTransform.pivot = new Vector2(0f, .5f);
        var bottom = UiKit.Group(hud.transform, new Vector2(.5f, 0f), new Vector2(0f, 32f));
        Label(bottom, 22, Vector2.zero, new Vector2(1700f, 42f)).text = L10n.T("ascent.controls");
        flash = UiKit.Panel(hud.transform, Vector2.zero, Vector2.zero, Color.clear, Color.clear, 0f);
        flash.rectTransform.anchorMin = Vector2.zero;
        flash.rectTransform.anchorMax = Vector2.one;
        flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        dialogPanel = UiKit.Panel(hud.transform, new Vector2(0f, -280f), new Vector2(1420f, 330f), UiKit.Navy, UiKit.Navy, .035f);
        speaker = Label(dialogPanel.transform, 28, new Vector2(0f, 118f), new Vector2(1280f, 42f));
        speaker.color = UiKit.Sky;
        dialogue = Label(dialogPanel.transform, 30, new Vector2(0f, 25f), new Vector2(1290f, 130f));
        var action = UiKit.Panel(dialogPanel.transform, new Vector2(0f, -110f), new Vector2(420f, 58f), UiKit.Sky, UiKit.Sky);
        action.raycastTarget = true;
        action.gameObject.AddComponent<Clicks>().click = _ => Confirm();
        actionText = Label(action.transform, 25, Vector2.zero, new Vector2(390f, 48f));
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
                SceneManager.LoadScene("historiaDialogos");
            }
            else SceneManager.LoadScene("ascenso");
        }
        else if (phase == Phase.Arrived)
        {
            leaving = true;
            if (Story.Active && Story.AscentPending) Story.PassAscent();
            SceneManager.LoadScene("sandScenario");
        }
    }

    void SetPaused(bool value)
    {
        paused = value;
        Sound.Play("Pause");
        if (paused) ShowDialog("ascent.pause.text", "ascent.resume");
        else dialogPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (leaving) return;
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
        Vector3 position = train.position + (Vector3)(input * (10f * dt));
        float edge = Mathf.Min(14f, view.orthographicSize * view.aspect - trainHalf.x - .6f);
        position.x = Mathf.Clamp(position.x, -edge, edge);
        position.y = Mathf.Clamp(position.y, -6f, 1f);
        train.position = position;
        // Small bank gives steering feedback while the nose continues pointing upward.
        train.rotation = Quaternion.Euler(0f, input.x * -9f, input.x * -6f);
        exhaust.localScale = new Vector3(.35f, 1.1f + .25f * Mathf.Sin(elapsed * 40f), .3f);
        backdrop.flight = elapsed / Mathf.Max(1f, duration);
        backdrop.travel += dt;
        backdrop.SetVerticesDirty();
        progressFill.rectTransform.sizeDelta = new Vector2(1050f * Mathf.Clamp01(elapsed / duration), 12f);
        spawnIn -= dt;
        if (spawnIn <= 0f && elapsed < duration - 5f)
        {
            Spawn(edge);
            spawnIn = Mathf.Lerp(.85f, .48f, elapsed / duration);
        }
        for (int i = debris.Count - 1; i >= 0; i--)
        {
            var piece = debris[i];
            Vector3 before = piece.body.position;
            piece.body.position += Vector3.down * (piece.speed * dt);
            piece.body.Rotate(0f, 0f, piece.spin * dt);
            Vector3 after = piece.body.position;
            // Swept vertical interval avoids missed impacts on slow frames; z is decorative.
            bool collision = Mathf.Abs(after.x - train.position.x) < piece.half.x + trainHalf.x &&
                after.y - piece.half.y <= train.position.y + trainHalf.y && before.y + piece.half.y >= train.position.y - trainHalf.y;
            if (collision && immunity <= 0f)
            {
                Destroy(piece.body.gameObject);
                debris.RemoveAt(i);
                Hit();
                if (phase != Phase.Flying) break;
            }
            else if (after.y < -13f) { Destroy(piece.body.gameObject); debris.RemoveAt(i); }
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
        immunity = 1.5f;
        flashTime = .35f;
        Sound.Play("vagonroto");
        if (hits < 3) return;
        phase = Phase.Lost;
        ShowDialog("ascent.lost", "quiz.ok");
    }

    void RefreshStatus()
    {
        status.text = L10n.T("ascent.hits", hits);
        status.color = hits >= 2 ? UiKit.Rose : UiKit.Ice;
        altitude.text = L10n.T("ascent.altitude", Mathf.RoundToInt(Mathf.Clamp01(elapsed / duration) * 100f));
    }

    void Spawn(float edge)
    {
        var root = new GameObject("FallingDebris").transform;
        int kind = Random.Range(0, 3);
        if (kind == 0) // Torn panels and tangled metal.
        {
            Part(root, Vector3.zero, new Vector3(1.1f, .7f, .45f), rust).localRotation = Quaternion.Euler(15f, 12f, 24f);
            Part(root, new Vector3(.2f, .15f, -.3f), new Vector3(1.6f, .16f, .18f), metal).localRotation = Quaternion.Euler(0f, 0f, -32f);
            Part(root, new Vector3(-.3f, -.3f, -.15f), new Vector3(.22f, .9f, .2f), dark);
        }
        else if (kind == 1) // Discarded canister.
        {
            Part(root, Vector3.zero, new Vector3(.85f, 1.2f, .65f), metal);
            Part(root, new Vector3(0f, .4f, -.35f), new Vector3(.9f, .16f, .1f), gold);
            Part(root, new Vector3(0f, -.4f, -.35f), new Vector3(.9f, .16f, .1f), gold);
            Part(root, new Vector3(0f, .7f, 0f), new Vector3(.4f, .2f, .4f), dark);
        }
        else // Satellite with recognizable solar arrays and antenna.
        {
            Part(root, Vector3.zero, new Vector3(.8f, 1f, .65f), gold);
            Part(root, Vector3.zero, new Vector3(3.3f, .13f, .15f), metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, new Vector3(side * 1.2f, 0f, 0f), new Vector3(1.05f, 1.35f, .1f), dark);
                for (int row = 0; row < 3; row++)
                    Part(root, new Vector3(side * 1.2f, -.43f + row * .43f, -.08f), new Vector3(.92f, .34f, .05f), blue);
            }
            Part(root, new Vector3(0f, .8f, 0f), new Vector3(.12f, .7f, .12f), metal);
        }
        root.position = new Vector3(Random.Range(-edge + 1f, edge - 1f), 12f, 0f);
        // Satellites stay nearly upright so their collision width matches the visible wings.
        debris.Add(new Debris { body = root, half = kind == 2 ? new Vector2(1.65f, 1.1f) : new Vector2(.72f, .75f), speed = Mathf.Lerp(7f, 12f, elapsed / duration), spin = kind == 2 ? 0f : Random.Range(-55f, 55f) });
    }

    void OnDestroy()
    {
        foreach (var mat in materials) if (mat) Destroy(mat);
        if (look && look.profile) Destroy(look.profile);
    }
}
