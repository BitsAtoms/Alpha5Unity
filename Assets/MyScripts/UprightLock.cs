using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UprightLock : MonoBehaviour
{
    public bool forceUpright = true;
    public float uprightSlerp = 20f; // cuánta fuerza para enderezar

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Garantiza el bloqueo de rotaciones físicas en X y Z
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.angularDamping = Mathf.Max(rb.angularDamping, 4f);
    }

    void FixedUpdate()
    {
        if (!forceUpright) return;

        // Mantén la cápsula vertical (X=Z=0), respetando el yaw actual (Y)
        Vector3 e = rb.rotation.eulerAngles;
        Quaternion target = Quaternion.Euler(0f, e.y, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, uprightSlerp * Time.fixedDeltaTime));
    }
}
