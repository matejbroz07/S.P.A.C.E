using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public VitalsController vitalsController;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    public float maxLookAngle = 85f;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    [Header("Air Control")]
    public float airControl = 0.4f;

    [Header("View Bobbing")]
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.05f;

    private CharacterController controller;
    private Vector3 velocity;
    private float cameraPitch;
    private Vector3 horizontalVelocity = Vector3.zero;
    private float bobTimer;
    private Vector3 originalCameraPos;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform != null)
            originalCameraPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        HandleMovement();
        if(!InventoryHandler.Instance.IsInventoryOpen) HandleCamera();
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDir = (transform.forward * input.y + transform.right * input.x).normalized;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        bool wantsToSprint = sprintAction.action.IsPressed() && input.magnitude > 0.1f; // Chce sprintovat a hýbe se
        bool canSprint = vitalsController != null && vitalsController.CanSprint(); // Má na to energii

        bool isSprinting = wantsToSprint && canSprint;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        
        // NOVÉ: Volání logiky staminy
        if (vitalsController != null)
        {
            vitalsController.HandleStamina(isSprinting);
        }

        if (isGrounded)
        {
            horizontalVelocity = moveDir * currentSpeed;
        }
        else
        {
            float airAcceleration = 3f;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDir * currentSpeed, airAcceleration * Time.deltaTime);
        }

        controller.Move(horizontalVelocity * Time.deltaTime);

        if (jumpAction.action.WasPressedThisFrame() && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }


    private void HandleCamera()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>() * mouseSensitivity * Time.deltaTime;

        cameraPitch -= lookInput.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * lookInput.x);

        HandleBobbing();
    }

    private void HandleBobbing()
    {
        if (controller.isGrounded && horizontalVelocity.magnitude > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            cameraTransform.localPosition = originalCameraPos + new Vector3(0, bobOffset, 0);
        }
        else
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalCameraPos, Time.deltaTime * 10f);
        }
    }
}
