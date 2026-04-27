using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TankShooterSound : MonoBehaviour {
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float launchForce = 15f;

    [Header("Fire Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private float fireSoundVolume = 1f;

    private AudioSource fireAudioSource;

    private void Awake() {
        fireAudioSource = GetComponent<AudioSource>();

        fireAudioSource.playOnAwake = false;
        fireAudioSource.loop = false;

        if (fireSound != null) {
            fireSound.LoadAudioData();
        }
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Shoot();
        }
    }

    private void Shoot() {
        if (projectilePrefab == null) {
            Debug.LogWarning("Projectile prefab is not assigned.", this);
            return;
        }

        if (firePoint == null) {
            Debug.LogWarning("Fire point is not assigned.", this);
            return;
        }

        PlayFireSound();

        GameObject projectile = Instantiate(
                                            projectilePrefab,
                                            firePoint.position,
                                            firePoint.rotation
                                           );

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null) {
            projectileRb.AddForce(
                                  firePoint.forward * launchForce,
                                  ForceMode.Impulse
                                 );
        }
    }

    private void PlayFireSound() {
        if (fireSound == null || fireAudioSource == null)
            return;

        fireAudioSource.PlayOneShot(fireSound, fireSoundVolume);
    }
}