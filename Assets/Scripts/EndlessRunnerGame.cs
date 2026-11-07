using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds and runs a small endless runner. All gameplay movement is calculated
/// directly; this scene does not use a Rigidbody or Unity physics collisions.
/// </summary>
public class EndlessRunnerGame : MonoBehaviour
{
    private const int MaxHealth = 100;
    private const float LaneWidth = 2.25f;
    private const float PlayerZ = 0f;
    private const float FocalLength = 18f;
    private const float SpawnZ = 34f;
    private const float CollisionZRange = 0.85f;
    private const float JumpDuration = 0.78f;
    private const float JumpHeight = 2.35f;
    private const float HitHeight = 1.25f;
    private const float LaneChangeDuration = 0.32f;

    private struct Obstacle
    {
        public GameObject Object;
        public int Lane;
        public bool Checked;
        public Vector3 BaseSize;
    }

    private struct RoadMarker
    {
        public Transform Transform;
        public float BaseX;
        public Vector3 BaseSize;
    }

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly List<RoadMarker> roadMarkers = new List<RoadMarker>();

    private Transform player;
    private Camera gameCamera;
    private Material playerMaterial;
    private Material obstacleMaterial;
    private Material roadMaterial;
    private Material markerMaterial;

    private int health = MaxHealth;
    private int currentLane = 1;
    private int targetLane = 1;
    private int score;
    private float elapsed;
    private float spawnTimer = 1.1f;
    private float worldSpeed = 10f;
    private float jumpTimer = -1f;
    private float jumpOffset;
    private float laneMoveTimer;
    private float laneMoveFrom;
    private float laneMoveTo;
    private float playerX;
    private bool gameOver;

    private void Awake()
    {
        CreateMaterials();
        CreateCamera();
        CreateTrack();
        CreatePlayer();
        CreateRoadMarkers();
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                RestartGame();
            return;
        }

        elapsed += delta;
        score = Mathf.FloorToInt(elapsed * 10f);
        worldSpeed = 10f + Mathf.Min(8f, elapsed * 0.12f);
        // Accumulate fractional regeneration so health restores one point per second.
        regenerationRemainder += delta;
        if (regenerationRemainder >= 1f)
        {
            int restored = Mathf.FloorToInt(regenerationRemainder);
            health = Mathf.Min(MaxHealth, health + restored);
            regenerationRemainder -= restored;
        }

