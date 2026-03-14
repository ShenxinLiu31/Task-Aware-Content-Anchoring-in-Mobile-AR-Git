using UnityEngine;
using UnityEngine.Video;

public class VideoReplayController : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    public void ReplayVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer not assigned!");
            return;
        }

        videoPlayer.Stop();
        videoPlayer.Play();
    }
}
