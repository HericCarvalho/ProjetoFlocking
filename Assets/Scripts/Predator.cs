using UnityEngine;

/// <summary>
/// Predador para os experimentos da Parte B.
/// Persegue o centro do bando de boids ou o boid mais proximo.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class Predator : MonoBehaviour
{
    [Header("Configuracoes de Movimento")]
    [SerializeField] private float speed = 7.0f;
    [SerializeField] private float turnSpeed = 4.0f;
    [SerializeField] private float boundaryRadius = 25.0f;

    private Vector3 targetPosition;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true; // Movimento controlado via script
        }
    }

    private void Update()
    {
        if (FlockManager.Instance != null && FlockManager.Instance.BoidsCount > 0)
        {
            // Persegue o centro de massa do bando
            targetPosition = FlockManager.Instance.FlockCenterOfMass;
        }
        else
        {
            targetPosition = Vector3.zero;
        }

        // Se sair do limite central, forca o retorno ao centro
        if (transform.position.magnitude > boundaryRadius)
        {
            targetPosition = Vector3.zero;
        }

        Vector3 direction = (targetPosition - transform.position);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            transform.position += transform.forward * (speed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 1.5f);
        if (FlockManager.Instance != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, FlockManager.Instance.PredatorPanicRadius);
        }
    }
}
