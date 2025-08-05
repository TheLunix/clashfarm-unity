using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public InputField passwordInput = null;

    public void ToggleInputType()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = (passwordInput.contentType == InputField.ContentType.Password) ?
                InputField.ContentType.Standard : InputField.ContentType.Password;

            passwordInput.ForceLabelUpdate();
        }
    }
}