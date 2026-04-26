using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTankWander : MonoBehaviour {
    [Header("Wander Area")]
    public Transform patrolCenter;
    public float wanderRadius = 50f;
    public float minDistanceForNewPoint = 12f;

    [Header("Timing")]
    public float waitTimeAtDestination = 1f;

    [Header("Stuck Detection")]
    public float stuckCheckTime = 2f;
    public float stuckMoveThreshold = 0.3f;

    [Header("Search")]
    public int maxAttempts = 30;
    public float navMeshSampleDistance = 8f;

    private NavMeshAgent agent;
    private float waitTimer;
    private float stuckTimer;
    private Vector3 lastPosition;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start() {
        if (patrolCenter == null) {
            patrolCenter = transform;
        }

        lastPosition = transform.position;
        ChooseNewDestination();
    }

    private void Update() {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (agent.pathPending)
            return;

        // If the path is bad, choose another point.
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            agent.pathStatus == NavMeshPathStatus.PathPartial) {
            ChooseNewDestination();
            return;
        }

        // If the enemy reached destination, wait a little and choose another one.
        if (agent.remainingDistance <= agent.stoppingDistance) {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTimeAtDestination) {
                ChooseNewDestination();
                waitTimer = 0f;
            }

            return;
        }

        // Stuck detection.
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < stuckMoveThreshold) {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckCheckTime) {
                ChooseNewDestination();
                stuckTimer = 0f;
            }
        } else {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    public void ChooseNewDestination() {
        for (int i = 0; i < maxAttempts; i++) {
            Vector3 randomPoint = GetRandomPointAroundCenter();

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas)) {
                float distanceFromEnemy = Vector3.Distance(transform.position, hit.position);

                if (distanceFromEnemy < minDistanceForNewPoint)
                    continue;

                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete) {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        // Fallback: if no good point is found, try a smaller random point near the enemy.
        Vector3 fallbackPoint = transform.position + Random.insideUnitSphere * 10f;

        if (NavMesh.SamplePosition(fallbackPoint, out NavMeshHit fallbackHit, 10f, NavMesh.AllAreas)) {
            agent.SetDestination(fallbackHit.position);
        }
    }

    private Vector3 GetRandomPointAroundCenter() {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistanceForNewPoint, wanderRadius);

        Vector3 center = patrolCenter != null ? patrolCenter.position : transform.position;

        return new Vector3(
            center.x + randomCircle.x,
            center.y,
            center.z + randomCircle.y
        );
    }
}