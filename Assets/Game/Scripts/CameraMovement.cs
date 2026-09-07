using UnityEngine;
using UnityEngine.InputSystem;

// Move before TowerPlacementManager calculates its mouse raycast.
[DefaultExecutionOrder(-100)]
public class CameraMovement : MonoBehaviour{
    [SerializeField, Min(0f)] private float moveSpeed = 10f;
    [SerializeField, Min(0f)] private float acceleration = 50f;
    [SerializeField, Min(0f)] private float deceleration = 60f;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    [SerializeField] private float minZ = -40f;
    [SerializeField] private float maxZ = 40f;

    private Vector3 currentVelocity;

    private void OnDisable(){
        currentVelocity = Vector3.zero;
    }

    private void Update(){
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || Time.timeScale == 0f){
            currentVelocity = Vector3.zero;
            return;
        }

        float horizontal =
            (keyboard.dKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed ? 1f : 0f);

        float vertical =
            (keyboard.wKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed ? 1f : 0f);

        bool hasInput = horizontal != 0f || vertical != 0f;

        if (!hasInput && currentVelocity == Vector3.zero){
            return;
        }

        // Yaw preserves horizontal directions even with a top-down camera.
        Quaternion heading = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 direction = heading * new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 targetVelocity = direction * moveSpeed;
        float changeRate = hasInput ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            changeRate * Time.unscaledDeltaTime
        );

        Vector3 position = transform.position;
        Vector3 movement = currentVelocity * Time.unscaledDeltaTime;

        position.x = Mathf.Clamp(position.x + movement.x, minX, maxX);
        position.z = Mathf.Clamp(position.z + movement.z, minZ, maxZ);

        if (
            (position.x <= minX && currentVelocity.x < 0f) ||
            (position.x >= maxX && currentVelocity.x > 0f)
        ){
            currentVelocity.x = 0f;
        }

        if (
            (position.z <= minZ && currentVelocity.z < 0f) ||
            (position.z >= maxZ && currentVelocity.z > 0f)
        ){
            currentVelocity.z = 0f;
        }

        transform.position = position;
    }
}
