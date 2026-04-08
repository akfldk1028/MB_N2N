using System;
using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    /// <summary>
    /// 벽돌 비주얼 — 둥근 젤리 스프라이트 적용 + 테마 색상 틴트 + 히트 애니메이션
    /// SpriteRenderer.color 사용 (GPU 최적, SRP Batcher 호환)
    /// </summary>
    public class BrickVisualController : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private int _hp = 1;
        private IDisposable _themeSubscription;
        private Vector3 _originalScale;
        private static Sprite _jellySprite;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _originalScale = transform.localScale;

            // 젤리 스프라이트 로드 + 적용
            if (_jellySprite == null)
                _jellySprite = Resources.Load<Sprite>("Sprites/JellyBrick");

            if (_jellySprite != null && _sr != null)
                _sr.sprite = _jellySprite;
        }

        private void OnEnable()
        {
            if (Managers.ActionBus != null)
                _themeSubscription = Managers.ActionBus.Subscribe(ActionId.Visual_ThemeChanged, UpdateColor);
            UpdateColor();
        }

        private void OnDisable()
        {
            _themeSubscription?.Dispose();
            _themeSubscription = null;
        }

        public void SetHP(int hp)
        {
            _hp = hp;
            UpdateColor();
        }

        public void OnHit()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(HitFlashCoroutine());
            StartCoroutine(SquashCoroutine());
        }

        private void UpdateColor()
        {
            if (_sr == null) return;
            var theme = Managers.Theme?.CurrentTheme;
            if (theme == null) return;

            Color c = theme.GetBrickColor(_hp);
            c.a = 0.92f;
            _sr.color = c;
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            if (_sr == null) yield break;
            Color original = _sr.color;
            _sr.color = Color.white;
            yield return new WaitForSeconds(0.06f);
            _sr.color = original;
        }

        private System.Collections.IEnumerator SquashCoroutine()
        {
            var s = _originalScale;
            transform.localScale = new Vector3(s.x * 1.25f, s.y * 0.75f, s.z);
            yield return new WaitForSeconds(0.04f);
            transform.localScale = new Vector3(s.x * 0.9f, s.y * 1.1f, s.z);
            yield return new WaitForSeconds(0.04f);
            transform.localScale = s;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _jellySprite = null; }
    }
}
