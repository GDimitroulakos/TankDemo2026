using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TankVisualSwitcher : MonoBehaviour {
    [Header("Visuals")]
    [SerializeField] private GameObject greyboxVisual;
    [SerializeField] private GameObject leopardVisual;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Input")]
    [SerializeField] private Key switchKey = Key.Tab;

    private GameObject activeVisual;
    private GameObject inactiveVisual;

    private void Awake() {
        if (greyboxVisual.activeSelf) {
            activeVisual = greyboxVisual;
            inactiveVisual = leopardVisual;
        } else {
            activeVisual = leopardVisual;
            inactiveVisual = greyboxVisual;
        }

        SetCameraTarget(activeVisual.transform);
    }

    private void Update() {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard[switchKey].wasPressedThisFrame) {
            SwitchVisual();
        }
    }

    public void SwitchVisual() {
        if (activeVisual == null || inactiveVisual == null)
            return;

        CopyTransformAndPhysics(activeVisual, inactiveVisual);

        inactiveVisual.SetActive(true);
        SetCameraTarget(inactiveVisual.transform);
        activeVisual.SetActive(false);

        GameObject oldActive = activeVisual;
        activeVisual = inactiveVisual;
        inactiveVisual = oldActive;
    }

    private void SetCameraTarget(Transform target) {
        if (playerCamera == null || target == null)
            return;

        playerCamera.Target.TrackingTarget = target;
        playerCamera.Target.LookAtTarget = target;
    }

    private void CopyTransformAndPhysics(GameObject from, GameObject to) {
        Transform fromTransform = from.transform;
        Transform toTransform = to.transform;

        toTransform.SetPositionAndRotation(
            fromTransform.position,
            fromTransform.rotation
        );

        Rigidbody fromRb = from.GetComponent<Rigidbody>();
        Rigidbody toRb = to.GetComponent<Rigidbody>();

        if (toRb == null)
            return;

        toRb.position = fromTransform.position;
        toRb.rotation = fromTransform.rotation;

        if (fromRb != null) {
            toRb.linearVelocity = fromRb.linearVelocity;
            toRb.angularVelocity = fromRb.angularVelocity;
        } else {
            toRb.linearVelocity = Vector3.zero;
            toRb.angularVelocity = Vector3.zero;
        }
    }
}
