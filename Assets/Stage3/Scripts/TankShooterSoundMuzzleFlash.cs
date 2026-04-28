using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TankShooterSoundMuzzleFlash : MonoBehaviour {
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float launchForce = 100f;

    [Tooltip("Πόσο πιο μπροστά από το FirePoint θα γεννιέται η σφαίρα, για να μη χτυπά το collider του tank.")]
    [SerializeField] private float projectileSpawnForwardOffset = 2f;

    [Header("Fire Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private float fireSoundVolume = 1f;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Tooltip("Το σημείο στο στόμιο του κανονιού. Αν μείνει κενό, χρησιμοποιείται το FirePoint.")]
    [SerializeField] private Transform muzzleFlashPoint;

    [Tooltip("Για tank cannon δοκίμασε 5 έως 10.")]
    [SerializeField] private float muzzleFlashScale = 8f;

    [Tooltip("Πόσο λίγο μένει ενεργό το muzzle flash. Είναι στιγμιαίο effect.")]
    [SerializeField] private float muzzleFlashVisibleTime = 0.12f;

    [Tooltip("Διόρθωση θέσης σε world/local κατεύθυνση του muzzle point.")]
    [SerializeField] private Vector3 muzzleFlashPositionOffset = Vector3.zero;

    [Tooltip("Διόρθωση περιστροφής αν κοιτάζει λάθος άξονα.")]
    [SerializeField] private Vector3 muzzleFlashRotationOffset = Vector3.zero;

    private AudioSource fireAudioSource;

    private GameObject muzzleFlashInstance;
    private ParticleSystem[] muzzleFlashParticles;
    private Coroutine hideFlashRoutine;

    private void Awake() {
        fireAudioSource = GetComponent<AudioSource>();
        fireAudioSource.playOnAwake = false;
        fireAudioSource.loop = false;

        if (fireSound != null) {
            fireSound.LoadAudioData();
        }

        PrepareMuzzleFlash();
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
        PlayMuzzleFlash();

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
            projectileRb.linearVelocity = Vector3.zero;
            projectileRb.angularVelocity = Vector3.zero;

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

    private void PrepareMuzzleFlash() {
        if (muzzleFlashPrefab == null)
            return;

        // Δεν το κάνουμε child του FirePoint/Cannon, για να μη στραβώνει από non-uniform scale.
        muzzleFlashInstance = Instantiate(muzzleFlashPrefab);
        muzzleFlashInstance.SetActive(false);

        muzzleFlashParticles =
            muzzleFlashInstance.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in muzzleFlashParticles) {
            ParticleSystem.MainModule main = ps.main;

            main.playOnAwake = false;
            main.loop = false;
            main.startDelay = 0f;

            // Σημαντικό: όχι Hierarchy scaling, γιατί μπορεί να τεντώσει το effect.
            main.scalingMode = ParticleSystemScalingMode.Local;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayMuzzleFlash() {
        if (muzzleFlashInstance == null)
            return;

        Transform point = muzzleFlashPoint != null
            ? muzzleFlashPoint
            : firePoint;

        Quaternion rotation =
            point.rotation * Quaternion.Euler(muzzleFlashRotationOffset);

        Vector3 position =
            point.position
            + point.right * muzzleFlashPositionOffset.x
            + point.up * muzzleFlashPositionOffset.y
            + point.forward * muzzleFlashPositionOffset.z;

        muzzleFlashInstance.transform.SetPositionAndRotation(position, rotation);
        muzzleFlashInstance.transform.localScale = Vector3.one * muzzleFlashScale;

        if (hideFlashRoutine != null) {
            StopCoroutine(hideFlashRoutine);
        }

        muzzleFlashInstance.SetActive(true);

        foreach (ParticleSystem ps in muzzleFlashParticles) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }

        hideFlashRoutine = StartCoroutine(HideMuzzleFlashAfterDelay());
    }

    private IEnumerator HideMuzzleFlashAfterDelay() {
        yield return new WaitForSeconds(muzzleFlashVisibleTime);

        if (muzzleFlashInstance != null) {
            foreach (ParticleSystem ps in muzzleFlashParticles) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }

            muzzleFlashInstance.SetActive(false);
        }

        hideFlashRoutine = null;
    }

    private void IgnoreCollisionWithOwnTank(GameObject projectile) {
        Collider[] projectileColliders =
            projectile.GetComponentsInChildren<Collider>();

        Collider[] ownTankColliders =
            transform.root.GetComponentsInChildren<Collider>(true);

        foreach (Collider projectileCollider in projectileColliders) {
            foreach (Collider tankCollider in ownTankColliders) {
                if (projectileCollider == null || tankCollider == null)
                    continue;

                Physics.IgnoreCollision(
                    projectileCollider,
                    tankCollider,
                    true
                );
            }
        }
    }
}
