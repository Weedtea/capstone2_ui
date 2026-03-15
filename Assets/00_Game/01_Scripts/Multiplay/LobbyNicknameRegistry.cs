using System;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// 로비에서 PlayerRef별 닉네임을 관리하는 정적 레지스트리.
/// 호스트가 ReliableData로 전송한 닉네임 맵을 클라이언트가 캐시한다.
/// </summary>
public static class LobbyNicknameRegistry
{
	private static readonly Dictionary<int, string> _nicknames = new Dictionary<int, string>(); // PlayerRef.PlayerId -> 닉네임
	public static readonly ReliableKey NICKNAME_KEY = ReliableKey.FromInts(1, 0, 0, 0);  // ReliableData 식별 키

	/// <summary>
	/// 플레이어 닉네임을 등록하거나 갱신한다.
	/// </summary>
	/// <param name="player">대상 PlayerRef</param>
	/// <param name="nickname">표시할 닉네임</param>
	public static void Register(PlayerRef player, string nickname)
	{
		_nicknames[player.PlayerId] = nickname;
	}

	/// <summary>
	/// 플레이어 닉네임을 제거한다.
	/// </summary>
	/// <param name="player">제거할 PlayerRef</param>
	public static void Remove(PlayerRef player)
	{
		_nicknames.Remove(player.PlayerId);
	}

	/// <summary>
	/// 등록된 닉네임을 반환한다.
	/// </summary>
	/// <param name="player">조회할 PlayerRef</param>
	/// <returns>닉네임. 미등록 시 null</returns>
	public static string GetNickname(PlayerRef player)
	{
		return _nicknames.TryGetValue(player.PlayerId, out string name) ? name : null;
	}

	/// <summary>
	/// 전체 캐시를 초기화한다. 새 세션 진입 시 호출.
	/// </summary>
	public static void Clear()
	{
		_nicknames.Clear();
	}

	/// <summary>
	/// 전체 닉네임 맵을 바이트 배열로 직렬화한다.
	/// 형식: "playerId:nickname\n" 반복
	/// </summary>
	/// <returns>직렬화된 바이트 배열</returns>
	public static byte[] SerializeAll()
	{
		var sb = new StringBuilder();
		foreach (var kvp in _nicknames)
			sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append('\n');
		return Encoding.UTF8.GetBytes(sb.ToString());
	}

	/// <summary>
	/// 바이트 배열에서 닉네임 맵을 복원한다. 기존 캐시를 덮어쓴다.
	/// </summary>
	/// <param name="data">수신된 바이트 데이터</param>
	public static void DeserializeAll(ArraySegment<byte> data)
	{
		_nicknames.Clear();
		string text = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
		string[] lines = text.Split('\n');
		foreach (string line in lines)
		{
			if (string.IsNullOrEmpty(line))
				continue;
			int sep = line.IndexOf(':');
			if (sep <= 0)
				continue;
			if (int.TryParse(line.Substring(0, sep), out int id))
				_nicknames[id] = line.Substring(sep + 1);
		}
	}
}
