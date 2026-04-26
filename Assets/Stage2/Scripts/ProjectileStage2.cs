using UnityEngine;

public class Projectile2 : MonoBehaviour {
    public float lifeTime = 3f;

    private bool hasHit;

    private void Start() {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision) {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other) {
        HandleHit(other);
    }

    private void HandleHit(Collider other) {
        if (hasHit)
            return;

        hasHit = true;

        EnemyTankRespawn enemy = other.GetComponentInParent<EnemyTankRespawn>();

        if (enemy != null) {
            enemy.RespawnFromHit();
        }

        Destroy(gameObject);
    }
}