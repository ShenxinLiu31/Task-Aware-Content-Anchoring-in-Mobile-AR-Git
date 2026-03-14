using UnityEngine;

public class ButtonGroupController : MonoBehaviour
{
    public GameObject[] targetButtons;
    private bool isVisible = true;

    public void ToggleButtons()
    {
        isVisible = !isVisible;

        foreach (var button in targetButtons)
        {
            if (button != null)
            {
                button.SetActive(isVisible);
            }
        }
    }
}
