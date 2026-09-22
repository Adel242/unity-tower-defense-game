using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class CameraMovement : MonoBehaviour {
    [SerializeField, Min(0f)] private float moveSpeed = 20f;
    [SerializeField, Min(0f)] private float acceleration = 90f;
    [SerializeField, Min(0f)] private float deceleration = 110f;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    [SerializeField] private float minZ = -40f;
    [SerializeField] private float maxZ = 40f;
    private Vector3 currentVelocity;
    private Quaternion baseRotation;
    private MMF_Player movementFeedbacks;
    private bool wasMoving;

    private void Awake(){
        baseRotation = transform.rotation;

        if (GetComponent<MMCameraFieldOfViewShaker>() == null){
            gameObject.AddComponent<MMCameraFieldOfViewShaker>();
        }

        movementFeedbacks = gameObject.AddComponent<MMF_Player>();
        movementFeedbacks.AddFeedback(new MMF_CameraFieldOfView{
            Duration = 0.4f,
            RelativeFieldOfView = true,
            RemapFieldOfViewZero = 0f,
            RemapFieldOfViewOne = 0.1f,
            ResetShakerValuesAfterShake = true,
            ResetTargetValuesAfterShake = true
        });
        movementFeedbacks.Initialization();
    }

    private void OnDisable(){
        currentVelocity = Vector3.zero;
        wasMoving = false;
        movementFeedbacks?.StopFeedbacks();
        transform.rotation = baseRotation;
    }

    private void Update(){
        if (RunUpgradeState.Current.BlocksInput) return;
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || Time.timeScale == 0f){
            currentVelocity = Vector3.zero;
            wasMoving = false;
            return;
        }

        float horizontal =
            (keyboard.dKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed ? 1f : 0f);

        float vertical =
            (keyboard.wKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed ? 1f : 0f);

        bool hasInput = horizontal != 0f || vertical != 0f;

        if (hasInput && !wasMoving){
            movementFeedbacks?.PlayFeedbacks(transform.position);
        }
        wasMoving = hasInput;

        if (!hasInput && currentVelocity == Vector3.zero){
            return;
        }

        // Yaw preserves horizontal directions even with a top-down camera.
        Quaternion heading = Quaternion.Euler(0f, baseRotation.eulerAngles.y, 0f);
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
