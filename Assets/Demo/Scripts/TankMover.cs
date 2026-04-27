using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankMover : MonoBehaviour {
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 90f;

    private Rigidbody rb;

    private float moveInput;
    private float turnInput;

    private void Awake() {
        rb = GetComponent<Rigidbody>();

        // Για tank συνήθως θέλουμε να μη γέρνει δεξιά/αριστερά
        rb.freezeRotation = true;
    }

    private void Update() {
        // Up/Down arrows ή W/S
        moveInput = Input.GetAxis("Vertical");

        // Left/Right arrows ή A/D
        turnInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate() {
        MoveTank();
        RotateTank();
    }

    private void MoveTank() {
        Vector3 movement = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void RotateTank() {
        float rotationAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, rotationAmount, 0f);

        rb.MoveRotation(rb.rotation * turnRotation);
    }
}