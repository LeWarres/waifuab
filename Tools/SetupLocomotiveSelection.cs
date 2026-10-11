using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class SetupLocomotiveSelection
{
    public static string Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play mode before editing the selection scene.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        LocalizationImport.Run();
        const string path="Assets/Scenes/selectHero.unity";
        Scene scene=SceneManager.GetSceneByPath(path);
        if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LocomotiveSelection");
        if(!root) root=new GameObject("LocomotiveSelection");
        var menu=root.GetComponent<SelectLocomotive>() ?? root.AddComponent<SelectLocomotive>();
        menu.gameScene="sandScenario";
        Transform stage=root.transform.Find("PreviewStage");
        if(!stage) {stage=new GameObject("PreviewStage").transform;stage.SetParent(root.transform,false);}
        stage.localRotation=Quaternion.Euler(0,180,0);
        menu.previews=new Transform[Locomotives.Models.Length];
        for(int i=0;i<menu.previews.Length;i++) {
            var body=stage.Find("Locomotive_"+i);
            if(!body) body=Locomotives.Create(stage,i);
            menu.previews[i]=body;
            // Persist the original's runtime toon twins in the selection scene too.
            foreach(Renderer renderer in body.GetComponentsInChildren<Renderer>(true)) {
                var slots=renderer.sharedMaterials;
                for(int j=0;j<slots.Length;j++) {
                    if(AssetDatabase.Contains(slots[j])) continue;
                    const string materials="Assets/Materials/Locomotive_Classic";
                    if(!AssetDatabase.IsValidFolder(materials)) AssetDatabase.CreateFolder("Assets/Materials","Locomotive_Classic");
                    string matPath=materials+"/"+slots[j].name+".mat";
                    var stored=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(!stored) {stored=new Material(slots[j]);AssetDatabase.CreateAsset(stored,matPath);}
                    else {stored.CopyPropertiesFromMaterial(slots[j]);EditorUtility.SetDirty(stored);}
                    slots[j]=stored;
                }
                renderer.sharedMaterials=slots;
            }
            body.gameObject.SetActive(i==Locomotives.Selected);
        }
        var silhouette=stage.Find("LockedLocomotive");
        if(!silhouette) {
            silhouette=UnityEngine.Object.Instantiate(Resources.Load<GameObject>(Locomotives.Models[0]),stage).transform;
            silhouette.name="LockedLocomotive";silhouette.localScale=Vector3.one*1.3f;
        }
        const string lockedPath="Assets/Materials/SelectionLocked.mat";
        var lockedMaterial=AssetDatabase.LoadAssetAtPath<Material>(lockedPath);
        if(!lockedMaterial) {lockedMaterial=new Material(Resources.Load<Material>("Toon"));AssetDatabase.CreateAsset(lockedMaterial,lockedPath);}
        lockedMaterial.SetColor("_MainColor",Color.black);lockedMaterial.SetColor("_HighlightColor",Color.black);
        lockedMaterial.SetFloat("_GlossIntensity",0);EditorUtility.SetDirty(lockedMaterial);
        foreach(var renderer in silhouette.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials=Enumerable.Repeat(lockedMaterial,renderer.sharedMaterials.Length).ToArray();
        menu.lockedPreview=silhouette;silhouette.gameObject.SetActive(false);
        if(!stage.Find("DisplayTrack")) {
            var track=new GameObject("DisplayTrack").transform;track.SetParent(stage,false);
            var ground=new Material(Resources.Load<Material>("Toon"));
            ground.name="SelectionGround";
            ground.SetColor("_MainColor",new Color(.76f,.66f,.42f).linear);
            ground.DisableKeyword("N_F_O_ON");ground.SetShaderPassEnabled("SRPDefaultUnlit",false);
            const string floorPath="Assets/Materials/SelectionGround.mat";
            var savedGround=AssetDatabase.LoadAssetAtPath<Material>(floorPath);
            if(!savedGround) {AssetDatabase.CreateAsset(ground,floorPath);savedGround=ground;}
            else UnityEngine.Object.DestroyImmediate(ground);
            var metal=new Material(Resources.Load<Material>("Toon"));metal.name="SelectionRail";
            metal.SetColor("_MainColor",new Color(.26f,.29f,.33f).linear);
            const string railPath="Assets/Materials/SelectionRail.mat";
            var savedRail=AssetDatabase.LoadAssetAtPath<Material>(railPath);
            if(!savedRail) {AssetDatabase.CreateAsset(metal,railPath);savedRail=metal;}
            else UnityEngine.Object.DestroyImmediate(metal);
            TrainSim.Box(track,new Vector3(0,-.22f,0),new Vector3(10,.26f,3.8f),savedGround).name="Platform";
            for(int i=0;i<12;i++) TrainSim.Box(track,new Vector3(-4.5f+i*.8f,-.06f,0),new Vector3(.18f,.07f,2.20f),savedRail).name="Sleeper";
            foreach(float z in new[]{-.80f,.80f}) TrainSim.Box(track,new Vector3(0,-.025f,z),new Vector3(9.8f,.05f,.08f),savedRail).name="Rail";
        }
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();
        camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5.0f;
        camera.transform.rotation=Quaternion.Euler(24,-45,0);
        stage.Find("DisplayTrack").gameObject.SetActive(false);
        camera.transform.position=new Vector3(0,.70f,0)-camera.transform.forward*25+camera.transform.right*3.65f;
        camera.nearClipPlane=.1f;camera.farClipPlane=100;
        var hd=camera.GetComponent<HDAdditionalCameraData>() ?? camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR=new Color(.10f,.23f,.37f);
        hd.volumeLayerMask=~0;

        // Copy the scene's actual light, and the same fixed HDRP grade used by TrainSim.
        const string gamePath="Assets/Scenes/sandScenario.unity";
        Scene game=SceneManager.GetSceneByPath(gamePath);bool opened=!game.isLoaded;
        if(opened) game=EditorSceneManager.OpenScene(gamePath,OpenSceneMode.Additive);
        float exposure=13.7f;
        try {
            var sim=game.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TrainSim>(true)).First();
            exposure=sim.exposure;
            menu.maximumSpeed=sim.maxSpeed;
            menu.initialHealth=sim.wagonMaxHealth*Balance.Scale;
            var source=game.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional);
            var target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional);
            EditorUtility.CopySerialized(source,target);
            target.transform.rotation=source.transform.rotation;
            var sourceHD=source.GetComponent<HDAdditionalLightData>();
            var targetHD=target.GetComponent<HDAdditionalLightData>();
            if(sourceHD && targetHD) EditorUtility.CopySerialized(sourceHD,targetHD);
            else if(!sourceHD && targetHD) UnityEngine.Object.DestroyImmediate(targetHD);
        } finally {if(opened) EditorSceneManager.CloseScene(game,true);}
        SceneManager.SetActiveScene(scene);
        const string lookPath="Assets/Materials/SelectionLook.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(lookPath);
        if(!profile) {profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,lookPath);}
        Exposure fixedExposure=Get<Exposure>(profile);fixedExposure.mode.value=ExposureMode.Fixed;fixedExposure.fixedExposure.value=exposure;
        Get<Tonemapping>(profile).mode.value=TonemappingMode.Neutral;
        Bloom bloom=Get<Bloom>(profile);bloom.intensity.value=.15f;bloom.threshold.value=1;
        ColorAdjustments grade=Get<ColorAdjustments>(profile);grade.saturation.value=8;grade.contrast.value=6;
        EditorUtility.SetDirty(profile);
        var volume=root.GetComponent<Volume>() ?? root.AddComponent<Volume>();
        volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
        menu.BuildUI();
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var build=EditorBuildSettings.scenes.ToList();
        if(!build.Any(s=>s.path==path)) build.Insert(Math.Min(1,build.Count),new EditorBuildSettingsScene(path,true));
        foreach(var item in build.Where(s=>s.path==path)) item.enabled=true;
        EditorBuildSettings.scenes=build.ToArray();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        return "selectHero saved with two RealToon previews, selection menu, shared gameplay grade and build registration.";
    }
    static T Get<T>(VolumeProfile profile) where T:VolumeComponent
    {
        if(profile.TryGet<T>(out var component)) return component;
        component=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(component,profile);return component;
    }
}
