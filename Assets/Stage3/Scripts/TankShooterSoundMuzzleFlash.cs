using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TankShooterSoundMuzzleFlash : MonoBehaviour {
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float launchForce = 100f;

    [Tooltip("Πόσο πιο μπροστά από το FirePoint θα γεννιέται η σφαίρα, για να μη χτυπά το collider του tank.")]
    [SerializeField] private float projectileSpawnForwardOffset = 1.0f;

    [Header("Fire Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private float fireSoundVolume = 1f;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Tooltip("Αν μείνει κενό, θα χρησιμοποιηθεί το FirePoint.")]
    [SerializeField] private Transform muzzleFlashPoint;

    [SerializeField] private float muzzleFlashLifetime = 2f;
    [SerializeField] private Vector3 muzzleFlashRotationOffset = Vector3.zero;

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
        SpawnMuzzleFlash();

        Vector3 projectileSpawnPosition =
            firePoint.position + firePoint.forward * projectileSpawnForwardOffset;

        GameObject projectile = Instantiate(
            projectilePrefab,
            projectileSpawnPosition,
            firePoint.rotation
        );

        IgnoreCollisionWithOwnTank(projectile);

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

    private void SpawnMuzzleFlash() {
        if (muzzleFlashPrefab == null)
            return;

        Transform spawnPoint = muzzleFlashPoint != null
            ? muzzleFlashPoint
            : firePoint;

        Quaternion rotation =
            spawnPoint.rotation * Quaternion.Euler(muzzleFlashRotationOffset);

        GameObject flash = Instantiate(
            muzzleFlashPrefab,
            spawnPoint.position,
            rotation
        );

        ParticleSystem[] particleSystems =
            flash.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particleSystems) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        Destroy(flash, muzzleFlashLifetime);
    }

    private void IgnoreCollisionWithOwnTank(GameObject projectile) {
        Collider[] projectileColliders =
            projectile.GetComponentsInChildren<Collider>();

        Collider[] ownTankColliders =
            GetComponentsInParent<Collider>();

        foreach (Collider projectileCollider in projectileColliders) {
            foreach (Collider tankCollider in ownTankColliders) {
                Physics.IgnoreCollision(
                    projectileCollider,
                    tankCollider,
                    true
                );
            }
        }
    }
}