using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FrozenFrontier.Systems
{
    public class WorldCameraController : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private MapWorldView mapWorldView;
        [SerializeField] private Transform baseFocusTarget;
        [SerializeField] private Transform mapFocusTarget;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float basePanSpeed = 16f;
        [SerializeField, Min(0.1f)] private float zoomSpeed = 5f;
        [SerializeField, Min(1f)] private float minZoom = 5f;
        [SerializeField, Min(1f)] private float maxZoom = 28f;
        [SerializeField, Min(0f)] private float worldPadding = 2f;
        [SerializeField, Min(1f)] private float fallbackBaseBoundsSize = 16f;

        private void Awake()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }

            if (controlledCamera == null)
            {
                controlledCamera = Camera.main;
            }

            if (buildingSystem == null)
            {
                buildingSystem = FindFirstObjectByType<BuildingSystem>();
            }

            if (mapWorldView == null)
            {
                mapWorldView = FindFirstObjectByType<MapWorldView>();
            }
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                return;
            }

            HandlePan();
            HandleZoom();
            HandleFocusHotkeys();
            ClampToWorldBounds();
        }

        private void HandlePan()
        {
            Vector2 input = Vector2.zero;
            if (IsUpPressed())
            {
                input.y += 1f;
            }

            if (IsDownPressed())
            {
                input.y -= 1f;
            }

            if (IsLeftPressed())
            {
                input.x -= 1f;
            }

            if (IsRightPressed())
            {
                input.x += 1f;
            }

            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            input = input.normalized;
            float zoomFactor = controlledCamera.orthographicSize / Mathf.Max(0.1f, minZoom);
            float speed = basePanSpeed * Mathf.Clamp(zoomFactor, 0.6f, 3.5f);
            Vector3 delta = new Vector3(input.x, input.y, 0f) * speed * Time.deltaTime;
            controlledCamera.transform.position += delta;
        }

        private void HandleZoom()
        {
            float scroll = GetScrollY();
            if (Mathf.Abs(scroll) <= 0.0001f)
            {
                return;
            }

            float targetSize = controlledCamera.orthographicSize - scroll * zoomSpeed;
            controlledCamera.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
        }

        private void HandleFocusHotkeys()
        {
            if (WasFocus1Pressed())
            {
                FocusOnBase();
            }

            if (WasFocus2Pressed())
            {
                FocusOnMap();
            }
        }

        private void FocusOnBase()
        {
            Vector3 target = GetBaseCenter();
            Vector3 current = controlledCamera.transform.position;
            controlledCamera.transform.position = new Vector3(target.x, target.y, current.z);
        }

        private void FocusOnMap()
        {
            Vector3 target = GetMapCenter();
            Vector3 current = controlledCamera.transform.position;
            controlledCamera.transform.position = new Vector3(target.x, target.y, current.z);
        }

        private void ClampToWorldBounds()
        {
            Bounds worldBounds = GetCombinedBounds();

            float halfHeight = Mathf.Max(0.1f, controlledCamera.orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.1f, controlledCamera.aspect);

            float xMin = worldBounds.min.x + halfWidth;
            float xMax = worldBounds.max.x - halfWidth;
            float yMin = worldBounds.min.y + halfHeight;
            float yMax = worldBounds.max.y - halfHeight;

            Vector3 pos = controlledCamera.transform.position;
            if (xMin > xMax)
            {
                pos.x = worldBounds.center.x;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            }

            if (yMin > yMax)
            {
                pos.y = worldBounds.center.y;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, yMin, yMax);
            }

            controlledCamera.transform.position = pos;
        }

        private Bounds GetCombinedBounds()
        {
            bool hasBounds = false;
            Bounds combined = default;

            Bounds mapBounds = GetMapBounds();
            if (mapBounds.size.sqrMagnitude > 0.0001f)
            {
                combined = mapBounds;
                hasBounds = true;
            }

            Bounds baseBounds = GetBaseBounds();
            if (baseBounds.size.sqrMagnitude > 0.0001f)
            {
                if (!hasBounds)
                {
                    combined = baseBounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(baseBounds.min);
                    combined.Encapsulate(baseBounds.max);
                }
            }

            if (!hasBounds)
            {
                combined = new Bounds(Vector3.zero, new Vector3(24f, 24f, 0f));
            }

            combined.Expand(new Vector3(worldPadding * 2f, worldPadding * 2f, 0f));
            return combined;
        }

        private Bounds GetMapBounds()
        {
            if (mapWorldView != null)
            {
                return mapWorldView.GetWorldBounds();
            }

            if (mapFocusTarget != null)
            {
                return new Bounds(mapFocusTarget.position, new Vector3(24f, 24f, 0f));
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        private Bounds GetBaseBounds()
        {
            if (buildingSystem != null)
            {
                Vector3 origin = buildingSystem.GetGridOriginWorld();
                float width = buildingSystem.GridWidth * buildingSystem.CellWorldSize;
                float height = buildingSystem.GridHeight * buildingSystem.CellWorldSize;
                Vector3 center = origin + new Vector3(width * 0.5f, height * 0.5f, 0f);
                return new Bounds(center, new Vector3(width, height, 0f));
            }

            if (baseFocusTarget != null)
            {
                return new Bounds(baseFocusTarget.position, new Vector3(fallbackBaseBoundsSize, fallbackBaseBoundsSize, 0f));
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        private Vector3 GetBaseCenter()
        {
            if (buildingSystem != null)
            {
                return buildingSystem.GridToWorldCenter(buildingSystem.GridWidth / 2, buildingSystem.GridHeight / 2, Vector2Int.one);
            }

            return baseFocusTarget != null ? baseFocusTarget.position : Vector3.zero;
        }

        private Vector3 GetMapCenter()
        {
            if (mapWorldView != null)
            {
                return mapWorldView.GetWorldBounds().center;
            }

            return mapFocusTarget != null ? mapFocusTarget.position : Vector3.zero;
        }

        private static bool IsUpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);
#else
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
#endif
        }

        private static bool IsDownPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
#else
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif
        }

        private static bool IsLeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
#else
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
#endif
        }

        private static bool IsRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
#else
            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
#endif
        }

        private static bool WasFocus1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        private static bool WasFocus2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        private static float GetScrollY()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return 0f;
            }

            return mouse.scroll.ReadValue().y * 0.02f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }
    }
}
