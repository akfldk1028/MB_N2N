using UnityEngine;
using System;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 대포 기본 구현
    /// 총알에 맞으면 체력 감소 → 0이 되면 게임 오버
    /// </summary>
    public class BaseCannon : MonoBehaviour, ICannon
    {
        #region Serialized Fields
        [Header("Cannon Settings")]
        [SerializeField] protected int _maxHealth = 100;
        [SerializeField] protected Transform _firePoint;
        [SerializeField] protected GameObject _bulletPrefab;

        [Header("Visual")]
        [SerializeField] protected SpriteRenderer _bodyRenderer;
        #endregion

        #region State
        protected string _componentId;
        protected int _ownerPlayerId = -1;
        protected Color _ownerColor = Color.gray;
        protected Vector2Int _gridPosition;
        protected int _currentHealth;
        protected bool _isInitialized = false;
        protected bool _isDestroyed = false;
        #endregion

        #region Events
        public event Action<ICannon> OnCannonDestroyed;
        #endregion

        #region IMapComponent Implementation
        public string ComponentId => _componentId;
        public MapComponentType ComponentType => MapComponentType.Cannon;
        public int OwnerPlayerId => _ownerPlayerId;
        public Color OwnerColor => _ownerColor;
        public bool CanChangeOwner => false; // 대포는 소유권 변경 불가
        public Vector2Int GridPosition => _gridPosition;
        public Vector3 WorldPosition => transform.position;
        #endregion

        #region ICannon Implementation
        public int Health => _currentHealth;
        public int MaxHealth => _maxHealth;
        public Transform FirePoint => _firePoint;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _componentId = System.Guid.NewGuid().ToString("N").Substring(0, 8);

            if (_bodyRenderer == null)
                _bodyRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_firePoint == null)
                _firePoint = transform;

            _currentHealth = _maxHealth;
        }
        #endregion

        #region Initialization
        public virtual void Initialize(Vector2Int gridPos, int ownerId, Color color)
        {
            _gridPosition = gridPos;
            _ownerPlayerId = ownerId;
            _ownerColor = color;
            _currentHealth = _maxHealth;
            _isInitialized = true;
            _isDestroyed = false;

            ApplyColor(color);

            gameObject.name = $"Cannon_Player{ownerId}";
            Debug.Log($"[BaseCannon] Initialized: Player {ownerId} at {gridPos}");
        }

        public virtual void Cleanup()
        {
            _isInitialized = false;
            OnCannonDestroyed = null;
            Destroy(gameObject);
        }
        #endregion

        #region Owner (대포는 변경 불가)
        public virtual bool SetOwner(int playerId, Color color)
        {
            // 대포는 소유권 변경 불가능
            return false;
        }

        protected virtual void ApplyColor(Color color)
        {
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = color;
            }
        }
        #endregion

        #region Firing
        public virtual void Fire(Vector3 direction, float speed)
        {
            if (_isDestroyed) return;
            if (_bulletPrefab == null)
            {
                Debug.LogWarning("[BaseCannon] Bullet prefab not assigned!");
                return;
            }

            Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
            var bulletGo = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);

            // IBullet 인터페이스로 설정
            var bullet = bulletGo.GetComponent<IBullet>();
            if (bullet != null)
            {
                // 서브클래스에서 총알 설정 가능
                SetupBullet(bullet, direction, speed);
            }
        }

        protected virtual void SetupBullet(IBullet bullet, Vector3 direction, float speed)
        {
            // 서브클래스에서 오버라이드
            // 예: bullet.Fire(OwnerPlayerId, direction, OwnerColor);
        }
        #endregion

        #region Damage & Destruction
        public virtual bool TakeDamage(int damage)
        {
            if (_isDestroyed) return true;

            _currentHealth -= damage;
            Debug.Log($"[BaseCannon] Player {_ownerPlayerId} took {damage} damage. Health: {_currentHealth}/{_maxHealth}");

            // 피격 이펙트 (선택)
            OnDamageTaken(damage);

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
                return true; // 사망
            }

            return false; // 생존
        }

        protected virtual void OnDamageTaken(int damage)
        {
            // 서브클래스에서 피격 이펙트 구현
        }

        protected virtual void Die()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;
            Debug.Log($"[BaseCannon] Player {_ownerPlayerId} cannon DESTROYED!");

            // 게임 오버 이벤트 발행
            OnCannonDestroyed?.Invoke(this);

            // 파괴 이펙트 (선택)
            OnDestruction();
        }

        protected virtual void OnDestruction()
        {
            // 서브클래스에서 파괴 이펙트 구현
            // 예: 폭발 파티클, 사운드 등
        }
        #endregion

        #region Bullet Collision
        public virtual bool OnBulletHit(IBullet bullet)
        {
            if (bullet == null) return false;
            if (_isDestroyed) return true;

            // 같은 팀 총알이면 무시
            if (bullet.OwnerPlayerId == _ownerPlayerId)
            {
                return false; // 총알 유지 (통과)
            }

            // 다른 팀 총알이면 데미지
            TakeDamage(bullet.Damage);

            return true; // 총알 파괴
        }
        #endregion
    }
}
