// using UnityEngine;
// using UnityEngine.UI;

// public class UILayoutManager : MonoBehaviour
// {
//     [Header("레이아웃 경계선")]
//     public bool showBorders = true;
//     public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
//     public float borderThickness = 2f;

//     [Header("화면 분할 비율")]
//     [Range(0.2f, 0.7f)]
//     public float topViewRatio = 0.4f;  // 상단 뷰 비율 (60%)

//     [Header("UI 패널")]
//     public RectTransform mainCanvas;
//     public RectTransform topPanel;     // 상단 게임 패널
//     public RectTransform bottomPanel;  // 하단 게임 패널
//     public RectTransform player2Panel; // 오른쪽 상단 패널
//     public RectTransform scorePanel;   // 왼쪽 하단 패널

//     // 경계선 요소
//     private Image horizDivider;    // 상/하단 분할선
//     private Image vertDivider;     // Player 2 패널 분할선
//     private Image scoreDivider;    // 점수 패널 분할선

//     private void Start()
//     {
//         SetupPanels();
//         if (showBorders) CreateBorders();
//     }

//     // UI 패널 설정
//     private void SetupPanels()
//     {
//         if (mainCanvas == null)
//         {
//             Debug.LogError("메인 캔버스가 할당되지 않았습니다.");
//             return;
//         }

//         // 캔버스 크기 가져오기
//         float canvasWidth = mainCanvas.rect.width;
//         float canvasHeight = mainCanvas.rect.height;

//         // 상단 패널 설정 (땅따먹기 게임)
//         if (topPanel != null)
//         {
//             topPanel.anchorMin = new Vector2(0, 1 - topViewRatio);
//             topPanel.anchorMax = new Vector2(1, 1);
//             topPanel.offsetMin = Vector2.zero;
//             topPanel.offsetMax = Vector2.zero;
//         }

//         // 하단 패널 설정 (벽돌깨기 게임)
//         if (bottomPanel != null)
//         {
//             bottomPanel.anchorMin = new Vector2(0, 0);
//             bottomPanel.anchorMax = new Vector2(1, 1 - topViewRatio);
//             bottomPanel.offsetMin = Vector2.zero;
//             bottomPanel.offsetMax = Vector2.zero;
//         }

//         // Player 2 패널 설정 (오른쪽 상단)
//         // if (player2Panel != null)
//         // {
//         //     player2Panel.anchorMin = new Vector2(0.8f, 0.85f);
//         //     player2Panel.anchorMax = new Vector2(1, 1);
//         //     player2Panel.offsetMin = Vector2.zero;
//         //     player2Panel.offsetMax = Vector2.zero;
//         // }
//         // Player 2 패널 설정 (오른쪽 상단)
//         if (player2Panel != null)
//         {
//             // Player2Panel이 상단 영역의 40%를 차지하도록 설정
//             player2Panel.anchorMin = new Vector2(0.8f, 1 - (0.4f * topViewRatio));
//             player2Panel.anchorMax = new Vector2(1, 1);
//             player2Panel.offsetMin = Vector2.zero;
//             player2Panel.offsetMax = Vector2.zero;
//         }
//         // 점수 패널 설정 (왼쪽 하단)
//         // 점수 패널 설정 (왼쪽 하단)
//         // 점수 패널 설정 (왼쪽 하단이 아닌, 하단 중앙에 위치)
// // 점수 패널 설정 (하단 중앙 stretch)
//         if (scorePanel != null)
//         {
//             // 하단에 길게 stretch되도록 설정
//             scorePanel.anchorMin = new Vector2(0.2f, 0);
//             scorePanel.anchorMax = new Vector2(0.8f, 0.15f);
//             scorePanel.offsetMin = Vector2.zero;
//             scorePanel.offsetMax = Vector2.zero;
//         }
//     }

//     // 경계선 생성
//     private void CreateBorders()
//     {
//         // 수평 분할선 (상단/하단 게임 분리)
//         CreateDivider("HorizontalDivider", new Vector2(0, 1 - topViewRatio), 
//                       new Vector2(1, 1 - topViewRatio), true);

//         // Player 2 패널 세로 분할선
//         CreateDivider("Player2VerticalDivider", new Vector2(0.8f, 1 - topViewRatio), 
//                       new Vector2(0.8f, 1), false);

