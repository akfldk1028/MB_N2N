/// <summary>
/// 점수 표시 인터페이스
/// UI 컴포넌트와의 결합도를 낮추기 위한 추상화
/// </summary>
public interface IScoreDisplay
{
    /// <summary>
    /// 점수를 화면에 표시
    /// </summary>
    /// <param name="score">표시할 점수</param>
    void UpdateScore(int score);
}


