using UnityEngine;

public class IsometricCameraSetup : MonoBehaviour
{
    public Transform gridTransform; // 그리드 오브젝트
    public float zoomPadding = 1.2f; // 줌 여유 공간
    
    void Start()
    {
        if (gridTransform == null)
        {
            Debug.LogWarning("그리드 오브젝트가 지정되지 않았습니다.");
            return;
        }
        
        SetupCamera();
    }
    
    void SetupCamera()
    {
        // 카메라를 Orthographic으로 변경
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            
            // 그리드 경계 계산
            Renderer[] renderers = gridTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            // 그리드 전체가 보이도록 orthographicSize 계산
            float vertical = bounds.size.y + bounds.size.z * Mathf.Sin(Mathf.Deg2Rad * transform.rotation.eulerAngles.x);
            float horizontal = bounds.size.x * 0.5f / cam.aspect;
            
            cam.orthographicSize = Mathf.Max(vertical, horizontal) * zoomPadding;
            
            // 카메라 위치 조정
            Vector3 center = bounds.center;
            transform.position = new Vector3(center.x, transform.position.y, center.z - bounds.size.z);
        }
    }
}