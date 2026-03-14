using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using Microsoft.MixedReality.Toolkit;
using UnityEngine.Animations;

public class LockSwitch : MonoBehaviour
{
    // Start is called before the first frame update

    // 1.需要控制物体的透明度
    private Renderer objectRenderer;
    private Color objectColor;  // 当前材质颜色

    public float WorldLockTransparentAlpha = 1.0f; // 世界锁定过程中的透明度
    public float BodyLockTransparentAlpha = 0.3f; // 身体锁定过程中的透明度
    public float EyeLockTransparentAlpha = 0.8f; // 视觉锁定过程中的透明度
    private float TransparentSpeed = 5.0f;   // 透明度变化速度
    private float curAlpha = 1.0f;

    // 2. 锁定限制
    public Transform userHead; // 用户的头部Transform，通常是摄像头的父对象
    public float distanceFromCamera = 0.85f; // 物体距离摄像机的距离
    public float distMultiplierOnTheGo = 1.4f;  // object移动时的距离是静止状态下多少倍
    public RotationConstraint RC = null;  // 旋转约束组件
    private float curMultiplier = 1.0f;

    // 3. 计算转动速度
    public float SHeadPerTime = 0.1f;
    private float curSHeadTime = 0.0f;
    private Vector3 lastAngles;     // 上一帧摄像机的视角角度
    private Vector3 SpeedHead = Vector3.zero;   // 当前转动速度

    // 4. 计算移动速度
    public float SBodyPerTime = 0.5f;
    private float curSBodyTime = 0.0f;
    private Vector3 lastPosition;   // 上一帧摄像机的位置
    private Vector3 SpeedBody = Vector3.zero;    // 当前移动速度

    // 5. 控制身体锁定
    private Vector3 initPositionOfObject;   // 物体初始位置
    private Vector3 initPositionOfUsrHead;  // 摄像头初始位置
    public float BodyDistThreshold = 0.8f;    // 与Eye不同，该值用于判断身体锁定的进度
    public float BodyLockSmoothTime = 2.0f;   // 身体锁定时达到目标所需的近似时间（平滑阻尼插值）
    private Vector3 SmoothVelocity = Vector3.zero;  // 当前物体速度

    // 6. 控制视野锁定
    public float EyeLockSmoothTime = 0.5f; 
    public float EyeDistThreshold = 0.1f;    // 防止出现抖动，物体与视线角度大于该角度再锁定
    private float EyeLockNum = 10000.0f;
    // private float angleOffset = 0.0f;
    private Vector3 relativePosition = new Vector3(0, 0, 10); // 物体相对于摄像头的相对位置

    // 7. 速度限制
    public float stationaryThreshold = 0.8f;
    public float SBodyThreshold = 3.0f;
    public Vector3 SHeadThreshold = new Vector3(100.0f, 100.0f, 100.0f);
    

    // 移动速度切换世界锁定和身体锁定
    // 转动速度切换世界锁定和视觉锁定 以及透明度    
    public bool isBodyLockSwitch = true;    // 目前都需要考虑身体锁定。
    public bool isEyeLockSwitch = false;

    // 当同时需要考虑身体锁定和视觉锁定时候，优先身体锁定到身体某个范围再进行视觉锁定。
    public float worldLimitedTime = 2.0f;   // 世界锁定时长
    private float curdeltaTime = 10.0f;     // 当前已有多少时长（初始要大于LimitedTime）

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        objectColor = objectRenderer.material.color; // 获取当前材质颜色
        SetTransparent();

        // 用户头部的旋转约束组件
        RC = gameObject.GetComponent<RotationConstraint>();
        RC.constraintActive = false;
        // 用于计算速度
        lastAngles = userHead.eulerAngles;
        lastPosition = userHead.position;

        // 用于身体锁定
        initPositionOfObject = transform.position;
        initPositionOfUsrHead = userHead.position;

