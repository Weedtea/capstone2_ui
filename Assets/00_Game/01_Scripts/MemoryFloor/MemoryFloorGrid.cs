using System.Collections.Generic;
using UnityEngine;

public class MemoryFloorGrid : MonoBehaviour
{
    [Header("그리드 설정")]
    public GameObject tilePrefab;
    public Material safeZoneMaterial;
    public Material endZoneMaterial;
    public int width = 5;
    public int baseLength = 5;
    public float tileSize = 2f;
    public float spacing = 0.1f;

    private List<MemoryFloorTile> allTiles = new List<MemoryFloorTile>();
    private List<MemoryFloorTile> safeTiles = new List<MemoryFloorTile>();
    
    private GameObject startZone;
    private GameObject endZone;
    private GameObject[] invisibleWalls = new GameObject[4];
    
    public int CurrentLength { get; private set; }

    public void GenerateGrid(int round)
    {
        // 기존 타일 및 장판 제거
        foreach (var tile in allTiles)
        {
            if (tile != null) Destroy(tile.gameObject);
        }
        allTiles.Clear();

        if (startZone != null) Destroy(startZone);
        if (endZone != null) Destroy(endZone);
        for (int i = 0; i < 4; i++)
        {
            if (invisibleWalls[i] != null) Destroy(invisibleWalls[i]);
        }

        // 라운드당 길이 2 증가
        CurrentLength = baseLength + (round - 1) * 2;

        float totalWidth = width * (tileSize + spacing);
        float totalLength = CurrentLength * (tileSize + spacing);
        Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0, 0); // Z축 시작점은 0으로 가정

        // 시작 장판 (Start Zone)
        CreateZone(ref startZone, "StartZone", startPos + new Vector3(totalWidth / 2f - (tileSize + spacing) / 2f, 0, -(tileSize + spacing)), totalWidth, tileSize, safeZoneMaterial);
        
        // 메인 그리드 타일 생성
        for (int z = 0; z < CurrentLength; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 spawnPos = startPos + new Vector3(x * (tileSize + spacing), 0, z * (tileSize + spacing));
                GameObject newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
                newTile.name = $"Tile_{x}_{z}";
                
                MemoryFloorTile tileScript = newTile.GetComponent<MemoryFloorTile>();
                if (tileScript != null)
                {
                    allTiles.Add(tileScript);
                }
            }
        }

        // 도착 장판 (End Zone)
        CreateZone(ref endZone, "EndZone", startPos + new Vector3(totalWidth / 2f - (tileSize + spacing) / 2f, 0, CurrentLength * (tileSize + spacing)), totalWidth, tileSize, endZoneMaterial);
        // 도착 장판임을 알리기 위한 태그 (플레이어 생존 식별용)
        endZone.tag = "Finish";

        // 투명 벽 중심점 ও 크기 계산
        float centerX = startPos.x + (width - 1) * (tileSize + spacing) / 2f;
        float centerZ = (CurrentLength * (tileSize + spacing)) / 2f;
        float wallHeight = 10f;
        float wallThickness = 1f;
        float extentZ = totalLength + (tileSize + spacing) * 4f; // 넉넉하게 확장
        float extentX = totalWidth + (tileSize + spacing) * 2f;

        // 투명 벽 생성 (좌, 우, 뒤, 앞)
        CreateInvisibleWall(0, "LeftWall", new Vector3(startPos.x - tileSize / 2f - spacing, wallHeight / 2f, centerZ), new Vector3(wallThickness, wallHeight, extentZ));
        CreateInvisibleWall(1, "RightWall", new Vector3(startPos.x + (width - 1) * (tileSize + spacing) + tileSize / 2f + spacing, wallHeight / 2f, centerZ), new Vector3(wallThickness, wallHeight, extentZ));
        CreateInvisibleWall(2, "BackWall", new Vector3(centerX, wallHeight / 2f, -(tileSize + spacing) * 2f), new Vector3(extentX, wallHeight, wallThickness));
        CreateInvisibleWall(3, "FrontWall", new Vector3(centerX, wallHeight / 2f, CurrentLength * (tileSize + spacing) + (tileSize + spacing) * 2f), new Vector3(extentX, wallHeight, wallThickness));
    }

    private void CreateInvisibleWall(int index, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.parent = transform;
        
        // 투명하게 만들기 위해 MeshRenderer 제거
        MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Destroy(renderer);
        }
        
        invisibleWalls[index] = wall;
    }

    private void CreateZone(ref GameObject zoneObj, string name, Vector3 position, float widthScale, float lengthScale, Material customMaterial)
    {
        zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zoneObj.name = name;
        zoneObj.transform.position = position;
        zoneObj.transform.localScale = new Vector3(widthScale, 0.5f, lengthScale);
        zoneObj.transform.parent = transform;
        if (customMaterial != null)
        {
            zoneObj.GetComponent<MeshRenderer>().material = customMaterial;
        }
    }

    public void ResetGrid()
    {
        safeTiles.Clear();
        foreach (var tile in allTiles)
        {
            tile.ResetTile();
        }
    }

    public void SelectSafePath()
    {
        safeTiles.Clear();

        // Start at z = 0, random x
        int currentX = Random.Range(0, width);
        int currentZ = 0;

        while (currentZ < CurrentLength)
        {
            MemoryFloorTile tile = GetTileAt(currentX, currentZ);
            if (tile != null && !safeTiles.Contains(tile))
            {
                tile.SetSafe();
                safeTiles.Add(tile);
            }

            if (currentZ == CurrentLength - 1)
            {
                break; // 끝 도달
            }

            // Move: 0=Forward, 1=Left, 2=Right
            int dir = Random.Range(0, 3);
            if (dir == 0) 
            {
                currentZ++; // 직진 우선
            }
            else if (dir == 1 && currentX > 0) 
            {
                currentX--; // 왼쪽
            }
            else if (dir == 2 && currentX < width - 1) 
            {
                currentX++; // 오른쪽
            }
            else
            {
                currentZ++; // 벽에 막히면 무조건 직진
            }
        }
    }

    private MemoryFloorTile GetTileAt(int x, int z)
    {
        int index = z * width + x; // Z 기준으로 loop 돌았으므로 인덱스 계산 주의 (x먼저 돌았음)
        if (index >= 0 && index < allTiles.Count)
        {
            return allTiles[index];
        }
        return null;
    }

    public void ShowSafeTiles(bool show)
    {
        foreach (var tile in safeTiles)
        {
            tile.ShowGlow(show);
        }
    }

    public void DropWrongTiles()
    {
        foreach (var tile in allTiles)
        {
            if (!tile.isSafe)
            {
                tile.Drop();
            }
        }
    }
    
    public Vector3 GetStartZonePosition()
    {
        if (startZone != null) return startZone.transform.position;
        return Vector3.zero;
    }
}
