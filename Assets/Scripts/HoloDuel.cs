using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A chapter's finale as a board game (Story.Part.holo), played in a room of the alien base of its own, away from the
// track: a holographic board on a round table between the hero and the rival, a centre and two rings of six, and three
// pieces a side, one of each kind, standing as glowing holograms, two lives each. A turn moves one piece one cell; into
// a rival's cell is an attack: both roll a die, the kind that has the edge over the other adds two (the beast over the
// warrior, the warrior over the mage, the mage over the beast), and the higher takes a life off the other; a tie,
// nobody. Out of lives, a piece leaves the board, and the one that beat it takes its cell. The one that takes all three
// of the other's wins. The rival plays an easy game: it attacks where the odds are with it, keeps clear of what has the
// edge over it, and now and then just moves. Outside, time stands still.
public class HoloDuel : MonoBehaviour
{
    public float rivalThinks = 0.7f, slide = 0.35f, mistakes = 0.5f, boldness = 6f; // set from Tools/holo_sim.py: who follows the green wins about 1 in 2, 3 in 4 with the second try; boldness keeps the rival attacking, so games do not stall
    public int turnLimit = 60; // moves in all; then the side with more lives left wins (a tie is a loss: play it again)
    const int Lives = 2, Edge = 2; // each piece's lives; what the kind with the edge adds to its roll

    public bool Active { get; private set; }

    const int Cells = 13;                 // 0 the centre, 1..6 the inner ring, 7..12 the outer
    const float Inner = 1.6f, Outer = 3.2f, Top = 1.12f; // the rings' radii and the board's height, in metres
    static readonly Vector3 Room = new Vector3(-12000f, 0f, 0f);
    static readonly string[] KindModel = { "Enemy_wolf", "Enemy_guardian", "Enemy_ghost" }; // beast, warrior, mage
    class Piece { public int kind, cell, lives = Lives; public bool mine; public Transform body, hologram; public Transform[] pips; public Vector3 from, nudge, face; public float movedAt = -1f, goneAt = -1f; } // nudge: off its cell in a fight; face: turned to its foe

    static readonly Color Mine = new Color(0.3f, 0.95f, 1f), Theirs = new Color(1f, 0.35f, 0.7f);
    static readonly Color[] KindColor = { new Color(1f, 0.42f, 0.08f), new Color(0.2f, 0.45f, 1f), new Color(0.62f, 0.22f, 1f) };
    static readonly int MainColor = Shader.PropertyToID("_MainColor");

    readonly List<Piece> pieces = new();
    readonly List<Piece> leaving = new(); // beaten, flickering out
    readonly Renderer[] cellLook = new Renderer[Cells];
    MaterialPropertyBlock tint;
    Transform room;
    Camera view;
    Canvas board;
    Text title, turnText, message, herName, rivalName, info, left;
    Prompt help;
    BrawlEvent.Rig rivalRig;
    System.Action<bool> done;
    int cursor, picked = -1, moves;
    bool myTurn, over, result;
    float rivalAt, endAt, messageUntil, held;
    Vector2 lastMouse;
    // a roll on: who attacks whom, the two dice, since when; and the two dice drawn on the screen
    Piece rollBy, rollOn;
    int rollA, rollB;
    float rollFrom = -1f;
    RectTransform dice;
    readonly Image[][] diePips = new Image[2][];
    readonly Text[] dieName = new Text[2], dieEdge = new Text[2];
    Text dieSum;

    // ---------------- the board ----------------

    static Vector3 CellAt(int c)
    {
        if (c == 0) return Room + Vector3.up * Top;
        int k = (c - 1) % 6;
        float r = c <= 6 ? Inner : Outer, a = (90f - k * 60f) * Mathf.Deg2Rad;
        return Room + new Vector3(Mathf.Cos(a) * r, Top, Mathf.Sin(a) * r);
    }

    // The cells one step from c: the centre touches the inner ring, each ring cell its two neighbours round it and
    // the cell in line with it on the other ring (or the centre).
    static IEnumerable<int> Next(int c)
    {
        if (c == 0) { for (int k = 1; k <= 6; k++) yield return k; yield break; }
        int k6 = (c - 1) % 6, ring = c <= 6 ? 1 : 7;
        yield return ring + (k6 + 1) % 6;
        yield return ring + (k6 + 5) % 6;
        yield return ring == 1 ? c + 6 : c - 6;
        if (ring == 1) yield return 0;
    }

    // What the attacker's kind adds to its roll against the defender's: the edge, the edge against it, or nothing.
    static int EdgeOf(int attacker, int defender) => (attacker + 1) % 3 == defender ? Edge : (defender + 1) % 3 == attacker ? -Edge : 0;

    // The attacker's chances of taking a life, of a tie and of losing one, over the 36 rolls.
    static (float win, float tie, float lose) Odds(int attacker, int defender)
    {
        int win = 0, tie = 0;
        for (int a = 1; a <= 6; a++)
            for (int d = 1; d <= 6; d++)
            {
                int s = a + EdgeOf(attacker, defender) - d;
                if (s > 0) win++; else if (s == 0) tie++;
            }
        return (win / 36f, tie / 36f, (36 - win - tie) / 36f);
    }

    static Material Glow(Color c, float power = 2.2f) => TrainSim.Mat(new Color(c.r * power, c.g * power, c.b * power), false);

