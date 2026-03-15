using UnityEngine;
using UnityEditor;

public class MemoryFloorSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Memory Players")]
    public static void SetupPlayers()
    {
        var player1 = GameObject.Find("MemoryPlayer");
        var player2 = GameObject.Find("MemoryPlayer_Copy");

        if (player1 != null)
        {
            var p1Script = player1.GetComponent<MemoryFloorPlayer>();
            p1Script.upKey = KeyCode.W;
            p1Script.downKey = KeyCode.S;
            p1Script.leftKey = KeyCode.A;
            p1Script.rightKey = KeyCode.D;
            player1.transform.position = new Vector3(-3, 2, -3);
            player1.name = "Player_WASD";
            EditorUtility.SetDirty(player1);
        }

        if (player2 != null)
        {
            var p2Script = player2.GetComponent<MemoryFloorPlayer>();
            p2Script.upKey = KeyCode.UpArrow;
            p2Script.downKey = KeyCode.DownArrow;
            p2Script.leftKey = KeyCode.LeftArrow;
            p2Script.rightKey = KeyCode.RightArrow;
            player2.transform.position = new Vector3(3, 2, -3);
            player2.name = "Player_Arrows";
            EditorUtility.SetDirty(player2);
        }

        // Create Player 3 (IJKL)
        if (player1 != null && GameObject.Find("Player_IJKL") == null)
        {
            var player3 = Instantiate(player1);
            player3.name = "Player_IJKL";
            var p3Script = player3.GetComponent<MemoryFloorPlayer>();
            p3Script.upKey = KeyCode.I;
            p3Script.downKey = KeyCode.K;
            p3Script.leftKey = KeyCode.J;
            p3Script.rightKey = KeyCode.L;
            player3.transform.position = new Vector3(-3, 2, 3);
            EditorUtility.SetDirty(player3);
        }

        // Create Player 4 (TFGH)
        if (player1 != null && GameObject.Find("Player_TFGH") == null)
        {
            var player4 = Instantiate(player1);
            player4.name = "Player_TFGH";
            var p4Script = player4.GetComponent<MemoryFloorPlayer>();
            p4Script.upKey = KeyCode.T;
            p4Script.downKey = KeyCode.G;
            p4Script.leftKey = KeyCode.F;
            p4Script.rightKey = KeyCode.H;
            player4.transform.position = new Vector3(3, 2, 3);
            EditorUtility.SetDirty(player4);
        }

        Debug.Log("4 Players setup complete!");
    }
}