//         // Player 2 패널 가로 분할선
//         // Player 2 패널 가로 분할선
//         CreateDivider("Player2HorizontalDivider", new Vector2(0.8f, 0.85f), 
//                       new Vector2(1, 0.85f), true);

//                 // 점수 패널 경계선 수정 (하단 중앙에 맞게)
//         CreateDivider("BottomHorizontalDivider", new Vector2(0, 0.1f), 
//                     new Vector2(1, 0.1f), true);
//             }

//     // 분할선 생성 헬퍼 메서드
//     private void CreateDivider(string name, Vector2 start, Vector2 end, bool isHorizontal)
//     {
//         GameObject divider = new GameObject(name);
//         divider.transform.SetParent(mainCanvas.transform, false);
        
//         RectTransform rectTransform = divider.AddComponent<RectTransform>();
        
//         // 시작점과 끝점 설정
//         rectTransform.anchorMin = start;
//         rectTransform.anchorMax = end;
//         rectTransform.sizeDelta = isHorizontal ? new Vector2(0, borderThickness) : new Vector2(borderThickness, 0);
        
//         // 이미지 컴포넌트 추가
//         Image image = divider.AddComponent<Image>();
//         image.color = borderColor;
//     }
// }

using UnityEngine;
using UnityEngine.UI;

public class UILayoutManager : MonoBehaviour
{
    [Header("레이아웃 경계선")]
    public bool showBorders = true;
    public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
    public float borderThickness = 2f;

    [Header("화면 분할 비율")]
    [Range(0.2f, 0.7f)]
    public float topViewRatio = 0.45f;  // 상단 뷰 비율

    // 패널 색상 (시각적으로 구분하기 위한 용도)
    [Header("패널 색상 (테스트용)")]
    public Color topPanelColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color bottomPanelColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color player2PanelColor = new Color(0.4f, 0.2f, 0.2f, 0.5f);
    public Color scorePanelColor = new Color(0.2f, 0.4f, 0.2f, 0.5f);

    // 패널 레퍼런스 (내부적으로 생성)
    private RectTransform mainCanvas;
    private RectTransform topPanel;     // 상단 게임 패널
    private RectTransform bottomPanel;  // 하단 게임 패널
    private RectTransform player2Panel; // 오른쪽 상단 패널
    private RectTransform scorePanel;   // 하단 중앙 패널

