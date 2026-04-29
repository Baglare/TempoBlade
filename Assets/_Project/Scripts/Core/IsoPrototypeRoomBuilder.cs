using UnityEngine;

[DisallowMultipleComponent]
public class IsoPrototypeRoomBuilder : MonoBehaviour
{
    [Header("Arena")]
    public float width = 18f;
    public float height = 11f;
    public float wallThickness = 0.75f;

    [Header("Visuals")]
    public Color floorColor = new Color(0.2f, 0.22f, 0.26f, 1f);
    public Color backPropColor = new Color(0.28f, 0.33f, 0.42f, 1f);
    public Color frontPropColor = new Color(0.18f, 0.2f, 0.25f, 1f);

    private static Sprite whiteSprite;

    private void Awake()
    {
        BuildIfNeeded();
    }

    [ContextMenu("Build Prototype Room Runtime Layout")]
    public void BuildIfNeeded()
    {
        RoomLayout layout = GetComponent<RoomLayout>();
        if (layout == null)
            layout = gameObject.AddComponent<RoomLayout>();

        Transform logic = GetOrCreateChild(transform, "Logic");
        Transform collision = GetOrCreateChild(transform, "Collision");
        Transform visuals = GetOrCreateChild(transform, "Visuals");

        Transform spawnPoints = GetOrCreateChild(logic, "SpawnPoints");
        Transform doors = GetOrCreateChild(logic, "Doors");
        Transform cameraBounds = GetOrCreateChild(logic, "CameraBounds");
        Transform wallColliders = GetOrCreateChild(collision, "WallColliders");
        Transform floor = GetOrCreateChild(visuals, "Floor");
        Transform backProps = GetOrCreateChild(visuals, "BackProps");
        Transform dynamicProps = GetOrCreateChild(visuals, "DynamicProps");
        Transform frontProps = GetOrCreateChild(visuals, "FrontProps");
        GetOrCreateChild(visuals, "GroundVFX");

        Transform playerStart = GetOrCreateChild(spawnPoints, "P_SpawnPoint");
        playerStart.localPosition = new Vector3(0f, -height * 0.32f, 0f);

        Transform[] enemySpawns =
        {
            GetOrCreateChild(spawnPoints, "E_SpawnPoint_1"),
            GetOrCreateChild(spawnPoints, "E_SpawnPoint_2"),
            GetOrCreateChild(spawnPoints, "E_SpawnPoint_3"),
            GetOrCreateChild(spawnPoints, "E_SpawnPoint_4")
        };

        enemySpawns[0].localPosition = new Vector3(-width * 0.25f, height * 0.18f, 0f);
        enemySpawns[1].localPosition = new Vector3(width * 0.22f, height * 0.2f, 0f);
        enemySpawns[2].localPosition = new Vector3(-width * 0.3f, -height * 0.05f, 0f);
        enemySpawns[3].localPosition = new Vector3(width * 0.3f, -height * 0.08f, 0f);

        layout.playerStartPoint = playerStart;
        layout.enemySpawnPoints = enemySpawns;
        layout.rewardDoors = doors.GetComponentsInChildren<RewardDoor>(true);

        EnsureBox(cameraBounds.gameObject, Vector2.zero, new Vector2(width, height), true);
        EnsureWall(wallColliders, "TopWall", new Vector2(0f, height * 0.5f + wallThickness * 0.5f), new Vector2(width + wallThickness * 2f, wallThickness));
        EnsureWall(wallColliders, "BottomWall", new Vector2(0f, -height * 0.5f - wallThickness * 0.5f), new Vector2(width + wallThickness * 2f, wallThickness));
        EnsureWall(wallColliders, "LeftWall", new Vector2(-width * 0.5f - wallThickness * 0.5f, 0f), new Vector2(wallThickness, height));
        EnsureWall(wallColliders, "RightWall", new Vector2(width * 0.5f + wallThickness * 0.5f, 0f), new Vector2(wallThickness, height));

        EnsureRect(floor, "IsoDebugFloor", Vector2.zero, new Vector2(width, height), floorColor, WorldSortingLayers.Floor, -500, false);
        EnsureRect(backProps, "BackDepthProp", new Vector2(-3.5f, 2.2f), new Vector2(2.2f, 1.1f), backPropColor, WorldSortingLayers.PropsBack, -80, true);
        EnsureRect(dynamicProps, "DynamicDepthProp", new Vector2(2f, 0.4f), new Vector2(1.2f, 1.8f), new Color(0.36f, 0.3f, 0.22f, 1f), WorldSortingLayers.Characters, 0, true);
        EnsureRect(frontProps, "FrontDepthProp", new Vector2(3.8f, -2.4f), new Vector2(2.4f, 1.1f), frontPropColor, WorldSortingLayers.PropsFront, 120, true);
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static void EnsureWall(Transform parent, string wallName, Vector2 position, Vector2 size)
    {
        Transform wall = GetOrCreateChild(parent, wallName);
        EnsureBox(wall.gameObject, position, size, false);
    }

    private static void EnsureBox(GameObject target, Vector2 localPosition, Vector2 size, bool isTrigger)
    {
        target.transform.localPosition = localPosition;
        BoxCollider2D box = target.GetComponent<BoxCollider2D>();
        if (box == null)
            box = target.AddComponent<BoxCollider2D>();

        box.size = size;
        box.isTrigger = isTrigger;
    }

    private static void EnsureRect(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, string sortingLayer, int sortingOrder, bool ySorted)
    {
        Transform rect = GetOrCreateChild(parent, objectName);
        rect.localPosition = position;
        rect.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = rect.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = rect.gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        WorldSortingUtility.ApplySorting(renderer, sortingLayer, sortingOrder);

        if (!ySorted)
            return;

        YSortByPosition ySort = rect.GetComponent<YSortByPosition>();
        if (ySort == null)
            ySort = rect.gameObject.AddComponent<YSortByPosition>();

        ySort.sortingLayerName = sortingLayer;
        ySort.baseOrder = sortingOrder;
        ySort.staticMode = true;
        ySort.includeChildRenderers = false;
        ySort.ApplySort();
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "RuntimePrototypeWhite",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "RuntimePrototypeWhiteSprite";
        return whiteSprite;
    }
}
