using UnityEngine;

/// <summary>
/// 统一控制实验生命周期：隐藏/恢复 UI，启动/关闭日志脚本
/// </summary>
public class ExperimentController : MonoBehaviour
{
    [Header("日志脚本（必填）")]
    public GazeMotionAOILogger gazeLogger;   // 眼动+运动日志
    public EventLogger eventLogger;   // 行为事件日志

    [Header("UI 引用（可选）")]
    public GameObject startPanel;            // “开始实验” 按钮所在面板
    public GameObject stopPanel;             // “结束实验” 按钮所在面板
    public GameObject[] hideDuringRun;       // 实验过程中要隐藏的一批 UI

    private bool running = false;

    /* ---------- 在“开始”按钮 OnClick 中绑定 ---------- */
    public void StartExperiment()
    {
        if (running) return;

        // 1) 隐藏 UI
        foreach (var go in hideDuringRun) if (go) go.SetActive(false);
        if (startPanel) startPanel.SetActive(false);
        if (stopPanel) stopPanel.SetActive(true);

        // 2) 启动日志
        gazeLogger?.Begin();
        eventLogger?.Begin();     // 自动写 StartExperiment
        eventLogger?.LogEvent("ManualStartButton");

        running = true;
    }

    /* ---------- 在“结束”按钮 OnClick 中绑定 ---------- */
    public void StopExperiment()
    {
        if (!running) return;

        // 1) 收尾日志
        eventLogger?.LogEvent("ManualStopButton");
        gazeLogger?.End();
        eventLogger?.End();       // 自动写 StopExperiment

        // 2) 恢复 UI
        foreach (var go in hideDuringRun) if (go) go.SetActive(true);
        if (stopPanel) stopPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        running = false;
    }
}
