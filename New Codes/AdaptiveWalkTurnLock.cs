using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;   // Follow
using Microsoft.MixedReality.Toolkit.Utilities;          // SolverOrientationType

[DisallowMultipleComponent]
[RequireComponent(typeof(SolverHandler))]
[RequireComponent(typeof(Follow))]
public class AdaptiveWalkTurnLock_NoAnchor : MonoBehaviour
{
    [Header("走路判定：线速度 > (m/s)")]
    [Range(0.01f, 0.3f)] public float stillSpeedThreshold = 0.06f;

    [Header("明显转头：角速度 > (°/s)")]
    [Range(50f, 360f)] public float turnSpeedThreshold = 300f;

    [Header("静止多久触发重新锁定 (s)")]
    [Range(0.2f, 2f)] public float stillTimeToRelock = 0.5f;

    private Follow follow;
    private Camera cam;
    private Transform pivot;          // 空节点，充当“伪锚”

    private Vector3 lastPos;
    private Quaternion lastRot;
    private float stillTimer;

    enum Mode { World, Head, Body }
    private Mode mode = Mode.World;

    void Awake()
    {
        follow = GetComponent<Follow>();
        follow.enabled = false;                       // World-Lock 时关闭
        follow.DefaultDistance = 0.9f;
        follow.MinDistance = 0.8f;
        follow.MaxDistance = 1.1f;
        follow.MoveLerpTime = 0.5f;

        cam = Camera.main;

        // 创建初始 Pivot，让 HUD 成为其子物体，实现第一帧 World-Lock
        pivot = new GameObject("AdaptivePivot").transform;
        pivot.position = cam.transform.position;
        transform.SetParent(pivot);

        lastPos = cam.transform.position;
        lastRot = cam.transform.rotation;
    }

    void Update()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // 线/角速度
        float linSpeed = Vector3.Distance(cam.transform.position, lastPos) / Time.deltaTime;
        float angSpeed = Quaternion.Angle(cam.transform.rotation, lastRot) / Time.deltaTime;
        lastPos = cam.transform.position;
        lastRot = cam.transform.rotation;

        bool isMoving = linSpeed > stillSpeedThreshold;
        bool turningFast = angSpeed > turnSpeedThreshold;

        if (!isMoving)                                // ── 静止 ──
        {
            stillTimer += Time.deltaTime;
            if (stillTimer >= stillTimeToRelock && mode != Mode.World)
                EnterWorldLock();
        }
        else                                          // ── 移动 ──
        {
            stillTimer = 0f;
            if (turningFast)
            {
                if (mode != Mode.Body) EnterBodyLock();
            }
            else
            {
                if (mode != Mode.Head) EnterHeadLock();
            }
        }
    }

    /* ───────── 三种模式切换 ───────── */

    void EnterWorldLock()
    {
        mode = Mode.World;
        follow.enabled = false;

        // 把 Pivot 移到当前相机位置并让 HUD 挂回去
        pivot.position = cam.transform.position;
        transform.SetParent(pivot, worldPositionStays: true);
    }

    void EnterHeadLock()
    {
        mode = Mode.Head;
        transform.SetParent(null);                    // 解绑 Pivot
        follow.enabled = true;
        follow.OrientationType = SolverOrientationType.CameraAligned;
        follow.MoveLerpTime = 0.1f;
    }

    void EnterBodyLock()
    {
        mode = Mode.Body;
        transform.SetParent(null);
        follow.enabled = true;
        follow.OrientationType = SolverOrientationType.Unmodified;
        follow.MoveLerpTime = 0.2f;
    }
}
