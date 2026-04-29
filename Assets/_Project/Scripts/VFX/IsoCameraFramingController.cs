using UnityEngine;
using Unity.Cinemachine;

public class IsoCameraFramingController : MonoBehaviour
{
    [Header("Apply")]
    public bool applyOnStart = false;
    public CinemachineCamera targetCamera;

    [Header("Framing")]
    public float orthographicSize = 8f;
    public Vector3 trackedObjectOffset = Vector3.zero;

    private void Start()
    {
        if (applyOnStart)
            ApplyFraming();
    }

    [ContextMenu("Apply Iso Camera Framing")]
    public void ApplyFraming()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<CinemachineCamera>() ?? FindFirstObjectByType<CinemachineCamera>();

        if (targetCamera == null)
            return;

        LensSettings lens = targetCamera.Lens;
        lens.OrthographicSize = Mathf.Max(1f, orthographicSize);
        targetCamera.Lens = lens;

        CinemachinePositionComposer composer = targetCamera.GetComponent<CinemachinePositionComposer>();
        if (composer != null)
            composer.TargetOffset = trackedObjectOffset;
    }
}
