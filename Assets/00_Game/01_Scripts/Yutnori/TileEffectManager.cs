using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 WayPoint에 힐/데미지 발판을 랜덤 배치하는 매니저
/// </summary>
public class TileEffectManager : MonoBehaviour
{
    [Header("발판 색상")]
    public Material healMaterial;   // 초록색
    public Material damageMaterial; // 빨강색
    public Material defaultMaterial; // 기본 검정색 (일반 발판)

    [Header("발판 설정")]
    [Tooltip("힐 발판 개수")]
    public int healTileCount = 10;
    [Tooltip("데미지 발판 개수")]
    public int damageTileCount = 10;

    void Start()
    {
        AssignRandomTiles();
    }

    void AssignRandomTiles()
    {
        // 시작/종료점을 제외한 모든 WayPoint 수집
        WayPoint[] allWayPoints = FindObjectsByType<WayPoint>(FindObjectsSortMode.None);
        List<WayPoint> availablePoints = new List<WayPoint>();

        foreach (var wp in allWayPoints)
        {
            if (!wp.isStartEndPoint)
            {
                availablePoints.Add(wp);
            }
        }

        Debug.Log($"[TileEffectManager] 사용 가능한 WayPoint: {availablePoints.Count}개");

        // 필요한 발판 수가 사용 가능한 WayPoint보다 많으면 조정
        int totalNeeded = healTileCount + damageTileCount;
        if (totalNeeded > availablePoints.Count)
        {
            Debug.LogWarning($"[TileEffectManager] 발판 수({totalNeeded})가 사용 가능한 WayPoint({availablePoints.Count})보다 많습니다. 조정합니다.");
            float ratio = (float)availablePoints.Count / totalNeeded;
            healTileCount = Mathf.FloorToInt(healTileCount * ratio);
            damageTileCount = Mathf.FloorToInt(damageTileCount * ratio);
        }

        // 셔플
        ShuffleList(availablePoints);

        int index = 0;

        // 힐 발판 배치
        for (int i = 0; i < healTileCount && index < availablePoints.Count; i++, index++)
        {
            WayPoint wp = availablePoints[index];
            wp.tileType = TileType.Heal;
            wp.tileEffectAmount = Random.Range(5, 16); // 5~15

            if (healMaterial != null)
            {
                Renderer renderer = wp.GetComponent<Renderer>();
                if (renderer != null) renderer.material = healMaterial;
                wp.originalMaterial = healMaterial;
            }

            Debug.Log($"[TileEffectManager] 힐 발판 배치: {wp.gameObject.name} - 회복량: {wp.tileEffectAmount}");
        }

        // 데미지 발판 배치
        for (int i = 0; i < damageTileCount && index < availablePoints.Count; i++, index++)
        {
            WayPoint wp = availablePoints[index];
            wp.tileType = TileType.Damage;
            wp.tileEffectAmount = Random.Range(1, 21); // 1~20

            if (damageMaterial != null)
            {
                Renderer renderer = wp.GetComponent<Renderer>();
                if (renderer != null) renderer.material = damageMaterial;
                wp.originalMaterial = damageMaterial;
            }

            Debug.Log($"[TileEffectManager] 데미지 발판 배치: {wp.gameObject.name} - 피해량: {wp.tileEffectAmount}");
        }

        // 나머지 일반 발판에 검정색 적용
        for (; index < availablePoints.Count; index++)
        {
            WayPoint wp = availablePoints[index];
            if (defaultMaterial != null)
            {
                Renderer renderer = wp.GetComponent<Renderer>();
                if (renderer != null) renderer.material = defaultMaterial;
                wp.originalMaterial = defaultMaterial;
            }
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
