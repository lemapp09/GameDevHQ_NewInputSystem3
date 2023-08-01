using System;
using UnityEngine;
using Cinemachine;
using Game.Scripts.UI;
using UnityEngine.InputSystem;

namespace Game.Scripts.LiveObjects
{
    public class Drone : MonoBehaviour
    {
        private enum Tilt
        {
            NoTilt, Forward, Back, Left, Right
        }

        [SerializeField]
        private Rigidbody _rigidbody;
        [SerializeField]
        private float _speed = 5f;
        private bool _inFlightMode = false;
        [SerializeField]
        private Animator _propAnim;
        [SerializeField]
        private CinemachineVirtualCamera _droneCam;
        [SerializeField]
        private InteractableZone _interactableZone;
        

        public static event Action OnEnterFlightMode;
        public static event Action onExitFlightmode;
        private PlayerInputActions _playerInputActions;
        private float thrust = 0;
        private float rotating = 0;
        private float forwardBackwards = 0;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterFlightMode;
            _playerInputActions = new PlayerInputActions();
        }

        private void EnterFlightMode(InteractableZone zone)
        {
            if (_inFlightMode != true && zone.GetZoneID() == 4) // drone Scene
            {
                _playerInputActions.Player.Disable();
                _playerInputActions.Drone.Enable();
                _propAnim.SetTrigger("StartProps");
                _droneCam.Priority = 11;
                _inFlightMode = true;
                OnEnterFlightMode?.Invoke();
                UIManager.Instance.DroneView(true);
                _interactableZone.CompleteTask(4);
            }
        }

        private void ExitFlightMode()
        {            
            _playerInputActions.Drone.Disable();
            _playerInputActions.Player.Enable();
            _droneCam.Priority = 9;
            _inFlightMode = false;
            UIManager.Instance.DroneView(false);            
        }

        private void Update()
        {
            if (_inFlightMode)
            {
                CalculateTilt();
                CalculateMovementUpdate();

                if (_playerInputActions.Drone.Escape.triggered )
                {
                    _inFlightMode = false;
                    onExitFlightmode?.Invoke();
                    ExitFlightMode();
                }
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.AddForce(transform.up * (9.81f), ForceMode.Acceleration);
            if (_inFlightMode)
                CalculateMovementFixedUpdate();
        }

        private void CalculateMovementUpdate()
        {
            var tempRot = transform.localRotation.eulerAngles;
            tempRot.y += (_speed / 3) * _playerInputActions.Drone.LeftRight.ReadValue<float>();
            transform.localRotation = Quaternion.Euler(tempRot);
        }

        private void CalculateMovementFixedUpdate()
        {
            thrust = _playerInputActions.Drone.Thrust.ReadValue<float>();
            _rigidbody.AddForce(thrust * _speed * transform.up, ForceMode.Acceleration);
        }

        private void CalculateTilt()
        {
            rotating = _playerInputActions.Drone.Rotating.ReadValue<float>();
            forwardBackwards = _playerInputActions.Drone.ForwardBackward.ReadValue<float>();
            transform.rotation = Quaternion.Euler(00, transform.localRotation.eulerAngles.y, 30 * rotating);
            transform.rotation = Quaternion.Euler(30 * forwardBackwards, transform.localRotation.eulerAngles.y, 0);
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterFlightMode;
        }
    }
}
