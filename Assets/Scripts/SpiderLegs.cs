using UnityEngine;

// The spider engine's eight legs, walking. Each leg is two bones (Models/SpiderLeg_Upper, _Lower) bent at a knee
// that is worked out every frame from where the hip is and where the foot has to be. A foot on the ground goes
// back exactly as fast as the ground does, then lifts and swings forward again; neighbours step in turn.
// All of it in the engine's own space (its nose is its -x), so it follows the engine round curves and over jumps.
public class SpiderLegs : MonoBehaviour
{
    public float upper = 1.75f, lower = 2.1f, stride = 1.3f, lift = 0.55f, planted = 0.6f; // planted: share of a step spent on the ground

    static readonly float[] HipX = { -1.5f, -0.5f, 0.5f, 1.5f }, FootX = { -2.7f, -1.05f, 0.75f, 2.4f }, FootZ = { 1.75f, 2.15f, 2.15f, 1.8f };
    const float HipY = 1.3f, HipZ = 0.72f;

    // Keep the live joints across an Editor script reload; Start is not called again for existing instances.
    [SerializeField] Transform[] thighs = new Transform[8], shins = new Transform[8];
    [SerializeField] TrainSim train;
    float walked;

    void Start()
    {
        train = FindAnyObjectByType<TrainSim>(); // none on the selection screen: it just stands
        for (int i = 0; i < 8; i++)
        {
            thighs[i] = Bone("Models/SpiderLeg_Upper");
            shins[i] = Bone("Models/SpiderLeg_Lower");
        }
        LateUpdate();
    }

    // The bone hangs under a pivot of its own, so whatever turn the import gave the model stays out of the way.
    Transform Bone(string model)
    {
        Transform pivot = new GameObject("Leg").transform;
        pivot.SetParent(transform, false);
        foreach (Renderer part in Instantiate(Resources.Load<GameObject>(model), pivot).GetComponentsInChildren<Renderer>()) TrainSim.Toon(part);
        return pivot;
    }

    void LateUpdate()
    {
        if (train) walked += train.Speed / transform.lossyScale.x * Time.deltaTime;
        for (int i = 0; i < 8; i++)
        {
            int along = i / 2, side = i % 2 == 0 ? 1 : -1;
            // Where in its step this leg is: neighbours half a step apart, and the two sides too.
            float step = Mathf.Repeat(walked / (stride / planted) + (i % 2 + along) % 2 * 0.5f, 1f);
            var foot = new Vector3(FootX[along], 0f, side * FootZ[along]);
            if (step < planted) foot.x += stride * (step / planted - 0.5f);          // on the ground, carried back with it
            else
            {
                float air = (step - planted) / (1f - planted);
                foot.x += stride * (0.5f - Mathf.SmoothStep(0f, 1f, air));           // in the air, swinging forward
                foot.y = lift * Mathf.Sin(air * Mathf.PI);
            }
            var hip = new Vector3(HipX[along], HipY, side * HipZ);
            // The knee: on the triangle hip - knee - foot whose sides are the two bones, its point up.
            Vector3 reach = foot - hip;
            float far = Mathf.Clamp(reach.magnitude, Mathf.Abs(upper - lower) + 0.05f, upper + lower - 0.05f);
            Vector3 towards = reach.normalized;
            float near = (upper * upper - lower * lower + far * far) / (2f * far);
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, towards).normalized;
            Vector3 knee = hip + towards * near + up * Mathf.Sqrt(Mathf.Max(0f, upper * upper - near * near));
            Lay(thighs[i], hip, knee);
            Lay(shins[i], knee, hip + towards * far);
        }
    }

    // The bone models run one unit along -x from their origin: turned and stretched to span from one joint to the next.
    static void Lay(Transform bone, Vector3 from, Vector3 to)
    {
        bone.localPosition = from;
        bone.localRotation = Quaternion.FromToRotation(Vector3.left, to - from);
        bone.localScale = new Vector3((to - from).magnitude, 1f, 1f);
    }
}
