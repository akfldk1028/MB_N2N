// 한 줄만 위치 계산
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SpawnableObjectType
{
    Brick,
    BonusBall,
    Star
}

public class ObjectPlacement : MonoBehaviour
{
    [Header("경계 참조")]
    [SerializeField] private Transform leftBorder;
    [SerializeField] private Transform rightBorder;
    [SerializeField] private Transform topBorder;
    
    [Header("오브젝트 프리팹")]
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private GameObject bonusBallPrefab;
    [SerializeField] private GameObject starPrefab;
    
    [Header("레이아웃 설정")]
    [SerializeField] private int maxBricksPerRow = 7;
    [SerializeField] private float topOffset = 0f; // 첫 행이 최종적으로 위치할 Y 오프셋
    [SerializeField] private float moveDownDistance = 0.85f;
    [SerializeField] private int numberOfRowsToSpawn = 3;
    [SerializeField] private float objectScaleMultiplier = 0.7f;
    [SerializeField] [Range(0.2f, 0.8f)] private float fillRateMin = 0.4f; // 최소 채우기 비율
    [SerializeField] [Range(0.3f, 1.0f)] private float fillRateMax = 0.7f; // 최대 채우기 비율
    
    [Header("애니메이션 설정")]
    [SerializeField] private float movingDownStep = 0.04f;
    
    private float initialSpawnYOffset = 0f; // 생성 시 Y 오프셋 (topBorder 위치에서 생성)
    private Dictionary<GameObject, bool> activeObjectData = new Dictionary<GameObject, bool>();
    private const float BottomBoundary = -2.3f;
    
    private void Awake()
    {
        ValidatePrefabs();
    }
    
    private void ValidatePrefabs()
    {
        if (brickPrefab == null) Debug.LogError("Brick Prefab이 할당되지 않았습니다!");
        if (bonusBallPrefab == null) Debug.LogError("Bonus Ball Prefab이 할당되지 않았습니다!");
        if (starPrefab == null) Debug.LogError("Star Prefab이 할당되지 않았습니다!");
    }
    
    private GameObject GetPrefabForType(SpawnableObjectType type)
    {
        switch (type)
        {
            case SpawnableObjectType.Brick:     return brickPrefab;
            case SpawnableObjectType.BonusBall: return bonusBallPrefab;
            case SpawnableObjectType.Star:      return starPrefab;
            default:
                Debug.LogError($"알 수 없는 오브젝트 타입: {type}");
                return brickPrefab;
        }
    }
    
    private struct PotentialSpawnInfo
    {
        public Vector3 SpawnPosition; 
        public float TargetY;
    }


  
 
    
    // 여러 행의 위치 계산 - 행 수를 매개변수로 받음
private List<PotentialSpawnInfo> CalculatePotentialSpawnPositions(int rowCount)
{
    List<PotentialSpawnInfo> potentialSpawns = new List<PotentialSpawnInfo>();

    float baseSpawnY = topBorder.position.y + initialSpawnYOffset;
    float baseTargetY = topBorder.position.y + topOffset;
    float totalWidth = rightBorder.position.x - leftBorder.position.x;
    
    // 블록 크기 계산
    Vector3 objectScale = CalculateObjectScale(totalWidth);
    float actualObjectWidth = objectScale.x;
    float actualObjectHeight = objectScale.y;
    
    // 이 부분이 중요: moveDownDistance 대신 actualObjectHeight 사용
    // 행 간 간격을 제거하려면 정확히 블록 높이만큼 이동해야 함
    float rowSpacing = actualObjectHeight; // 간격 없이 딱 붙이기 위함

    int finalColumnCount = CalculateFinalColumnCount(totalWidth, actualObjectWidth);

    for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
    {   
        // 여기서 rowSpacing(actualObjectHeight)를 사용하여 정확히 행 간격 계산
        float targetY = baseTargetY - (rowIndex * rowSpacing);
        
        for (int colIndex = 0; colIndex < finalColumnCount; colIndex++)
        {
            float leftEdgeX = leftBorder.position.x + (colIndex * actualObjectWidth);
            float centerX = leftEdgeX + (actualObjectWidth / 2);
            Vector3 spawnPosition = new Vector3(centerX, baseSpawnY, 0);
            
            potentialSpawns.Add(new PotentialSpawnInfo { 
                SpawnPosition = spawnPosition, 
                TargetY = targetY 
            });
        }
    }
    
    return potentialSpawns;
}
        

    private Vector3 CalculateObjectScale(float totalWidth)
    {
        // 간격 없이 정확히 maxBricksPerRow 개의 블록이 들어갈 수 있도록 계산
        float baseBrickWidth = totalWidth / maxBricksPerRow;
        
        // 정확한 크기로 설정 (objectScaleMultiplier는 1.0에 가깝게 설정)
        // 블록 사이 간격이 없도록 하려면 objectScaleMultiplier를 1.0에 가깝게 설정
        return new Vector3(baseBrickWidth, baseBrickWidth, 1) * objectScaleMultiplier;
    }
        

