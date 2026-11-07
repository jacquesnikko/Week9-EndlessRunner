using System.Collections.Generic;
using UnityEngine;

public class EndlessRunnerGame : MonoBehaviour
{
    private const float PlayerZ = 0f;
    private const float SpawnZ = 24f;
    private const float LaneWidth = 2.2f;
    private const float FocalLength = 18f;

    private class Card
    {
        public Transform transform;
        public SpriteRenderer sprite;
        public Vector3 size;
        public int lane;
        public bool checkedForHit;
    }

    private readonly List<Card> cards = new List<Card>();
    private readonly List<Card> obstacles = new List<Card>();

    private Sprite squareSprite;
    private Transform player;

    private int health = 100;
    private int lane = 1;
    private int targetLane = 1;
    private int score;
    private float playerX;
    private float laneFrom;
    private float laneTo;
    private float laneTime;
    private float jumpTime = -1f;
    private float jumpHeight;
    private float regenerateTime;
    private float spawnTime = 1f;
    private float elapsed;
    private float speed = 9f;
    private bool gameOver;

    private void Awake()
    {
        squareSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        Camera gameCamera = new GameObject("Runner Camera").AddComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 5f;
        gameCamera.backgroundColor = new Color(0.08f, 0.1f, 0.17f);
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.tag = "MainCamera";

        CreateCard("Flat Track", new Vector3(0f, 0f, 40f),
            new Vector3(9f, 12f, 1f), new Color(0.17f, 0.2f, 0.28f));

        CreateLaneLine(0);
        CreateLaneLine(1);
        CreateLaneLine(2);

        Card playerCard = CreateCard("2D Player", new Vector3(0f, -3.1f, PlayerZ),
            new Vector3(0.85f, 1.15f, 1f), new Color(0.12f, 0.78f, 0.98f));
        player = playerCard.transform;

    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                Restart();
            return;
        }

        elapsed += dt;
        score = Mathf.FloorToInt(elapsed * 10f);
        speed = 9f + Mathf.Min(5f, elapsed * 0.08f);

        regenerateTime += dt;
        while (regenerateTime >= 1f)
        {
            health = Mathf.Min(100, health + 1);
            regenerateTime -= 1f;
        }

