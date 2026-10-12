using UnityEngine;

// Only the selection model: keep its face aimed at the viewer while its social gesture plays.
[ExecuteAlways]
public class PreviewHeroLook : MonoBehaviour
{
    void OnAnimatorIK(int layer)
    {
        var animator = GetComponent<Animator>();
        if (!animator || !animator.isHuman || !Camera.main) return;
        animator.SetLookAtWeight(1f, 0.1f, 1f, 0f, 0.25f);
        animator.SetLookAtPosition(Camera.main.transform.position + Vector3.up * 5f);
    }
}
