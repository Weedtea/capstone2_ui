using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    [Header("Player Info UI (Top-Left)")]
    [SerializeField] private List<PlayerInfoSlot> playerSlots; // 4 Players
    
    [Header("Inventory UI (Bottom-Center)")]
    [SerializeField] private List<Image> inventorySlots; // 5 Slots
    
    [Header("Turn Timer (Top-Right)")]
    [SerializeField] private TextMeshProUGUI turnTimerText;
    
    [Header("Game Status UI")]
    [SerializeField] private TextMeshProUGUI turnInfoText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject resultPanel;
    
    public static HUDManager Instance { get; private set; }

    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 1-3. Update Player Info (Color, HP, Rank)
    public void UpdatePlayerInfo(int playerIndex, string name, Color color, int hp, int rank)
    {
        if (playerIndex >= 0 && playerIndex < playerSlots.Count)
        {
            playerSlots[playerIndex].nameText.text = name;
            playerSlots[playerIndex].colorIndicator.color = color;
            playerSlots[playerIndex].hpText.text = $"HP: {hp}";
            playerSlots[playerIndex].rankText.text = rank switch {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{rank}th"
            };
        }
    }

    // 5. Update Inventory Slots
    public void UpdateInventory(List<Sprite> itemSprites)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < itemSprites.Count && itemSprites[i] != null)
            {
                inventorySlots[i].sprite = itemSprites[i];
                inventorySlots[i].color = Color.white;
            }
            else
            {
                inventorySlots[i].sprite = null;
                inventorySlots[i].color = new Color(1, 1, 1, 0.2f); // Empty slot appearance
            }
        }
    }

    // 6. Start Turn Timer (15 seconds)
    public void StartTurnTimer(float timeLimit = 15f)
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine(timeLimit));
    }

    public void StopTurnTimer()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        if (turnTimerText != null) turnTimerText.text = "";
    }

    private IEnumerator TimerRoutine(float timeLimit)
    {
        float timeLeft = timeLimit;
        while (timeLeft > 0)
        {
            if (turnTimerText != null)
            {
                turnTimerText.text = Mathf.CeilToInt(timeLeft).ToString() + "s";
                if (timeLeft <= 5f) turnTimerText.color = Color.red;
                else turnTimerText.color = Color.white;
            }
            
            yield return null;
            timeLeft -= Time.deltaTime;
        }

        if (turnTimerText != null) turnTimerText.text = "0s";
        
        // Auto-throw or turn over logic can be triggered here
    }

    public void UpdateTurn(string playerName)
    {
        if (turnInfoText != null)
            turnInfoText.text = $"{playerName}'s Turn";
        
        // When turn updates, automatically start the 15s timer if requested
        StartTurnTimer(15f);
    }

    public void ShowResult(string result)
    {
        if (resultText != null)
        {
            resultText.text = result;
            StopAllCoroutines();
            StartCoroutine(FlashResult());
        }
    }

    private IEnumerator FlashResult()
    {
        resultPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        resultPanel.SetActive(false);
    }


}

[System.Serializable]
public class PlayerInfoSlot
{
    public Image colorIndicator;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI rankText;
}