        // 用于视觉锁定
        relativePosition = transform.position - userHead.position;
        // angleOffset = Vector3.Angle(userHead.transform.forward, relativePosition);
    }

    // Update is called once per frame
    void Update()
    {
        // 1.是否切换身体锁定。如果不考虑，直接身体锁定。
        if(!isBodyLockSwitch)
        {
            BodyLock();
            return;
        }

        // 2.计算移动速度和视角转动速度。
        getSpeedBody();
        float bodySpeed = SpeedBody.magnitude;
        if(isEyeLockSwitch)getSpeedHead();  // 只有考虑视觉锁定才计算转动速度。

        // 3.判断在移动还是静止
        if(bodySpeed <= stationaryThreshold)curMultiplier = 1.0f;
        else curMultiplier = distMultiplierOnTheGo;

        // 4.判断是否高速
        if(bodySpeed >= SBodyThreshold)
        {
            // 高速时候直接世界锁定不考虑视觉锁定问题
            curdeltaTime = 0.0f;
            curAlpha = WorldLockTransparentAlpha;
        }
        else
        {
            // 5.慢移速判断是否到达世界锁定限制时间。
            curdeltaTime += Time.deltaTime;
            if(curdeltaTime >= worldLimitedTime)
            {
                curdeltaTime = 10.0f;
                // 6.世界锁定时间结束，开始身体锁定
                if(!isEyeLockSwitch)
                {
                    // 不需要考虑视觉锁定
                    BodyLock();
                }
                else
                {
                    // 7.需要考虑视觉锁定
                    if(SpeedHead.x >= SHeadThreshold.x || SpeedHead.y >= SHeadThreshold.y || SpeedHead.z >= SHeadThreshold.z)
                    {
                        // 高转速
                        curdeltaTime = 0.0f;
                        curAlpha = WorldLockTransparentAlpha;
                        // 防止旋转
                        RC.constraintActive = false;  
                    }
                    else
                    {
                        // 8.开始视觉锁定
                        EyeLock();
                    }
                }
            }
        }
        // 9.设置好位置后，进行渲染，调整透明度之类的。
        SetTransparent();
    }

    void SetTransparent()
    {
        // objectColor.a = curAlpha;
        // 使用 TransparentSpeed 控制透明度变化速度
        objectColor.a = Mathf.Lerp(objectColor.a, curAlpha, Time.deltaTime * TransparentSpeed);
        objectRenderer.material.color = objectColor;

        // 确保使用透明渲染模式
        objectRenderer.material.SetFloat("_Mode", 3); // 3 = Transparent
        objectRenderer.material.renderQueue = 3000; // 透明物体的渲染队列
    }

    void getSpeedHead()
    {
        curSHeadTime += Time.deltaTime;
        if(curSHeadTime >= SHeadPerTime)
        {
            Vector3 currentAngles = userHead.eulerAngles;
            Vector3 deltaAngles = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                float difference = currentAngles[i] - lastAngles[i];
                if (difference > 180f)
                {
                    difference -= 360f;
                }
                else if (difference < -180f)
                {
                    difference += 360f;
                }
                deltaAngles[i] = Math.Abs(difference);
            }
            // Debug.Log("deltaAngles: " + deltaAngles);
            SpeedHead = deltaAngles / curSHeadTime;
            
            lastAngles = currentAngles;
            curSHeadTime = 0.0f;
        }
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

    void BodyLock()
    {
        // 思路是: 摄像头从初始移动多少，那么物体也跟着移动多少。
        Vector3 Targetposition = initPositionOfObject + (userHead.position - initPositionOfUsrHead);
        Vector3 deltaPosition = (Targetposition - userHead.position) * curMultiplier;
        Targetposition = userHead.position + deltaPosition;
        // 如果身体锁定即将结束则开始设置透明度为1.0f
        float distance = (Targetposition - transform.position).magnitude;
        if(distance < BodyDistThreshold)curAlpha = 1.0f;
        else curAlpha = BodyLockTransparentAlpha;

        transform.position = Vector3.SmoothDamp(transform.position, Targetposition, ref SmoothVelocity, BodyLockSmoothTime);
    }

    void EyeLock()
    {
        EyeLockNum += 1;
        // 检查物体是否需要锁定

        // 获取摄像头的旋转四元数
        Quaternion cameraRotation = userHead.rotation;

        // 将四元数转换为旋转矩阵
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(cameraRotation);

        // 使用旋转矩阵将物体的相对坐标转换到新的世界坐标
        Vector3 newRelativePosition = rotationMatrix.MultiplyPoint3x4(relativePosition);


        // 计算物体在世界空间中的位置
        Vector3 Targetposition = userHead.position + newRelativePosition * curMultiplier;
        // Vector3 Targetposition = userHead.position + userHead.forward * distanceFromCamera * curMultiplier;
        
        // 计算物体与摄像头距离值，在该范围内视觉锁定一段时间后停止
        float distance = (Targetposition - transform.position).magnitude;
        float curEyeDistThreshold = EyeDistThreshold * (newRelativePosition.magnitude / distanceFromCamera);
        if(distance >= curEyeDistThreshold)
        {
            EyeLockNum = 0;
            curAlpha = EyeLockTransparentAlpha;
        }
        
        if(EyeLockNum < 500)
        {
            // 将物体移动到指定位置
            transform.position = Vector3.SmoothDamp(transform.position, Targetposition, ref SmoothVelocity, EyeLockSmoothTime);
            // 可以旋转
            RC.constraintActive = true; 
        }
        else  
        {
            EyeLockNum = 10000.0f;
            curAlpha = 1.0f;
            // 防止旋转
            RC.constraintActive = false;
        }
        
    }
}
