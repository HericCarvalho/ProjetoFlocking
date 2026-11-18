using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador principal do bando de Boids (Craig Reynolds, 1987).
/// Responsavel pelo instanciamento, parametros globais ajustaveis no Inspector
/// e suporte ao Desafio Opcional de Grid Espacial (Parte C).
/// </summary>
public class FlockManager : MonoBehaviour
{
    public static FlockManager Instance { get; private set; }

    [Header("Instanciacao do Bando")]
    [Tooltip("Quantidade de agentes boids instanciados (minimo 150)")]
    [SerializeField] private int boidCount = 150;
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private float spawnRadius = 10.0f;

    [Header("Pesos das Regras Fundamentais (Ajustaveis no Inspector)")]
    [Tooltip("Peso da forca de Separacao (evitar vizinhos proximos, proporcional a 1/d^2)")]
    [Range(0f, 10f)] public float separationWeight = 2.5f;

    [Tooltip("Peso da forca de Alinhamento (voar na direcao media dos vizinhos)")]
    [Range(0f, 10f)] public float alignmentWeight = 1.8f;

    [Tooltip("Peso da forca de Coesao (nadar/voar em direcao ao centro de massa local)")]
    [Range(0f, 10f)] public float cohesionWeight = 1.2f;

    [Header("Raios e Dimensoes")]
    [Tooltip("Raio de percepcao do boid (usado no teste de 0.5 vs 5.0 da Parte B)")]
    [Range(0.2f, 10f)] public float perceptionRadius = 2.5f;

    [Tooltip("Distancia minima para comecar a aplicar a forca de separacao")]
    [Range(0.2f, 5f)] public float separationDistance = 1.2f;

    [Header("Limites Dinamicos e Seguranca Numerica")]
    [Tooltip("Velocidade maxima permitida para cada boid")]
    public float maxSpeed = 8.0f;

    [Tooltip("Velocidade minima para manter o boid em movimento fluido")]
    public float minSpeed = 2.0f;

    [Tooltip("Forca maxima aplicada a cada frame (limite clamp)")]
    public float maxForce = 15.0f;

    [Tooltip("Velocidade de suavizacao da rotacao")]
    public float rotationSpeed = 6.0f;

    [Header("Limites do Mundo (Raio Central)")]
    [Tooltip("Raio central do mundo. Dentro dele a forca e zero; fora dele, aplica forca de retorno.")]
    public float boundaryRadius = 22.0f;
    [Tooltip("Intensidade da forca de retorno aos limites")]
    public float boundaryWeight = 6.0f;

    [Header("Desvio de Obstaculos")]
    [Tooltip("Camada de obstaculos para o SphereCast")]
    public LayerMask obstacleLayer;
    [Tooltip("Distancia frontal do raio de deteccao de obstaculo")]
    public float obstacleProbeDistance = 4.0f;
    [Tooltip("Raio da esfera projetada pelo SphereCast")]
    public float obstacleProbeRadius = 0.5f;
    [Tooltip("Peso da forca de desvio de obstaculos")]
    public float obstacleAvoidanceWeight = 18.0f;

    [Header("Predador (Experimento da Parte B)")]
    public Transform predator;
    public float predatorPanicRadius = 7.0f;
    public float predatorEvadeWeight = 22.0f;

    [Header("Desafio Opcional: Grid Espacial (Parte C)")]
    [Tooltip("Ativa a busca por Grid Espacial 3D (Dictionary<Vector3Int, List<Boid>>) para 500+ boids")]
    public bool useSpatialGrid = false;
    public float gridCellSize = 2.0f;

    [Header("Gizmos e Debug")]
    public bool drawAllGizmos = false;

    private readonly List<Boid> boids = new List<Boid>(512);
    private SpatialGrid spatialGrid;

    public int BoidsCount => boids.Count;
    public Vector3 FlockCenterOfMass { get; private set; }
    public float PredatorPanicRadius => predatorPanicRadius;
    public SpatialGrid Grid => spatialGrid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        spatialGrid = new SpatialGrid(gridCellSize);
    }

    private void Start()
    {
        SpawnFlock();
    }

    private void SpawnFlock()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("[FlockManager] Prefab do Boid nao atribuido no Inspector!");
            return;
        }

        for (int i = 0; i < boidCount; i++)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion randomRot = Random.rotation;

            GameObject go = Instantiate(boidPrefab, randomPos, randomRot, transform);
            Boid boid = go.GetComponent<Boid>();

            if (boid != null)
            {
                // Configura o primeiro boid para exibir os gizmos do raio de percepcao
                if (i == 0)
                {
                    boid.IsGizmoLeader = true;
                }
                boids.Add(boid);
            }
        }
    }

    private void Update()
    {
        // Reconstroi o Grid Espacial se o modo otimizado estiver ativado
        if (useSpatialGrid)
        {
            spatialGrid.Clear();
            int count = boids.Count;
            for (int i = 0; i < count; i++)
            {
                spatialGrid.Insert(boids[i]);
            }
        }

        // Calcula o centro de massa medio do bando (util para o predador)
        if (boids.Count > 0)
        {
            Vector3 center = Vector3.zero;
            int count = boids.Count;
            for (int i = 0; i < count; i++)
            {
                center += boids[i].transform.position;
            }
            FlockCenterOfMass = center / count;
        }
    }

    private void OnDrawGizmos()
    {
        // Desenha a esfera limite do mundo
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, boundaryRadius);
    }
}