        ReadInput();
        MovePlayer(dt);
        MoveObstacles(dt);
        SpawnObstacle(dt);
        SortCards();
    }

    private Card CreateCard(string name, Vector3 position, Vector3 size, Color color)
    {
        GameObject cardObject = new GameObject(name);
        cardObject.transform.position = position;
        cardObject.transform.localScale = size;

        SpriteRenderer renderer = cardObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;

        Card card = new Card();
        card.transform = cardObject.transform;
        card.sprite = renderer;
        card.size = size;
        cards.Add(card);
        return card;
    }

    private void CreateLaneLine(int laneIndex)
    {
        float bottomX = LaneX(laneIndex);
        float topX = bottomX * 0.4f;
        float centerX = (bottomX + topX) * 0.5f;
        float centerY = -3.1f + SpawnZ * 0.11f;

        Card line = CreateCard("Lane " + (laneIndex + 1),
            new Vector3(centerX, centerY, 38f),
            new Vector3(0.045f, SpawnZ * 0.22f, 1f),
            new Color(0.42f, 0.38f, 0.92f));

        float angle = laneIndex == 0 ? -14f : laneIndex == 2 ? 14f : 0f;
        line.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    private void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.Space) && jumpTime < 0f)
            jumpTime = 0f;
    }

    private void ChangeLane(int direction)
    {
        int nextLane = Mathf.Clamp(targetLane + direction, 0, 2);
        if (nextLane == targetLane)
            return;

        targetLane = nextLane;
        laneFrom = playerX;
        laneTo = LaneX(targetLane);
        laneTime = 0f;
    }

    private void MovePlayer(float dt)
    {
        if (targetLane != lane)
        {
            laneTime += dt;
            float t = Mathf.Clamp01(laneTime / 0.3f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            playerX = laneFrom + (laneTo - laneFrom) * eased;
            if (t >= 1f)
                lane = targetLane;
        }

        if (jumpTime >= 0f)
        {
            jumpTime += dt;
            float t = Mathf.Clamp01(jumpTime / 0.75f);
            jumpHeight = Mathf.Sin(t * Mathf.PI) * 1.8f;
            if (jumpTime >= 0.75f)
            {
                jumpTime = -1f;
                jumpHeight = 0f;
            }
        }

        player.position = new Vector3(playerX, -3.1f + jumpHeight, PlayerZ);
    }

    private void SpawnObstacle(float dt)
    {
        spawnTime -= dt;
        if (spawnTime > 0f)
            return;

        int obstacleLane = Random.Range(0, 3);
        Card obstacle = CreateCard("2D Obstacle", new Vector3(0f, 0f, SpawnZ),
            new Vector3(1.35f, 1.1f, 1f), new Color(1f, 0.28f, 0.14f));
        obstacle.lane = obstacleLane;
        obstacles.Add(obstacle);
        UpdateObstacle(obstacle);

        spawnTime = 1.2f;
    }

    private void MoveObstacles(float dt)
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            Card obstacle = obstacles[i];
            Vector3 position = obstacle.transform.position;
            position.z -= speed * dt;
            obstacle.transform.position = position;
            UpdateObstacle(obstacle);

            float zDifference = Mathf.Abs(obstacle.transform.position.z - PlayerZ);
            float xDifference = Mathf.Abs(obstacle.transform.position.x - playerX);
            if (!obstacle.checkedForHit && zDifference < 0.8f && xDifference < 0.9f)
            {
                obstacle.checkedForHit = true;
                if (jumpHeight < 1f)
                    health = Mathf.Max(0, health - 5);
                if (health == 0)
                    gameOver = true;
            }

            if (obstacle.transform.position.z < -2f)
            {
                cards.Remove(obstacle);
                obstacles.RemoveAt(i);
                Destroy(obstacle.transform.gameObject);
            }
        }
    }

    private void UpdateObstacle(Card obstacle)
    {
        float z = obstacle.transform.position.z;
        float scale = PerspectiveScale(z);
        obstacle.transform.position = new Vector3(
            LanePositionAtDepth(obstacle.lane, z),
            -3.1f + z * 0.22f,
            z);
        obstacle.transform.localScale = obstacle.size * scale;
    }

    private void SortCards()
    {
        // Far cards first, near cards last, like the painter's algorithm.
        cards.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
        for (int i = 0; i < cards.Count; i++)
            cards[i].sprite.sortingOrder = i;
    }

    private float PerspectiveScale(float z)
    {
        return FocalLength / (FocalLength + z);
    }

    private float LaneX(int laneIndex)
    {
        return (laneIndex - 1) * LaneWidth;
    }

    private float LanePositionAtDepth(int laneIndex, float z)
    {
        float depth = Mathf.Clamp01(z / SpawnZ);
        float perspective = 1f - 0.6f * depth;
        return LaneX(laneIndex) * perspective;
    }

    private void Restart()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            cards.Remove(obstacles[i]);
            Destroy(obstacles[i].transform.gameObject);
        }
        obstacles.Clear();
        health = 100;
        lane = 1;
        targetLane = 1;
        playerX = 0f;
        laneTime = 0f;
        laneFrom = 0f;
        laneTo = 0f;
        jumpTime = -1f;
        jumpHeight = 0f;
        regenerateTime = 0f;
        spawnTime = 1f;
        elapsed = 0f;
        gameOver = false;
        player.position = new Vector3(0f, -3.1f, PlayerZ);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(18f, 18f, 220f, 26f), "HP: " + health + " / 100");
        GUI.Label(new Rect(18f, 44f, 220f, 26f), "Score: " + score);
        GUI.Label(new Rect(18f, 70f, 380f, 26f), "Move: A / D or arrows     Jump: Space");

        if (gameOver)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 70f, Screen.height * 0.5f, 220f, 30f),
                "Game over! Press R to restart");
        }
    }
}
