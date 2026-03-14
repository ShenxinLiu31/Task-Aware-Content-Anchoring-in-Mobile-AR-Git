using UnityEngine;

public class SimpleMicTest : MonoBehaviour
{
    void Start()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("🎤 Found device: " + device);
        }

        var audio = gameObject.AddComponent<AudioSource>();
        audio.loop = true;
        audio.clip = Microphone.Start(null, true, 10, 44100);
        while (!(Microphone.GetPosition(null) > 0)) { }
        audio.Play();
        Debug.Log("🎤 Mic started");
    }
}
