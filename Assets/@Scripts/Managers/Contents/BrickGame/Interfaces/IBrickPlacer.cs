/// <summary>
/// 벽돌 배치 인터페이스
/// ObjectPlacement와의 결합도를 낮추기 위한 추상화
/// </summary>
public interface IBrickPlacer
{
    /// <summary>
    /// 지정된 수의 행을 생성
    /// </summary>
    /// <param name="rowCount">생성할 행 수</param>
    void PlaceMultipleRows(int rowCount);
}


