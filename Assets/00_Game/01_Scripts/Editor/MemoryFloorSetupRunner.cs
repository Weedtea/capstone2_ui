using UnityEngine;
using UnityEditor;

public class MemoryFloorSetupRunner : MonoBehaviour
{
    [MenuItem("Tools/Run Setup Now")]
    public static void Run()
    {
        var managerItem = GameObject.Find("GameManager");
        if (managerItem != null)
        {
            var manager = managerItem.GetComponent<MemoryFloorManager>();
            var prefabPath = "Assets/00_Game/02_Prefabs/MemoryPlayer.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            manager.playerPrefab = prefab;
            EditorUtility.SetDirty(manager);
            Debug.Log("Linked player prefab to manager.");
        }
    }
}