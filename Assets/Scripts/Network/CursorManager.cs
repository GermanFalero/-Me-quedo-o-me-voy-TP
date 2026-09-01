using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorBlocker : MonoBehaviour
{
    void LateUpdate()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (escena == "MenuPrincipal")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && SceneManager.GetActiveScene().name != "MenuPrincipal")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}