    Transform Shape(PrimitiveType type, Transform parent, Vector3 at, Vector3 size, Material m, Quaternion? turn = null)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        go.transform.rotation = turn ?? Quaternion.identity;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    // The bands the board's tiles lie in: the centre's edge, the inner ring's outer edge, the board's edge.
    static readonly float[] Bands = { 0.8f, 2.4f, 4.0f };
    readonly Color[] cellShade = new Color[Cells];

    // A board tile: the sector between two radii and two angles (degrees, counter-clockwise from +x seen from above),
    // `height` thick, its top and every side, flat-shaded; a disc with r0 = 0 and the whole turn.
    Transform Tile(Vector3 at, float r0, float r1, float a0, float a1, float height, Material m)
    {
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var tris = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int i = v.Count;
            v.AddRange(new[] { a, b, c, d });
            for (int k = 0; k < 4; k++) n.Add(normal);
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f;
            tris.AddRange(flip ? new[] { i, i + 2, i + 1, i, i + 3, i + 2 } : new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        Vector3 P(float r, float deg, float y) => new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad) * r, y, Mathf.Sin(deg * Mathf.Deg2Rad) * r);
        int steps = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / 5f));
        for (int s = 0; s < steps; s++)
        {
            float t0 = Mathf.Lerp(a0, a1, s / (float)steps), t1 = Mathf.Lerp(a0, a1, (s + 1f) / steps), tm = (t0 + t1) * 0.5f;
            Quad(P(r0, t0, height), P(r0, t1, height), P(r1, t1, height), P(r1, t0, height), Vector3.up);
            Quad(P(r1, t0, height), P(r1, t1, height), P(r1, t1, 0f), P(r1, t0, 0f), P(1f, tm, 0f));
            if (r0 > 0f) Quad(P(r0, t0, height), P(r0, t1, height), P(r0, t1, 0f), P(r0, t0, 0f), -P(1f, tm, 0f));
        }
        if (a1 - a0 < 359f)
        {
            Quad(P(r0, a0, height), P(r1, a0, height), P(r1, a0, 0f), P(r0, a0, 0f), P(1f, a0 - 90f, 0f));
            Quad(P(r0, a1, height), P(r1, a1, height), P(r1, a1, 0f), P(r0, a1, 0f), P(1f, a1 + 90f, 0f));
        }
        var mesh = new Mesh { name = "Tile" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, v.ConvertAll(p => new Vector2(p.x + p.y, p.z) * 0.35f)); // the stone's grain laid across the board
        mesh.SetNormals(n);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        var go = new GameObject("Tile", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(room, false);
        go.transform.position = at;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        return go.transform;
    }

    // A ring of glowing segments lying on the board.
    void Ring(float radius, float height, Material m, int segments = 48, float thick = 0.06f)
    {
        float length = 2f * Mathf.PI * radius / segments * 1.02f;
        for (int s = 0; s < segments; s++)
        {
            float a = s * 2f * Mathf.PI / segments;
            Vector3 at = Room + new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
            Shape(PrimitiveType.Cube, room, at, new Vector3(thick, thick * 0.6f, length), m, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a))));
        }
    }

    // The room: a dark floor in panels, a round table in the middle with the hologram on it, pillars with light
    // strips round the walls, the rival across the table; and the camera from the hero's side,
    // shoulder, looking down on the board.
    void BuildRoom()
    {
        room = new GameObject("HoloDuelRoom").transform;
        room.position = Room;
        Material floor = Interior.Textured(Interior.Panels(), new Color(0.12f, 0.13f, 0.18f), new Vector2(10f, 10f));
        Interior.Box(room, new Vector3(0f, -0.25f, 0f), new Vector3(44f, 0.5f, 44f), floor);
        Material metal = TrainSim.Mat(new Color(0.16f, 0.17f, 0.22f)), dark = TrainSim.Mat(new Color(0.05f, 0.06f, 0.09f));
        // neon: bright enough to glow, not so bright the bloom smears it
        Material cyan = Glow(Mine, 1.8f), dim = Glow(Mine, 0.5f), pink = Glow(Theirs, 1.6f);
        // the table: a stepped plinth, a dark top, a lit rim; and a glowing floor ring round it
        Shape(PrimitiveType.Cylinder, room, Room + Vector3.up * 0.15f, new Vector3(9.6f, 0.15f, 9.6f), metal);
        Shape(PrimitiveType.Cylinder, room, Room + Vector3.up * 0.55f, new Vector3(4f, 0.4f, 4f), metal);
        Shape(PrimitiveType.Cylinder, room, Room + Vector3.up * 1f, new Vector3(9.4f, 0.08f, 9.4f), dark);
        Ring(4.72f, 1.06f, cyan, 72, 0.08f); // the table's lit edge
        Ring(5.2f, 0.32f, dim, 64, 0.12f);
        // the board: a round chessboard of stone tiles, light and dark by turns (the centre, then each ring's six
        // sectors, every tile against tiles of the other shade), set in a gold frame with gold inlay between them
        Material gold = TrainSim.Mat(new Color(0.85f, 0.62f, 0.22f)), frame = TrainSim.Mat(new Color(0.09f, 0.08f, 0.12f)), stone = Interior.Textured(Interior.Rock(), Color.white, Vector2.one);
        Tile(Room + Vector3.up * (Top - 0.14f), 0f, 4.45f, 0f, 360f, 0.1f, frame);                  // the frame's dark bed
        Ring(4.38f, Top - 0.03f, gold, 96, 0.1f);                                                    // its gold rim
        for (int c = 0; c < Cells; c++)
        {
            bool light = c == 0 || (c <= 6 ? (c - 1) % 2 == 0 : (c - 7) % 2 == 1);
            cellShade[c] = c == 0 ? new Color(0.42f, 0.3f, 0.12f) : light ? new Color(0.62f, 0.66f, 0.76f) : new Color(0.1f, 0.12f, 0.2f); // the centre in bronze, touching both shades
            float r0 = c == 0 ? 0f : c <= 6 ? Bands[0] : Bands[1], r1 = c == 0 ? Bands[0] : c <= 6 ? Bands[1] : Bands[2];
            float mid = c == 0 ? 0f : 90f - (c - 1) % 6 * 60f;
            cellLook[c] = Tile(Room + Vector3.up * (Top - 0.08f), r0, r1, mid - 30f, mid + 30f, 0.08f, stone).GetComponent<Renderer>();
        }
        // the inlay: a gold line round each band and along each sector's edge, standing proud of the tiles
        foreach (float r in Bands) Ring(r, Top + 0.005f, gold, r < 1f ? 32 : 72, 0.05f);
        for (int k = 0; k < 6; k++)
        {
            float a = (30f + k * 60f) * Mathf.Deg2Rad;
            Vector3 way = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Shape(PrimitiveType.Cube, room, Room + Vector3.up * (Top + 0.005f) + way * (Bands[0] + Bands[2]) * 0.5f, new Vector3(0.05f, 0.03f, Bands[2] - Bands[0]), gold, Quaternion.LookRotation(way));
        }
        // and the hologram's glow round it all: a cyan halo just outside the frame
        Ring(4.55f, Top - 0.05f, cyan, 96, 0.05f);
        // the walls' pillars, each with a strip of light, the near ones in cyan, the far ones in pink
        for (int k = 0; k < 10; k++)
        {
            float a = k * 36f * Mathf.Deg2Rad + 0.3f;
            Vector3 at = Room + new Vector3(Mathf.Cos(a) * 14f, 0f, Mathf.Sin(a) * 14f);
            Shape(PrimitiveType.Cube, room, at + Vector3.up * 4f, new Vector3(1.4f, 8f, 1.4f), metal, Quaternion.LookRotation(Room - at));
            Shape(PrimitiveType.Cube, room, at + (Room - at).normalized * 0.72f + Vector3.up * 4f, new Vector3(0.2f, 7f, 0.05f), at.z > Room.z ? pink : cyan, Quaternion.LookRotation(Room - at));
        }
        Interior.Lamp(room, new Vector3(0f, 6f, 0f), new Color(0.5f, 0.9f, 1f), 9000f, 18f, false);
        Interior.Lamp(room, new Vector3(0f, 3f, 8f), new Color(1f, 0.4f, 0.75f), 4000f, 12f, false);

        // the rival across the table, at rest
        var clips = Resources.Load<BrawlClips>("Brawl/BrawlClips");
        rivalRig = Stand(Locomotives.HeroId(Story.Active ? Story.Current.rival : 7), Room + new Vector3(0f, 0f, 5.4f), clips);

        view = new GameObject("HoloDuelCamera").AddComponent<Camera>();
        view.transform.SetParent(room, false);
        view.fieldOfView = 50f;
        view.nearClipPlane = 0.2f;
        view.farClipPlane = 200f;
        view.depth = 10f;
        view.clearFlags = CameraClearFlags.SolidColor;
        var hd = view.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        hd.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.01f, 0.012f, 0.03f);
        view.transform.position = Room + new Vector3(1.6f, 10.6f, -10.2f);
        view.transform.LookAt(Room + new Vector3(2.3f, Top, -0.2f)); // the board on the left of the picture, the rules on the right
    }

    BrawlEvent.Rig Stand(string id, Vector3 at, BrawlClips clips)
    {
        var root = new GameObject(id + "AtTheTable").transform;
        root.SetParent(room, false);
        root.position = at;
        root.rotation = Quaternion.LookRotation(new Vector3(Room.x - at.x, 0f, Room.z - at.z));
        string who = "Characters/" + id + "/" + id;
        Animator body = BrawlEvent.Body(Resources.Load<GameObject>(who), root, 1.8f);
        Player.Skin(body.transform, who);
        var rig = new BrawlEvent.Rig(root, body);
        if (clips) rig.Play(clips.idle, true);
        return rig;
    }

    // A piece: a disc in its side's colour, and over it its kind's model as a hologram in its kind's colour.
    Piece Make(int kind, int cell, bool mine)
    {
        var p = new Piece { kind = kind, cell = cell, mine = mine };
        p.body = new GameObject("HoloPiece").transform;
        p.body.SetParent(room, false);
        p.body.position = CellAt(cell);
        Shape(PrimitiveType.Cylinder, p.body, p.body.position + Vector3.up * 0.02f, new Vector3(0.78f, 0.02f, 0.78f), Glow(mine ? Mine : Theirs, 1.6f));
        Mesh mesh = Resources.Load<Mesh>("Models/" + KindModel[kind]);
        var model = new GameObject("Hologram", typeof(MeshFilter), typeof(MeshRenderer)).transform;
        model.SetParent(p.body, false);
        model.GetComponent<MeshFilter>().sharedMesh = mesh;
        model.GetComponent<MeshRenderer>().sharedMaterial = Glow(KindColor[kind], 0.9f); // not so bright the bloom washes its colour out
        float tall = mesh ? Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.x * 0.8f) : 1f;
        model.localScale = Vector3.one * (1.2f / Mathf.Max(0.01f, tall));
        model.localPosition = Vector3.up * (0.08f - (mesh ? mesh.bounds.min.y : 0f) * model.localScale.y);
        model.localRotation = Quaternion.Euler(0f, mine ? 0f : 180f, 0f); // facing the other side
        p.hologram = model;
        // its lives: little lights in its side's colour floating over it
        p.pips = new Transform[Lives];
        for (int l = 0; l < Lives; l++)
            p.pips[l] = Shape(PrimitiveType.Sphere, p.body, p.body.position + new Vector3((l - (Lives - 1) * 0.5f) * 0.3f, 1.55f, 0f), Vector3.one * 0.18f, Glow(mine ? Mine : Theirs, 1.8f));
        return p;
    }

    // The words round it on the screen: the title, whose turn, what happened, the names, the rules and the buttons.
    void BuildBoard()
    {
        board = UiKit.NewCanvas("HoloDuel", 6);
        title = UiKit.Label(board.transform, UiKit.Display, 64, Color.white, TextAnchor.MiddleCenter, new Vector2(-180f, 465f), new Vector2(1200f, 80f), true);
        turnText = UiKit.Label(board.transform, UiKit.Display, 34, Mine, TextAnchor.MiddleCenter, new Vector2(-180f, 405f), new Vector2(1000f, 50f), true);
        message = UiKit.Label(board.transform, UiKit.Display, 40, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(-180f, -412f), new Vector2(1200f, 56f), true);
        foreach (var line in new[] { title, turnText, message }) line.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        herName = UiKit.Label(board.transform, UiKit.Display, 32, Mine, TextAnchor.MiddleCenter, new Vector2(-180f, -372f), new Vector2(800f, 44f), true);
        herName.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        rivalName = UiKit.Label(board.transform, UiKit.Display, 32, Theirs, TextAnchor.MiddleCenter, new Vector2(-180f, 345f), new Vector2(800f, 44f), true);
        var rules = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(660f, 40f), new Vector2(500f, 560f), UiKit.Navy, UiKit.Navy, 0.04f), 4f, 9f);
        UiKit.Label(rules.transform, UiKit.Display, 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 230f), new Vector2(460f, 50f), true).text = L10n.T("holo.rules.title").ToUpperInvariant();
        for (int kind = 0; kind < 3; kind++)
        {
            float y = 168f - kind * 70f;
            var dot = UiKit.Panel(rules.transform, new Vector2(-170f, y), Vector2.one * 60f, KindColor[kind], Color.Lerp(KindColor[kind], Color.black, 0.25f), 0f);
            dot.sprite = UiKit.Disc();
            UiKit.Label(dot.transform, UiKit.Display, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 2f), Vector2.one * 60f, true).text = L10n.T("holo.kind." + kind).Substring(0, 1).ToUpperInvariant();
            UiKit.Label(rules.transform, UiKit.Body, 24, Color.white, TextAnchor.MiddleLeft, new Vector2(40f, y), new Vector2(330f, 60f), true).text =
                L10n.T("holo.beats", L10n.T("holo.kind." + kind), L10n.T("holo.kind." + (kind + 1) % 3));
        }
        UiKit.Label(rules.transform, UiKit.Body, 22, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperLeft, new Vector2(0f, -130f), new Vector2(450f, 200f), true).text = L10n.T("holo.rules", turnLimit);
        left = UiKit.Label(rules.transform, UiKit.Display, 28, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(0f, -248f), new Vector2(460f, 40f), true);
        // what the piece under the cursor is and what it beats, in its kind's colour
        info = UiKit.Label(board.transform, UiKit.Display, 28, Color.white, TextAnchor.MiddleCenter, new Vector2(-180f, -448f), new Vector2(1200f, 40f), true);
        info.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        help = Prompt.Make(board.transform, new Vector2(0f, -495f), 22, Color.white); // the buttons drawn
        // the dice: one each side, white with dots, edged in the side's colour, the names and the +2 under them
        var plate = UiKit.Persona(UiKit.Panel(board.transform, new Vector2(-690f, 300f), new Vector2(680f, 300f), UiKit.Navy, UiKit.Navy, 0.04f), 4f, 9f);
        plate.transform.localScale = Vector3.one * DiceSize;
        dice = (RectTransform)plate.transform;
        for (int side = 0; side < 2; side++)
        {
            Vector2 at = new Vector2(side == 0 ? -170f : 170f, 30f);
            UiKit.Panel(plate.transform, at, Vector2.one * 156f, side == 0 ? Mine : Theirs, side == 0 ? Mine : Theirs, 0f);
            UiKit.Panel(plate.transform, at, Vector2.one * 140f, Color.white, new Color(0.85f, 0.88f, 0.95f), 0f);
            diePips[side] = new Image[9];
            for (int i = 0; i < 9; i++)
            {
                var dot = UiKit.Panel(plate.transform, at + new Vector2((i % 3 - 1) * 40f, (1 - i / 3) * 40f), Vector2.one * 28f, UiKit.Navy, UiKit.Navy, 0f);
                dot.sprite = UiKit.Disc();
                diePips[side][i] = dot;
            }
            dieName[side] = UiKit.Label(plate.transform, UiKit.Display, 40, side == 0 ? Mine : Theirs, TextAnchor.MiddleCenter, new Vector2(at.x, -90f), new Vector2(320f, 40f), true);
            dieEdge[side] = UiKit.Label(plate.transform, UiKit.Display, 64, UiKit.Gold, TextAnchor.MiddleCenter, new Vector2(at.x + (side == 0 ? -115f : 115f), 40f), new Vector2(90f, 60f), true);
        }
        dieSum = UiKit.Label(plate.transform, UiKit.Display, 60, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(160f, 60f), true);
        dice.gameObject.SetActive(false);
        board.gameObject.SetActive(false);
    }

    // ---------------- start and end ----------------

    public void Begin(System.Action<bool> done)
    {
        this.done = done;
        if (room) Destroy(room.gameObject);
        rivalRig?.Free();
        BuildRoom();
        if (!board) BuildBoard();
        tint ??= new MaterialPropertyBlock();
        pieces.Clear();
        leaving.Clear();
        // each side on three of its four home cells (its edge of the outer ring and the inner cell in line with the
        // middle of it), its kinds shuffled among them: never quite the same game twice
        int[] mineAt = Shuffled(10, 9, 11, 4), theirsAt = Shuffled(7, 8, 12, 1);
        for (int kind = 0; kind < 3; kind++)
        {
            pieces.Add(Make(kind, mineAt[kind], true));
            pieces.Add(Make(kind, theirsAt[kind], false));
        }
        title.text = L10n.T("holo.title").ToUpperInvariant();
        herName.text = L10n.T(Locomotives.HeroNameKey(Locomotives.Selected)).ToUpperInvariant();
        rivalName.text = Rival.ToUpperInvariant();
        Say(L10n.T("holo.intro", Rival), 3f);
        cursor = mineAt[0];
        picked = -1;
        moves = 0;
        myTurn = true;
        over = false;
        rollFrom = -1f;
        dice.gameObject.SetActive(false);
        Active = true;
        board.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    static int[] Shuffled(params int[] cells)
    {
        for (int i = cells.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (cells[i], cells[j]) = (cells[j], cells[i]); }
        return cells;
    }

    string Rival => L10n.T(Locomotives.HeroNameKey(Story.Active ? Story.Current.rival : 7));

    public void TestWin() { if (Active) Finish(true); }

    void Finish(bool won, bool even = false)
    {
        over = true;
        result = won;
        Say(L10n.T(won ? "holo.won" : even ? "holo.even" : "holo.lost", Rival), 99f);
        Sound.Play(won ? "confirm" : "menuback");
        endAt = Time.unscaledTime + 1.8f;
    }

    void End()
    {
        Active = false;
        board.gameObject.SetActive(false);
        rivalRig?.Free();
        rivalRig = null;
        if (room) Destroy(room.gameObject);
        Time.timeScale = 1f;
        System.Action<bool> finished = done;
        done = null;
        finished?.Invoke(result);
    }

    void OnDestroy()
    {
        rivalRig?.Free();
        if (room) Destroy(room.gameObject);
        if (board) Destroy(board.gameObject);
    }

    // ---------------- play ----------------

    void Update()
    {
        if (!Active) return;
        Time.timeScale = 0f;
        float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        rivalRig?.Tick(dt);
        foreach (Piece p in pieces)
        {
            // sliding to its cell with a hop, turning slowly, flickering a little like any hologram
            float t = p.movedAt < 0f ? 1f : Mathf.Clamp01((now - p.movedAt) / slide);
            float ease = 1f - (1f - t) * (1f - t);
            p.body.position = Vector3.Lerp(p.movedAt < 0f ? CellAt(p.cell) : p.from, CellAt(p.cell), ease) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.5f + p.nudge;
            bool lifted = picked == p.cell && p.mine;
            p.hologram.localPosition = new Vector3(0f, p.hologram.localPosition.y, 0f);
            if (p.face != Vector3.zero) p.hologram.rotation = Quaternion.Slerp(p.hologram.rotation, Quaternion.LookRotation(p.face), 1f - Mathf.Exp(-14f * dt)); // squared up to its foe
            else p.hologram.Rotate(0f, (lifted ? 120f : 25f) * dt, 0f, Space.World);
            float flicker = 1f + (Mathf.PerlinNoise(now * 6f, p.kind + (p.mine ? 0f : 10f)) - 0.5f) * 0.06f;
            p.body.localScale = Vector3.one * flicker * (lifted ? 1.15f + Mathf.Sin(now * 8f) * 0.04f : PieceAt(cursor) == p && myTurn ? 1.08f : 1f);
        }
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var (spark, until) = sparks[i];
            if (!spark || now > until) { if (spark) Destroy(spark.gameObject); sparks.RemoveAt(i); continue; }
            spark.localScale *= 1f + 6f * dt;
        }
        for (int i = leaving.Count - 1; i >= 0; i--)
        {
            Piece p = leaving[i];
            float t = (now - p.goneAt) / 0.5f;
            if (t >= 1f) { Destroy(p.body.gameObject); leaving.RemoveAt(i); continue; }
            p.body.localScale = new Vector3(1f + t, Mathf.Max(0.01f, 1f - t), 1f + t) * ((int)(now * 30f) % 2 == 0 ? 1f : 0.85f); // squashed into the board, crackling
        }
        if (now > messageUntil) message.text = "";
        if (over) { if (now >= endAt) End(); return; }
        if (rollFrom >= 0f) Roll(now);
        else if (myTurn) Steer(now);
        else if (now >= rivalAt) RivalMove();
        Paint(now);
        // whose turn, and on hers what to do now: pick a piece, then where it goes
        turnText.text = rollFrom >= 0f ? L10n.T("holo.rolling") : myTurn ? L10n.T("holo.turn.you") + "  ·  " + L10n.T(picked < 0 ? "holo.pick" : "holo.where") : L10n.T("holo.turn.rival", Rival);
        left.text = L10n.T("holo.left", turnLimit - moves);
        // under the cursor: the odds of the attack there, or what the piece is and what it has the edge over
        Piece under = myTurn && rollFrom < 0f ? PieceAt(cursor) : null;
        if (under != null && !under.mine && picked >= 0 && Legal(picked, cursor, true))
        {
            var (win, tie, lose) = Odds(PieceAt(picked).kind, under.kind);
            info.text = L10n.T("holo.odds", Mathf.RoundToInt(win * 100f), Mathf.RoundToInt(tie * 100f), Mathf.RoundToInt(lose * 100f));
            info.color = win > 0.5f ? new Color(0.5f, 1f, 0.6f) : win > 0.3f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.5f, 0.5f);
        }
        else
        {
            info.text = under == null ? "" : under.mine ? L10n.T("holo.piece.mine", L10n.T("holo.kind." + under.kind), L10n.T("holo.kind." + (under.kind + 1) % 3), under.lives)
                : L10n.T("holo.piece.theirs", L10n.T("holo.kind." + under.kind), L10n.T("holo.kind." + (under.kind + 1) % 3), Rival, under.lives);
            if (under != null) info.color = KindColor[under.kind] * 0.5f + Color.white * 0.5f;
        }
        turnText.color = rollFrom >= 0f ? UiKit.Gold : myTurn ? Mine : Theirs;
        help.Set(L10n.T("holo.help"));
    }

    // The stick or the arrows move the cursor to the cell lying most that way on the screen; the mouse points at
    // one; confirm (or a click) picks a piece of hers, then where it goes; back lets it go.
    void Steer(float now)
    {
        Vector2 stick = Vector2.zero;
        var keys = Keyboard.current;
        if (keys != null) stick = new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                                              (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
        if (stick == Vector2.zero && Gamepad.current != null)
        {
            Vector2 cross = Gamepad.current.dpad.ReadValue();
            stick = cross != Vector2.zero ? cross : Gamepad.current.leftStick.ReadValue();
        }
        if (stick.magnitude > 0.5f)
        {
            if (now >= held)
            {
                held = now + 0.25f;
                Vector2 from = view.WorldToScreenPoint(CellAt(cursor));
                int best = cursor;
                float bestScore = float.MaxValue;
                for (int c = 0; c < Cells; c++)
                {
                    Vector2 to = (Vector2)view.WorldToScreenPoint(CellAt(c)) - from;
                    if (c == cursor || Vector2.Dot(to.normalized, stick.normalized) < 0.5f) continue;
                    float score = to.magnitude * (2f - Vector2.Dot(to.normalized, stick.normalized));
                    if (score < bestScore) { bestScore = score; best = c; }
                }
                if (best != cursor) { cursor = best; Sound.Play("click"); }
            }
        }
        else held = 0f;
        bool ok = (keys != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        bool back = (keys != null && keys.escapeKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 at = mouse.position.ReadValue();
            int under = CellUnder(at);
            if (under >= 0 && (at - lastMouse).sqrMagnitude > 4f) cursor = under; // moved over a cell: the cursor follows it
            lastMouse = at;
            if (mouse.leftButton.wasPressedThisFrame && under >= 0) { cursor = under; ok = true; }
            if (mouse.rightButton.wasPressedThisFrame) back = true;
        }
        if (ok) Choose();
        if (back && picked >= 0) { picked = -1; Sound.Play("menuback"); }
    }

    // The cell the mouse points at on the board, or -1.
    int CellUnder(Vector2 screen)
    {
        Ray ray = view.ScreenPointToRay(screen);
        if (Mathf.Abs(ray.direction.y) < 0.01f) return -1;
        Vector3 hit = ray.GetPoint((Top - ray.origin.y) / ray.direction.y);
        for (int c = 0; c < Cells; c++) if ((hit - CellAt(c)).sqrMagnitude < 0.55f * 0.55f) return c;
        return -1;
    }

    void Choose()
    {
        Piece at = PieceAt(cursor);
        if (at != null && at.mine) { picked = cursor; Sound.Play("click"); return; }
        if (picked >= 0 && Legal(picked, cursor, true)) { Move(PieceAt(picked), cursor); picked = -1; return; }
        Sound.Play("menuback");
    }

    bool Legal(int from, int to, bool mine)
    {
        bool near = false;
        foreach (int n in Next(from)) if (n == to) near = true;
        Piece there = PieceAt(to);
        return near && (there == null || there.mine != mine);
    }

    // A move; into a rival, the dice are rolled (Roll) and the turn waits on them.
    void Move(Piece p, int to)
    {
        Piece there = PieceAt(to);
        moves++;
        if (there != null)
        {
            rollBy = p;
            rollOn = there;
            rollA = Random.Range(1, 7);
            rollB = Random.Range(1, 7);
            rollFrom = Time.unscaledTime;
            struck = false;
            dice.gameObject.SetActive(true);
            dieSum.text = "VS";
            for (int side = 0; side < 2; side++)
            {
                Piece who = side == 0 ? (p.mine ? p : there) : (p.mine ? there : p);
                dieName[side].text = Name(who);
                int edge = EdgeOf(who.kind, (who == p ? there : p).kind);
                dieEdge[side].text = edge > 0 ? "+" + edge : "";
            }
            Sound.Play("click");
            return;
        }
        Slide(p, to);
        Sound.Play("click");
        Pass();
    }

    void Slide(Piece p, int to)
    {
        p.from = p.body.position;
        p.movedAt = Time.unscaledTime;
        p.cell = to;
    }

    string Name(Piece p) => p.mine ? L10n.T("holo.name.mine", L10n.T("holo.kind." + p.kind)) : L10n.T("holo.name.theirs", L10n.T("holo.kind." + p.kind), Rival);

    // A burst of light where two pieces meet, swelling and gone.
    readonly List<(Transform spark, float until)> sparks = new();
    void Spark(Vector3 at, Color colour, float size)
    {
        Transform s = Shape(PrimitiveType.Sphere, room, at, Vector3.one * size, Glow(colour, 2.4f));
        sparks.Add((s, Time.unscaledTime + 0.25f));
    }

    // The fight played out on the board while the dice roll: the attacker squares up and steps in, the defender
    // braces; the dice land, the winner lunges and strikes, the loser is knocked back shaking (a tie: they clash and
    // both bounce off); then they step back to their cells.
    void Clash(float t, float now)
    {
        Vector3 a = CellAt(rollBy.cell), d = CellAt(rollOn.cell), way = d - a;
        float gap = way.magnitude;
        way /= Mathf.Max(0.01f, gap);
        rollBy.face = way;
        rollOn.face = -way;
        int s = rollA + EdgeOf(rollBy.kind, rollOn.kind) - rollB;
        Piece winner = s > 0 ? rollBy : rollOn, loser = s > 0 ? rollOn : rollBy;
        float step = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f)) * 0.3f * gap; // the attacker's step in
        Vector3 by = way * step + Vector3.up * Mathf.Abs(Mathf.Sin(now * 9f)) * 0.08f, on = -way * 0.04f * Mathf.Sin(now * 12f);
        if (t >= 0.9f && t < 1.5f)
        {
            float k = (t - 0.9f) / 0.6f, hit = Mathf.Sin(Mathf.Clamp01(k / 0.35f) * Mathf.PI); // in to the contact and out
            if (s == 0)
            {
                by += way * hit * 0.15f * gap;
                on -= way * hit * 0.15f * gap;
            }
            else
            {
                Vector3 toward = winner == rollBy ? way : -way;
                Vector3 lunge = toward * hit * 0.25f * gap;
                Vector3 knock = toward * Mathf.Clamp01((k - 0.25f) / 0.3f) * 0.35f + Random.insideUnitSphere * 0.08f * (1f - k);
                if (winner == rollBy) { by += lunge; on += knock * (k > 0.25f ? 1f : 0f); }
                else { on += lunge; by += knock * (k > 0.25f ? 1f : 0f); }
            }
            if (!struck && k >= 0.3f)
            {
                struck = true; // the blow lands
                Spark(Vector3.Lerp(a, d, 0.5f) + Vector3.up * 0.7f, s == 0 ? Color.white : KindColor[winner.kind], 0.5f);
                Sound.Play(s == 0 ? "click" : loser.mine ? "wagonbad" : "vagonroto");
            }
        }
        else if (t >= 1.5f)
        {
            float back = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.5f) / 0.5f)); // home again
            Vector3 toward = s >= 0 ? way : -way;
            by = way * 0.3f * gap * back * (s < 0 ? 0.3f : 1f);
            on = s == 0 ? Vector3.zero : toward * 0.35f * back * (s > 0 ? 1f : 0f);
            if (s < 0) by = -way * 0.35f * back;
        }
        rollBy.nudge = by;
        rollOn.nudge = on;
        // the one that took the blow flickers red
        if (s != 0 && t >= 1.05f && t < 1.6f) loser.body.localScale *= (int)(now * 24f) % 2 == 0 ? 1.12f : 0.9f;
    }
    bool struck;
    const float DiceSize = 0.6f; // the dice small, in the top corner off the board: the fight on it is what to watch

    // The dice tumble a moment, land, and the one that rolled higher (with its kind's edge) takes a life off the other.
    void Roll(float now)
    {
        float t = now - rollFrom;
        Clash(t, now);
        int mineDie = rollBy.mine ? rollA : rollB, theirDie = rollBy.mine ? rollB : rollA;
        bool landed = t >= 0.9f;
        Face(0, landed ? mineDie : 1 + (int)(now * 13f) % 6);
        Face(1, landed ? theirDie : 1 + (int)(now * 11f + 3f) % 6);
        dice.localScale = Vector3.one * DiceSize * (landed ? 1f + Mathf.Max(0f, 0.12f - (t - 0.9f) * 0.4f) : 1f + Mathf.Sin(now * 40f) * 0.015f);
        if (landed && dieSum.text == "VS")
        {
            int edge = EdgeOf(rollBy.kind, rollOn.kind), a = rollA + Mathf.Max(0, edge), b = rollB + Mathf.Max(0, -edge); // each die with its +2, if it has it
            dieSum.text = (rollBy.mine ? a : b) + " - " + (rollBy.mine ? b : a);
            Sound.Play("confirm");
        }
        if (t < 2.2f) return;
        dice.gameObject.SetActive(false);
        rollFrom = -1f;
        rollBy.nudge = rollOn.nudge = rollBy.face = rollOn.face = Vector3.zero;
        int s = rollA + EdgeOf(rollBy.kind, rollOn.kind) - rollB;
        if (s == 0) { Say(L10n.T("holo.tie"), 2f); Pass(); return; }
        Piece winner = s > 0 ? rollBy : rollOn, loser = s > 0 ? rollOn : rollBy;
        loser.lives--;
        loser.pips[loser.lives].gameObject.SetActive(false);
        Sound.Play(loser.mine ? "wagonbad" : "vagonroto");
        if (loser.lives > 0) Say(L10n.T("holo.hit", Name(winner), Name(loser)), 2.2f);
        else
        {
            Say(L10n.T("holo.out", Name(winner), Name(loser)), 2.2f);
            pieces.Remove(loser);
            loser.goneAt = now;
            leaving.Add(loser);
            if (winner == rollBy) Slide(rollBy, rollOn.cell); // into the cell it emptied
        }
        Pass();
    }

    // Each die's dots for a number.
    static readonly int[][] Faces = { new[] { 4 }, new[] { 0, 8 }, new[] { 0, 4, 8 }, new[] { 0, 2, 6, 8 }, new[] { 0, 2, 4, 6, 8 }, new[] { 0, 2, 3, 5, 6, 8 } };
    void Face(int side, int number)
    {
        for (int i = 0; i < 9; i++) diePips[side][i].enabled = System.Array.IndexOf(Faces[number - 1], i) >= 0;
    }

    // After a move: over when a side is out of pieces or the moves are spent (then the more lives left wins), else the
    // other side's turn.
    void Pass()
    {
        if (Count(true) == 0 || Count(false) == 0 || moves >= turnLimit) { Finish(Count(true) > 0 && (Count(false) == 0 || LivesLeft(true) > LivesLeft(false)), Count(true) > 0 && Count(false) > 0 && LivesLeft(true) == LivesLeft(false)); return; }
        myTurn = !myTurn;
        if (!myTurn) rivalAt = Time.unscaledTime + rivalThinks + slide;
    }

    int LivesLeft(bool mine) { int n = 0; foreach (Piece p in pieces) if (p.mine == mine) n += p.lives; return n; }

    // The rival's move: the best by a short look (attack where the odds are with it, keep off what has the edge over
    // it), or now and then any.
    void RivalMove()
    {
        var options = new List<(Piece p, int to, float score)>();
        foreach (Piece p in pieces.ToArray())
        {
            if (p.mine) continue;
            foreach (int to in Next(p.cell))
            {
                if (!Legal(p.cell, to, false)) continue;
                Piece there = PieceAt(to);
                float score = Random.Range(0f, 2f);
                if (there != null) { var (win, _, lose) = Odds(p.kind, there.kind); score += 10f * win - 10f * lose + boldness; }
                foreach (int n in Next(to))
                {
                    Piece near = PieceAt(n);
                    if (near == null || !near.mine || near == there) continue;
                    var (win, _, lose) = Odds(near.kind, p.kind);
                    score += -6f * win + 3f * lose;
                }
                options.Add((p, to, score));
            }
        }
        if (options.Count == 0) { myTurn = true; return; }
        var pick = options[0];
        if (Random.value < mistakes) pick = options[Random.Range(0, options.Count)];
        else foreach (var o in options) if (o.score > pick.score) pick = o;
        Move(pick.p, pick.to);
    }

    // The tiles lit: the cursor, the piece picked, where it can go (green: a free cell or an attack with the odds with
    // it; yellow: even odds; red: odds against it); the rest in their own light or dark stone.
    void Paint(float now)
    {
        for (int c = 0; c < Cells; c++)
        {
            Color look = cellShade[c];
            if (picked >= 0 && myTurn && Legal(picked, c, true))
            {
                Piece there = PieceAt(c);
                float win = there == null ? 1f : Odds(PieceAt(picked).kind, there.kind).win;
                look = Color.Lerp(look, win > 0.5f ? new Color(0.25f, 0.85f, 0.4f) : win > 0.3f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.95f, 0.22f, 0.28f), 0.55f + Mathf.Sin(now * 4f) * 0.08f); // the stone shows through
            }
            if (c == picked) look = Color.Lerp(look, new Color(1f, 0.8f, 0.2f) * 1.3f, 0.8f);
            if (c == cursor && myTurn) look = Color.Lerp(look, Mine * 1.4f, 0.5f + Mathf.Sin(now * 6f) * 0.15f);
            tint.SetColor(MainColor, look.linear);
            cellLook[c].SetPropertyBlock(tint);
        }
    }

    Piece PieceAt(int c) => pieces.Find(p => p.cell == c);
    int Count(bool mine) => pieces.FindAll(p => p.mine == mine).Count;

    void Say(string line, float seconds)
    {
        message.text = line;
        messageUntil = Time.unscaledTime + seconds;
    }
}
