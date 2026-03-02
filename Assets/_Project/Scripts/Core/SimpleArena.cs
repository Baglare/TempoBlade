using UnityEngine;

public class SimpleArena : MonoBehaviour
{
    public float width = 20f;
    public float height = 12f;
    public float wallThickness = 1f;
    public bool generateOnStart = true;

    [Header("Visuals")]
    public Sprite wallSprite;
    public Color wallColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateWalls();
        }
    }

    [ContextMenu("Generate Walls")]
    public void GenerateWalls()
    {
        // Clear existing walls (children)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        // Create 4 Walls
        // Top
        CreateWall("TopWall", new Vector3(0, height / 2 + wallThickness / 2, 0), new Vector3(width + wallThickness * 2, wallThickness, 1));
        // Bottom
        CreateWall("BottomWall", new Vector3(0, -height / 2 - wallThickness / 2, 0), new Vector3(width + wallThickness * 2, wallThickness, 1));
        // Left
        CreateWall("LeftWall", new Vector3(-width / 2 - wallThickness / 2, 0, 0), new Vector3(wallThickness, height, 1));
        // Right
        CreateWall("RightWall", new Vector3(width / 2 + wallThickness / 2, 0, 0), new Vector3(wallThickness, height, 1));

        // Create Camera Confiner Bounds (Cinemachine icin)
        CreateCameraBounds();
    }

    private void CreateCameraBounds()
    {
        GameObject boundsObj = new GameObject("CameraBounds");
        boundsObj.transform.parent = transform;
        boundsObj.transform.localPosition = Vector3.zero;

        BoxCollider2D bc = boundsObj.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(width, height);
        bc.isTrigger = true; // Oyuncu icinden gecebilsin, sadece kamera siniri

        // Kamera on planda/arka planda objelere carpmasin diye layer
        boundsObj.layer = LayerMask.NameToLayer("Ignore Raycast"); 
    }

    private void CreateWall(string name, Vector3 pos, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.position = pos;
        wall.transform.localScale = Vector3.one; // Görsellerin bozulmaması için scale 1'de kalmalı.

        // Layer: Environment (Cok onemli, mermiler ve oyuncu carpsin)
        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer == -1) envLayer = 0; // Default
        wall.layer = envLayer;

        // Collider boyutlarını scale yerine kendi üzerinden ayarlıyoruz
        BoxCollider2D bc = wall.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(size.x, size.y);
        
        // Görsel Bölüm (Visual)
        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        if (wallSprite != null)
        {
            sr.sprite = wallSprite;
            sr.color = wallColor;
            sr.drawMode = SpriteDrawMode.Tiled; // Görseli uzatmak yerine döşer (Tile)
            sr.size = new Vector2(size.x, size.y);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 1));
    }
}
