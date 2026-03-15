using System.Collections.Generic;
using UnityEngine;

public class ColorTilesGrid : MonoBehaviour
{
    [Header("그리드 설정")]
    public GameObject tilePrefab;
    public int width = 10;
    public int length = 10;
    public float tileSize = 2f;
    public float spacing = 0.1f;
    public Material neutralMaterial;

    private List<ColorTilesTile> allTiles = new List<ColorTilesTile>();
    private GameObject[] invisibleWalls = new GameObject[4];

    public void GenerateGrid()
    {
        float totalWidth = width * (tileSize + spacing);
        float totalLength = length * (tileSize + spacing);
        Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0, totalLength / 2f);

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 spawnPos = startPos + new Vector3(x * (tileSize + spacing), 0, z * (tileSize + spacing));
                GameObject newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
                newTile.name = $"ColorTile_{x}_{z}";
                
                ColorTilesTile tileScript = newTile.GetComponent<ColorTilesTile>();
                if (tileScript != null)
                {
                    tileScript.Initialize(neutralMaterial);
                    allTiles.Add(tileScript);
                }
            }
        }

        CreateBoundaries(startPos, totalWidth, totalLength);
    }

    private void CreateBoundaries(Vector3 startPos, float totalWidth, float totalLength)
    {
        float centerX = startPos.x + totalWidth / 2f - (tileSize + spacing) / 2f;
        float centerZ = startPos.z + totalLength / 2f - (tileSize + spacing) / 2f;
        float wallHeight = 10f;
        float wallThickness = 1f;

        CreateInvisibleWall(0, "LeftWall", new Vector3(startPos.x - tileSize / 2f - spacing, wallHeight / 2f, centerZ), new Vector3(wallThickness, wallHeight, totalLength + 4f));
        CreateInvisibleWall(1, "RightWall", new Vector3(startPos.x + totalWidth, wallHeight / 2f, centerZ), new Vector3(wallThickness, wallHeight, totalLength + 4f));
        CreateInvisibleWall(2, "BackWall", new Vector3(centerX, wallHeight / 2f, startPos.z - tileSize / 2f - spacing), new Vector3(totalWidth + 4f, wallHeight, wallThickness));
        CreateInvisibleWall(3, "FrontWall", new Vector3(centerX, wallHeight / 2f, startPos.z + totalLength), new Vector3(totalWidth + 4f, wallHeight, wallThickness));
    }

    private void CreateInvisibleWall(int index, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.parent = transform;
        
        MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null) Destroy(renderer);
        
        invisibleWalls[index] = wall;
    }

    public List<ColorTilesTile> GetAllTiles()
    {
        return allTiles;
    }

    public Vector3[] GetSpawnPoints()
    {
        float totalWidth = width * (tileSize + spacing);
        float totalLength = length * (tileSize + spacing);
        Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0, totalLength / 2f);

        return new Vector3[] {
            startPos + new Vector3(0, 1.5f, 0), // BL
            startPos + new Vector3((width - 1) * (tileSize + spacing), 1.5f, (length - 1) * (tileSize + spacing)), // TR
            startPos + new Vector3((width - 1) * (tileSize + spacing), 1.5f, 0), // BR
            startPos + new Vector3(0, 1.5f, (length - 1) * (tileSize + spacing)) // TL
        };
    }
}
