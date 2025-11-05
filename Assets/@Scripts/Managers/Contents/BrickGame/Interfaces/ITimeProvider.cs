/// <summary>
/// 시간 제공 인터페이스
/// Unity Time API 의존성을 제거하여 테스트 가능하게 만듦
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// 게임 시작 이후 경과 시간 (Time.time)
    /// </summary>
    float CurrentTime { get; }
    
    /// <summary>
    /// 이전 프레임과의 시간 차이 (Time.deltaTime)
    /// </summary>
    float DeltaTime { get; }
}


