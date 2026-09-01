using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Controles de teclado")]
    public Key teclaAdelante = Key.W;
    public Key teclaAtras = Key.S;
    public Key teclaIzquierda = Key.A;
    public Key teclaDerecha = Key.D;
    public Key teclaSaltar = Key.Space;
    public Key teclaCorrer = Key.LeftShift;
    public Key teclaRespawn = Key.R;

    private Keyboard keyboard;

    void Update()
    {
        keyboard = Keyboard.current;
        if (keyboard == null) return;
    }

    public Vector2 ObtenerMovimiento()
    {
        if (keyboard == null) return Vector2.zero;

        float x = 0f;
        float z = 0f;

        if (keyboard[teclaAdelante].isPressed || keyboard.upArrowKey.isPressed) z = 1f;
        if (keyboard[teclaAtras].isPressed || keyboard.downArrowKey.isPressed) z = -1f;
        if (keyboard[teclaIzquierda].isPressed || keyboard.leftArrowKey.isPressed) x = -1f;
        if (keyboard[teclaDerecha].isPressed || keyboard.rightArrowKey.isPressed) x = 1f;

        return new Vector2(x, z);
    }

    public bool SaltarPresionado()
    {
        return keyboard != null && keyboard[teclaSaltar].wasPressedThisFrame;
    }

    public bool SaltarMantenido()
    {
        return keyboard != null && keyboard[teclaSaltar].isPressed;
    }

    public bool CorrerPresionado()
    {
        return keyboard != null && keyboard[teclaCorrer].isPressed;
    }

    public bool RespawnPresionado()
    {
        return keyboard != null && keyboard[teclaRespawn].wasPressedThisFrame;
    }
}