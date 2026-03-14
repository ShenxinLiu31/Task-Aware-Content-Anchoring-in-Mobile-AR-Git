using UnityEngine;

public class StableWebcamFeedToQuad : MonoBehaviour
{
    [Header("Target Quad (贴图到这个物体的材质)")]
    public Renderer targetQuad;

    [Header("Capture Settings")]
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFPS = 30;

    [Header("Stabilization")]
    [Tooltip("每多少秒更新一次 (0.1 = 10FPS)")]
    public float updateInterval = 0.1f;
    [Range(0f, 1f)] public float smoothing = 0.4f;

    private WebCamTexture camTex;
    private Texture2D smoothTex;
    private float timer;
    private Color32[] lastFrame;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("没有检测到摄像头！");
            return;
        }

        // 使用第一个摄像头
        camTex = new WebCamTexture(devices[0].name, requestedWidth, requestedHeight, requestedFPS);
        camTex.Play();

        // 初始化平滑纹理
        smoothTex = new Texture2D(requestedWidth, requestedHeight, TextureFormat.RGBA32, false);
        lastFrame = new Color32[requestedWidth * requestedHeight];

        // 给 Quad 赋值材质纹理
        if (targetQuad != null)
        {
            targetQuad.material.mainTexture = smoothTex;
        }

        Debug.Log($"启动摄像头: {devices[0].name}, 请求分辨率 {requestedWidth}x{requestedHeight}@{requestedFPS}");
    }

    void Update()
    {
        if (camTex == null || !camTex.isPlaying) return;

        timer += Time.deltaTime;
        if (timer < updateInterval || !camTex.didUpdateThisFrame) return;
        timer = 0f;

        var newFrame = camTex.GetPixels32();

        // 分辨率变化时重建纹理
        if (smoothTex.width != camTex.width || smoothTex.height != camTex.height)
        {
            smoothTex.Reinitialize(camTex.width, camTex.height);
            lastFrame = new Color32[camTex.width * camTex.height];
            Debug.Log($"检测到分辨率变化 → {camTex.width}x{camTex.height}");
        }

        // 平滑混合
        for (int i = 0; i < newFrame.Length; i++)
        {
            lastFrame[i].r = (byte)Mathf.Lerp(newFrame[i].r, lastFrame[i].r, smoothing);
            lastFrame[i].g = (byte)Mathf.Lerp(newFrame[i].g, lastFrame[i].g, smoothing);
            lastFrame[i].b = (byte)Mathf.Lerp(newFrame[i].b, lastFrame[i].b, smoothing);
            lastFrame[i].a = 255;
        }

        smoothTex.SetPixels32(lastFrame);
        smoothTex.Apply(false);
    }

    void OnDestroy()
    {
        if (camTex != null && camTex.isPlaying)
            camTex.Stop();
    }
}