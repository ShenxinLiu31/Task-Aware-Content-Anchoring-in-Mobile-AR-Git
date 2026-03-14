using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

public class MediaPlay : MonoBehaviour
{
    public MediaPlayer mediaPlayer;
    public RawImage rawImage;

    void Start()
    {
        string url = "https://www.bilibili.com/video/BV1zA4y197Ar?t=6.5";

        // 注册回调
        mediaPlayer.Events.AddListener(OnMediaEvent);

        // 播放
        mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, url, autoPlay: true);
    }

    void OnMediaEvent(MediaPlayer mp, MediaPlayerEvent.EventType evt, ErrorCode errorCode)
    {
        if (evt == MediaPlayerEvent.EventType.FirstFrameReady)
        {
            Debug.Log("第一帧准备好了");

            // ✅ 使用 TextureProducer 获取纹理
            if (rawImage != null && mediaPlayer.TextureProducer != null)
            {
                rawImage.texture = mediaPlayer.TextureProducer.GetTexture();
            }
        }
    }
}
