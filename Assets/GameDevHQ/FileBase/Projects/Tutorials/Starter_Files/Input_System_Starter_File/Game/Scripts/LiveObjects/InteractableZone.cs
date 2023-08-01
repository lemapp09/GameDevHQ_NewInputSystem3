using System;
using UnityEngine;
using Game.Scripts.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Scripts.LiveObjects
{
    public class InteractableZone : MonoBehaviour
    {
        private enum ZoneType
        {
            Collectable,
            Action,
            HoldAction
        }

        private enum KeyState
        {
            Press,
            PressHold
        }

        [SerializeField]
        private ZoneType _zoneType;
        [SerializeField]
        private int _zoneID;
        [SerializeField]
        private int _requiredID;
        [SerializeField]
        [Tooltip("Press the (---) Key to .....")]
        private string _displayMessage;
        [SerializeField]
        private GameObject[] _zoneItems;
        private bool _inZone = false;
        private bool _itemsCollected = false;
        private bool _actionPerformed = false;
        [SerializeField]
        private Sprite _inventoryIcon;
        [SerializeField]
        private Key _zoneKeyInput;
        public Key ZoneKeyInput {
            get { return _zoneKeyInput; }
        }
        [SerializeField]
        private KeyState _keyState;
        [SerializeField]
        private GameObject _marker;

        private static int _currentZoneID = 0;
        public static int CurrentZoneID
        { 
            get 
            { 
               return _currentZoneID; 
            }
            set
            {
                _currentZoneID = value; 
                         
            }
        }
        private PlayerInputActions _playerInputActions;
        private Key _pressedKey;
        public Key PressedKey
        {
            get { return _pressedKey; }
        }
        public static event Action<InteractableZone> onZoneInteractionComplete;
        public static event Action<int> onHoldStarted;
        public static event Action<int> onHoldEnded;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += SetMarker;
            _playerInputActions = new PlayerInputActions();
            _playerInputActions.Interactables.Pressed.performed += OnPressed;
            _playerInputActions.Interactables.Hold.performed += OnHeld;
            _playerInputActions.Interactables.Hold.canceled += EndPressHold;
        }

        private void EndPressHold(InputAction.CallbackContext context)
        {
            if (_inZone) {
                onHoldEnded?.Invoke(_zoneID);
            }
        }

        private void OnPressed(InputAction.CallbackContext context)
        {
            if ((_inZone == true))
            {
                // AnyKey Pressed 
                if (Keyboard.current.anyKey.wasPressedThisFrame) {
                    foreach (KeyControl k in Keyboard.current.allKeys) {
                        // Compares pressed key against all known 111 keyboard codes
                        if (k.wasPressedThisFrame) // Only if it was 'freshly' pressed
                        { 
                            Debug.Log("Interactable Zone - Pressed reached.");
                            _pressedKey = k.keyCode;
                            if (k.keyCode == _zoneKeyInput  && _keyState != KeyState.PressHold) {
                                // pressed key must match expected snd the Zone isn't PressHold
                                switch (_zoneType)
                                {
                                    case ZoneType.Collectable:
                                        if (_itemsCollected == false) {
                                            CollectItems();
                                            _itemsCollected = true;
                                            UIManager.Instance.DisplayInteractableZoneMessage(false);
                                        }
                                        break;

                                    case ZoneType.Action:
                                        if (_actionPerformed == false) {
                                            PerformAction();
                                            _actionPerformed = true;
                                            UIManager.Instance.DisplayInteractableZoneMessage(false);
                                        }
                                        break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
        
        private void OnHeld(InputAction.CallbackContext context)
        {
            if (_inZone) {
                if (_keyState == KeyState.PressHold ) {
                    // AnyKey Held 

                    if (_pressedKey == _zoneKeyInput) {
                        switch (_zoneType) {
                            case ZoneType.HoldAction:
                                PerformHoldAction();
                                break;
                        }
                    }
                }
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && _currentZoneID > _requiredID)
            {
                _playerInputActions.Interactables.Enable();
                switch (_zoneType)
                {
                    case ZoneType.Collectable:
                        if (_itemsCollected == false)
                        {
                            _inZone = true;
                            if (_displayMessage != null)
                            {
                                string message = $"Press the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                                UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                            }
                            else
                                UIManager.Instance.DisplayInteractableZoneMessage(true, $"Press the {_zoneKeyInput.ToString()} key to collect");
                        }
                        break;

                    case ZoneType.Action:
                        if (_actionPerformed == false)
                        {
                            _inZone = true;
                            if (_displayMessage != null)
                            {
                                string message = $"Press the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                                UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                            }
                            else
                                UIManager.Instance.DisplayInteractableZoneMessage(true, $"Press the {_zoneKeyInput.ToString()} key to perform action");
                        }
                        break;

                    case ZoneType.HoldAction:
                        _inZone = true;
                        if (_displayMessage != null)
                        {
                            string message = $"Hold the {_zoneKeyInput.ToString()} key to {_displayMessage}.";
                            UIManager.Instance.DisplayInteractableZoneMessage(true, message);
                        }
                        else
                            UIManager.Instance.DisplayInteractableZoneMessage(true, $"Hold the {_zoneKeyInput.ToString()} key to perform action");
                        break;
                }
            }
        }
        
        private void CollectItems()
        {
            foreach (var item in _zoneItems) {
                item.SetActive(false);
            }

            UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);

            CompleteTask(_zoneID);

            onZoneInteractionComplete?.Invoke(this);

        }

        private void PerformAction()
        {
            foreach (var item in _zoneItems) {
                item.SetActive(true);
            }

            if (_inventoryIcon != null)
                UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);

            onZoneInteractionComplete?.Invoke(this);
        }

        private void PerformHoldAction()
        {
            UIManager.Instance.DisplayInteractableZoneMessage(false);
            onHoldStarted?.Invoke(_zoneID);
        }

        public GameObject[] GetItems()
        {
            return _zoneItems;
        }

        public int GetZoneID()
        {
            return _zoneID;
        }

        public void CompleteTask(int zoneID)
        {
            if (zoneID == _zoneID)
            {
                _currentZoneID++;
                onZoneInteractionComplete?.Invoke(this);
            }
        }

        public void ResetAction(int zoneID)
        {
            if (zoneID == _zoneID)
                _actionPerformed = false;
        }

        public void SetMarker(InteractableZone zone)
        {
            if (_zoneID == _currentZoneID)
                _marker.SetActive(true);
            else
                _marker.SetActive(false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _inZone = false;
                UIManager.Instance.DisplayInteractableZoneMessage(false);
            }
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= SetMarker;
        }       
        
    }
}


