using UnityEngine;

namespace SimKit
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController instance;
        public float scrollSpeed;
        public float movementSpeed;
        public float movementTime;
        public float cameraMinZoom;
        public float cameraMaxZoom;
        public float minimapCamMinOrthoSize;
        public float minimapCamMaxOrthoSize;
        public float cameraBoundsBuffer;
        public bool enableCameraRotateWhenZooming = true;

        private Vector3 _newPosition;
        private float _currentCamHeight;
        private InputManager _inputManager;
        private bool _rightMouseDown;
        private Transform _cam;
        private float _cachedMovementSpeed;
        private float _cachedMovementTime;
        private Vector2 _cameraMovementArea;
        private bool _isInteractable = true;

        private void Awake()
        {
            instance = this;
            _inputManager = new InputManager();
            foreach (Camera cam in GetComponentsInChildren<Camera>())
            {
                if (cam.CompareTag("MainCamera")) _cam = cam.transform;
            }
            _cachedMovementSpeed = movementSpeed;
            _cachedMovementTime = movementTime;
        }

        private void OnEnable()
        {
            _inputManager.Movement.Enable();
            _inputManager.Movement.MouseRight.started += _ => _rightMouseDown = true;
            _inputManager.Movement.MouseRight.canceled += _ => _rightMouseDown = false;
        }

        private void OnDisable()
        {
            _inputManager.Movement.Disable();
            _inputManager.Movement.MouseRight.started -= _ => _rightMouseDown = true;
            _inputManager.Movement.MouseRight.canceled -= _ => _rightMouseDown = false;
        }

        private void Start()
        {
            _currentCamHeight = (cameraMaxZoom + cameraMinZoom) / 2;
        }

        private void Update()
        {
            if(_isInteractable) HandleMovement();
        }

        public void SetCameraBounds()
        {
            Bounds bounds = new();

            foreach (MeshCollider collider in FindObjectsOfType<MeshCollider>()) bounds.Encapsulate(collider.bounds);
            transform.position = bounds.center;
            _newPosition = transform.position;
            _cameraMovementArea = new Vector2(bounds.size.x, bounds.size.z);
        }

        private Vector3 CheckCameraBounds(Vector3 pos)
        {
            if (pos.x >= _cameraMovementArea.x - cameraBoundsBuffer)
                pos = new Vector3(_cameraMovementArea.x - cameraBoundsBuffer, pos.y, pos.z);

            if (pos.x <= cameraBoundsBuffer)
                pos = new Vector3(cameraBoundsBuffer, pos.y, pos.z);

            if (pos.z >= _cameraMovementArea.y - cameraBoundsBuffer)
                pos = new Vector3(pos.x, pos.y, _cameraMovementArea.y - cameraBoundsBuffer);

            if (pos.z <= -cameraBoundsBuffer)
                pos = new Vector3(pos.x, pos.y, -cameraBoundsBuffer);

            return pos;
        }

        private void HandleMovement()
        {
            if (_rightMouseDown)
            {
                Vector2 mouseDelta = _inputManager.Movement.MouseDelta.ReadValue<Vector2>();
                if (mouseDelta != Vector2.zero)
                    _newPosition += movementSpeed * Time.deltaTime * new Vector3(-mouseDelta.x, 0, -mouseDelta.y);
            }

            Vector2 mouseScrollDelta = _inputManager.Movement.MouseScrollDelta.ReadValue<Vector2>();
            if (mouseScrollDelta.y != 0)
            {
                if (mouseScrollDelta.y > 0)
                    _currentCamHeight -= scrollSpeed + Time.deltaTime;
                else if (mouseScrollDelta.y < 0)
                    _currentCamHeight += scrollSpeed + Time.deltaTime;

                if (_currentCamHeight > cameraMaxZoom)
                    _currentCamHeight = cameraMaxZoom;

                if (_currentCamHeight < cameraMinZoom)
                    _currentCamHeight = cameraMinZoom;
            }

            _newPosition = CheckCameraBounds(_newPosition);
            transform.position = Vector3.Lerp(transform.position, _newPosition, Time.deltaTime * movementTime);
            _cam.position = transform.position + new Vector3(0, _currentCamHeight, 0);

            movementSpeed = _currentCamHeight.Remap(cameraMinZoom, cameraMaxZoom, _cachedMovementSpeed / 2, _cachedMovementSpeed);
            movementTime = _currentCamHeight.Remap(cameraMinZoom, cameraMaxZoom, _cachedMovementTime * 2, _cachedMovementTime);

            if (enableCameraRotateWhenZooming)
            {
                _cam.eulerAngles = new Vector3(_currentCamHeight.Remap(cameraMinZoom, cameraMaxZoom, 40, 70), 0, 0);
            }
        }

        public void IsInteractable(bool val)
        {
            _isInteractable = val;
        }
    }
}