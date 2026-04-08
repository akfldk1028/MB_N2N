using System;
using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    /// <summary>
    /// 벽돌 비주얼 컴포넌트 — SpriteRenderer/Renderer 둘 다 지원
    /// 기존 Brick 로직 수정 없이 색상 + 히트 애니메이션 처리
    /// </summary>
    public class BrickVisualController : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Renderer _renderer;
        private int _hp = 1;
        private IDisposable _themeSubscription;
        private Vector3 _originalScale;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _renderer = GetComponent<Renderer>();
            _originalScale = transform.localScale;
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
            StartCoroutine(HitFlashCoroutine());
            StartCoroutine(SquashCoroutine());
        }

        private void UpdateColor()
        {
            var theme = Managers.Theme?.CurrentTheme;
            if (theme == null) return;

            Color c = theme.GetBrickColor(_hp);
            c.a = 0.9f; // 살짝 반투명

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = c;
            }
            else if (_renderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                _renderer.SetPropertyBlock(mpb);
            }
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            // Flash white
            SetColor(new Color(1f, 1f, 1f, 0.95f));
            yield return new WaitForSeconds(0.08f);
            UpdateColor();
        }

        private System.Collections.IEnumerator SquashCoroutine()
        {
            var s = _originalScale;
            transform.localScale = new Vector3(s.x * 1.3f, s.y * 0.7f, s.z);
            yield return new WaitForSeconds(0.05f);
            transform.localScale = new Vector3(s.x * 0.85f, s.y * 1.15f, s.z);
            yield return new WaitForSeconds(0.05f);
            transform.localScale = new Vector3(s.x * 1.05f, s.y * 0.95f, s.z);
            yield return new WaitForSeconds(0.04f);
            transform.localScale = s;
        }

        private void SetColor(Color c)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = c;
            else if (_renderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                _renderer.SetPropertyBlock(mpb);
            }
        }
    }
}
