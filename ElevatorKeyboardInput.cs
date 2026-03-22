using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Allows keyboard shortcuts for calling elevators to floors.
/// Keys 0-9 map to floors 0-9 (0 = Ground).
/// Uses the new Input System package (Unity 6 default).
/// </summary>
public class ElevatorKeyboardInput : MonoBehaviour
{
    private Keyboard keyboard;

    void Update()
    {
        if (ElevatorManager.Instance == null) return;

        keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Number row keys
        CheckKey(keyboard.digit0Key, 0);
        CheckKey(keyboard.digit1Key, 1);
        CheckKey(keyboard.digit2Key, 2);
        CheckKey(keyboard.digit3Key, 3);
        CheckKey(keyboard.digit4Key, 4);
        CheckKey(keyboard.digit5Key, 5);
        CheckKey(keyboard.digit6Key, 6);
        CheckKey(keyboard.digit7Key, 7);
        CheckKey(keyboard.digit8Key, 8);
        CheckKey(keyboard.digit9Key, 9);

        // Numpad keys
        CheckKey(keyboard.numpad0Key, 0);
        CheckKey(keyboard.numpad1Key, 1);
        CheckKey(keyboard.numpad2Key, 2);
        CheckKey(keyboard.numpad3Key, 3);
        CheckKey(keyboard.numpad4Key, 4);
        CheckKey(keyboard.numpad5Key, 5);
        CheckKey(keyboard.numpad6Key, 6);
        CheckKey(keyboard.numpad7Key, 7);
        CheckKey(keyboard.numpad8Key, 8);
        CheckKey(keyboard.numpad9Key, 9);

        // G key for ground floor
        CheckKey(keyboard.gKey, 0);
    }

    private void CheckKey(KeyControl key, int floor)
    {
        if (key.wasPressedThisFrame && floor < ElevatorManager.Instance.totalFloors)
        {
            ElevatorManager.Instance.RequestElevator(floor);
            Debug.Log($"[Keyboard] Requested floor {floor}");
        }
    }
}

