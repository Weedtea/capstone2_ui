using UnityEngine;

public enum TileType
{
    None,
    Heal,
    Damage
}

public class WayPoint : MonoBehaviour
{
    [SerializeField]private GameObject nextPoint1;
    [SerializeField]private GameObject shortcutPoint1;
    [SerializeField]private GameObject backPoint1;
    [SerializeField]private GameObject shortcutBackPoint1;
    public WayPoint nextPoint;
    public WayPoint shortcutPoint;
    public WayPoint backPoint;
    public WayPoint shortcutBackPoint;
    public bool isStartEndPoint = false;

    [Header("발판 효과")]
    public TileType tileType = TileType.None;
    public int tileEffectAmount = 0;

    [Header("원래 색상")]
    [HideInInspector] public Material originalMaterial;

    /// <summary>
    /// 원래 Material로 복원합니다.
    /// </summary>
    public void RestoreOriginalMaterial()
    {
        if (originalMaterial != null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material = originalMaterial;
        }
    }

    void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) originalMaterial = renderer.sharedMaterial;

        if(nextPoint1 != null)
            nextPoint = nextPoint1.GetComponent<WayPoint>();
        if(shortcutPoint1 != null)
            shortcutPoint = shortcutPoint1.GetComponent<WayPoint>();
        if(backPoint1 != null)
            backPoint = backPoint1.GetComponent<WayPoint>();
        if(shortcutBackPoint1 != null)
            shortcutBackPoint = shortcutBackPoint1.GetComponent<WayPoint>();
    }
}
