using UnityEngine;

public class GorePiece : MonoBehaviour
{
    public float maxDistance = 10f;
    public float drag = 1.5f;

    private Rigidbody rb;
    private Vector3 spawnPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPoint = transform.position;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = drag;
        }
    }

    void Update()
    {
        if (rb == null) return;

        // Slow down if beyond max distance
        if (Vector3.Distance(spawnPoint, transform.position) > maxDistance)
        {
            rb.velocity *= 0.5f;
            rb.angularVelocity *= 0.5f;
        }
    }
}
