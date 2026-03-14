using UnityEngine;
using UnityEngine.UI;

public class PcCameraFeed : MonoBehaviour
{
    public RawImage target;
    private WebCamTexture camTex;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            // 指定分辨率 & 帧率
            camTex = new WebCamTexture(devices[0].name, 1920, 1080, 30);

            if (target != null)
                target.texture = camTex;
            else
                GetComponent<Renderer>().material.mainTexture = camTex;

            camTex.Play();

            Debug.Log($"WebCam: {camTex.deviceName}, Res: {camTex.width}x{camTex.height}");
        }
        else
        {
            Debug.LogWarning("No camera found!");
        }
    }

    void OnDestroy()
    {
        if (camTex != null && camTex.isPlaying)
            camTex.Stop();
    }
}