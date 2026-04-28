using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TankDestructionSequence : MonoBehaviour {
    [Header("Pieces")]
    [Tooltip("Τα κομμάτια που θα αποσπαστούν, εκτός από το turret.")]
    [SerializeField] private Transform[] pieces;

    [Tooltip("Το turret που θα εκτοξευτεί προς τα πάνω. Αν έχει child το cannon, θα φύγουν μαζί.")]
    [SerializeField] private Transform turretPiece;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDelay = 0.25f;
    [SerializeField] private float explosionLifetime = 6f;

    [Header("Piece Forces")]
    [SerializeField] private float pieceMass = 1f;
    [SerializeField] private float outwardImpulse = 4f;
    [SerializeField] private float upwardImpulse = 3f;
    [SerializeField] private float torqueImpulse = 5f;

    [Header("Turret Force")]
    [SerializeField] private float turretMass = 1.5f;
    [SerializeField] private float turretOutwardImpulse = 3f;
    [SerializeField] private float turretUpwardImpulse = 16f;
    [SerializeField] private float turretTorqueImpulse = 8f;

    [Header("Cleanup")]
    [SerializeField] private float piecesLifetime = 5f;
    [SerializeField] private bool destroyOriginalTank = true;

    private bool isDestroyed;
    private readonly List<GameObject> spawnedPieces = new();

    public void DestroyTank(Vector3 hitPoint) {
        if (isDestroyed)
            return;

        StartCoroutine(DestroyRoutine(hitPoint));
    }

    private IEnumerator DestroyRoutine(Vector3 hitPoint) {
        isDestroyed = true;

        StopTankLogic();

        GameObject debrisRoot = new GameObject($"{name}_DestroyedPieces");
        debrisRoot.transform.SetPositionAndRotation(transform.position, transform.rotation);

        SpawnAllPieces(debrisRoot.transform, hitPoint);

        HideOriginalTank();

        yield return new WaitForSeconds(explosionDelay);

        if (explosionPrefab != null) {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(explosion, explosionLifetime);
        }

        yield return new WaitForSeconds(piecesLifetime);

        foreach (GameObject piece in spawnedPieces) {
            if (piece != null)
                Destroy(piece);
        }

        if (debrisRoot != null)
            Destroy(debrisRoot);

        if (destroyOriginalTank)
            Destroy(gameObject);
    }

    private void SpawnAllPieces(Transform debrisRoot, Vector3 hitPoint) {
        HashSet<Transform> usedPieces = new HashSet<Transform>();

        if (turretPiece != null && usedPieces.Add(turretPiece)) {
            SpawnPiece(turretPiece, debrisRoot, hitPoint, true);
        }

        if (pieces == null)
            return;

        foreach (Transform piece in pieces) {
            if (piece == null)
                continue;

            if (!usedPieces.Add(piece))
                continue;

            SpawnPiece(piece, debrisRoot, hitPoint, false);
        }
    }

    private void SpawnPiece(
        Transform source,
        Transform debrisRoot,
        Vector3 hitPoint,
        bool isTurret
    ) {
        GameObject fragment = Instantiate(source.gameObject);

        fragment.name = $"{source.name}_Destroyed";
        fragment.SetActive(true);

        fragment.transform.SetPositionAndRotation(source.position, source.rotation);
        fragment.transform.localScale = source.lossyScale;
        fragment.transform.SetParent(debrisRoot, true);

        RemoveScripts(fragment);
        EnsureColliders(fragment);

        Rigidbody rb = fragment.GetComponent<Rigidbody>();

        if (rb == null)
            rb = fragment.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = isTurret ? turretMass : pieceMass;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Vector3 awayFromHit = fragment.transform.position - hitPoint;

        if (awayFromHit.sqrMagnitude < 0.01f)
            awayFromHit = Random.insideUnitSphere;

        awayFromHit.y = Mathf.Abs(awayFromHit.y) + 0.2f;
        awayFromHit.Normalize();

        float horizontalForce = isTurret ? turretOutwardImpulse : outwardImpulse;
        float verticalForce = isTurret ? turretUpwardImpulse : upwardImpulse;
        float torqueForce = isTurret ? turretTorqueImpulse : torqueImpulse;

        rb.AddForce(
            awayFromHit * horizontalForce + Vector3.up * verticalForce,
            ForceMode.Impulse
        );

        rb.AddTorque(
            Random.insideUnitSphere * torqueForce,
            ForceMode.Impulse
        );

        spawnedPieces.Add(fragment);
    }

    private void StopTankLogic() {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        if (agent != null) {
            if (agent.enabled && agent.isOnNavMesh) {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>()) {
            if (behaviour == this)
                continue;

            behaviour.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void HideOriginalTank() {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) {
            renderer.enabled = false;
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) {
            collider.enabled = false;
        }
    }

    private void RemoveScripts(GameObject fragment) {
        foreach (MonoBehaviour behaviour in fragment.GetComponentsInChildren<MonoBehaviour>(true)) {
            Destroy(behaviour);
        }
    }

    private void EnsureColliders(GameObject fragment) {
        Collider[] colliders = fragment.GetComponentsInChildren<Collider>(true);

        if (colliders.Length > 0) {
            foreach (Collider collider in colliders) {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            return;
        }

        Renderer[] renderers = fragment.GetComponentsInChildren<Renderer>(true);
        BoxCollider boxCollider = fragment.AddComponent<BoxCollider>();

        if (renderers.Length == 0) {
            boxCollider.size = Vector3.one;
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++) {
            bounds.Encapsulate(renderers[i].bounds);
        }

        boxCollider.center = fragment.transform.InverseTransformPoint(bounds.center);

        Vector3 localSize = fragment.transform.InverseTransformVector(bounds.size);

        boxCollider.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z)
        );
    }
}