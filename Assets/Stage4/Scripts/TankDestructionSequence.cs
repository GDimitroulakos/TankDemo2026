using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TankDestructionSequence : MonoBehaviour {
    [Header("Visual Source")]
    [Tooltip("Put Leopard2Visual here. If empty, the script will try to find it automatically.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Optional: put the real turret object from the Leopard hierarchy here.")]
    [SerializeField] private Transform turretRootOverride;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDelay = 0.35f;
    [SerializeField] private float explosionLifetime = 6f;
    [SerializeField] private bool explosionAtTankCenter = true;

    [Header("Piece Physics")]
    [SerializeField] private float pieceMass = 0.8f;
    [SerializeField] private float outwardImpulse = 5f;
    [SerializeField] private float upwardImpulse = 2.5f;
    [SerializeField] private float torqueImpulse = 6f;

    [Header("Turret Physics")]
    [SerializeField] private float turretMass = 2f;
    [SerializeField] private float turretOutwardImpulse = 3f;
    [SerializeField] private float turretUpwardImpulse = 18f;
    [SerializeField] private float turretTorqueImpulse = 10f;

    [Header("Cleanup")]
    [SerializeField] private float piecesLifetime = 5f;

    [Tooltip("For respawn, this must usually stay false.")]
    [SerializeField] private bool destroyOriginalTank = false;

    [Header("Respawn")]
    [SerializeField] private bool respawnAfterDestruction = true;

    [Header("Auto Turret Detection")]
    [SerializeField] private bool autoDetectTurretIfMissing = true;

    private bool isDestroyed;
    private EnemyTankRespawn enemyRespawn;

    private readonly List<GameObject> spawnedPieces = new List<GameObject>();
    private readonly List<MonoBehaviour> temporarilyDisabledBehaviours = new List<MonoBehaviour>();

    public void DestroyTank(Vector3 hitPoint) {
        if (isDestroyed)
            return;

        StartCoroutine(DestroyRoutine(hitPoint));
    }

    private IEnumerator DestroyRoutine(Vector3 hitPoint) {
        isDestroyed = true;

        if (visualRoot == null)
            visualRoot = FindVisualRoot();

        enemyRespawn = GetComponent<EnemyTankRespawn>();

        StopTankLogic();

        GameObject debrisRoot = new GameObject($"{name}_LeopardDebris");
        debrisRoot.transform.SetPositionAndRotation(transform.position, transform.rotation);

        SpawnLeopardPieces(debrisRoot.transform, hitPoint);

        HideOriginalTank();

        yield return new WaitForSeconds(explosionDelay);

        SpawnExplosion(hitPoint);

        yield return new WaitForSeconds(piecesLifetime);

        spawnedPieces.Clear();

        if (debrisRoot != null)
            Destroy(debrisRoot);

        if (respawnAfterDestruction && enemyRespawn != null) {
            enemyRespawn.RespawnFromHit();

            yield return new WaitForSeconds(enemyRespawn.respawnDelay + 0.1f);

            RestoreTankLogicAfterRespawn();

            isDestroyed = false;

            yield break;
        }

        if (destroyOriginalTank) {
            Destroy(gameObject);
        } else {
            RestoreTankLogicAfterRespawn();
            ShowOriginalTank();
            isDestroyed = false;
        }
    }

    private Transform FindVisualRoot() {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms) {
            if (t.name == "Leopard2Visual")
                return t;
        }

        foreach (Transform t in allTransforms) {
            if (t != transform && t.GetComponentInChildren<Renderer>(true) != null)
                return t;
        }

        return transform;
    }

    private void SpawnLeopardPieces(Transform debrisRoot, Vector3 hitPoint) {
        if (visualRoot == null) {
            Debug.LogWarning("TankDestructionSequence: visualRoot was not found.", this);
            return;
        }

        Transform turretRoot = turretRootOverride;

        if (turretRoot == null && autoDetectTurretIfMissing)
            turretRoot = TryAutoDetectTurret();

        if (turretRoot != null) {
            GameObject turretPiece = CloneHierarchyAsPhysicsPiece(
                turretRoot,
                debrisRoot,
                hitPoint,
                true
            );

            if (turretPiece != null)
                spawnedPieces.Add(turretPiece);
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers) {
            if (renderer == null)
                continue;

            if (!renderer.enabled)
                continue;

            if (turretRoot != null && IsChildOf(renderer.transform, turretRoot))
                continue;

            GameObject piece = CloneRendererAsPhysicsPiece(
                renderer,
                debrisRoot,
                hitPoint,
                false
            );

            if (piece != null)
                spawnedPieces.Add(piece);
        }
    }

    private Transform TryAutoDetectTurret() {
        if (visualRoot == null)
            return null;

        Transform[] allTransforms = visualRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms) {
            string lowerName = t.name.ToLowerInvariant();

            if (lowerName.Contains("turret") ||
                lowerName.Contains("tower") ||
                lowerName.Contains("gun") ||
                lowerName.Contains("cannon")) {
                if (t.GetComponentInChildren<Renderer>(true) != null)
                    return t;
            }
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return null;

        Bounds totalBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            totalBounds.Encapsulate(renderers[i].bounds);

        Renderer bestRenderer = null;
        float bestScore = 0f;

        foreach (Renderer r in renderers) {
            Bounds b = r.bounds;

            float relativeHeight = Mathf.InverseLerp(
                totalBounds.min.y,
                totalBounds.max.y,
                b.center.y
            );

            float volume = b.size.x * b.size.y * b.size.z;

            bool isUpperPart = relativeHeight > 0.45f;
            bool isNotWholeTank =
                b.size.x < totalBounds.size.x * 0.85f &&
                b.size.z < totalBounds.size.z * 0.85f;

            if (!isUpperPart || !isNotWholeTank)
                continue;

            float score = volume * relativeHeight;

            if (score > bestScore) {
                bestScore = score;
                bestRenderer = r;
            }
        }

        return bestRenderer != null ? bestRenderer.transform : null;
    }

    private GameObject CloneHierarchyAsPhysicsPiece(
        Transform sourceRoot,
        Transform debrisRoot,
        Vector3 hitPoint,
        bool isTurret
    ) {
        GameObject clone = Instantiate(sourceRoot.gameObject);

        clone.name = $"{sourceRoot.name}_Destroyed";
        clone.SetActive(true);

        clone.transform.SetPositionAndRotation(sourceRoot.position, sourceRoot.rotation);
        clone.transform.localScale = sourceRoot.lossyScale;
        clone.transform.SetParent(debrisRoot, true);

        RemoveScripts(clone);
        RemoveColliders(clone);

        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0) {
            Destroy(clone);
            return null;
        }

        Bounds bounds = GetRendererBounds(renderers);

        AddBoxColliderFromBounds(clone, bounds);
        AddPhysics(clone, bounds.center, hitPoint, isTurret);

        return clone;
    }

    private GameObject CloneRendererAsPhysicsPiece(
        Renderer sourceRenderer,
        Transform debrisRoot,
        Vector3 hitPoint,
        bool isTurret
    ) {
        MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
        MeshRenderer sourceMeshRenderer = sourceRenderer as MeshRenderer;

        if (sourceMeshFilter == null ||
            sourceMeshFilter.sharedMesh == null ||
            sourceMeshRenderer == null) {
            return CloneHierarchyAsPhysicsPiece(
                sourceRenderer.transform,
                debrisRoot,
                hitPoint,
                isTurret
            );
        }

        GameObject piece = new GameObject($"{sourceRenderer.name}_Destroyed");

        piece.transform.SetPositionAndRotation(
            sourceRenderer.transform.position,
            sourceRenderer.transform.rotation
        );

        piece.transform.localScale = sourceRenderer.transform.lossyScale;
        piece.transform.SetParent(debrisRoot, true);

        MeshFilter meshFilter = piece.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        MeshRenderer meshRenderer = piece.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
        meshRenderer.shadowCastingMode = sourceMeshRenderer.shadowCastingMode;
        meshRenderer.receiveShadows = sourceMeshRenderer.receiveShadows;

        Bounds bounds = sourceRenderer.bounds;

        AddBoxColliderFromBounds(piece, bounds);
        AddPhysics(piece, bounds.center, hitPoint, isTurret);

        return piece;
    }

    private void AddPhysics(
        GameObject piece,
        Vector3 worldCenter,
        Vector3 hitPoint,
        bool isTurret
    ) {
        Rigidbody rb = piece.AddComponent<Rigidbody>();

        rb.mass = isTurret ? turretMass : pieceMass;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.1f;
#else
        rb.drag = 0.2f;
        rb.angularDrag = 0.1f;
#endif

        Vector3 awayFromHit = worldCenter - hitPoint;

        if (awayFromHit.sqrMagnitude < 0.01f)
            awayFromHit = Random.insideUnitSphere;

        awayFromHit.y = Mathf.Abs(awayFromHit.y) + 0.25f;
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
    }

    private void AddBoxColliderFromBounds(GameObject piece, Bounds worldBounds) {
        BoxCollider boxCollider = piece.AddComponent<BoxCollider>();

        boxCollider.center = piece.transform.InverseTransformPoint(worldBounds.center);

        Vector3 lossyScale = piece.transform.lossyScale;

        float sx = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
        float sy = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);
        float sz = Mathf.Approximately(lossyScale.z, 0f) ? 1f : Mathf.Abs(lossyScale.z);

        boxCollider.size = new Vector3(
            Mathf.Max(0.05f, worldBounds.size.x / sx),
            Mathf.Max(0.05f, worldBounds.size.y / sy),
            Mathf.Max(0.05f, worldBounds.size.z / sz)
        );

        boxCollider.isTrigger = false;
    }

    private Bounds GetRendererBounds(Renderer[] renderers) {
        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private void SpawnExplosion(Vector3 hitPoint) {
        if (explosionPrefab == null)
            return;

        Vector3 explosionPosition = hitPoint;

        if (explosionAtTankCenter && visualRoot != null) {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length > 0)
                explosionPosition = GetRendererBounds(renderers).center;
        }

        GameObject explosion = Instantiate(
            explosionPrefab,
            explosionPosition,
            Quaternion.identity
        );

        Destroy(explosion, explosionLifetime);
    }

    private void StopTankLogic() {
        temporarilyDisabledBehaviours.Clear();

        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        if (agent != null && agent.enabled && agent.isOnNavMesh) {
            agent.isStopped = true;
            agent.ResetPath();
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours) {
            if (behaviour == null)
                continue;

            if (behaviour == this)
                continue;

            if (behaviour is EnemyTankRespawn)
                continue;

            if (behaviour.enabled) {
                temporarilyDisabledBehaviours.Add(behaviour);
                behaviour.enabled = false;
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null) {
            if (!rb.isKinematic) {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
        }
    }

    private void RestoreTankLogicAfterRespawn() {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        if (agent != null) {
            agent.enabled = true;

            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = true;

        foreach (MonoBehaviour behaviour in temporarilyDisabledBehaviours) {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        temporarilyDisabledBehaviours.Clear();

        EnemyTankWander wander = GetComponent<EnemyTankWander>();

        if (wander != null)
            wander.ChooseNewDestination();
    }

    private void HideOriginalTank() {
        if (visualRoot != null) {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
                renderer.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            collider.enabled = false;
    }

    private void ShowOriginalTank() {
        if (visualRoot != null) {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
                renderer.enabled = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            collider.enabled = true;
    }

    private void RemoveScripts(GameObject target) {
        MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
            Destroy(behaviour);
    }

    private void RemoveColliders(GameObject target) {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            Destroy(collider);
    }

    private bool IsChildOf(Transform possibleChild, Transform possibleParent) {
        Transform current = possibleChild;

        while (current != null) {
            if (current == possibleParent)
                return true;

            current = current.parent;
        }

        return false;
    }
}