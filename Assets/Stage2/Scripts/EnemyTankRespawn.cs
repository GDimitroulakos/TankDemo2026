using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTankRespawn : MonoBehaviour {
    [Header("References")]
    public Transform player;
    public Transform respawnCenter;

    [Header("Respawn Settings")]
    public float minDistanceFromPlayer = 25f;
    public float respawnRadius = 80f;
    public float respawnDelay = 1f;

    [Header("NavMesh Search")]
    public int maxAttempts = 50;
    public float navMeshSampleDistance = 8f;

    private NavMeshAgent agent;
    private EnemyTankWander wanderScript;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool isRespawning;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        wanderScript = GetComponent<EnemyTankWander>();

        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void RespawnFromHit() {
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine() {
        isRespawning = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh) {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetVisibleAndCollidable(false);

        yield return new WaitForSeconds(respawnDelay);

        if (TryFindRespawnPosition(out Vector3 respawnPosition)) {
            if (agent != null && agent.enabled) {
                bool warped = agent.Warp(respawnPosition);

                if (!warped) {
                    transform.position = respawnPosition;
                }

                agent.isStopped = false;
            } else {
                transform.position = respawnPosition;
            }
        }

        SetVisibleAndCollidable(true);

        if (wanderScript != null) {
            wanderScript.ChooseNewDestination();
        }

        isRespawning = false;
    }

    private bool TryFindRespawnPosition(out Vector3 position) {
        Vector3 center;

        if (respawnCenter != null) {
            center = respawnCenter.position;
        } else if (player != null) {
            center = player.position;
        } else {
            center = transform.position;
        }

        for (int i = 0; i < maxAttempts; i++) {
            Vector2 randomCircle = Random.insideUnitCircle * respawnRadius;

            Vector3 randomPoint = new Vector3(
                center.x + randomCircle.x,
                center.y,
                center.z + randomCircle.y
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas)) {
                if (player == null || Vector3.Distance(hit.position, player.position) >= minDistanceFromPlayer) {
                    position = hit.position;
                    return true;
                }
            }
        }

        position = transform.position;
        return false;
    }

    private void SetVisibleAndCollidable(bool value) {
        foreach (Renderer r in renderers) {
            r.enabled = value;
        }

        foreach (Collider c in colliders) {
            c.enabled = value;
        }
    }
}