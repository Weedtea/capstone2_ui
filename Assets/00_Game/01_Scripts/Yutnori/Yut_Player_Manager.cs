using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 말(Piece)의 상태 데이터를 관리하는 중앙 컴포넌트
/// HP 시스템 + 업기 + 말 2개 체계 지원
/// </summary>
[RequireComponent(typeof(YutPlayerMove))]
[RequireComponent(typeof(YutWayPointColorChange))]
public class Yut_Player_Manager : MonoBehaviour
{
    [Header("턴 상태")]
    public bool isPlayerTurn = false;
    public bool isThrowed = false;

    [Header("이동")]
    public int currentMoveCount = 0;
    public List<int> moveCountList = new List<int>();

    [Header("선택")]
    public bool isSelected = false;

    [Header("HP")]
    public int maxHp = 100;
    public int currentHp;

    [Header("업기")]
    public List<Yut_Player_Manager> carriedPieces = new List<Yut_Player_Manager>();
    public Yut_Player_Manager carriedBy = null;

    [Header("게임 상태")]
    public bool hasFinished = false;

    void Awake()
    {
        currentHp = maxHp;

        // 같은 팀의 첫째 말과 moveCountList 레퍼런스 공유
        var siblings = transform.parent.GetComponentsInChildren<Yut_Player_Manager>(true);
        if (siblings.Length > 0 && siblings[0] != this)
        {
            moveCountList = siblings[0].moveCountList;
        }
    }

    /// <summary>
    /// 같은 팀의 모든 말을 반환합니다.
    /// </summary>
    public Yut_Player_Manager[] GetTeamPieces()
    {
        return transform.parent.GetComponentsInChildren<Yut_Player_Manager>(true);
    }

    /// <summary>
    /// 아직 활성화된(도착하지 않고, 업혀있지 않은) 같은 팀 말 목록
    /// </summary>
    public List<Yut_Player_Manager> GetActivePieces()
    {
        var pieces = GetTeamPieces();
        var active = new List<Yut_Player_Manager>();
        foreach (var p in pieces)
        {
            if (!p.hasFinished && p.carriedBy == null && p.gameObject.activeInHierarchy)
                active.Add(p);
        }
        return active;
    }

    /// <summary>
    /// 팀 전체의 isThrowed를 설정합니다.
    /// </summary>
    public void SetTeamIsThrowed(bool value)
    {
        foreach (var p in GetTeamPieces()) p.isThrowed = value;
    }

    /// <summary>
    /// 팀 전체의 isPlayerTurn을 설정합니다.
    /// </summary>
    public void SetTeamIsPlayerTurn(bool value)
    {
        foreach (var p in GetTeamPieces()) p.isPlayerTurn = value;
    }

    /// <summary>
    /// 데미지를 받습니다. HP가 0 이하가 되면 시작점으로 리스폰합니다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        Debug.Log($"[HP] {gameObject.name} 데미지 {amount} 받음! (현재 HP: {currentHp}/{maxHp})");

        if (currentHp <= 0)
        {
            currentHp = maxHp;
            Debug.Log($"[HP] {gameObject.name} HP가 0 이하! 시작점으로 리스폰합니다. (HP 회복: {currentHp}/{maxHp})");

            // 업기 해제 후 리스폰
            UnstackAndReset();
        }
    }

    /// <summary>
    /// HP를 회복합니다. 최대 HP를 초과하지 않습니다.
    /// </summary>
    public void Heal(int amount)
    {
        int beforeHp = currentHp;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        Debug.Log($"[HP] {gameObject.name} 회복 {currentHp - beforeHp}! (현재 HP: {currentHp}/{maxHp})");
    }

    /// <summary>
    /// 업기 해제 후 시작점으로 리스폰 (업힌 말들도 모두 리스폰)
    /// </summary>
    public void UnstackAndReset()
    {
        // 업힌 말들도 리스폰
        foreach (var carried in carriedPieces)
        {
            carried.carriedBy = null;
            carried.currentHp = carried.maxHp;
            var carriedMove = carried.GetComponent<YutPlayerMove>();
            if (carriedMove != null)
            {
                carriedMove.ResetToStart();
            }
            Debug.Log($"[업기 해제] {carried.gameObject.name}도 시작점으로 리스폰!");
        }
        carriedPieces.Clear();

        // 본인 리스폰
        carriedBy = null;
        YutPlayerMove playerMove = GetComponent<YutPlayerMove>();
        if (playerMove != null)
        {
            playerMove.ResetToStart();
        }
    }
}
