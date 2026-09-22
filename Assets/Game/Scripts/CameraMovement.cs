using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class CameraMovement : MonoBehaviour {
    [SerializeField, Min(0f)] private float moveSpeed = 20f;
    [SerializeField, Min(0f)] private float acceleration = 90f;
    [SerializeField, Min(0f)] private float deceleration = 110f;
    [SerializeField] private bool edgeScrolling = true;
    [SerializeField, Range(4f, 64f)] private float edgeScrollThickness = 18f;
    [SerializeField, Range(0.03f, 0.5f)] private float rotationSensitivity = 0.12f;
    [SerializeField, Range(0.15f, 0.5f)] private float doubleClickWindow = 0.28f;
    [SerializeField, Min(0.1f)] private float neutralReturnDuration = 0.38f;
    [Header("Cursor")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Vector2 normalCursorHotspot = new Vector2(11f, 3f);
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    [SerializeField] private float minZ = -40f;
    [SerializeField] private float maxZ = 40f;
    private Vector3 currentVelocity;
    private Quaternion baseRotation;
    private MMF_Player movementFeedbacks;
    private MMF_Player rotationFeedbacks;
    private bool wasMoving;
    private WaveManager waveManager;
    private TowerPlacementManager placementManager;
    private bool rotating;
    private float cameraYaw;
    private float lastRightClickTime = -10f;
    private Coroutine neutralReturn;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisibility;
    private Vector2 cursorPositionBeforeRotation;
    private bool cursorInitialized;

    private void Awake(){
        baseRotation = transform.rotation;
        cameraYaw = baseRotation.eulerAngles.y;

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

        GameObject rotationFeedbackObject =
            new GameObject("Camera Rotation Feedbacks");
        rotationFeedbackObject.transform.SetParent(transform, false);
        rotationFeedbacks = rotationFeedbackObject.AddComponent<MMF_Player>();
        rotationFeedbacks.AddFeedback(new MMF_CameraFieldOfView{
            Duration = 0.24f,
            RelativeFieldOfView = true,
            RemapFieldOfViewZero = 0f,
            RemapFieldOfViewOne = 0.18f,
            ResetShakerValuesAfterShake = true,
            ResetTargetValuesAfterShake = true
        });
        rotationFeedbacks.Initialization();
    }

    private void Start(){
        waveManager = FindFirstObjectByType<WaveManager>();
        placementManager = FindFirstObjectByType<TowerPlacementManager>();
        SetGameplayCursor();
    }

    private void OnDisable(){
        EndRotation();
        currentVelocity = Vector3.zero;
        wasMoving = false;
        movementFeedbacks?.StopFeedbacks();
        rotationFeedbacks?.StopFeedbacks();
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        cursorInitialized = false;
        transform.rotation = baseRotation;
        cameraYaw = baseRotation.eulerAngles.y;
    }

    private void OnApplicationFocus(bool hasFocus){
        if (!hasFocus){
            EndRotation();
        }
    }

    private void Update(){
        if (
            RunUpgradeState.Current.BlocksInput ||
            (waveManager != null && !waveManager.GameplayReady)
        ){
            EndRotation();
            SetGameplayCursor();
            StopMovement();
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (Time.timeScale == 0f){
            EndRotation();
            SetGameplayCursor();
            StopMovement();
            return;
        }

        HandleRotationInput();

        float horizontal = !rotating
            ? (keyboard != null && keyboard.dKey.isPressed ? 1f : 0f) -
                (keyboard != null && keyboard.aKey.isPressed ? 1f : 0f)
            : 0f;

        float vertical = !rotating
            ? (keyboard != null && keyboard.wKey.isPressed ? 1f : 0f) -
                (keyboard != null && keyboard.sKey.isPressed ? 1f : 0f)
            : 0f;

        Vector2 edgeInput = rotating ? Vector2.zero : GetEdgeScrollInput();
        horizontal = Mathf.Clamp(horizontal + edgeInput.x, -1f, 1f);
        vertical = Mathf.Clamp(vertical + edgeInput.y, -1f, 1f);

        bool hasInput = horizontal != 0f || vertical != 0f;

        if (hasInput && !wasMoving){
            movementFeedbacks?.PlayFeedbacks(transform.position);
        }
        wasMoving = hasInput;

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

    private void HandleRotationInput(){
        Mouse mouse = Mouse.current;
        if (mouse == null){
            EndRotation();
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame){
            // Right click keeps its established cancel action while building.
            if (placementManager != null && placementManager.IsBuildMode){
                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject()){
                return;
            }

            float now = Time.unscaledTime;
            bool doubleClick = now - lastRightClickTime <= doubleClickWindow;
            lastRightClickTime = now;

            if (doubleClick){
                EndRotation();
                StartNeutralReturn();
                return;
            }

            BeginRotation();
        }

        if (!rotating){
            return;
        }

        if (mouse.rightButton.wasReleasedThisFrame ||
            !mouse.rightButton.isPressed){
            EndRotation();
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        cameraYaw += delta.x * rotationSensitivity;
        transform.rotation = Quaternion.Euler(
            baseRotation.eulerAngles.x,
            cameraYaw,
            baseRotation.eulerAngles.z
        );
    }

    private void BeginRotation(){
        if (neutralReturn != null){
            StopCoroutine(neutralReturn);
            neutralReturn = null;
        }

        rotating = true;
        StopMovement();
        previousCursorLock = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        if (Mouse.current != null){
            cursorPositionBeforeRotation = Mouse.current.position.ReadValue();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rotationFeedbacks?.PlayFeedbacks(transform.position);
    }

    private void EndRotation(){
        if (!rotating){
            return;
        }

        rotating = false;
        Cursor.lockState = previousCursorLock;
        if (previousCursorLock == CursorLockMode.None && Mouse.current != null){
            Vector2 restoredPosition = new Vector2(
                Mathf.Clamp(cursorPositionBeforeRotation.x, 0f, Screen.width),
                Mathf.Clamp(cursorPositionBeforeRotation.y, 0f, Screen.height)
            );
            Mouse.current.WarpCursorPosition(restoredPosition);
        }
        Cursor.visible = previousCursorVisibility;
        SetGameplayCursor();
    }

    private void StartNeutralReturn(){
        if (neutralReturn != null){
            StopCoroutine(neutralReturn);
        }

        neutralReturn = StartCoroutine(ReturnToNeutral());
    }

    private IEnumerator ReturnToNeutral(){
        Quaternion startingRotation = transform.rotation;
        rotationFeedbacks?.PlayFeedbacks(transform.position);
        float elapsed = 0f;

        while (elapsed < neutralReturnDuration){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / neutralReturnDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.rotation = Quaternion.Slerp(
                startingRotation,
                baseRotation,
                eased
            );
            yield return null;
        }

        transform.rotation = baseRotation;
        cameraYaw = baseRotation.eulerAngles.y;
        neutralReturn = null;
    }

    private Vector2 GetEdgeScrollInput(){
        Mouse mouse = Mouse.current;
        if (
            !edgeScrolling ||
            mouse == null ||
            !Application.isFocused ||
            (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
        ){
            return Vector2.zero;
        }

        Vector2 pointer = mouse.position.ReadValue();
        if (
            pointer.x < 0f || pointer.y < 0f ||
            pointer.x > Screen.width || pointer.y > Screen.height
        ){
            return Vector2.zero;
        }

        float horizontal = pointer.x <= edgeScrollThickness ? -1f :
            pointer.x >= Screen.width - edgeScrollThickness ? 1f : 0f;
        float vertical = pointer.y <= edgeScrollThickness ? -1f :
            pointer.y >= Screen.height - edgeScrollThickness ? 1f : 0f;
        return new Vector2(horizontal, vertical);
    }

    private void StopMovement(){
        currentVelocity = Vector3.zero;
        wasMoving = false;
    }

    private void SetGameplayCursor(){
        if (cursorInitialized && normalCursor != null){
            return;
        }

        cursorInitialized = true;
        Cursor.SetCursor(
            normalCursor,
            normalCursorHotspot,
            CursorMode.ForceSoftware
        );
    }

}
