using System.IO;
using UnityEngine;

/// <summary>
/// 统一的事件时间戳记录器：
/// Begin()  -> 打开文件并写 StartExperiment
/// LogEvent("标签") -> 写一行时间戳+标签
/// End()    -> 写 StopExperiment 并关闭文件
/// </summary>
public class EventLogger : MonoBehaviour
{
    private StreamWriter writer;
    private bool recording = false;

    /// <summary>开始记录，自动写 StartExperiment</summary>
    public void Begin()
    {
        if (recording) return;

        string path = Path.Combine(Application.persistentDataPath, "event_log.csv");
        writer = new StreamWriter(path, false);
        writer.WriteLine("t,event");
        WriteLine("StartExperiment");

        recording = true;
    }

    /// <summary>写一条自定义事件</summary>
    public void LogEvent(string evt)
    {
        if (!recording || writer == null) return;
        WriteLine(evt);
    }

    /// <summary>结束记录，自动写 StopExperiment 并关文件</summary>
    public void End()
    {
        if (!recording || writer == null) return;

        WriteLine("StopExperiment");
        writer.Close();
        recording = false;
    }

    /* ---------- 工具 ---------- */
    private void WriteLine(string evt)
    {
        float t = Time.realtimeSinceStartup;
        writer.WriteLine($"{t:F3},{evt}");
        writer.Flush();            // 事件通常不多，直接落盘最安全
    }
}
