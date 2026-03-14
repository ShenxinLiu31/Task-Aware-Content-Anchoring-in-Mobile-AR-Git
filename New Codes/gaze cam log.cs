// File: GazeAnalytics_Optimized.cs (Full Enhanced Version)
// Additions:
// + Head Pose logging
// + Speed (m/s), Angular Velocity (deg/s)
// + Walk / Static state
// + Full locomotion summary stats

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

[DefaultExecutionOrder(100)]
public class GazeAnalytics_Optimized : MonoBehaviour
{
    [Header("Scene References")]
    public Camera mainCamera;
    public Collider cameraQuad;
    public Collider displayQuad;
    public LayerMask raycastMask = ~0;
    public float raycastDistance = 100f;

    [Header("Saccade Thresholds (degrees)")]
    public float saccadeAngleThresholdDeg = 2.5f;
    public float largeSaccadeThresholdDeg = 7.5f;
    public float saccadeRefractorySec = 0.06f;

    [Header("Logging")]
    public bool writeCsv = true;
    public float csvSampleHz = 20f;

    [Header("Movement Thresholds")]
    public float walkSpeedThreshold = 0.30f;
    public float turnSpeedThreshold = 360f;
    public float stillRelockTime = 0.5f;

    [Header("Debug Options")]
    public bool showDebugRay = true;
    public bool logHitObject = false;

    private Vector3 _lastDir;
    private float _lastSaccadeTime = -999f;
    private int _smallSaccades;
    private int _largeSaccades;

    private enum Region { CameraQuad, DisplayQuad, Outside }
    private Region _currentRegion = Region.Outside;
    private float _regionEnterTime;
    private float _dwellCamera, _dwellDisplay, _dwellOutside;

    private string _csvPath;
    private float _nextCsvTime;
    private string _desktopPath;
    private bool _csvRecording = true;
    private List<string> _csvBuffer = new List<string>(512);

    private string _sceneName;
    private string _timestamp;

    // head movement tracking
    private Vector3 _lastHeadPos;
    private Quaternion _lastHeadRot;
    private float _lastMovementTime;
    private string _locomotionState = "static";

    // locomotion summary stats
    private float _walkTime = 0f;
    private float _staticTime = 0f;
    private int _stateSwitches = 0;

    private float _maxSpeed = 0f;
    private float _maxAngVel = 0f;
    private float _speedSum = 0f;
    private float _angVelSum = 0f;
    private int _speedSamples = 0;
    private string _lastLocomotionState = "static";

    void Awake()
    {
        if (!mainCamera) mainCamera = Camera.main;

        if (!cameraQuad) cameraQuad = GameObject.Find("CameraQuad")?.GetComponent<Collider>();
        if (!displayQuad) displayQuad = GameObject.Find("DisplayQuad")?.GetComponent<Collider>();

        _sceneName = SceneManager.GetActiveScene().name;
        _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        Directory.CreateDirectory(_desktopPath);

        _lastHeadPos = mainCamera.transform.position;
        _lastHeadRot = mainCamera.transform.rotation;

        if (writeCsv)
        {
            _csvPath = Path.Combine(_desktopPath, $"gaze_{_sceneName}_{_timestamp}.csv");
            File.WriteAllText(_csvPath,
                "Time,Region," +
                "GazeOriginX,GazeOriginY,GazeOriginZ," +
                "GazeDirX,GazeDirY,GazeDirZ," +
                "HitX,HitY,HitZ," +
                "PosX,PosY,PosZ," +
                "RotX,RotY,RotZ," +
                "Speed,AngVel,LocomotionState\n"
            );
        }
    }

    void Update()
    {
        var gazeRay = GetGazeRay();
        TrackSaccades(gazeRay.direction);

        Vector3 hitPoint;
        var region = ClassifyRegion(gazeRay, out hitPoint);
        UpdateDwell(region);

        if (showDebugRay)
            Debug.DrawRay(gazeRay.origin, gazeRay.direction * 5f, Color.red);

        Vector3 headPos = mainCamera.transform.position;
        Quaternion headRot = mainCamera.transform.rotation;

        float speed = (headPos - _lastHeadPos).magnitude / Time.deltaTime;
        float angVel = Quaternion.Angle(_lastHeadRot, headRot) / Time.deltaTime;

        // locomotion state
        if (speed > walkSpeedThreshold || angVel > turnSpeedThreshold)
        {
            _locomotionState = "walk";
            _lastMovementTime = Time.time;
        }
        else if (Time.time - _lastMovementTime > stillRelockTime)
        {
            _locomotionState = "static";
        }

        // locomotion summary stats
        _speedSum += speed;
        _angVelSum += angVel;
        _maxSpeed = Mathf.Max(_maxSpeed, speed);
        _maxAngVel = Mathf.Max(_maxAngVel, angVel);
        _speedSamples++;

        if (_locomotionState == "walk")
            _walkTime += Time.deltaTime;
        else
            _staticTime += Time.deltaTime;

        if (_locomotionState != _lastLocomotionState)
        {
            _stateSwitches++;
            _lastLocomotionState = _locomotionState;
        }

        _lastHeadPos = headPos;
        _lastHeadRot = headRot;

        // save sample
        if (writeCsv && _csvRecording && Time.time >= _nextCsvTime)
        {
            _nextCsvTime = Time.time + 1f / Mathf.Max(1f, csvSampleHz);
            AppendCsv(Time.time, region, gazeRay.origin, gazeRay.direction, hitPoint, headPos, headRot, speed, angVel);
        }
    }

