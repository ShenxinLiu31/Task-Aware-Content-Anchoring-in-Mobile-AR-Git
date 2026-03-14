using UnityEngine;
using UnityEngine.Video;

public class VideoSwitcher : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip newVideoClip;

    public void SwitchVideo()
    {
        if (videoPlayer == null || newVideoClip == null)
        {
            Debug.LogError("Missing VideoPlayer or new VideoClip!");
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = newVideoClip;
        videoPlayer.Play();
    }
}
