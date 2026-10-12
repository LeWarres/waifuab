// Gives the heroes that came on the Meshy rig (Hips > Spine02 > Spine01 > Spine, arms hanging, knees bent) a proper
// humanoid avatar: every bone named for what it is (their spine is numbered upside down, which the automatic
// mapping gets wrong) and a T-pose to measure from (they come in whatever stance they were modelled in), so
// animations made for other characters fit them instead of crushing them.
// Run in the editor, not playing:  unity command eval_file --file Tools/fix_hero_avatars.cs
var names = new System.Collections.Generic.Dictionary<string, string>
{
    { "Hips", "Hips" }, { "Spine02", "Spine" }, { "Spine01", "Chest" }, { "Spine", "UpperChest" }, { "neck", "Neck" }, { "Head", "Head" },
    { "LeftShoulder", "LeftShoulder" }, { "LeftArm", "LeftUpperArm" }, { "LeftForeArm", "LeftLowerArm" }, { "LeftHand", "LeftHand" },
    { "RightShoulder", "RightShoulder" }, { "RightArm", "RightUpperArm" }, { "RightForeArm", "RightLowerArm" }, { "RightHand", "RightHand" },
    { "LeftUpLeg", "LeftUpperLeg" }, { "LeftLeg", "LeftLowerLeg" }, { "LeftFoot", "LeftFoot" }, { "LeftToeBase", "LeftToes" },
    { "RightUpLeg", "RightUpperLeg" }, { "RightLeg", "RightLowerLeg" }, { "RightFoot", "RightFoot" }, { "RightToeBase", "RightToes" },
};
string report = "";
foreach (string folder in System.IO.Directory.GetDirectories("Assets/Resources/Characters"))
{
    string id = System.IO.Path.GetFileName(folder), path = folder.Replace('\\', '/') + "/" + id + ".fbx";
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (importer == null || model == null) continue;
    var copy = UnityEngine.Object.Instantiate(model);
    copy.name = model.name;
    copy.transform.position = UnityEngine.Vector3.zero;
    copy.transform.rotation = UnityEngine.Quaternion.identity;
    var all = copy.GetComponentsInChildren<UnityEngine.Transform>(true);
    System.Func<string, UnityEngine.Transform> bone = name => System.Array.Find(all, t => t.name == name);
    if (bone("Spine02") == null || bone("LeftArm") == null)
    {
        report += id + ": another rig, left alone.  ";
        UnityEngine.Object.DestroyImmediate(copy);
        continue;
    }
    // The T-pose: spine straight up, arms straight out to the sides, legs straight down, toes forward.
    UnityEngine.Vector3 up = UnityEngine.Vector3.up, right = bone("RightArm").position - bone("LeftArm").position;
    right.y = 0f;
    right.Normalize();
    UnityEngine.Vector3 forward = UnityEngine.Vector3.Cross(right, up);
    System.Action<string, string, UnityEngine.Vector3> point = (from, to, along) =>
    {
        UnityEngine.Transform a = bone(from), b = bone(to);
        if (a && b) a.rotation = UnityEngine.Quaternion.FromToRotation(b.position - a.position, along) * a.rotation;
    };
    point("Hips", "Spine02", up); point("Spine02", "Spine01", up); point("Spine01", "Spine", up);
    point("Spine", "neck", up); point("neck", "Head", up); point("Head", "head_end", up);
    foreach (string side in new[] { "Left", "Right" })
    {
        UnityEngine.Vector3 out_ = side == "Left" ? -right : right;
        point(side + "Shoulder", side + "Arm", out_); point(side + "Arm", side + "ForeArm", out_); point(side + "ForeArm", side + "Hand", out_);
        point(side + "UpLeg", side + "Leg", -up); point(side + "Leg", side + "Foot", -up);
        point(side + "Foot", side + "ToeBase", (forward * 0.85f - up * 0.5f).normalized);
    }
    var human = new System.Collections.Generic.List<UnityEngine.HumanBone>();
    foreach (var pair in names)
        if (bone(pair.Key)) human.Add(new UnityEngine.HumanBone { boneName = pair.Key, humanName = pair.Value, limit = new UnityEngine.HumanLimit { useDefaultValues = true } });
    var skeleton = new System.Collections.Generic.List<UnityEngine.SkeletonBone>();
    foreach (var t in all) skeleton.Add(new UnityEngine.SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
    var description = importer.humanDescription;
    description.human = human.ToArray();
    description.skeleton = skeleton.ToArray();
    importer.humanDescription = description;
    UnityEngine.Object.DestroyImmediate(copy);
    importer.SaveAndReimport();
    UnityEngine.Avatar avatar = null;
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is UnityEngine.Avatar) avatar = (UnityEngine.Avatar)asset;
    report += id + ": " + human.Count + " bones, " + (avatar && avatar.isValid && avatar.isHuman ? "avatar good" : "AVATAR BAD") + ".  ";
}
return report;
