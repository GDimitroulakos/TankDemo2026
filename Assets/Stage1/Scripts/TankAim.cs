using UnityEngine;

public class TankAim : MonoBehaviour {
    public Transform turret;

    private void Update() {
        if (turret == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, turret.position);

        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - turret.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f) {
                turret.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}