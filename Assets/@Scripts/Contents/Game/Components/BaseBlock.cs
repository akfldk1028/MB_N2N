using UnityEngine;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 블록 기본 구현
    /// 총알에 맞으면 색상/소유권 변경
    /// </summary>
    public class BaseBlock : MonoBehaviour, IBlock
    {
        #region Serialized Fields
        [Header("Block Settings")]
        [SerializeField] protected int _durability = 1;
        [SerializeField] protected bool _isDestructible = false;
        #endregion

        #region State
        protected string _componentId;
        protected int _ownerPlayerId = -1;
        protected Color _ownerColor = Color.gray;
        protected Vector2Int _gridPosition;
        protected bool _isInitialized = false;
        #endregion

        #region Components
        protected SpriteRenderer _spriteRenderer;
        protected MeshRenderer _meshRenderer;
        #endregion

        #region IMapComponent Implementation
        public string ComponentId => _componentId;
        public MapComponentType ComponentType => MapComponentType.Block;
        public int OwnerPlayerId => _ownerPlayerId;
        public Color OwnerColor => _ownerColor;
        public bool CanChangeOwner => true; // 블록은 항상 변경 가능
        public Vector2Int GridPosition => _gridPosition;
        public Vector3 WorldPosition => transform.position;
        #endregion

        #region IBlock Implementation
        public int Durability => _durability;
        public bool IsDestructible => _isDestructible;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _componentId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        #endregion

        #region Initialization
        public virtual void Initialize(Vector2Int gridPos, int ownerId, Color color)
        {
            _gridPosition = gridPos;
            _ownerPlayerId = ownerId;
            _ownerColor = color;
            _isInitialized = true;

            ApplyColor(color);

            gameObject.name = $"Block_{gridPos.x}_{gridPos.y}";
        }

        public virtual void Cleanup()
        {
            _isInitialized = false;
            Destroy(gameObject);
        }
        #endregion

        #region Owner Change
        public virtual bool SetOwner(int playerId, Color color)
        {
            if (!CanChangeOwner) return false;

            _ownerPlayerId = playerId;
            _ownerColor = color;
            ApplyColor(color);

            return true;
        }

        protected virtual void ApplyColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }

            if (_meshRenderer != null)
            {
                var material = _meshRenderer.material;
                if (material != null)
                {
                    material.color = color;
                }
            }
        }
        #endregion

        #region Bullet Collision
        public virtual bool OnBulletHit(IBullet bullet)
        {
            if (bullet == null) return false;

            // 같은 팀 총알이면 통과 (파괴 안 함)
            if (bullet.OwnerPlayerId == _ownerPlayerId)
            {
                return false; // 총알 유지
            }

            // 다른 팀 총알이면 소유권 변경
            SetOwner(bullet.OwnerPlayerId, bullet.OwnerColor);

            // 파괴 가능한 블록이면 내구도 감소
            if (_isDestructible)
            {
                _durability -= bullet.Damage;
                if (_durability <= 0)
                {
                    Cleanup();
                }
            }

            return true; // 총알 파괴
        }
        #endregion
    }
}
