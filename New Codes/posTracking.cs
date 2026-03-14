using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class posTracking : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform usrHead;
    // public float distance = 1.0f;
    private Vector3 initUserHead;
    private Vector3 initPosition;

    void Start()
    {
        initPosition = transform.position;
        initUserHead = usrHead.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = initPosition + usrHead.position - initUserHead;

    }
}