    private void Awake()
    {
        // 기존에 캔버스가 있는지 확인
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        
        if (existingCanvas != null && existingCanvas.isRootCanvas)
        {
            mainCanvas = existingCanvas.GetComponent<RectTransform>();
        }
        else
        {
            // 캔버스 생성
            GameObject canvasObject = new GameObject("UI_Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            mainCanvas = canvasObject.GetComponent<RectTransform>();
        }
        
        // 패널 생성
        CreatePanels();
        
        // 경계선 생성
        if (showBorders) CreateBorders();
    }

    // 패널 생성
    private void CreatePanels()
    {
        // // 상단 패널 생성 (땅따먹기 게임)
        topPanel = CreatePanel("TopPanel", topPanelColor);
        topPanel.anchorMin = new Vector2(0, 1 - topViewRatio);
        topPanel.anchorMax = new Vector2(1, 1);
        
        // 하단 패널 생성 (벽돌깨기 게임)
        bottomPanel = CreatePanel("BottomPanel", bottomPanelColor);
        bottomPanel.anchorMin = new Vector2(0, 0);
        bottomPanel.anchorMax = new Vector2(1, 1 - topViewRatio);
        
        // Player 2 패널 생성 (오른쪽 상단)
        player2Panel = CreatePanel("Player2Panel", player2PanelColor);
        player2Panel.anchorMin = new Vector2(0.8f, 1 - topViewRatio);
        player2Panel.anchorMax = new Vector2(1, 1);
        
        // 점수 패널 생성 (하단 중앙)
        scorePanel = CreatePanel("ScorePanel", scorePanelColor);
        scorePanel.anchorMin = new Vector2(0, 0);
        scorePanel.anchorMax = new Vector2(1, 0.12f);


            // 오른쪽 상단 Player2 패널 먼저 생성
    // player2Panel = CreatePanel("Player2Panel", player2PanelColor);
    // player2Panel.anchorMin = new Vector2(0.8f, 1 - topViewRatio);
    // player2Panel.anchorMax = new Vector2(1, 1);
    
    // // TopPanel을 Player2Panel 영역을 제외하고 생성
    // topPanel = CreatePanel("TopPanel", topPanelColor);
    // topPanel.anchorMin = new Vector2(0, 1 - topViewRatio);
    // topPanel.anchorMax = new Vector2(0.8f, 1); // 너비를 0.8f로 제한
    
    // // 하단 패널 생성 (벽돌깨기 게임)
    // bottomPanel = CreatePanel("BottomPanel", bottomPanelColor);
    // bottomPanel.anchorMin = new Vector2(0, 0);
    // bottomPanel.anchorMax = new Vector2(1, 1 - topViewRatio);
    
    // // 점수 패널 생성 (하단 중앙)
    // scorePanel = CreatePanel("ScorePanel", scorePanelColor);
    // scorePanel.anchorMin = new Vector2(0, 0);
    // scorePanel.anchorMax = new Vector2(1, 0.12f);
    }

    // 패널 생성 헬퍼 메서드
    private RectTransform CreatePanel(string name, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        
        // 이미지 컴포넌트 추가 (시각적 구분을 위한 배경색)
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        
        return rectTransform;
    }

    // 경계선 생성
    private void CreateBorders()
    {
        // 수평 분할선 (상단/하단 게임 분리)
        CreateDivider("HorizontalDivider", new Vector2(0, 1 - topViewRatio), 
                      new Vector2(1, 1 - topViewRatio), true);

        // Player 2 패널 세로 분할선
        CreateDivider("Player2VerticalDivider", new Vector2(0.8f, 1 - topViewRatio), 
                      new Vector2(0.8f, 1), false);

        // Player 2 패널 가로 분할선
        CreateDivider("Player2HorizontalDivider", new Vector2(0.8f, 1 - (0.4f * topViewRatio)), 
                      new Vector2(1, 1 - (0.4f * topViewRatio)), true);

        // 하단 점수 패널 경계선
        CreateDivider("BottomHorizontalDivider", new Vector2(0, 0.12f), 
                    new Vector2(1, 0.12f), true);

            // 수평 분할선 (상단/하단 게임 분리)
    // CreateDivider("HorizontalDivider", new Vector2(0, 1 - topViewRatio), 
    //               new Vector2(1, 1 - topViewRatio), true);

    // // Player 2 패널 세로 분할선 - TopPanel과 Player2Panel 사이
    // CreateDivider("Player2VerticalDivider", new Vector2(0.8f, 1 - topViewRatio), 
    //               new Vector2(0.8f, 1), false);

    // // Player 2 패널 가로 분할선 - 여전히 Player2Panel 내부 구분선
    // CreateDivider("Player2HorizontalDivider", new Vector2(0.8f, 1 - (0.4f * topViewRatio)), 
    //               new Vector2(1, 1 - (0.4f * topViewRatio)), true);

    // // 하단 점수 패널 경계선
    // CreateDivider("BottomHorizontalDivider", new Vector2(0, 0.12f), 
    //             new Vector2(1, 0.12f), true);
    }

    // 분할선 생성 헬퍼 메서드
    private void CreateDivider(string name, Vector2 start, Vector2 end, bool isHorizontal)
    {
        GameObject divider = new GameObject(name);
        divider.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rectTransform = divider.AddComponent<RectTransform>();
        
        // 시작점과 끝점 설정
        rectTransform.anchorMin = start;
        rectTransform.anchorMax = end;
        rectTransform.sizeDelta = isHorizontal ? new Vector2(0, borderThickness) : new Vector2(borderThickness, 0);
        
        // 이미지 컴포넌트 추가
        Image image = divider.AddComponent<Image>();
        image.color = borderColor;
    }
    
    // 외부에서 패널 참조를 얻기 위한 메서드
    public RectTransform GetTopPanel() { return topPanel; }
    public RectTransform GetBottomPanel() { return bottomPanel; }
    public RectTransform GetPlayer2Panel() { return player2Panel; }
    public RectTransform GetScorePanel() { return scorePanel; }
}