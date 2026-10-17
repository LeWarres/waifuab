using UnityEngine;

// A wreck left burning on the ground by an event. The train never makes headway through the world (the ground
// slides back under it instead), so a plain world effect hangs on the screen where it was played; this one
// slides back with the landscape, out of sight, and puts itself out on the way. Used for the boss's death fire.
public class Wreck : MonoBehaviour
{
    public TrainSim train;
    public int wagon;
    public float burn = 4f;      // seconds it burns before it goes out

    float born;

    void Start()
    {
        born = Time.time;
        foreach (var graph in GetComponentsInChildren<UnityEngine.VFX.VisualEffect>())
        {
            graph.Reinit();
            graph.Play();
        }
        // The fire prefab carries a looping ParticleSystem as well: replay it once so it dies out instead of burning forever.
        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            system.Play();
        }
    }

    void Update()
    {
        if (train) transform.position -= Ground(train.Forward(wagon)) * train.GroundScroll; // stays glued to the ground it burns on

        if (Time.time - born < burn) return;
        foreach (var graph in GetComponentsInChildren<UnityEngine.VFX.VisualEffect>()) graph.Stop();
        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        enabled = false;
        Destroy(gameObject, 3f); // the embers are given a moment to die out, out of sight
    }

    static Vector3 Ground(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 0.001f ? Vector3.right : v.normalized; }
}