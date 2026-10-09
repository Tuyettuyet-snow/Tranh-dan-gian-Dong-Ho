using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class RayVisibilityController : MonoBehaviour
{
    private XRInteractorLineVisual lineVisual;

    private void Awake()
    {
        lineVisual = GetComponent<XRInteractorLineVisual>();
    }

    private void Start()
    {
        // Kiểm tra có headset VR thật đang kết nối không
        bool hasRealHMD = XRSettings.isDeviceActive
                          && !string.IsNullOrEmpty(XRSettings.loadedDeviceName)
                          && XRSettings.loadedDeviceName != "MockHMD";

        if (lineVisual != null)
            lineVisual.enabled = hasRealHMD;

        Debug.Log($"[RayVisibility] Device: '{XRSettings.loadedDeviceName}' | Ray enabled: {hasRealHMD}");
    }
}