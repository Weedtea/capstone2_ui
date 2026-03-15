using UnityEngine;

/// <summary>
/// 윷 결과 판정 로직 (순수 계산만 담당)
/// 도/개/걸/윷/모/빽도/낙 판정
/// </summary>
public static class YutResultCalculator
{
    /// <summary>
    /// YutFrontBack 배열을 받아 윷 결과를 계산합니다.
    /// 반환값: -1=빽도, 0=낙, 1=도, 2=개, 3=걸, 4=윷, 5=모
    /// </summary>
    public static int Calculate(YutFrontBack[] yuts)
    {
        int backCount = 0;
        bool goBack = false;
        bool isFall = false;

        foreach (var yut in yuts)
        {
            if (yut.isfalling)
            {
                isFall = true;
                break;
            }
            if (!yut.isFront)
            {
                backCount++;
                if (yut.backYut)
                {
                    goBack = true;
                }
            }
        }

        if (isFall)
        {
            return 0; // 낙
        }

        int result = backCount switch
        {
            0 => 5, // 모
            1 => 1, // 도
            2 => 2, // 개
            3 => 3, // 걸
            4 => 4, // 윷
            _ => 0, // 낙
        };

        if (result == 1 && goBack)
        {
            result = -1; // 빽도
        }

        return result;
    }

    /// <summary>
    /// 결과값을 한글 이름으로 변환합니다.
    /// </summary>
    public static string GetResultName(int result)
    {
        return result switch
        {
            -1 => "빽도",
            0 => "낙",
            1 => "도",
            2 => "개",
            3 => "걸",
            4 => "윷",
            5 => "모",
            _ => "알 수 없음",
        };
    }

    /// <summary>
    /// 윷/모 결과인지 확인 (추가 던지기 여부 판단용)
    /// </summary>
    public static bool IsExtraThrow(int result)
    {
        return result == 4 || result == 5;
    }
}
