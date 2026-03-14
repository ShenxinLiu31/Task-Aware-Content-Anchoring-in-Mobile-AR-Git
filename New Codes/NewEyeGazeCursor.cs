using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class NewEyeGazeCursor : MonoBehaviour
{

    public Transform userHead;
    public List<GameObject> gameObjects;
    public float defaultDistance = 2.0f;
    public float smoothingFactor = 20f;
    public float minScale = 0.7f;
    public float maxScale = 1.1f;
    public float scaleDistanceFactor = 0.5f;

    // 眼动光标的大小和位置
    private Vector3 targetPosition;
    private float targetSize;
    private Vector3 initScale = Vector3.one;

    // 用于判断目光是否在某个物体内。
    private bool curInObject = false;
    private bool LastInObject = false;
    private GameObject LastTargetObject;
    private GameObject TargetObject;
    private Vector3 curTargetPosition = Vector3.zero;

    void Start()
    {
        initScale = transform.localScale;
    }

    void Update()
    {
        // 检测实现,将前方物体进行操作
        RayInObject();
        // 更新眼动光标的状态
        upDateCursor();
    }

    private void RayInObject()
    {
        Ray ray = new Ray(userHead.position, userHead.forward);
        curInObject = false;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            foreach (GameObject targetObject in gameObjects)
            {
                if (hit.collider.gameObject == targetObject)
                {
                    curInObject = true;
                    TargetObject = targetObject;
                    curTargetPosition = hit.point;
                    break;
                }
            }
        }
        // 判断情况
        if (LastInObject && !curInObject)
        {
            // 之前有物体，现在没物体。表示穿过了LastTargetObject
            LastTargetObject.GetComponent<NewEyeInteract>().OnFocusExit();
        }
        else if (LastInObject && curInObject)
        {
            // 之前有物体，现在有物体。
            if (LastTargetObject != TargetObject)
            {
                // 换物体了
                LastTargetObject.GetComponent<NewEyeInteract>().OnFocusExit();
                TargetObject.GetComponent<NewEyeInteract>().OnFocusEnter();
                LastTargetObject = TargetObject;
            }
            else
            {
                // 仍然在LastTargetObject内，此时为注视中
                TargetObject.GetComponent<NewEyeInteract>().OnFocusIng();
            }
        }
        else if (!LastInObject && curInObject)
        {
            // 之前没物体，现在有物体。表示刚注视新的物体
            TargetObject.GetComponent<NewEyeInteract>().OnFocusEnter();
            LastTargetObject = TargetObject;
        }
        // 最后一种情况，之前没物体。现在没物体。表示 gaze 不在物体上，无需操作。

        // 更新 LastInObject
        LastInObject = curInObject;
    }

    private void upDateCursor()
    {
        if (!curInObject)
        {
            // 显示在默认距离
            targetPosition = userHead.position + userHead.forward * defaultDistance;
        }
        else
        {
            targetPosition = curTargetPosition;
        }
        // 平滑移动
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothingFactor
        );
        // 根据距离调整大小
        float distance = Vector3.Distance(transform.position, userHead.position);
        targetSize = Mathf.Clamp(distance * scaleDistanceFactor, minScale, maxScale);
        transform.localScale = initScale * targetSize;
    }
}