    private int CalculateFinalColumnCount(float totalWidth, float actualObjectWidth)
    {
        if (actualObjectWidth <= 0)
        {
            Debug.LogWarning("계산된 오브젝트 너비가 0 이하입니다. 열 개수를 계산할 수 없습니다.");
            return 1;
        }
        
        // Floor 대신 Round 사용하여 더 정확한 블록 수 계산
        // 또는 간격 없이 정확히 맞추려면 Floor를 사용하는 것이 더 적합할 수 있음
        return Mathf.FloorToInt(totalWidth / actualObjectWidth);
    }
    private void ShuffleSpawnPositions(List<PotentialSpawnInfo> potentialSpawns)
    {
        System.Random rng = new System.Random();
        int n = potentialSpawns.Count;
        
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            PotentialSpawnInfo value = potentialSpawns[k];
            potentialSpawns[k] = potentialSpawns[n];
            potentialSpawns[n] = value;
        }
    }
    
    private void SpawnObjectsAtRandomPositions(List<PotentialSpawnInfo> potentialSpawns)
    {
        // 총 가능한 위치 중에서 실제로 생성할 오브젝트 수를 계산
        // 채우기 비율에 따라 자동으로 개수 결정
        float fillRate = Random.Range(fillRateMin, fillRateMax);
        int objectsToSpawnCount = Mathf.FloorToInt(potentialSpawns.Count * fillRate);
        
        // 최소 하나는 생성되도록
        objectsToSpawnCount = Mathf.Max(1, objectsToSpawnCount);
        
        for (int i = 0; i < objectsToSpawnCount && i < potentialSpawns.Count; i++)
        {
            PotentialSpawnInfo spawnInfo = potentialSpawns[i];
            SpawnableObjectType objectType = DetermineRandomObjectType();
            
            GameObject prefabToSpawn = GetPrefabForType(objectType);
            if (prefabToSpawn != null)
            {
                SpawnAndInitializeObject(prefabToSpawn, spawnInfo);
            }
        }
    }
    
    private SpawnableObjectType DetermineRandomObjectType()
    {
        int randomType = Random.Range(0, 20);
        
        if (randomType == 0) return SpawnableObjectType.BonusBall;
        if (randomType == 1) return SpawnableObjectType.Star;
        return SpawnableObjectType.Brick;
    }
    
    // 랜덤 빈칸을 포함하는 패턴 생성 메서드
    private List<PotentialSpawnInfo> GeneratePatternWithRandomGaps(List<PotentialSpawnInfo> allPositions)
    {
        List<PotentialSpawnInfo> patternPositions = new List<PotentialSpawnInfo>();
        
        // 기본 격자에서 랜덤 패턴 생성 - 여러 가지 패턴 유형 중 하나 선택
        int patternType = Random.Range(0, 5);
        
        switch (patternType)
        {
            case 0: // 체스보드 패턴
                CreateCheckerboardPattern(allPositions, patternPositions);
                break;
            case 1: // 랜덤 구멍 패턴
                CreateRandomHolesPattern(allPositions, patternPositions);
                break;
            case 2: // 지그재그 패턴
                CreateZigzagPattern(allPositions, patternPositions);
                break;
            case 3: // 군집 패턴 (작은 그룹들로 나누기)
                CreateClusterPattern(allPositions, patternPositions);
                break;
            case 4: // 완전 랜덤
            default:
                CreateFullRandomPattern(allPositions, patternPositions);
                break;
        }
        
        return patternPositions;
    }
    
    // 체스보드 패턴 (격자의 절반만 사용)
    private void CreateCheckerboardPattern(List<PotentialSpawnInfo> allPositions, List<PotentialSpawnInfo> patternPositions)
    {
        // 열과 행 개수 계산 (전체 위치 개수로부터 근사값 추정)
        int totalPositions = allPositions.Count;
        int columnsCount = Mathf.FloorToInt(Mathf.Sqrt(totalPositions / numberOfRowsToSpawn));
        int rowsCount = numberOfRowsToSpawn;
        
        for (int i = 0; i < allPositions.Count; i++)
        {
            // 위치 인덱스에서 행과 열 번호 계산
            int row = i / columnsCount;
            int col = i % columnsCount;
            
            // 체스보드 패턴: (row + col) % 2 == 0인 위치만 선택
            if ((row + col) % 2 == 0)
            {
                patternPositions.Add(allPositions[i]);
            }
        }
    }
    
    // 랜덤 구멍이 있는 패턴 (전체 격자에서 랜덤하게 일부 제외)
    private void CreateRandomHolesPattern(List<PotentialSpawnInfo> allPositions, List<PotentialSpawnInfo> patternPositions)
    {
        // 각 위치마다 일정 확률로 포함
        foreach (var position in allPositions)
        {
            // 60~80% 확률로 포함
            if (Random.value < Random.Range(0.6f, 0.8f))
            {
                patternPositions.Add(position);
            }
        }
    }
    
    // 지그재그 패턴
    private void CreateZigzagPattern(List<PotentialSpawnInfo> allPositions, List<PotentialSpawnInfo> patternPositions)
    {
        // 열과 행 개수 계산
        int totalPositions = allPositions.Count;
        int columnsCount = Mathf.FloorToInt(Mathf.Sqrt(totalPositions / numberOfRowsToSpawn));
        int rowsCount = numberOfRowsToSpawn;
        
        for (int i = 0; i < allPositions.Count; i++)
        {
            int row = i / columnsCount;
            int col = i % columnsCount;
            
            // 지그재그 패턴: 홀수 행에서는 홀수 열만, 짝수 행에서는 짝수 열만 선택
            if ((row % 2 == 0 && col % 2 == 0) || (row % 2 == 1 && col % 2 == 1))
            {
                patternPositions.Add(allPositions[i]);
            }
        }
    }
    
    // 군집 패턴 (작은 그룹들로 나눔)
    private void CreateClusterPattern(List<PotentialSpawnInfo> allPositions, List<PotentialSpawnInfo> patternPositions)
    {
        // 열과 행 개수 계산
        int totalPositions = allPositions.Count;
        int columnsCount = Mathf.FloorToInt(Mathf.Sqrt(totalPositions / numberOfRowsToSpawn));
        int rowsCount = numberOfRowsToSpawn;
        
        // 클러스터 크기 (2x2 또는 3x3)
        int clusterSize = Random.Range(0, 2) == 0 ? 2 : 3;
        
        for (int i = 0; i < allPositions.Count; i++)
        {
            int row = i / columnsCount;
            int col = i % columnsCount;
            
            // 클러스터 ID 계산 (어느 클러스터에 속하는지)
            int clusterRow = row / clusterSize;
            int clusterCol = col / clusterSize;
            
            // 각 클러스터마다 일정 확률로 포함 여부 결정
            int clusterID = clusterRow * 1000 + clusterCol; // 유니크한 클러스터 ID 생성
            Random.InitState(clusterID); // 같은 클러스터는 같은 결정을 하도록 시드 설정
            
            if (Random.value < 0.7f) // 70% 확률로 클러스터 포함
            {
                patternPositions.Add(allPositions[i]);
            }
        }
    }
    
    // 완전 랜덤 패턴
    private void CreateFullRandomPattern(List<PotentialSpawnInfo> allPositions, List<PotentialSpawnInfo> patternPositions)
    {
        // 각 위치마다 개별적으로 포함 여부 결정
        foreach (var position in allPositions)
        {
            if (Random.value < 0.5f) // 50% 확률로 포함
            {
                patternPositions.Add(position);
            }
        }
        
        // 너무 적게 선택되면 추가
        if (patternPositions.Count < allPositions.Count * 0.3f)
        {
            int additionalNeeded = Mathf.FloorToInt(allPositions.Count * 0.3f) - patternPositions.Count;
            List<PotentialSpawnInfo> remainingPositions = allPositions
                .Except(patternPositions)
                .ToList();
                
            // 셔플하고 필요한 만큼 추가
            ShuffleList(remainingPositions);
            for (int i = 0; i < additionalNeeded && i < remainingPositions.Count; i++)
            {
                patternPositions.Add(remainingPositions[i]);
            }
        }
    }
    
    // 리스트 셔플 헬퍼 메서드
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    private void SpawnAndInitializeObject(GameObject prefabToSpawn, PotentialSpawnInfo spawnInfo)
    {
        Vector3 spawnPosition = spawnInfo.SpawnPosition;
        float targetY = spawnInfo.TargetY;
        Vector3 objectScale = CalculateObjectScale(rightBorder.position.x - leftBorder.position.x);
        
        GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        newObject.transform.localScale = objectScale;
        
        SetupRigidbody(newObject);
        
        if (!activeObjectData.ContainsKey(newObject))
        {
            activeObjectData.Add(newObject, true);
        }
        
        StartCoroutine(MoveObjectToTargetY(newObject, targetY));
    }
    
    private void SetupRigidbody(GameObject obj)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = obj.AddComponent<Rigidbody2D>();
        
        rb.gravityScale = 0;
        rb.isKinematic = true;
    }
    
    private IEnumerator MoveObjectToTargetY(GameObject obj, float targetY)
    {
        if (obj == null) yield break;
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        while (obj != null && Mathf.Abs(obj.transform.position.y - targetY) > 0.01f)
        {
            Vector3 pos = obj.transform.position;
            float step = movingDownStep * Time.fixedDeltaTime * 50;
            pos.y = Mathf.MoveTowards(pos.y, targetY, step);

            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }

        if (obj != null)
        {
            Vector3 finalPos = obj.transform.position; 
            finalPos.y = targetY;
            rb.MovePosition(finalPos);

            if (activeObjectData.ContainsKey(obj))
            {
                activeObjectData[obj] = false;
            }
        }
    }
    
    private void MoveDownAllObjects()
    {
        List<GameObject> keysToRemove = new List<GameObject>();
        var currentActiveObjects = activeObjectData.ToList();
        
        // 전체 너비를 기준으로 블록 크기 계산
        float totalWidth = rightBorder.position.x - leftBorder.position.x;
        Vector3 objectScale = CalculateObjectScale(totalWidth);
        float actualObjectHeight = objectScale.y;
        
        // 간격 없이 정확히 블록 높이만큼 이동
        float exactMoveDistance = actualObjectHeight;

        foreach (KeyValuePair<GameObject, bool> pair in currentActiveObjects)
        {
            GameObject obj = pair.Key;
            bool isFirstRow = pair.Value;

            if (obj == null)
            {
                keysToRemove.Add(obj);
                continue;
            }

            if (isFirstRow)
            {
                continue;
            }

            // moveDownDistance 대신 정확한 블록 높이로 설정
            float newY = obj.transform.position.y - exactMoveDistance;

            if (newY < BottomBoundary)
            {
                Destroy(obj);
                keysToRemove.Add(obj);
            }
            else
            {
                StartCoroutine(MoveDown(obj, newY));
            }
        }

        foreach (GameObject key in keysToRemove)
        {
            activeObjectData.Remove(key);
        }
    }
    private IEnumerator MoveDown(GameObject obj, float targetY)
    {
        if (obj == null) yield break;
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;
        
        while (obj != null && obj.transform.position.y > targetY)
        {
            Vector3 pos = obj.transform.position;
            pos.y -= movingDownStep;
            
            if (pos.y <= targetY)
                pos.y = targetY;
                
            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }
    }
    // 지정된 수의 행을 생성하는 메서드
    public void PlaceMultipleRows(int rowCount)
    {
        MoveDownAllObjects();
        
        // 지정된 행 수로 위치 계산
        List<PotentialSpawnInfo> potentialSpawns = CalculatePotentialSpawnPositions(rowCount);
        
        // 두 가지 방식으로 랜덤 패턴 생성
        if (Random.value < 0.5f)
        {
            // 방식 1: 패턴 기반 생성 (체스보드, 지그재그 등)
            List<PotentialSpawnInfo> patternPositions = GeneratePatternWithRandomGaps(potentialSpawns);
            // 생성된 패턴 내에서 순서는 섞음
            ShuffleSpawnPositions(patternPositions);
            SpawnObjectsAtRandomPositions(patternPositions);
        }
        else
        {
            // 방식 2: 모든 위치 중 일부만 랜덤 선택
            ShuffleSpawnPositions(potentialSpawns);
            SpawnObjectsAtRandomPositions(potentialSpawns);
        }
    }


