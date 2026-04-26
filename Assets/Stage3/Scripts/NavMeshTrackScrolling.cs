using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshTrackScrolling : MonoBehaviour {
    [Header("Track Materials")]
    [SerializeField] private Material leftTrackMaterial;
    [SerializeField] private Material rightTrackMaterial;

    [Header("Track Animation")]
    [SerializeField] private float linearScrollMultiplier = 0.08f;
    [SerializeField] private float turnScrollMultiplier = 0.015f;
    [SerializeField] private bool invertLeftTrack = false;
    [SerializeField] private bool invertRightTrack = false;
    [SerializeField] private string uvOffsetProperty = "_UvOffset";

    private NavMeshAgent navMeshAgent;

    private float leftCurrentYOffset;
    private float rightCurrentYOffset;

    private Vector3 previousPosition;
    private float previousYRotation;

    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable() {
        previousPosition = transform.position;
        previousYRotation = transform.eulerAngles.y;
    }

    private void LateUpdate() {
        if (navMeshAgent == null)
            return;

        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return;

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        Vector3 velocity = navMeshAgent.velocity;

        float currentMoveSpeed = Vector3.Dot(transform.forward, velocity);

        float currentYRotation = transform.eulerAngles.y;
        float currentTurnSpeed = Mathf.DeltaAngle(previousYRotation, currentYRotation) / deltaTime;

        previousYRotation = currentYRotation;
        previousPosition = transform.position;

        UpdateTrackMaterial(leftTrackMaterial, currentMoveSpeed, currentTurnSpeed, true, deltaTime);
        UpdateTrackMaterial(rightTrackMaterial, currentMoveSpeed, currentTurnSpeed, false, deltaTime);
    }

    private void UpdateTrackMaterial(
        Material trackMaterial,
        float currentMoveSpeed,
        float currentTurnSpeed,
        bool isLeftTrack,
        float deltaTime) {
        if (trackMaterial == null)
            return;

        if (!trackMaterial.HasProperty(uvOffsetProperty)) {
            Debug.LogWarning($"{trackMaterial.name} does not have property {uvOffsetProperty}");
            return;
        }

        float linearComponent = currentMoveSpeed * linearScrollMultiplier;
        float turnComponent = currentTurnSpeed * turnScrollMultiplier;

        float scrollSpeed;

        if (isLeftTrack) {
            scrollSpeed = linearComponent + turnComponent;

            if (invertLeftTrack)
                scrollSpeed *= -1f;

            leftCurrentYOffset = Mathf.Repeat(
                leftCurrentYOffset + scrollSpeed * deltaTime,
                1f
            );

            trackMaterial.SetVector(
                uvOffsetProperty,
                new Vector2(0f, leftCurrentYOffset)
            );
        } else {
            scrollSpeed = linearComponent - turnComponent;

            if (invertRightTrack)
                scrollSpeed *= -1f;

            rightCurrentYOffset = Mathf.Repeat(
                rightCurrentYOffset + scrollSpeed * deltaTime,
                1f
            );

            trackMaterial.SetVector(
                uvOffsetProperty,
                new Vector2(0f, rightCurrentYOffset)
            );
        }
    }
}