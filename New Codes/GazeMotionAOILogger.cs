using System.IO;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class GazeMotionAOILogger : MonoBehaviour
{
    [Header("阈值设置")]
    [Tooltip("头部线速度超过此值视为走路 (m/s)")]
    public float walkSpeedThreshold = 0.25f;
    [Tooltip("视线角速度超过此值计一次眼跳 (°/s)")]
    public float saccadeAngularSpeedDeg = 200f;

    private enum AOI { None, PPT, Avatar, Other }
    private enum Mot { Static, Walk }

    private IMixedRealityGazeProvider gp;
    private StreamWriter writer;
    private bool recording;

    // [运动状态][AOI] 停留秒数
    private readonly float[,] dwell = new float[2, 3];
    // 走路 / 静止 的眼跳次数
    private readonly int[] switches = new int[2];

    private AOI prevAOI = AOI.None;
    private Mot prevMot = Mot.Static;
    private float segStart, prevTime;
    private Vector3 prevDir, prevPos;

    void Awake() => gp = CoreServices.InputSystem?.GazeProvider;

    /* ---------- Begin / End ---------- */

    public void Begin()
    {
        if (recording) return;

        // 调试：写文件标记，方便确认 Begin 被触发
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "gaze_debug.txt"),
            $"Begin() hit  {System.DateTime.UtcNow:HH:mm:ss.fff}\n");

        string path = Path.Combine(Application.persistentDataPath, "gaze_motion_aoi.csv");
        writer = new StreamWriter(path, false);
        writer.WriteLine("t,mot,aoi,x,y,z");

        segStart = prevTime = Time.realtimeSinceStartup;
        prevDir = gp?.GazeDirection ?? Vector3.forward;
        prevPos = Camera.main.transform.position;
        prevAOI = AOI.None; prevMot = Mot.Static;
        switches[0] = switches[1] = 0;
        dwell[0, 0] = dwell[0, 1] = dwell[0, 2] = 0;
        dwell[1, 0] = dwell[1, 1] = dwell[1, 2] = 0;

        recording = true;
    }

    public void End()
    {
        if (!recording) return;

        File.AppendAllText(Path.Combine(Application.persistentDataPath, "gaze_debug.txt"),
            $"End() hit    {System.DateTime.UtcNow:HH:mm:ss.fff}\n");

        FlushSegment(Time.realtimeSinceStartup);
        WriteSummary();
        writer.Close();
        recording = false;
    }

    /* ---------- 主循环 ---------- */
    void Update()
    {
        if (!recording || gp == null) return;

        float now = Time.realtimeSinceStartup;
        float dt = now - prevTime;
        Vector3 dir = gp.GazeDirection.normalized;
        Vector3 pos = Camera.main.transform.position;

        float linVel = (pos - prevPos).magnitude / Mathf.Max(dt, 1e-4f);
        float angVel = Vector3.Angle(prevDir, dir) / Mathf.Max(dt, 1e-4f);
        Mot mot = linVel > walkSpeedThreshold ? Mot.Walk : Mot.Static;

        /* 判定 AOI */
        AOI aoi = AOI.Other; Vector3 hit = Vector3.zero;
        if (Physics.Raycast(new Ray(pos, dir), out RaycastHit h, 10f))
        {
            hit = h.point;
            if (h.collider.CompareTag("PPT")) aoi = AOI.PPT;
            else if (h.collider.CompareTag("Avatar")) aoi = AOI.Avatar;
        }

        writer.WriteLine($"{now:F3},{mot},{aoi},{hit.x:F3},{hit.y:F3},{hit.z:F3}");

        /* AOI 或 运动状态改变 → 结算上一段 */
        if (aoi != prevAOI || mot != prevMot)
        {
            FlushSegment(now);
            if (angVel > saccadeAngularSpeedDeg) switches[(int)prevMot]++;
            prevAOI = aoi; prevMot = mot; segStart = now;
        }

        prevDir = dir; prevPos = pos; prevTime = now;
    }

    /* ---------- 辅助 ---------- */
    void FlushSegment(float now)
    {
        if (prevAOI == AOI.None) return;
        int m = (int)prevMot;
        int a = prevAOI == AOI.PPT ? 0 : prevAOI == AOI.Avatar ? 1 : 2;
        dwell[m, a] += now - segStart;
    }

    void WriteSummary()
    {
        writer.WriteLine($"#SUMMARY,walk_ppt,{dwell[1, 0]:F3}");
        writer.WriteLine($"#SUMMARY,walk_avatar,{dwell[1, 1]:F3}");
        writer.WriteLine($"#SUMMARY,walk_other,{dwell[1, 2]:F3}");
        writer.WriteLine($"#SUMMARY,static_ppt,{dwell[0, 0]:F3}");
        writer.WriteLine($"#SUMMARY,static_avatar,{dwell[0, 1]:F3}");
        writer.WriteLine($"#SUMMARY,static_other,{dwell[0, 2]:F3}");
        writer.WriteLine($"#SUMMARY,walk_switch,{switches[1]}");
        writer.WriteLine($"#SUMMARY,static_switch,{switches[0]}");
    }
}
