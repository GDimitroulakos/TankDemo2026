using UnityEngine;

public class Projectile : MonoBehaviour {
    public float lifeTime = 3f;

    private void Start() {
        Destroy(gameObject, lifeTime);
    }
    
}