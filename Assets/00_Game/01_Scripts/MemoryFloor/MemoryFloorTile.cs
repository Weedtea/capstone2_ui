using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class MemoryFloorTile : MonoBehaviour
{
    [Header("타일 상태")]
    public bool isSafe = false;

    [Header("머티리얼 설정")]
    public Material normalMaterial;
    public Material safeMaterial; // 발광하는 머티리얼

    private MeshRenderer meshRenderer;
    private Collider tileCollider;
    private Vector3 originalPosition;
    private bool isDropped = false;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        tileCollider = GetComponent<Collider>();
        originalPosition = transform.position;
    }

    public void SetSafe()
    {
        isSafe = true;
    }

    public void ShowGlow(bool show)
    {
        if (isSafe && show)
        {
            meshRenderer.material = safeMaterial;
        }
        else
        {
            meshRenderer.material = normalMaterial;
        }
    }

    // 플레이어가 밟았을 때 즉시 떨어짐 판정
    private void OnCollisionEnter(Collision collision)
    {
        if (MemoryFloorManager.Instance != null && MemoryFloorManager.Instance.currentState == MemoryFloorManager.GameState.MovePhase)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                if (!isSafe && !isDropped)
                {
                    Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // 밟자마자 강하게 아래로 꽂히도록 속도 부여
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, -20f, rb.linearVelocity.z);
                    }
                    Drop();
                }
            }
        }
    }

    public void Drop()
    {
        if (!isDropped && !isSafe)
        {
            isDropped = true;
            tileCollider.enabled = false;
            StartCoroutine(DropRoutine());
        }
    }

    private IEnumerator DropRoutine()
    {
        float dropSpeed = 10f;
        while (transform.position.y > originalPosition.y - 10f)
        {
            transform.position += Vector3.down * dropSpeed * Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void ResetTile()
    {
        isSafe = false;
        isDropped = false;
        gameObject.SetActive(true);
        tileCollider.enabled = true;
        transform.position = originalPosition;
        meshRenderer.material = normalMaterial;
    }
}
