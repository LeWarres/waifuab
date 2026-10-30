using UnityEngine;
using UnityEngine.SceneManagement;

// One persistent music source; changing a wagon or arriving at a station does not restart its track.
public class SceneMusic : MonoBehaviour
{
    AudioSource source;
    AudioClip selection, desert, desertBoss, city, cityBoss;
    TrainSim train;
    string scene;
    int sceneHandle = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var owner = new GameObject("SceneMusic");
        DontDestroyOnLoad(owner);
        owner.AddComponent<SceneMusic>();
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        Sound.AsMusic(source, 0.45f);
        selection = Resources.Load<AudioClip>("Music/selectMenu");
        desert = Resources.Load<AudioClip>("Music/desierto");
        desertBoss = Resources.Load<AudioClip>("Music/desiertofinalboss");
        city = Resources.Load<AudioClip>("Music/city");
        cityBoss = Resources.Load<AudioClip>("Music/cityfinalboss");
    }

    void Update()
    {
        Scene current = SceneManager.GetActiveScene();
        if (sceneHandle != current.handle)
        {
            sceneHandle = current.handle;
            scene = current.name;
            train = FindAnyObjectByType<TrainSim>();
        }
        AudioClip wanted = scene == "selectHero" ? selection
            : train && train.BiomeId == "desert" ? (train.BossFight && desertBoss ? desertBoss : desert)
            : train && train.BiomeId == "city" ? ((train.BossFight || train.Racing) && cityBoss ? cityBoss : city) : null;
        if (source.clip == wanted) return;
        source.Stop();
        source.clip = wanted;
        if (wanted) source.Play();
    }
}
