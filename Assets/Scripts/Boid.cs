using UnityEngine;

/// <summary>
/// Agente individual do bando (Craig Reynolds, 1987).
/// Executa as 3 regras locais (Separacao 1/d^2, Alinhamento, Coesao),
/// desvio de obstaculos com SphereCast e Vector3.Reflect,
/// forca de retorno em raio central e seguranca numerica anti-NaN.
/// Zero alocacao de memoria no Garbage Collector (GC) por frame.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Boid : MonoBehaviour
{
    private Rigidbody rb;
    private FlockManager manager;
    private Vector3 velocity;

    // Buffer fixo reutilizado a cada frame para evitar qualquer alocacao no GC
    private readonly Collider[] neighborColliders = new Collider[64];
    private readonly Boid[] gridNeighborBuffer = new Boid[64];

    // LayerMask para filtrar apenas outros boids na busca
    private int boidLayerMask;

    // Flag para desenhar gizmo no primeiro boid
    public bool IsGizmoLeader { get; set; }

    public Vector3 Velocity => velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Configura a mascara de colisao com a camada Boid
        int boidLayer = LayerMask.NameToLayer("Boid");
        if (boidLayer >= 0)
        {
            boidLayerMask = 1 << boidLayer;
        }
        else
        {
            boidLayerMask = ~0; // fallback para todas as camadas
        }
    }

    private void Start()
    {
        manager = FlockManager.Instance;

        // Inicializa com direcao aleatoria e velocidade inicial
        float initSpeed = manager != null ? (manager.minSpeed + manager.maxSpeed) * 0.5f : 5.0f;
        velocity = transform.forward * initSpeed;
        if (velocity.sqrMagnitude < 0.001f)
        {
            velocity = Random.insideUnitSphere.normalized * initSpeed;
        }

        ApplyVelocityToRigidbody();
    }

    private void Update()
    {
        if (manager == null)
        {
            manager = FlockManager.Instance;
            if (manager == null) return;
        }

        Vector3 totalForce = Vector3.zero;

        // 1. Forcas de Bando (Separacao 1/d^2, Alinhamento e Coesao)
        if (manager.useSpatialGrid && manager.Grid != null)
        {
            totalForce += ComputeFlockForcesSpatialGrid();
        }
        else
        {
            totalForce += ComputeFlockForcesPhysics();
        }

        // 2. Desvio de Obstaculos (SphereCast + Vector3.Reflect)
        totalForce += ComputeObstacleAvoidance();

        // 3. Limite do Mundo (Forca de Retorno ao Raio Central)
        totalForce += ComputeBoundaryForce();

        // 4. Fuga do Predador (Experimento da Parte B)
        totalForce += ComputePredatorEvade();

        // 5. Seguranca Numerica (Protecao contra NaN e Clamping)
        ApplyPhysicsSafety(totalForce);
    }

    /// <summary>
    /// Calcula as 3 regras usando Physics.OverlapSphereNonAlloc (Zero GC Alloc).
    /// </summary>
    private Vector3 ComputeFlockForcesPhysics()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            manager.perceptionRadius,
            neighborColliders,
            boidLayerMask
        );

        Vector3 separationForce = Vector3.zero;
        Vector3 alignmentSum = Vector3.zero;
        Vector3 centerOfMassSum = Vector3.zero;
        int validNeighbors = 0;

        float separationDistSqr = manager.separationDistance * manager.separationDistance;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Collider col = neighborColliders[i];
            if (col == null || col.gameObject == gameObject) continue;

            Boid neighbor = col.GetComponent<Boid>();
            if (neighbor == null) continue;

            Vector3 diff = currentPos - neighbor.transform.position;
            float distSqr = diff.sqrMagnitude;

            // Evita divisao por zero
            if (distSqr < 0.0001f) continue;

            float dist = Mathf.Sqrt(distSqr);

            // Regra 1: Separacao proporcional a 1/d^2 (quanto mais perto, mais repele)
            if (distSqr < separationDistSqr)
            {
                float safeDist = Mathf.Max(dist, 0.05f);
                Vector3 pushDir = diff / dist;
                float intensity = 1.0f / (safeDist * safeDist);
                separationForce += pushDir * intensity;
            }

            // Regra 2: Alinhamento (direcao media das velocidades dos vizinhos)
            alignmentSum += neighbor.Velocity;

            // Regra 3: Coesao (centro de massa local do grupo)
            centerOfMassSum += neighbor.transform.position;

            validNeighbors++;
        }

        Vector3 combinedForce = Vector3.zero;

        if (validNeighbors > 0)
        {
            // Forca de Separacao ponderada
            combinedForce += separationForce * manager.separationWeight;

            // Forca de Alinhamento ponderada (media das velocidades - velocidade atual)
            Vector3 avgVelocity = alignmentSum / validNeighbors;
            Vector3 alignmentForce = (avgVelocity - velocity);
            combinedForce += alignmentForce * manager.alignmentWeight;

            // Forca de Coesao ponderada (direcao para o centro de massa)
            Vector3 centerOfMass = centerOfMassSum / validNeighbors;
            Vector3 cohesionDir = (centerOfMass - currentPos);
            if (cohesionDir.sqrMagnitude > 0.0001f)
            {
                Vector3 desiredVelocity = cohesionDir.normalized * manager.maxSpeed;
                Vector3 cohesionForce = (desiredVelocity - velocity);
                combinedForce += cohesionForce * manager.cohesionWeight;
            }
        }

        return combinedForce;
    }

    /// <summary>
    /// Desafio Opcional (Parte C): Calcula as 3 regras usando Grid Espacial (500+ boids a 60 fps).
    /// </summary>
    private Vector3 ComputeFlockForcesSpatialGrid()
    {
        int count = manager.Grid.GetNeighborsNonAlloc(
            transform.position,
            manager.perceptionRadius,
            gridNeighborBuffer
        );

        Vector3 separationForce = Vector3.zero;
        Vector3 alignmentSum = Vector3.zero;
        Vector3 centerOfMassSum = Vector3.zero;
        int validNeighbors = 0;

        float separationDistSqr = manager.separationDistance * manager.separationDistance;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Boid neighbor = gridNeighborBuffer[i];
            if (neighbor == null || neighbor == this) continue;

            Vector3 diff = currentPos - neighbor.transform.position;
            float distSqr = diff.sqrMagnitude;
            if (distSqr < 0.0001f) continue;

            float dist = Mathf.Sqrt(distSqr);

            if (distSqr < separationDistSqr)
            {
                float safeDist = Mathf.Max(dist, 0.05f);
                Vector3 pushDir = diff / dist;
                float intensity = 1.0f / (safeDist * safeDist);
                separationForce += pushDir * intensity;
            }

            alignmentSum += neighbor.Velocity;
            centerOfMassSum += neighbor.transform.position;
            validNeighbors++;
        }

        Vector3 combinedForce = Vector3.zero;
        if (validNeighbors > 0)
        {
            combinedForce += separationForce * manager.separationWeight;

            Vector3 avgVelocity = alignmentSum / validNeighbors;
            Vector3 alignmentForce = (avgVelocity - velocity);
            combinedForce += alignmentForce * manager.alignmentWeight;

            Vector3 centerOfMass = centerOfMassSum / validNeighbors;
            Vector3 cohesionDir = (centerOfMass - currentPos);
            if (cohesionDir.sqrMagnitude > 0.0001f)
            {
                Vector3 desiredVelocity = cohesionDir.normalized * manager.maxSpeed;
                Vector3 cohesionForce = (desiredVelocity - velocity);
                combinedForce += cohesionForce * manager.cohesionWeight;
            }
        }

        return combinedForce;
    }

    /// <summary>
    /// Desvio de obstaculos usando SphereCast e Vector3.Reflect sem atravessar.
    /// </summary>
    private Vector3 ComputeObstacleAvoidance()
    {
        if (velocity.sqrMagnitude < 0.001f) return Vector3.zero;

        Ray ray = new Ray(transform.position, velocity.normalized);
        if (Physics.SphereCast(ray, manager.obstacleProbeRadius, out RaycastHit hit, manager.obstacleProbeDistance, manager.obstacleLayer))
        {
            // Calcula o vetor refletido a partir da normal do ponto de impacto
            Vector3 reflectDir = Vector3.Reflect(velocity.normalized, hit.normal);

            // A forca combina o vetor de reflexao com a normal para afastar firmemente da superficie
            Vector3 avoidDir = (reflectDir + hit.normal).normalized;

            // Quanto mais perto da colisao, maior a urgencia do desvio
            float distanceRatio = 1.0f - Mathf.Clamp01(hit.distance / manager.obstacleProbeDistance);
            return avoidDir * (distanceRatio * manager.obstacleAvoidanceWeight * manager.maxSpeed);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Limites do mundo: raio central. Forca zero dentro do raio; forca de retorno fora dele.
    /// </summary>
    private Vector3 ComputeBoundaryForce()
    {
        Vector3 center = manager.transform.position;
        Vector3 offset = transform.position - center;
        float distFromCenter = offset.magnitude;

        if (distFromCenter > manager.boundaryRadius)
        {
            float excess = distFromCenter - manager.boundaryRadius;
            Vector3 returnDir = -offset / distFromCenter;
            return returnDir * (excess * manager.boundaryWeight);
        }

        return Vector3.zero; // Forca zero dentro do raio central
    }

    /// <summary>
    /// Comportamento de fuga do Predador com peso alto (Parte B).
    /// </summary>
    private Vector3 ComputePredatorEvade()
    {
        if (manager.predator == null) return Vector3.zero;

        Vector3 toPredator = manager.predator.position - transform.position;
        float distToPredator = toPredator.magnitude;

        if (distToPredator < manager.predatorPanicRadius && distToPredator > 0.001f)
        {
            Vector3 fleeDir = -toPredator / distToPredator;
            float panicFactor = 1.0f - (distToPredator / manager.predatorPanicRadius);
            return fleeDir * (panicFactor * manager.predatorEvadeWeight * manager.maxSpeed);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Garante a seguranca numerica do comportamento:
    /// - limite da forca por maxForce
    /// - limite de velocidade por maxSpeed
    /// - LookRotation apenas quando velocity > 0.01
    /// - protecao total contra NaN
    /// </summary>
    private void ApplyPhysicsSafety(Vector3 force)
    {
        // Protecao contra valores NaN na forca
        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
        {
            force = Vector3.zero;
        }

        // Limita a forca final por maxForce
        force = Vector3.ClampMagnitude(force, manager.maxForce);

        // Atualiza a velocidade
        velocity += force * Time.deltaTime;

        // Protecao contra valores NaN na velocidade
        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
        {
            velocity = transform.forward * manager.minSpeed;
        }

        // Limita a velocidade maxima por maxSpeed
        velocity = Vector3.ClampMagnitude(velocity, manager.maxSpeed);

        // Garante velocidade minima para evitar boids parados
        if (velocity.sqrMagnitude < manager.minSpeed * manager.minSpeed)
        {
            velocity = (velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward) * manager.minSpeed;
        }

        // Atualiza Rigidbody
        ApplyVelocityToRigidbody();

        // Rotaciona somente quando velocity > 0.01 (sqrMagnitude > 0.0001)
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * manager.rotationSpeed);
        }
    }

    private void ApplyVelocityToRigidbody()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    /// <summary>
    /// Desenha o raio de percepcao do boid no Scene View / Play Mode.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (IsGizmoLeader || (FlockManager.Instance != null && FlockManager.Instance.drawAllGizmos))
        {
            float radius = FlockManager.Instance != null ? FlockManager.Instance.perceptionRadius : 2.5f;
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);

            // Desenha o raio de separacao interno
            float sepRadius = FlockManager.Instance != null ? FlockManager.Instance.separationDistance : 1.2f;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, sepRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        float radius = FlockManager.Instance != null ? FlockManager.Instance.perceptionRadius : 2.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
