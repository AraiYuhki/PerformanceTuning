using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector2 speed = Vector2.zero;
    private InputSystem_Actions inputSystem;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        inputSystem = new InputSystem_Actions();
        inputSystem.Disable();
        inputSystem.Player.Enable();
        targetCamera ??= Camera.main;
    }

    private void Update()
    {
        var accel = inputSystem.Player.Move.ReadValue<Vector2>() * Time.deltaTime;
        speed += accel;
        transform.localPosition += new Vector3(speed.x, speed.y, 0f);
        speed *= 0.9f;

        if (targetCamera == null)
        {
            return;
        }

        var worldPos = transform.position;
        var camDistance = worldPos.z - targetCamera.transform.position.z;
        var minBounds = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, camDistance));
        var maxBounds = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, camDistance));
        var clampedX = Mathf.Clamp(worldPos.x, minBounds.x, maxBounds.x);
        var clampedY = Mathf.Clamp(worldPos.y, minBounds.y, maxBounds.y);
        var clamped = !Mathf.Approximately(worldPos.x, clampedX) || !Mathf.Approximately(worldPos.y, clampedY);

        transform.position = new Vector3(clampedX, clampedY, worldPos.z);

        if (clamped)
        {
            speed = Vector2.zero;
        }
    }
}
