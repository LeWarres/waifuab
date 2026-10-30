using System.Collections.Generic;
using UnityEngine;

// Giger's hell finale shares the race's real vehicle controller, camera and audio.
public partial class RaceEvent
{
    public const int CrateCount = 40, CrateGoal = 35;
    public static bool CratesPassed(int broken) => broken >= CrateGoal;
    bool crateRun;
    public bool CrateRun => crateRun;
    public int CratesBroken { get; private set; }
    public int CratesSeen { get; private set; }
    readonly List<(Transform body, Vector3 at, float progress)> crates = new();
    readonly HashSet<int> resolvedCrates = new();
    Vector3 cratePrevious;

    public void BeginCrates(System.Action<bool> finished)
    {
        crateRun = true;
        Begin(finished);
        cratePrevious = me.pos;
    }

    void BuildCrates()
    {
        crates.Clear(); resolvedCrates.Clear(); CratesBroken = CratesSeen = 0;
        var wood = TrainSim.Mat(new Color(.46f, .22f, .09f));
        var rim = TrainSim.Mat(new Color(.76f, .47f, .19f));
        var rune = TrainSim.Mat(new Color(1f, .35f, .05f), false);
        float[] lanes = {0f, -9f, 0f, 9f, 9f, 0f, -9f, -9f};
        for (int i = 0; i < CrateCount; i++)
        {
            float s = 220f + i * 105f;
            Vector3 at = PointAt(s, out Vector3 dir) + Vector3.Cross(Vector3.up, dir) * lanes[i % lanes.Length];
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(box.GetComponent<Collider>());
            box.name = "SmashCrate_" + (i+1);
            box.transform.SetParent(course, false);
            box.transform.SetPositionAndRotation(at + Vector3.up * 2f, Quaternion.LookRotation(dir));
            box.transform.localScale = new Vector3(5f, 4f, 4f);
            box.GetComponent<Renderer>().sharedMaterial = wood;
            foreach (float y in new[]{-.38f,.38f})
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(band.GetComponent<Collider>());
                band.transform.SetParent(box.transform, false); band.transform.localPosition = new Vector3(0,y,-.52f);
                band.transform.localScale = new Vector3(1.04f,.12f,.06f); band.GetComponent<Renderer>().sharedMaterial = rim;
            }
            var cross = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(cross.GetComponent<Collider>());
            cross.transform.SetParent(box.transform,false);cross.transform.localPosition=new Vector3(0,0,-.54f);
            cross.transform.localRotation=Quaternion.Euler(0,0,38);cross.transform.localScale=new Vector3(1f,.08f,.04f);
            cross.GetComponent<Renderer>().sharedMaterial=rune;
            crates.Add((box.transform, at, s));
        }
        // Low platforms, lava vents and chains are taken from the existing hell biome.
        var scenery = Resources.LoadAll<GameObject>("Scenery/hell");
        for (int i=0; i<60 && scenery.Length>0; i++)
        {
            Vector3 at=PointAt(100f+i*70f,out Vector3 d)+Vector3.Cross(Vector3.up,d)*(i%2==0 ? -32f : 32f);
            Instantiate(scenery[i%scenery.Length],at,Quaternion.LookRotation(d),course);
        }
    }

    void CratesUpdate(float now, bool going)
    {
        place.text = CratesBroken.ToString(); // the crates broken, big where the place goes, out of how many beside it
        placeOf.text = "/ " + CrateCount;
        if (endsAt > 0f) return;
        message.text = L10n.T("giger.crates.target", CrateGoal, CrateCount);
        if (!going) { cratePrevious=me.pos; return; }
        Vector3 travel=me.pos-cratePrevious;
        for(int i=0;i<crates.Count;i++)
        {
            if(resolvedCrates.Contains(i))continue;
            var c=crates[i];
            float t=travel.sqrMagnitude<.001f ? 0f : Mathf.Clamp01(Vector3.Dot(c.at-cratePrevious,travel)/travel.sqrMagnitude);
            Vector3 near=cratePrevious+travel*t;near.y=c.at.y;
            bool hit=(near-c.at).sqrMagnitude<4.9f*4.9f;
            if(!hit && me.progress<c.progress+12f)continue;
            resolvedCrates.Add(i);CratesSeen++;
            if(hit)
            {
                CratesBroken++;c.body.gameObject.SetActive(false);Sound.Play("vagonroto");
                shake=Mathf.Max(shake,.2f);
                for(int k=0;k<8;k++)fireballs.Add((c.at+Vector3.up*2f,Random.onUnitSphere*8f,now,now+.55f,.5f,FireMats[0]));
            }
        }
        cratePrevious=me.pos;
        if(me.finishedAt>=0f || now-startsAt>=180f)
        {
            // Reaching the end or running out of time resolves the run, including missed boxes.
            CratesSeen=CrateCount;
            message.text=L10n.T(CratesPassed(CratesBroken) ? "giger.crates.won" : "giger.crates.lost",CratesBroken,CrateCount);
            raceSound.Cue(CratesPassed(CratesBroken) ? "race_win" : "race_lose");
            endsAt=now+2.5f;
        }
    }
}
