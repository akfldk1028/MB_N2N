using UnityEngine;

public class SimpleCubeGrid : MonoBehaviour
{
    [Header("Target Camera")]
    public Camera targetCamera;

    [Header("Grid Settings")]
    public GameObject cubePrefab;
    public float cubeScale = 0.5f;
    public int countX = 5;
    public int countY = 5;
    public float worldZPosition = 0f;

    void Start()
    {
        if (targetCamera == null)
        {
            Debug.LogError("Target Camera가 설정되지 않았습니다.");
            return;
        }
        CreateCubeGrid();
    }

    void CreateCubeGrid()
    {
        // 카메라의 월드 공간 뷰 경계 직접 계산
        float height = 2f * targetCamera.orthographicSize;
        float width = height * targetCamera.aspect;
        
        // 카메라 위치 기준 경계 계산
        Vector3 cameraPos = targetCamera.transform.position;
        float left = cameraPos.x - width / 2f;
        float right = cameraPos.x + width / 2f;
        float bottom = cameraPos.y - height / 2f;
        float top = cameraPos.y + height / 2f;
        
        // 디버그 정보
        Debug.Log($"카메라 뷰 경계: 좌({left}), 우({right}), 하({bottom}), 상({top})");
        
        // 그리드 간격 계산
        float stepX = (right - left) / (countX - 1);
        float stepY = (top - bottom) / (countY - 1);
        
        // 큐브 생성
        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                float posX = left + stepX * x;
                float posY = bottom + stepY * y;
                Vector3 position = new Vector3(posX, posY, worldZPosition);
                
                GameObject cube;
                if (cubePrefab != null)
                {
                    cube = Instantiate(cubePrefab, position, Quaternion.identity);
                }
                else
                {
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = position;
                }
                
                cube.transform.localScale = Vector3.one * cubeScale;
                cube.transform.parent = this.transform;
            }
        }
    }
}