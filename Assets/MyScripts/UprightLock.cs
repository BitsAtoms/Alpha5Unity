using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UprightLock : MonoBehaviour
{
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // El motor de físicas se encarga automáticamente de que no se caiga.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Obligatorio para evitar "tirones" visuales en objetos físicos
        rb.interpolation = RigidbodyInterpolation.Interpolate; 
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    
    // 🗑️ Hemos ELIMINADO el FixedUpdate() que forzaba los Slerp manuales y causaba el temblor.
}