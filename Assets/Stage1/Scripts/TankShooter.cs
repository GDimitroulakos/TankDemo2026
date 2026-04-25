using UnityEngine;

public class TankShooter : MonoBehaviour {
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float launchForce = 15f;

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Shoot();
        }
    }

    private void Shoot() {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null) {
            rb.AddForce(firePoint.forward * launchForce, ForceMode.Impulse);
        }
    }
}