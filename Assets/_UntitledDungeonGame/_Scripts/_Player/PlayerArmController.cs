using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerArmController : NetworkBehaviour
    {
        [SerializeField]
        private GameObject _heldItemPivot;
        [SerializeField]
        private GameObject _heldItemHolder;

        [Header("Pivots")]
        [SerializeField]
        private Transform _northPivot;
        [SerializeField]
        private Transform _southPivot;
        [SerializeField]
        private Transform _eastPivot;
        [SerializeField]
        private Transform _westPivot;

        public bool IsSwinging { get; private set; }
        
        public NetworkVariable<CardinalDirection> AimDirection { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> AngleToMouse { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<CardinalDirection> SwingDirection { get; private set; } = new(CardinalDirection.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private SwingObject _currentSwingObject;
        private SwingObject _currentSwingPrefab;

        private void Awake()
        {
            _heldItemHolder.SetActive(false);
        }

        private void Update()
        {
            if(IsOwner)
            {
                Vector3 direction = GameManager.MouseWorldPosition - (Vector2)transform.position;
                AngleToMouse.Value = NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                AimDirection.Value = DetermineCardinalDirection(AngleToMouse.Value);
            }
        }

        public void PerformSwing(Quaternion startRotation, Quaternion endRotation, float duration, CardinalDirection swingDirection, ushort toolItemId)
        {
            SwingDirection.Value = swingDirection;

            PerformSwingClientRpc(startRotation, endRotation, duration, swingDirection, toolItemId);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PerformSwingClientRpc(Quaternion startRotation, Quaternion endRotation, float duration, CardinalDirection direction, ushort toolItemId)
        {
            IsSwinging = true;
            
            ToolItemSO toolItemSO = GameDataRegistry.Instance.GetItemSOFromItemId(toolItemId) as ToolItemSO;

            EnsureCurrentSwingObject(toolItemSO.SwingObject);
            SetPivotPosition(direction);

            _heldItemPivot.transform.rotation = startRotation;
            _heldItemHolder.SetActive(true);
            _currentSwingObject.OnStartSwing();

            _heldItemPivot.transform.DORotateQuaternion(endRotation, duration).SetEase(Ease.OutSine).OnComplete(() =>
            {
                _heldItemPivot.transform.rotation = endRotation;
                _heldItemHolder.SetActive(false);
                _currentSwingObject.OnEndSwing();
                
                SwingDirection.Value = CardinalDirection.None;
                IsSwinging = false;
            });
        }

        private void EnsureCurrentSwingObject(SwingObject swingPrefab)
        {
            if (_currentSwingObject != null && _currentSwingPrefab == swingPrefab)
            {
                return;
            }

            if (_currentSwingObject != null)
            {
                Destroy(_currentSwingObject.gameObject);
            }

            _currentSwingPrefab = swingPrefab;
            _currentSwingObject = Instantiate(swingPrefab, _heldItemHolder.transform);
        }

        private void SetPivotPosition(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    _heldItemPivot.transform.position = _northPivot.transform.position;
                    break;
                case CardinalDirection.South:
                    _heldItemPivot.transform.position = _southPivot.transform.position;
                    break;
                case CardinalDirection.West:
                    _heldItemPivot.transform.position = _westPivot.transform.position;
                    break;
                case CardinalDirection.East:
                    _heldItemPivot.transform.position = _eastPivot.transform.position;
                    break;
            }
        }

        private float NormalizeAngle(float angle)
        {
            return (angle % 360 + 360) % 360;
        }

        private CardinalDirection DetermineCardinalDirection(float angle)
        {
            if (angle < 45 || angle > 315) return CardinalDirection.East;
            if (angle < 135) return CardinalDirection.North;
            if (angle < 225) return CardinalDirection.West;
            return CardinalDirection.South;
        }

        
    }
}
