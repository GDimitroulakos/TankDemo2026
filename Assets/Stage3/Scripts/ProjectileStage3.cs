using UnityEngine;

public class ProjectileStage3 : MonoBehaviour {
    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 3f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifetime = 6f;

    [Header("Enemy Hit Behaviour")]
    [SerializeField] private bool respawnEnemyIfPossible = true;
    [SerializeField] private bool destroyEnemyIfNoRespawnComponent = false;
    [SerializeField] private string enemyTag = "Enemy";

    private bool hasHit;

    private void Start() {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision) {
        if (hasHit)
            return;

        Vector3 hitPoint = transform.position;

        if (collision.contactCount > 0) {
            hitPoint = collision.GetContact(0).point;
        }

        HandleHit(collision.collider, hitPoint);
    }

    private void OnTriggerEnter(Collider other) {
        if (hasHit)
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        HandleHit(other, hitPoint);
    }

    private void HandleHit(Collider other, Vector3 hitPoint) {
        if (hasHit)
            return;

        hasHit = true;

        SpawnExplosion(hitPoint);

        HandleEnemyHit(other);

        Destroy(gameObject);
    }

    private void SpawnExplosion(Vector3 position) {
        if (explosionPrefab == null) {
            Debug.LogWarning("Explosion prefab is not assigned.", this);
            return;
        }

        GameObject explosionInstance = Instantiate(
            explosionPrefab,
            position,
            Quaternion.identity
        );

        Destroy(explosionInstance, explosionLifetime);
    }

    private void HandleEnemyHit(Collider other) {
        if (other == null)
            return;

        EnemyTankRespawn enemyRespawn =
            other.GetComponentInParent<EnemyTankRespawn>();

        if (enemyRespawn != null) {
            if (respawnEnemyIfPossible) {
                enemyRespawn.RespawnFromHit();
            }

            return;
        }

        if (!destroyEnemyIfNoRespawnComponent)
            return;

        Transform enemyRoot = other.transform.root;

        if (enemyRoot.CompareTag(enemyTag)) {
            Destroy(enemyRoot.gameObject);
        }
    }
}