    void OnDestroy()
    {
        FlushCsv();
        SaveSummaryTxt();
    }

    // gaze ray
    private Ray GetGazeRay()
    {
        var eye = CoreServices.InputSystem?.EyeGazeProvider;
        if (eye != null && eye.IsEyeTrackingDataValid)
            return new Ray(eye.GazeOrigin, eye.GazeDirection.normalized);

        var camTr = mainCamera.transform;
        return new Ray(camTr.position, camTr.forward);
    }

    private void TrackSaccades(Vector3 currDir)
    {
        if (_lastDir == Vector3.zero) { _lastDir = currDir; return; }
        float angle = Vector3.Angle(_lastDir, currDir);
        _lastDir = currDir;

        if (angle >= saccadeAngleThresholdDeg &&
            (Time.time - _lastSaccadeTime) >= saccadeRefractorySec)
        {
            _lastSaccadeTime = Time.time;
            if (angle >= largeSaccadeThresholdDeg) _largeSaccades++;
            else _smallSaccades++;
        }
    }

    private Region ClassifyRegion(Ray gazeRay, out Vector3 hitPoint)
    {
        hitPoint = gazeRay.origin + gazeRay.direction * 2f;

        if (cameraQuad && cameraQuad.Raycast(gazeRay, out RaycastHit hitA, raycastDistance))
        {
            hitPoint = hitA.point;
            return Region.CameraQuad;
        }

        if (displayQuad && displayQuad.Raycast(gazeRay, out RaycastHit hitB, raycastDistance))
        {
            hitPoint = hitB.point;
            return Region.DisplayQuad;
        }

        if (Physics.Raycast(gazeRay, out RaycastHit hit, raycastDistance, raycastMask))
            hitPoint = hit.point;

        return Region.Outside;
    }

    private void UpdateDwell(Region newRegion)
    {
        if (newRegion != _currentRegion)
        {
            float dt = Time.time - _regionEnterTime;
            AccumulateDwell(_currentRegion, dt);
            _currentRegion = newRegion;
            _regionEnterTime = Time.time;
        }
        else AccumulateDwell(_currentRegion, Time.deltaTime);
    }

    private void AccumulateDwell(Region r, float dt)
    {
        switch (r)
        {
            case Region.CameraQuad: _dwellCamera += dt; break;
            case Region.DisplayQuad: _dwellDisplay += dt; break;
            case Region.Outside: _dwellOutside += dt; break;
        }
    }

    private void AppendCsv(
        float t, Region r, Vector3 o, Vector3 d, Vector3 hit,
        Vector3 pos, Quaternion rot, float speed, float angVel)
    {
        string line = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:F3},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4}," +
            "{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13:F4}," +
            "{14:F2},{15:F2},{16:F2},{17:F3},{18:F3},{19}\n",
            t, r,
            o.x, o.y, o.z, d.x, d.y, d.z,
            hit.x, hit.y, hit.z,
            pos.x, pos.y, pos.z,
            rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z,
            speed, angVel, _locomotionState
        );

        _csvBuffer.Add(line);
        if (_csvBuffer.Count >= 100) FlushCsv();
    }

    private void FlushCsv()
    {
        if (_csvBuffer.Count > 0)
        {
            File.AppendAllLines(_csvPath, _csvBuffer);
            _csvBuffer.Clear();
        }
    }

    public void SaveSummaryTxt()
    {
        float avgSpeed = _speedSamples > 0 ? _speedSum / _speedSamples : 0f;
        float avgAngVel = _speedSamples > 0 ? _angVelSum / _speedSamples : 0f;
        float totalTime = _walkTime + _staticTime;
        float walkPct = totalTime > 0 ? (_walkTime / totalTime * 100f) : 0f;
        float staticPct = totalTime > 0 ? (_staticTime / totalTime * 100f) : 0f;

        string summaryPath = Path.Combine(_desktopPath, $"summary_{_sceneName}_{_timestamp}.txt");

        string txt =
$@"[GazeAnalytics Summary]
Scene: {_sceneName}
Created: {_timestamp}

--- Eye Saccades ---
Small Saccades: {_smallSaccades}
Large Saccades: {_largeSaccades}

--- Dwell Time (sec) ---
CameraQuad : {_dwellCamera:F3}
DisplayQuad: {_dwellDisplay:F3}
Outside    : {_dwellOutside:F3}

--- Locomotion ---
Total Time:   {totalTime:F2}s
Walk Time:    {_walkTime:F2}s ({walkPct:F1}%)
Static Time:  {_staticTime:F2}s ({staticPct:F1}%)
State Switches: {_stateSwitches}

--- Speed (m/s) ---
Avg Speed: {avgSpeed:F3}
Max Speed: {_maxSpeed:F3}

--- Angular Velocity (deg/s) ---
Avg AngVel: {avgAngVel:F3}
Max AngVel: {_maxAngVel:F3}

--- Thresholds ---
walkSpeedThreshold = {walkSpeedThreshold}
turnSpeedThreshold = {turnSpeedThreshold}
stillRelockTime   = {stillRelockTime}s
";

        File.WriteAllText(summaryPath, txt);
        Debug.Log($"[GazeAnalytics] Summary saved: {summaryPath}");
    }

    public void StartCsv() { _csvRecording = true; }
    public void StopCsv() { _csvRecording = false; }
}