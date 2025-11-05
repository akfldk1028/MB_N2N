using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.UI;

public class PhysicsPlank : PhysicsObject
{
    // public bool movable; // 이 변수는 MainBall.cs에서 공의 상태에 따라 제어할 수 있습니다. (선택적)
                           // 여기서는 항상 움직일 수 있다고 가정하고 진행합니다.
                           // 만약 공이 발사되기 전에는 움직이지 않게 하려면 MainBall.cs에서 이 값을 조절해야 합니다.

    public Transform leftEnd = null;  // 왼쪽 이동 한계점 Transform
    public Transform rightEnd = null; // 오른쪽 이동 한계점 Transform

    [Tooltip("플랭크가 마우스를 따라가는 속도. 값이 클수록 빠르게 반응합니다.")]
    [Range(1f, 20f)] // Inspector에서 슬라이더로 조절 가능
    public float smoothSpeed = 20f; // 따라가는 속도 조절 변수

    public Camera mainCamera = null; // Public으로 변경하고 Inspector에서 할당
    private Plane plankPlane; // Raycast를 위한 평면

    void Start()
    {
        // mainCamera = Camera.main; // 더 이상 Camera.main 사용 안 함 (삭제)
        if (mainCamera == null)
        {
            // 오류 메시지를 Inspector 할당 확인으로 수정
            Debug.LogError("Main Camera가 Inspector에서 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd == null || rightEnd == null)
        {
            Debug.LogError("Plank의 leftEnd 또는 rightEnd가 설정되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            Debug.LogWarning($"Plank 경고: leftEnd({leftEnd.position.x})의 x좌표가 rightEnd({rightEnd.position.x})보다 크거나 같습니다!", this);
        }

        plankPlane = new Plane(Vector3.forward, transform.position);
    }

    void Update()
    {
        if (leftEnd == null || rightEnd == null || mainCamera == null) return; // 이동 한계점이 없으면 실행 중단

        Vector3 inputPosition = Vector3.zero;
        bool inputDetected = false;

        if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 또는 터치
        {
            inputPosition = Input.mousePosition;
            inputDetected = true;
        }
        // // 터치 입력을 별도로 처리하려면 (멀티터치 등)
        // else if (Input.touchCount > 0)
        // {
        //     inputPosition = Input.GetTouch(0).position;
        //     inputDetected = true;
        // }

        if (inputDetected)
        {
            // 1. 마우스 위치로 Ray 생성
            Ray ray = mainCamera.ScreenPointToRay(inputPosition);

            // 2. Ray와 플랭크 평면의 교차점 계산
            float enterDistance;
            if (plankPlane.Raycast(ray, out enterDistance))
            {
                // 교차점 월드 좌표 얻기
                Vector3 worldPosition = ray.GetPoint(enterDistance);

                float targetXBeforeClamp = worldPosition.x;

                // 3. 목표 X 좌표 계산 및 제한
                float leftBoundaryX = leftEnd.position.x;
                float rightBoundaryX = rightEnd.position.x;
                float targetXAfterClamp = Mathf.Clamp(targetXBeforeClamp, leftBoundaryX, rightBoundaryX);

                // 4. 현재 위치에서 목표 위치까지 부드럽게 이동할 새 위치 계산

                Vector3 targetPosition = new Vector3(targetXAfterClamp, transform.position.y, transform.position.z);

                // MoveTowards를 사용하여 일정한 속도로 이동하도록 변경
                smoothSpeed = 20f;
                Vector3 smoothedPosition = Vector3.MoveTowards(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

                if (rb != null && rb.isKinematic)
                {
                     rb.MovePosition(smoothedPosition);
                }
                else
                {
                    // Rigidbody가 없거나 Kinematic이 아니면 기존 방식 사용 (경고 로깅 추가 가능)
                    transform.position = smoothedPosition;
                    if(rb == null) Debug.LogWarning("[PhysicsPlank] Rigidbody2D not found for MovePosition.", this);
                    else if(!rb.isKinematic) Debug.LogWarning("[PhysicsPlank] Rigidbody2D is not kinematic, using transform.position directly.", this);
                }

                 // --- 상세 추적 로그 (주석 처리)
                //  Debug.Log($"MouseXY: {inputPosition.x:F0},{inputPosition.y:F0} | Viewport: {mainCamera.pixelRect} | WorldX: {worldPosition.x:F2} | TargetX_PreClamp: {targetXBeforeClamp:F2} | LeftB: {leftBoundaryX:F2} | RightB: {rightBoundaryX:F2} | TargetX_PostClamp: {targetXAfterClamp:F2} | CurrentX: {transform.position.x:F2}");
            }
            else
            {
                // Ray가 평면과 교차하지 않는 경우
                // Debug.LogWarning("Ray가 플랭크 평면과 교차하지 않습니다.");
            }
        }
    }

    /// <summary>
    /// 플랭크와 공의 충돌 시 튕겨나갈 속도를 계산하여 반환합니다.
    /// </summary>
    /// <param name="ballRb">충돌한 공의 Rigidbody2D</param>
    /// <param name="collision">충돌 정보</param>
    /// <returns>계산된 반사 속도 벡터</returns>
    public Vector2 CalculateBounceVelocity(Rigidbody2D ballRb, Collision2D collision)
    {
            if (ballRb == null) return Vector2.zero; // 공 Rigidbody 없으면 처리 불가

            Vector2 hitPoint = collision.contacts[0].point;
            Transform plankTransform = collision.transform; // 플랭크 자신의 Transform
            Collider2D plankCollider = collision.collider; // 플랭크 자신의 Collider

            float xOffset = hitPoint.x - plankTransform.position.x;
            float normalizedOffset = xOffset / (plankCollider.bounds.size.x / 2f);
            normalizedOffset = Mathf.Clamp(normalizedOffset, -1f, 1f);

            float bounceAngle = normalizedOffset * 75f; // 최대 반사각 (75도)
            float bounceAngleRad = bounceAngle * Mathf.Deg2Rad;
            Vector2 bounceDirection = new Vector2(Mathf.Sin(bounceAngleRad), Mathf.Cos(bounceAngleRad)).normalized;

            // 공의 현재 속력 사용
            float currentSpeed = ballRb.linearVelocity.magnitude;
            float targetSpeed = currentSpeed;

            if (targetSpeed < 5f) targetSpeed = 10f; // 최소 속도 보정

            Vector2 bounceVelocity = bounceDirection * targetSpeed;
            // Debug.Log($"[PhysicsPlank] Calculated Bounce: Offset={normalizedOffset:F2}, Angle={bounceAngle:F1}, Dir={bounceDirection}, Speed={targetSpeed:F2}");

            return bounceVelocity;
    }

    // 기존 PlankBallCollision 메서드는 CalculateBounceVelocity로 대체되었으므로 제거 또는 주석 처리
    // public void PlankBallCollision(Collision2D collision)
    // {
    //     // ... 기존 코드 ...
    // }
}
