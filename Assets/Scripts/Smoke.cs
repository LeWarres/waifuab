using System.Collections.Generic;
using UnityEngine;

// Smoke pouring out of a locomotive's stack: dark puffs that rise, swell, trail back the faster the train goes
// and thin out to nothing. Drawn instanced like the enemies and bullets, so it costs next to nothing.
// Locomotives.Create puts one on the engines that smoke, at the mouth of the stack.
public class Smoke : MonoBehaviour
{
    public Color colour = new Color(0.07f, 0.07f, 0.09f);
    public float every = 0.09f, life = 1.7f, rise = 2.4f, size = 1.25f;
    public float trail = 0.3f, wander = 0.4f; // how much of the train's speed carries it back; how far it strays sideways

    struct Puff
    {
        public Vector3 at, drift;
        public float age, turn;
    }

    readonly List<Puff> puffs = new();
    Combat.Batch batch;
    TrainSim train;
    float next;

    void Start() => train = FindAnyObjectByType<TrainSim>(); // none on the selection screen: the smoke just goes straight up

    // Made the first time it is drawn, not in Awake: whoever adds it sets its colour after (the jetpack's fire), and
    // not in Start either: a copy made while the frame is being drawn gets a LateUpdate before its Start.
    static Mesh ball;
    Combat.Batch Batch()
    {
        if (batch != null) return batch;
        if (!ball) ball = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        return batch = new Combat.Batch(colour, false, ball);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        // The engine's nose is its -x, so the smoke trails off along +x; a little sideways wander on top.
        Vector3 back = transform.right * (train ? train.Speed * trail : 0f);
        for (next -= dt; next <= 0f; next += every)
            puffs.Add(new Puff { at = transform.position, drift = back + new Vector3(Random.Range(-wander, wander), 0f, Random.Range(-wander, wander)), turn = Random.value * 360f });
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            Puff puff = puffs[i];
            puff.age += dt;
            if (puff.age >= life) { puffs.RemoveAt(i); continue; }
            float along = puff.age / life;
            puff.at += (Vector3.up * rise * (1f - along * 0.5f) + puff.drift * along) * dt; // slows as it climbs, leans back more and more
            puffs[i] = puff;
        }
    }

    void LateUpdate()
    {
        foreach (Puff puff in puffs)
        {
            float along = puff.age / life;
            // Swells out of the stack, then thins away.
            float wide = size * Mathf.Min(0.3f + along * 2.2f, 1f) * (1f - Mathf.Clamp01((along - 0.6f) / 0.4f));
            Batch().Add(puff.at, Vector3.one * wide, Quaternion.Euler(0f, puff.turn, 0f));
        }
        Batch().Flush();
    }
}
