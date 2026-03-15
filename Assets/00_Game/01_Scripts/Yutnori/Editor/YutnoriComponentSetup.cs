using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 리팩토링 후 누락된 컴포넌트를 자동으로 추가하는 에디터 유틸리티
/// 메뉴: Tools > Yutnori > Setup Refactored Components
/// </summary>
public class YutnoriComponentSetup : EditorWindow
{
    [MenuItem("Tools/Yutnori/Setup Refactored Components")]
    static void SetupComponents()
    {
        int addedCount = 0;

        // 1. 모든 Yut_Player_Manager가 있는 오브젝트에 새 컴포넌트 추가
        Yut_Player_Manager[] allPlayers = Object.FindObjectsByType<Yut_Player_Manager>(FindObjectsSortMode.None);
        foreach (var playerManager in allPlayers)
        {
            GameObject go = playerManager.gameObject;

            if (go.GetComponent<YutPieceSelector>() == null)
            {
                go.AddComponent<YutPieceSelector>();
                addedCount++;
                Debug.Log($"[Setup] {go.name}에 YutPieceSelector 추가됨");
            }

            if (go.GetComponent<YutCatchAndStack>() == null)
            {
                go.AddComponent<YutCatchAndStack>();
                addedCount++;
                Debug.Log($"[Setup] {go.name}에 YutCatchAndStack 추가됨");
            }

            if (go.GetComponent<YutTurnReset>() == null)
            {
                go.AddComponent<YutTurnReset>();
                addedCount++;
                Debug.Log($"[Setup] {go.name}에 YutTurnReset 추가됨");
            }
        }

        // 2. 모든 CountYut가 있는 오브젝트에 YutResultHandler 추가
        CountYut[] allCountYuts = Object.FindObjectsByType<CountYut>(FindObjectsSortMode.None);
        foreach (var countYut in allCountYuts)
        {
            GameObject go = countYut.gameObject;

            if (go.GetComponent<YutResultHandler>() == null)
            {
                YutResultHandler handler = go.AddComponent<YutResultHandler>();

                // YutGameTurn에서 p1, p2 참조를 가져와서 자동 할당 시도
                YutGameTurn gameTurn = Object.FindAnyObjectByType<YutGameTurn>();
                if (gameTurn != null)
                {
                    // SerializedObject를 사용하여 p1, p2 필드 복사
                    SerializedObject turnSO = new SerializedObject(gameTurn);
                    SerializedObject handlerSO = new SerializedObject(handler);

                    SerializedProperty turnP1 = turnSO.FindProperty("p1");
                    SerializedProperty turnP2 = turnSO.FindProperty("p2");
                    SerializedProperty handlerP1 = handlerSO.FindProperty("p1");
                    SerializedProperty handlerP2 = handlerSO.FindProperty("p2");

                    if (turnP1 != null && handlerP1 != null)
                    {
                        handlerP1.objectReferenceValue = turnP1.objectReferenceValue;
                    }
                    if (turnP2 != null && handlerP2 != null)
                    {
                        handlerP2.objectReferenceValue = turnP2.objectReferenceValue;
                    }

                    handlerSO.ApplyModifiedProperties();
                }

                addedCount++;
                Debug.Log($"[Setup] {go.name}에 YutResultHandler 추가됨 (p1, p2 자동 할당 시도)");
            }
        }

        if (addedCount > 0)
        {
            // 씬 변경 사항을 저장할 수 있도록 dirty 처리
            foreach (var pm in allPlayers)
            {
                EditorUtility.SetDirty(pm.gameObject);
            }
            foreach (var cy in allCountYuts)
            {
                EditorUtility.SetDirty(cy.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Setup] 완료! 총 {addedCount}개의 컴포넌트가 추가되었습니다. 씬을 저장하세요.");
        }
        else
        {
            Debug.Log("[Setup] 모든 컴포넌트가 이미 추가되어 있습니다.");
        }
    }
}