private void OnDrawGizmos()
{
    if (leftBorder && rightBorder && topBorder)
    {
        float leftBoundary = leftBorder.position.x;
        float rightBoundary = rightBorder.position.x;
        float totalWidth = rightBoundary - leftBoundary;
        
        Vector3 objectScale = CalculateObjectScale(totalWidth);
        float actualObjectHeight = objectScale.y;

        // 초기 스폰 영역 표시
        Gizmos.color = Color.cyan;
        float baseSpawnY = topBorder.position.y + initialSpawnYOffset;
        Gizmos.DrawLine(new Vector3(leftBoundary, baseSpawnY, 0), 
                       new Vector3(rightBoundary, baseSpawnY, 0));

        // 각 행의 목표 위치 표시 (actualObjectHeight 사용)
        Gizmos.color = Color.yellow;
        float baseTargetY = topBorder.position.y + topOffset;
        for (int i = 0; i < numberOfRowsToSpawn; i++)
        {
            // 정확한 블록 높이로 간격 계산
            float targetY = baseTargetY - (i * actualObjectHeight);
            Gizmos.DrawLine(new Vector3(leftBoundary, targetY, 0), 
                           new Vector3(rightBoundary, targetY, 0));
        }

        // 게임 오버 라인
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftBoundary, BottomBoundary, 0), 
                       new Vector3(rightBoundary, BottomBoundary, 0));
    }
}
}