using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeChanged : MonoBehaviour
{
    // Start is called before the first frame update
    public float FocusSize = 1.5f;
    private float targetSize = 1.0f;
    private Vector3 initScale = Vector3.zero;
    void Start()
    {
        // 初始化大小
        initScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetScale = initScale * targetSize;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.1f);
    }

    public void ToBigger()
    {
        targetSize = FocusSize;
    }
    
    public void ToSmaller()
    {
        targetSize = 1.0f;
    }
}
