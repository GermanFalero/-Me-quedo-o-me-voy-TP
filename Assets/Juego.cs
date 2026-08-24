using UnityEngine;
using UnityEngine.InputSystem; // Sistema moderno de Unity 6

public class Juego : MonoBehaviour
{
    void Start()
    {
        // Se asegura de que el juego empiece con el mouse oculto para mirar en 3D
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("¡El cerebro del Juego está activo!");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
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
    }
}
