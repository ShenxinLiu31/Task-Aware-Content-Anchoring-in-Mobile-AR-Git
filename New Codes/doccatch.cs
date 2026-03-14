using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class DocFetcher : MonoBehaviour
{
    [Header("PC 上的 txt 地址 (本地测试用 localhost, HoloLens 用 IP)")]
    public string url = "http://localhost/test.txt";

    [Header("显示文本的 TMP 组件")]
    public TextMeshProUGUI textDisplay;

    [Header("刷新间隔 (秒)")]
    public float refreshInterval = 5f;

    void Start()
    {
        if (textDisplay == null)
        {
            Debug.LogError("❌ TextMeshProUGUI 没有绑定！");
            return;
        }

        StartCoroutine(UpdateTextRoutine());
    }

    IEnumerator UpdateTextRoutine()
    {
        while (true)
        {
            Debug.Log("🌐 正在请求: " + url);

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string content = www.downloadHandler.text;
                    textDisplay.text = content;
                    Debug.Log("✅ 获取成功: " + content.Substring(0, Mathf.Min(50, content.Length)) + "...");
                }
                else
                {
                    Debug.LogError("❌ 请求失败: " + www.error);
                    textDisplay.text = "加载失败: " + www.error;
                }
            }

            yield return new WaitForSeconds(refreshInterval);
        }
    }
}
