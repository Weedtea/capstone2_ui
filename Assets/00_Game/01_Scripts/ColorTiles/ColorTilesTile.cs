using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(Collider))]
public class ColorTilesTile : MonoBehaviour
{
    public int OwnerID { get; private set; } = -1; // -1 = 중립

    private MeshRenderer meshRenderer;
    private Color neutralColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(Material neutralMat)
    {
        OwnerID = -1;
        if (meshRenderer != null)
        {
            if (neutralMat != null)
            {
                // 각 타일이 독립적인 material 인스턴스를 가지도록 .material 사용
                meshRenderer.material = neutralMat;
            }
            // 중립 색 저장
            neutralColor = meshRenderer.material.color;
        }
    }

    public void ChangeColor(int playerID, Color playerColor)
    {
        if (OwnerID == playerID) return;

        OwnerID = playerID;
        if (meshRenderer != null)
        {
            // .material은 인스턴스를 생성하므로 다른 타일에 영향 없음
            meshRenderer.material.color = playerColor;
        }
    }

    public void ResetTile()
    {
        OwnerID = -1;
        if (meshRenderer != null)
            meshRenderer.material.color = neutralColor;
    }
}
