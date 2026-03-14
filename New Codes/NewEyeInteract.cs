using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;
using UnityEngine.Events;

public class NewEyeInteract : MonoBehaviour
{
    // Start is called before the first frame update
    // 改变大小
    public UnityEvent focusEnter;
    public UnityEvent focusOff;
    public UnityEvent focusIng;


    public void OnFocusEnter()
    {
        focusEnter.Invoke();
    }

    public void OnFocusExit()
    {
        focusOff.Invoke();
    }

    public void OnFocusIng()
    {
        focusIng.Invoke();
    }
}
