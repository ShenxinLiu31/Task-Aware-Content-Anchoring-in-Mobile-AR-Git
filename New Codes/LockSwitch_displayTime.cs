using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using Microsoft.MixedReality.Toolkit;
using UnityEngine.Animations;

public class LockSwitch_displayTime : MonoBehaviour
{
    // Start is called before the first frame update

    // 1.需要控制物体的透明度
    private Renderer objectRenderer;
    private Color objectColor;  // 当前材质颜色

    public float highSpeedTransparentAlpha = 0.0f; // 世界锁定过程中的透明度
    public float LowSpeedTransparentAlpha = 1.0f; // 视觉锁定过程中的透明度
    private float TransparentSpeed = 5.0f;   // 透明度变化速度
    private float curAlpha = 1.0f;

    // 2. 锁定限制
    public Transform userHead; // 用户的头部Transform，通常是摄像头的父对象
    public RotationConstraint RC = null;  // 旋转约束组件
    private float curMultiplier = 1.0f;  // 物体距离头部距离的倍数
    private Vector3 SmoothVelocity = Vector3.zero;  // 当前物体速度
    public float SmoothTime = 0.1f; // 锁定过程中的平滑时间
    private Vector3 relativePosition = new Vector3(0, 0, 10); // 物体相对于摄像头的相对位置
    private Vector3 curTargetPosition = Vector3.zero; // 用于世界锁定

    // 计算速度
    public float SBodyPerTime = 0.1f;
    private float curSBodyTime = 0.0f;
    private Vector3 lastPosition;   // 上一帧摄像机的位置
    private Vector3 SpeedBody = Vector3.zero;    // 当前移动速度
    public float stationaryThreshold = 0.5f;
    public float highSpeedThreshold = 2.0f;


    // 以下为新添加参数
    private bool is_WorldLock = false;  // 当前是否为世界锁定(身体锁定)
    public float move_to_task_time = 0.3f; // 用于锁定切换,当维持新状态达这个时间,则切换为新的锁定。
    public float task_to_move_time = 0.1f; // 用于锁定切换,当维持新状态达这个时间,则切换为新的锁定。
    private float cur_switch_time = 0.0f;
    private float taskTime = 0.0f;

    // 子对象
    public Transform childObject;
    displayTime child;
    
    void Start()
    {
        // 初始化
        objectRenderer = GetComponent<Renderer>();
        objectColor = objectRenderer.material.color; // 获取当前材质颜色
        SetTransparent();

        // 用户头部的旋转约束组件
        RC = gameObject.GetComponent<RotationConstraint>();
        RC.constraintActive = false;

        // 用于视觉锁定
        relativePosition = transform.position - userHead.position;
        curTargetPosition = transform.position;

        // 子对象
        child = childObject.GetComponent<displayTime>();
    }

    // Update is called once per frame
    void Update()
    {
        // 先设置透明度
        SetTransparent();

        if(is_WorldLock)
        {
            // 静止做任务==世界锁定
            taskTime += Time.deltaTime;
            child.updateTaskTime(taskTime.ToString());
            RC.constraintActive = false;
            transform.position = Vector3.SmoothDamp(transform.position, curTargetPosition, ref SmoothVelocity, SmoothTime);
        }
        else
        {
            // 移动==视觉锁定
            RC.constraintActive = true;
            EyeLock();
        }
        
        // 计算速度判断接下来应该是哪种锁定
        getSpeedBody();
        float bodySpeed = SpeedBody.magnitude;

        // 先判断物体接下来应该是静止还是移动
        if(bodySpeed > stationaryThreshold)
        {
            if(is_WorldLock)
            {
                // 现在静止，接下来移动
                cur_switch_time += Time.deltaTime;
                if(cur_switch_time >= task_to_move_time)
                {
                    // 可以切换
                    is_WorldLock = false;
                    cur_switch_time = 0.0f;

                    // 操作taskTime, 此时的taskTime是任务的进展时间
                    child.switchTaskTime();
                }
            }

            // 确定透明度
            if(bodySpeed >= highSpeedThreshold)
            {
                // 高速时候物体透明不显示
                curAlpha = highSpeedTransparentAlpha;
            }
            else
            {
                curAlpha = LowSpeedTransparentAlpha;
            }
        }
        else
        {
            if(!is_WorldLock)
            {
                // 现在移动，接下来静止
                cur_switch_time += Time.deltaTime;
                if(cur_switch_time >= move_to_task_time)
                {
                    // 可以切换
                    is_WorldLock = true;
                    cur_switch_time = 0.0f;

                    taskTime = 0.0f; // 重置taskTime
                }
            }

            curAlpha = LowSpeedTransparentAlpha;
        }

    }

    void SetTransparent()
    {
        // 使用 TransparentSpeed 控制透明度变化速度
        objectColor.a = Mathf.Lerp(objectColor.a, curAlpha, Time.deltaTime * TransparentSpeed);
        objectRenderer.material.color = objectColor;

        // 确保使用透明渲染模式
        objectRenderer.material.SetFloat("_Mode", 3); // 3 = Transparent
        objectRenderer.material.renderQueue = 3000; // 透明物体的渲染队列
    }

    void EyeLock()
    {
        // 获取摄像头的旋转四元数
        Quaternion cameraRotation = userHead.rotation;

        // 将四元数转换为旋转矩阵
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(cameraRotation);

        // 使用旋转矩阵将物体的相对坐标转换到新的世界坐标
        Vector3 newRelativePosition = rotationMatrix.MultiplyPoint3x4(relativePosition);

        // 计算物体在世界空间中的位置
        Vector3 Targetposition = userHead.position + newRelativePosition * curMultiplier;
        // Vector3 Targetposition = userHead.position + userHead.forward * distanceFromCamera * curMultiplier;
        
        // 移动的时候, 始终视觉锁定, 不需要在规定距离内在停止 
        transform.position = Vector3.SmoothDamp(transform.position, Targetposition, ref SmoothVelocity, SmoothTime);
        
        curTargetPosition = Targetposition;
    }

    void getSpeedBody()
    {
        curSBodyTime += Time.deltaTime;
        if(curSBodyTime >= SBodyPerTime)
        {
            Vector3 curPosition = userHead.position;
            SpeedBody = (curPosition - lastPosition) / curSBodyTime;

            // 更新上一帧位置
            lastPosition = curPosition;
            curSBodyTime = 0.0f;
        }
    }

}