        ReadInput();
        MovePlayer(delta);
        MoveRoadMarkers(delta);
        UpdateObstacles(delta);
        SpawnObstacles(delta);
    }

    private float regenerationRemainder;

    private void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.Space) && jumpTimer < 0f)
            jumpTimer = 0f;
    }

    private void ChangeLane(int direction)
    {
        int nextLane = Mathf.Clamp(targetLane + direction, 0, 2);
        if (nextLane == targetLane)
            return;

        targetLane = nextLane;
        laneMoveFrom = playerX;
        laneMoveTo = LanePosition(targetLane);
        laneMoveTimer = 0f;
    }

    private void MovePlayer(float delta)
    {
        if (targetLane != currentLane || Mathf.Abs(playerX - laneMoveTo) > 0.001f)
        {
            laneMoveTimer += delta;
            float t = Mathf.Clamp01(laneMoveTimer / LaneChangeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            playerX = laneMoveFrom + (laneMoveTo - laneMoveFrom) * eased;
            if (t >= 1f)
                currentLane = targetLane;
        }

        if (jumpTimer >= 0f)
        {
            jumpTimer += delta;
            float t = Mathf.Clamp01(jumpTimer / JumpDuration);
            jumpOffset = Mathf.Sin(t * Mathf.PI) * JumpHeight;
            if (jumpTimer >= JumpDuration)
            {
                jumpTimer = -1f;
                jumpOffset = 0f;
            }
        }

        player.position = new Vector3(playerX, 0.9f + jumpOffset, PlayerZ);
    }

    private void SpawnObstacles(float delta)
    {
        spawnTimer -= delta;
        if (spawnTimer > 0f)
            return;

        int lane = Random.Range(0, 3);
        Vector3 size = new Vector3(1.35f, 1.05f, 0.65f);
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Obstacle";
        block.transform.position = new Vector3(0f, 0f, SpawnZ);
        block.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        block.GetComponent<Collider>().enabled = false;

        Obstacle obstacle = new Obstacle();
        obstacle.Object = block;
        obstacle.Lane = lane;
        obstacle.Checked = false;
        obstacle.BaseSize = size;
        obstacles.Add(obstacle);
        ApplyObstaclePerspective(obstacle);

        float interval = Mathf.Max(0.72f, 1.3f - elapsed * 0.006f);
        spawnTimer = interval + Random.Range(0.05f, 0.55f);
    }

    private void UpdateObstacles(float delta)
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = obstacles[i];
            if (obstacle.Object == null)
            {
                obstacles.RemoveAt(i);
                continue;
            }

            Vector3 position = obstacle.Object.transform.position;
            position.z -= worldSpeed * delta;
            obstacle.Object.transform.position = position;
            ApplyObstaclePerspective(obstacle);

            float zDifference = Mathf.Abs(obstacle.Object.transform.position.z - PlayerZ);
            float laneDifference = Mathf.Abs(obstacle.Object.transform.position.x - playerX);
            if (!obstacle.Checked && laneDifference <= 0.95f && zDifference <= CollisionZRange)
            {
                obstacle.Checked = true;
                if (jumpOffset < HitHeight)
                    TakeDamage(5);
            }

            if (obstacle.Object.transform.position.z < -4f)
            {
                Destroy(obstacle.Object);
                obstacles.RemoveAt(i);
            }
        }
    }

    private void ApplyObstaclePerspective(Obstacle obstacle)
    {
        Vector3 position = obstacle.Object.transform.position;
        float depth = Mathf.Max(0f, position.z - PlayerZ);
        float scale = PerspectiveScale(depth);
        position.x = LanePosition(obstacle.Lane) * scale;
        position.y = obstacle.BaseSize.y * scale * 0.5f;
        obstacle.Object.transform.position = position;
        obstacle.Object.transform.localScale = obstacle.BaseSize * scale;
    }

    private void TakeDamage(int amount)
    {
        health = Mathf.Max(0, health - amount);
        if (health == 0)
            gameOver = true;
    }

    private void RestartGame()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].Object != null)
                Destroy(obstacles[i].Object);
        }
        obstacles.Clear();
        health = MaxHealth;
        currentLane = 1;
        targetLane = 1;
        playerX = 0f;
        laneMoveFrom = 0f;
        laneMoveTo = 0f;
        laneMoveTimer = 0f;
        jumpTimer = -1f;
        jumpOffset = 0f;
        elapsed = 0f;
        score = 0;
        worldSpeed = 10f;
        spawnTimer = 1.1f;
        regenerationRemainder = 0f;
        gameOver = false;
        player.position = new Vector3(0f, 0.9f, PlayerZ);
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        playerMaterial = MakeMaterial(shader, new Color(0.15f, 0.78f, 0.98f));
        obstacleMaterial = MakeMaterial(shader, new Color(1f, 0.28f, 0.12f));
        roadMaterial = MakeMaterial(shader, new Color(0.105f, 0.13f, 0.19f));
        markerMaterial = MakeMaterial(shader, new Color(0.92f, 0.9f, 0.72f));
    }

    private Material MakeMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Runner Camera");
        cameraObject.tag = "MainCamera";
        gameCamera = cameraObject.AddComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 7.2f;
        gameCamera.backgroundColor = new Color(0.05f, 0.08f, 0.15f);
        cameraObject.transform.position = new Vector3(0f, 5.5f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(17f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Scene Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
    }

    private void CreateTrack()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Endless Track";
        ground.transform.position = new Vector3(0f, -0.2f, 17f);
        ground.transform.localScale = new Vector3(9f, 0.4f, 62f);
        ground.GetComponent<Renderer>().sharedMaterial = roadMaterial;
        ground.GetComponent<Collider>().enabled = false;
    }

    private void CreatePlayer()
    {
        GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(0f, 0.9f, PlayerZ);
        playerObject.transform.localScale = new Vector3(0.82f, 0.9f, 0.72f);
        playerObject.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        playerObject.GetComponent<Collider>().enabled = false;
        player = playerObject.transform;
    }

    private void CreateRoadMarkers()
    {
        for (int i = 0; i < 18; i++)
        {
            float z = 2f + i * 2f;
            CreateRoadMarker(-LaneWidth * 0.5f, z);
            CreateRoadMarker(LaneWidth * 0.5f, z);
        }
    }

    private void CreateRoadMarker(float x, float z)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Perspective Lane Marker";
        marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        marker.GetComponent<Collider>().enabled = false;
        RoadMarker roadMarker = new RoadMarker();
        roadMarker.Transform = marker.transform;
        roadMarker.BaseX = x;
        roadMarker.BaseSize = new Vector3(0.055f, 0.025f, 0.72f);
        roadMarkers.Add(roadMarker);
        SetRoadMarker(roadMarker, z);
    }

    private void MoveRoadMarkers(float delta)
    {
        for (int i = 0; i < roadMarkers.Count; i++)
        {
            RoadMarker marker = roadMarkers[i];
            Vector3 position = marker.Transform.position;
            position.z -= worldSpeed * delta;
            if (position.z < 1f)
                position.z += 36f;
            SetRoadMarker(marker, position.z);
        }
    }

    private void SetRoadMarker(RoadMarker marker, float z)
    {
        float scale = PerspectiveScale(Mathf.Max(0f, z - PlayerZ));
        marker.Transform.position = new Vector3(marker.BaseX * scale, -0.005f, z);
        marker.Transform.localScale = marker.BaseSize * scale;
    }

    private float PerspectiveScale(float z)
    {
        return Mathf.Clamp(FocalLength / (FocalLength + z), 0.24f, 1f);
    }

    private float LanePosition(int lane)
    {
        return (lane - 1) * LaneWidth;
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Box(new Rect(20f, 18f, 258f, 72f), GUIContent.none);
        GUI.Label(new Rect(34f, 24f, 220f, 24f), "HP  " + health + " / " + MaxHealth);

        GUI.color = new Color(0.12f, 0.16f, 0.2f);
        GUI.Box(new Rect(34f, 53f, 220f, 18f), GUIContent.none);
        GUI.color = health > 30 ? new Color(0.25f, 0.88f, 0.48f) : new Color(1f, 0.25f, 0.18f);
        GUI.Box(new Rect(34f, 53f, 220f * health / MaxHealth, 18f), GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 98f, 250f, 24f), "Score  " + score);
        GUI.Label(new Rect(20f, 122f, 340f, 24f), "Move: A / D or arrows     Jump: Space");

        if (gameOver)
        {
            float centerX = Screen.width * 0.5f;
            GUI.Box(new Rect(centerX - 150f, Screen.height * 0.5f - 58f, 300f, 116f), GUIContent.none);
            GUI.Label(new Rect(centerX - 110f, Screen.height * 0.5f - 40f, 220f, 32f), "GAME OVER");
            GUI.Label(new Rect(centerX - 125f, Screen.height * 0.5f - 7f, 250f, 28f), "Press R to run again");
        }
    }
}
