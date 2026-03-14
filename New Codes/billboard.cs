using UnityEngine;

public class BillboardFaceUser : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            // 让物体的 forward 始终朝向相机（即用户）
            transform.forward = Camera.main.transform.forward;
        }
    }
}
