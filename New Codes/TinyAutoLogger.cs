using System.IO;
using UnityEngine;

public class TinyAutoLogger : MonoBehaviour
{
    void Start()          // 程序启动立刻执行
    {
        string path = Path.Combine(Application.persistentDataPath, "tiny_test.txt");

        // 把完整路径写进设备日志，方便在 Live Logs 搜索
        Debug.Log($"[TinyAutoLogger] path = {path}");

        File.AppendAllText(path,
            $"Hello HoloLens!  UTC = {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n");

        Debug.Log("[TinyAutoLogger] wrote tiny_test.txt");
    }
}
