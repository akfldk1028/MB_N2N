using UnityEngine;

namespace MB.Visual
{
    public class HitEffectManager
    {
        private GameObject _hitParticlePrefab;
        private bool _initialized = false;

        public void Init()
        {
            CreateHitParticlePrefab();
            _initialized = true;
            GameLogger.Success("HitEffectManager", "초기화 완료");
        }

        /// <summary>
        /// 코드로 파티클 프리팹 생성 (외부 에셋 불필요)
        /// </summary>
        private void CreateHitParticlePrefab()
        {
            var go = new GameObject("JuicyHitEffect");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.3f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.maxParticles = 20;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white; // 런타임에 변경

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 8, 12)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }
            );
            colorOverLifetime.color = gradient;

            // 렌더러 — 기본 Particle 머티리얼
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.SetColor("_BaseColor", Color.white);

            // AutoDestroy 컴포넌트 추가
            go.AddComponent<ParticleAutoDestroy>();

            go.SetActive(false);
            _hitParticlePrefab = go;
            // 씬에 남겨두되 비활성화 (프리팹 역할)
            Object.DontDestroyOnLoad(go);
        }

        public void PlayBrickHit(Vector3 position, Color color)
        {
            if (_hitParticlePrefab == null) return;

            var go = Object.Instantiate(_hitParticlePrefab, position, Quaternion.identity);
            go.SetActive(true);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                ps.Play();
            }
        }

        public void PlayBlockCapture(Vector3 position, Color color)
        {
            PlayBrickHit(position, color);
        }
    }
}
