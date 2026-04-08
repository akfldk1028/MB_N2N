using System;
using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    public class BrickVisualController : MonoBehaviour
    {
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _hp = 1;
        private IDisposable _themeSubscription;
        private static Material _jellyMaterial;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();

            // 젤리 머티리얼 적용 (한 번만 로드)
            if (_jellyMaterial == null)
                _jellyMaterial = Resources.Load<Material>("Materials/Mat_JellyBrick");

            if (_jellyMaterial != null && _renderer != null)
                _renderer.sharedMaterial = _jellyMaterial;
        }

        private void OnEnable()
        {
            // 테마 변경 이벤트 구독
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
            if (theme == null || _renderer == null) return;

            _renderer.GetPropertyBlock(_mpb);
            Color c = theme.GetBrickColor(_hp);
            c.a = 0.85f;
            _mpb.SetColor(BaseColorId, c);
            _renderer.SetPropertyBlock(_mpb);
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            if (_renderer == null) yield break;
            _renderer.GetPropertyBlock(_mpb);

            _mpb.SetColor(BaseColorId, Color.white);
            _renderer.SetPropertyBlock(_mpb);
            yield return new WaitForSeconds(0.08f);

            UpdateColor();
        }

        private System.Collections.IEnumerator SquashCoroutine()
        {
            transform.localScale = new Vector3(1.3f, 0.7f, 1f);
            yield return new WaitForSeconds(0.05f);
            transform.localScale = new Vector3(0.85f, 1.15f, 1f);
            yield return new WaitForSeconds(0.05f);
            transform.localScale = new Vector3(1.05f, 0.95f, 1f);
            yield return new WaitForSeconds(0.04f);
            transform.localScale = Vector3.one;
        }

        // Domain Reload off 대응
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _jellyMaterial = null;
        }
    }
}
