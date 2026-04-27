using UnityEngine;
using UnityEngine.InputSystem;

public class LeopardMovementAnimated : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 7f;
    [SerializeField] private float reverseSpeed = 4f;
    [SerializeField] private float turnSpeed = 90f;

    [Header("Smoothing")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float turnAcceleration = 10f;

    [Header("Track Materials")]
    [SerializeField] private Material leftTrackMaterial;
    [SerializeField] private Material rightTrackMaterial;

    [Header("Track Animation")]
    [SerializeField] private float linearScrollMultiplier = 1.0f;
    [SerializeField] private float turnScrollMultiplier = 1.5f;
    [SerializeField] private bool invertLeftTrack = false;
    [SerializeField] private bool invertRightTrack = false;
    [SerializeField] private string uvOffsetProperty = "_UvOffset";

    private Rigidbody rb;

    private float moveInput;
    private float turnInput;

    private float currentMoveSpeed;
    private float currentTurnSpeed;

    private float leftCurrentYOffset;
    private float rightCurrentYOffset;

    private void Awake() {
        rb = GetComponentInParent<Rigidbody>();
    }

    private void Update() {
        ReadKeyboardInput();
    }

    private void FixedUpdate() {
        ApplyMovement();
        ApplyRotation();
        UpdateTrackScroll();
    }

    private void ReadKeyboardInput() {
        moveInput = 0f;
        turnInput = 0f;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            moveInput += 1f;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            moveInput -= 1f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            turnInput -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            turnInput += 1f;
    }

    private void ApplyMovement() {
        float targetSpeed = 0f;

        if (moveInput > 0f)
            targetSpeed = moveInput * forwardSpeed;
        else if (moveInput < 0f)
            targetSpeed = moveInput * reverseSpeed;

        float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(currentMoveSpeed) ? acceleration : deceleration;
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        Vector3 movement = transform.forward * currentMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void ApplyRotation() {
        float turnFactor = Mathf.Abs(currentMoveSpeed) > 0.1f ? 1f : 0.45f;
        float targetTurn = turnInput * turnSpeed * turnFactor;

        currentTurnSpeed = Mathf.MoveTowards(
            currentTurnSpeed,
            targetTurn,
            turnAcceleration * turnSpeed * Time.fixedDeltaTime
        );

        Quaternion deltaRotation = Quaternion.Euler(0f, currentTurnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void UpdateTrackScroll() {
        if (leftTrackMaterial == null || rightTrackMaterial == null)
            return;

        // Linear contribution: both tracks same direction.
        float linearComponent = currentMoveSpeed * linearScrollMultiplier;

        // Turning contribution:
        // right turn => left track forward faster, right track slower/backward.
        float turnComponent = currentTurnSpeed * turnScrollMultiplier;

        float leftTrackScrollSpeed = linearComponent + turnComponent;
        float rightTrackScrollSpeed = linearComponent - turnComponent;

        if (invertLeftTrack)
            leftTrackScrollSpeed *= -1f;

        if (invertRightTrack)
            rightTrackScrollSpeed *= -1f;

        leftCurrentYOffset = Mathf.Repeat(
            leftCurrentYOffset + leftTrackScrollSpeed * Time.fixedDeltaTime,
            1f
        );

        rightCurrentYOffset = Mathf.Repeat(
            rightCurrentYOffset + rightTrackScrollSpeed * Time.fixedDeltaTime,
            1f
        );

        leftTrackMaterial.SetVector(uvOffsetProperty, new Vector2(0f, leftCurrentYOffset));
        rightTrackMaterial.SetVector(uvOffsetProperty, new Vector2(0f, rightCurrentYOffset));
    }
}