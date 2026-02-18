using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임 저장 데이터 모델
/// PlayerPrefs + JSON 직렬화를 통한 진행 상태 영속화
/// </summary>
[Serializable]
public class BrickGameSaveData
{
    private const string PREF_SAVE_KEY = "BrickGame_SaveData";

    #region 저장 필드
    /// <summary>최고 점수</summary>
    public int HighScore;

    /// <summary>최대 도달 레벨</summary>
    public int MaxLevel;

    /// <summary>총 플레이 횟수</summary>
    public int TotalGamesPlayed;

    /// <summary>총 파괴한 벽돌 수</summary>
    public int TotalBricksDestroyed;

    /// <summary>총 승리 횟수</summary>
    public int TotalVictories;

    /// <summary>마지막 플레이 날짜 (ISO 8601)</summary>
    public string LastPlayDate;
    #endregion

    #region 저장/로드
    /// <summary>
    /// 현재 데이터를 PlayerPrefs에 JSON으로 저장
    /// </summary>
    public static void Save(BrickGameSaveData data)
    {
        if (data == null)
        {
            GameLogger.Warning("BrickGameSaveData", "저장할 데이터가 null입니다.");
            return;
        }

        try
        {
            string json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(PREF_SAVE_KEY, json);
            PlayerPrefs.Save();
            GameLogger.Info("BrickGameSaveData", $"저장 완료 (HighScore: {data.HighScore}, MaxLevel: {data.MaxLevel})");
        }
        catch (Exception e)
        {
            GameLogger.Error("BrickGameSaveData", $"저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// PlayerPrefs에서 JSON을 읽어 데이터 복원. 저장된 데이터가 없으면 기본값 반환.
    /// </summary>
    public static BrickGameSaveData Load()
    {
        string json = PlayerPrefs.GetString(PREF_SAVE_KEY, "");

        if (string.IsNullOrEmpty(json))
        {
            GameLogger.Info("BrickGameSaveData", "저장된 데이터 없음, 기본값 생성");
            return new BrickGameSaveData();
        }

        try
        {
            var data = JsonConvert.DeserializeObject<BrickGameSaveData>(json);
            GameLogger.Info("BrickGameSaveData", $"로드 완료 (HighScore: {data.HighScore}, MaxLevel: {data.MaxLevel})");
            return data;
        }
        catch (Exception e)
        {
            GameLogger.Error("BrickGameSaveData", $"로드 실패, 기본값 생성: {e.Message}");
            return new BrickGameSaveData();
        }
    }

    /// <summary>
    /// 저장된 데이터 삭제
    /// </summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PREF_SAVE_KEY);
        PlayerPrefs.Save();
        GameLogger.Info("BrickGameSaveData", "저장 데이터 초기화됨");
    }
    #endregion
}
