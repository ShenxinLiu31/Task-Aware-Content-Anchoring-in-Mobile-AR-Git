using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class displayTime : MonoBehaviour
{
    private TMP_Text timeText;
    private string cur_times;
    private string last_task_times = "nothing...\n";
    private string cur_task_times = "waiting...\n";
    private int task_id = 0;


    void Start()
    {
        timeText = GetComponent<TMP_Text>();
        // 每秒更新一次时间（优化性能）
        InvokeRepeating(nameof(UpdateTime), 0, 0.1f);
    }

    void UpdateTime()
    {
        // 获取当前时间并格式化为 mm:ss:fff
        cur_times = "NowTime: " + System.DateTime.Now.ToString("mm:ss:fff") + "\n";
        string text = cur_times;
        text += "LastTaskTime: " + last_task_times;
        text += "curTaskTime: " + cur_task_times;
        timeText.text = text;
    }

    public void updateTaskTime(string taskTime)
    {
        cur_task_times = "task(" + task_id.ToString() + "): " + taskTime + "s\n";
    }
    public void switchTaskTime()
    {
        task_id += 1;
        last_task_times = cur_task_times;
        cur_task_times = "waiting...\n";
    } 
}
