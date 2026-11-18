#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlockingSceneBuilder
{
    [MenuItem("Flocking/Construir Cena e Prefabs Automaticamente")]
    public static void BuildScene()
    {
        Debug.Log("[FlockingSceneBuilder] Iniciando geracao de materiais, prefabs e cena...");

        // 1. Cria pastas essenciais
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        // 2. Cria Materiais
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        Material boidMat = new Material(standardShader) { color = new Color(0.1f, 0.75f, 1.0f) };
        AssetDatabase.CreateAsset(boidMat, "Assets/Materials/M_Boid.mat");

        Material obstacleMat = new Material(standardShader) { color = new Color(1.0f, 0.45f, 0.1f) };
        AssetDatabase.CreateAsset(obstacleMat, "Assets/Materials/M_Obstacle.mat");

        Material predatorMat = new Material(standardShader) { color = new Color(0.9f, 0.1f, 0.1f) };
        AssetDatabase.CreateAsset(predatorMat, "Assets/Materials/M_Predator.mat");

        AssetDatabase.SaveAssets();

        // 3. Cria Prefab do Boid
        int boidLayer = LayerMask.NameToLayer("Boid");
        if (boidLayer < 0) boidLayer = 0;

        GameObject boidGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boidGo.transform.localScale = new Vector3(0.35f, 0.35f, 0.85f);

        boidGo.name = "BoidPrefab";
        boidGo.layer = boidLayer;

        Renderer boidRenderer = boidGo.GetComponent<Renderer>();
        if (boidRenderer != null) boidRenderer.sharedMaterial = boidMat;

        Rigidbody rb = boidGo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Collider col = boidGo.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        boidGo.AddComponent<Boid>();

        string prefabPath = "Assets/Prefabs/BoidPrefab.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(boidGo, prefabPath);
        Object.DestroyImmediate(boidGo);
        Debug.Log("[FlockingSceneBuilder] Prefab salvo em: " + prefabPath);

        // 4. Cria Nova Cena
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Iluminacao
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Camera
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
        camGo.transform.position = new Vector3(0f, 12f, -38f);
        camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        // Obstaculos (Camada Obstacle)
        int obsLayer = LayerMask.NameToLayer("Obstacle");
        if (obsLayer < 0) obsLayer = 0;

        GameObject obs1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obs1.name = "Obstaculo_Esfera1";
        obs1.layer = obsLayer;
        obs1.transform.position = new Vector3(6f, 0f, 6f);
        obs1.transform.localScale = new Vector3(6.5f, 6.5f, 6.5f);
        obs1.GetComponent<Renderer>().sharedMaterial = obstacleMat;

        GameObject obs2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obs2.name = "Obstaculo_Esfera2";
        obs2.layer = obsLayer;
        obs2.transform.position = new Vector3(-7f, 2f, -5f);
        obs2.transform.localScale = new Vector3(7.5f, 7.5f, 7.5f);
        obs2.GetComponent<Renderer>().sharedMaterial = obstacleMat;

        GameObject obs3 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obs3.name = "Obstaculo_ColunaCentral";
        obs3.layer = obsLayer;
        obs3.transform.position = new Vector3(0f, -2f, 0f);
        obs3.transform.localScale = new Vector3(4.5f, 7.0f, 4.5f);
        obs3.GetComponent<Renderer>().sharedMaterial = obstacleMat;

        // Predador (Parte B)
        int predLayer = LayerMask.NameToLayer("Predator");
        if (predLayer < 0) predLayer = 0;

        GameObject predatorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        predatorGo.name = "Predador";
        predatorGo.layer = predLayer;
        predatorGo.transform.position = new Vector3(0f, 0f, 15f);
        predatorGo.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        predatorGo.GetComponent<Renderer>().sharedMaterial = predatorMat;
        predatorGo.AddComponent<Predator>();

        // FlockManager
        GameObject managerGo = new GameObject("FlockManager");
        FlockManager fm = managerGo.AddComponent<FlockManager>();

        SerializedObject so = new SerializedObject(fm);
        so.FindProperty("boidCount").intValue = 150;
        so.FindProperty("boidPrefab").objectReferenceValue = prefabAsset;
        so.FindProperty("spawnRadius").floatValue = 8.0f;
        so.FindProperty("separationWeight").floatValue = 2.5f;
        so.FindProperty("alignmentWeight").floatValue = 1.8f;
        so.FindProperty("cohesionWeight").floatValue = 1.2f;
        so.FindProperty("perceptionRadius").floatValue = 2.5f;
        so.FindProperty("separationDistance").floatValue = 1.2f;
        so.FindProperty("maxSpeed").floatValue = 8.0f;
        so.FindProperty("minSpeed").floatValue = 2.0f;
        so.FindProperty("maxForce").floatValue = 15.0f;
        so.FindProperty("boundaryRadius").floatValue = 22.0f;
        so.FindProperty("boundaryWeight").floatValue = 6.0f;
        so.FindProperty("obstacleProbeDistance").floatValue = 4.5f;
        so.FindProperty("obstacleProbeRadius").floatValue = 0.6f;
        so.FindProperty("obstacleAvoidanceWeight").floatValue = 18.0f;
        so.FindProperty("obstacleLayer").intValue = 1 << obsLayer;
        so.FindProperty("predator").objectReferenceValue = predatorGo.transform;
        so.FindProperty("predatorPanicRadius").floatValue = 7.0f;
        so.FindProperty("predatorEvadeWeight").floatValue = 22.0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Salva a Cena
        string scenePath = "Assets/Scenes/FlockingScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[FlockingSceneBuilder] Cena salva em: " + scenePath);

        // Adiciona a cena aos Build Settings
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FlockingSceneBuilder] CONCLUIDO COM SUCESSO! A cena principal foi gerada e configurada.");
    }
}
#endif
