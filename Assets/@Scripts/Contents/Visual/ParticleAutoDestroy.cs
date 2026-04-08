using UnityEngine;

namespace MB.Visual
{
    public class ParticleAutoDestroy : MonoBehaviour
    {
        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (_ps != null && !_ps.isPlaying && _ps.particleCount == 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
