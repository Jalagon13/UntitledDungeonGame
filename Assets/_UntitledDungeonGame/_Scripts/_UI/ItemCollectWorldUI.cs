using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UntitledDungeonGame
{
    // Facilitates destination of item collect plate
    public class ItemCollectWorldUI : MonoBehaviour
    {
        public event Action OnAnimationComplete;

        [SerializeField] private TextMeshProUGUI _itemText;
        [SerializeField] private float _leviateHeight = 2f;
        [SerializeField] private float _levitateDuration = 2f;
        [SerializeField] private float _pauseDuration = 1f;
        [SerializeField] private float _shrinkDuration = 0.25f;

        private Collider2D _plateCollider;
        private Bounds _plateBounds;
        private Transform _plateToMoveTf;
        private InventoryStack _displayedItem = new();
        private Sequence _sequence;
        private float _yOffSet = 0.5f; // If there is another plate on the destination, keep adding this offset until it reaches a free spot
        private int _displayAmount;

        // NOTE: I need this so that frustrating count bug doesn't happen again, for some reason this works
        public int DisplayAmount { get => _displayAmount; set => _displayAmount = value; }

        public InventoryStack DisplayedItem
        {
            get => _displayedItem;
            set
            {
                _displayedItem = value;
                _displayAmount = value.Amount;

                _itemText.text = $"+{DisplayAmount} {_displayedItem.Item.ItemName}";
                _itemText.color = _itemText.color;
            }
        }

        private void Awake()
        {
            _plateCollider = GetComponent<Collider2D>();
            _plateToMoveTf = transform.GetChild(0);
            _plateBounds = _plateCollider.bounds;
        }

        private void OnDestroy()
        {
            // Stop all dotweens
            DOTween.ClearCachedTweens();
            _sequence.Kill();

            OnAnimationComplete = null;
        }

        private void OnEnable()
        {
            // Start with an initial travel destination
            Vector2 initialProjectedPosition = new(transform.position.x, transform.position.y + _leviateHeight);

            // Used for calculating the free space position in world space
            Vector2 worldSpacePosition = initialProjectedPosition;

            // Used to keep track of the number of times the y offset has been added to the initial projected position
            int offSetTracker = 0;

            // While the initialProjectedPosition is not free, add the y offset to the initialProjectedPosition and try again until it is free
            while (!PositionIsPlateFree(ref worldSpacePosition))
            {
                worldSpacePosition += new Vector2(0f, _yOffSet);
                offSetTracker++;
            }

            // these variables are used to calculate the child object position equivalent of the position to move to in world space
            float yOffset = _leviateHeight + (offSetTracker * _yOffSet);
            Vector3 childPositionToMoveTo = new(0f, yOffset, 0f);

            // Set the plate collider and tween the plate to move to in "child world space"
            _plateCollider.transform.position += childPositionToMoveTo;

            // Initiate the levitate and disappear animation
            Vector3 localScale = _plateToMoveTf.localScale;
            _plateToMoveTf.localScale = Vector3.zero;
            _plateToMoveTf.DOScale(localScale, _levitateDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            _sequence = DOTween.Sequence();
            _sequence.Append(_plateToMoveTf.DOLocalMove(childPositionToMoveTo, _levitateDuration));
            _sequence.Append(_plateToMoveTf.DOScale(Vector3.zero, _shrinkDuration).SetEase(Ease.InFlash).SetDelay(_pauseDuration));
            _sequence.OnComplete(() =>
            {
                OnAnimationComplete?.Invoke();
            });
        }

        // If there are not other plates at this position
        private bool PositionIsPlateFree(ref Vector2 position)
        {
            // Check if there is a free spot on the position
            var colliders = Physics2D.OverlapBoxAll(position, _plateBounds.size, 0f);

            // If there is a collider found with an ItemCollectPlate, return false
            foreach (Collider2D col in colliders)
            {
                // If plate is found on this collider,
                if (col.CompareTag("ItemNamePlate"))
                {
                    // Space is not free, return false
                    return false;
                }
            }

            // If it makes it through, return true
            return true;
        }
    }
}
