using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class EyeGazeCursor : MonoBehaviour
{
    public float defaultDistance = 2.0f;
    public float maxRayDistance = 10.0f;
    public LayerMask raycastLayers = Physics.DefaultRaycastLayers;

    private void Update()
    {
        Debug.Log("EyeGazeCursor running..."); // ✅ 用于调试

        var eyeProvider = CoreServices.InputSystem?.EyeGazeProvider;

        if (eyeProvider == null)
        {
            Debug.LogWarning("EyeGazeProvider is NULL. Eye tracking service not found.");
            return;
        }

        if (!eyeProvider.IsEyeTrackingEnabledAndValid)
        {
            Debug.Log("Eye tracking not enabled or not valid.");
            return;
        }

        Vector3 origin = eyeProvider.GazeOrigin;
        Vector3 direction = eyeProvider.GazeDirection;

        Debug.DrawRay(origin, direction * maxRayDistance, Color.green);  // 调试射线

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, maxRayDistance, raycastLayers))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.LookRotation(hit.normal);
        }
        else
        {
            transform.position = origin + direction * defaultDistance;
            transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
        }
    }
}
