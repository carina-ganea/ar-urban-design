using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class XRCustomGrabInteractable : XRGrabInteractable
{
    Transform _OriginalSceneParent;

    protected override void Grab()
    {
        _OriginalSceneParent = transform.parent;
        Vector3 parentScale = _OriginalSceneParent.localScale;
        _OriginalSceneParent.localScale = Vector3.one;
        base.Grab();
        _OriginalSceneParent.localScale = parentScale;
    }

    protected override void Drop()
    {
        Vector3 parentScale = _OriginalSceneParent.localScale;
        _OriginalSceneParent.localScale = Vector3.one;
        base.Drop();
        _OriginalSceneParent.localScale = parentScale;
    }
}
