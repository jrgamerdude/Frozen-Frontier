using FrozenFrontier.Data;
using UnityEngine;
using UIEventSystem = UnityEngine.EventSystems.EventSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FrozenFrontier.Systems
{
    public class MapWorldInput : MonoBehaviour
    {
        [SerializeField] private MapSystem mapSystem;
        [SerializeField] private MapWorldView mapWorldView;
        [SerializeField] private Camera worldCamera;

        private void Awake()
        {
            if (mapSystem == null)
            {
                mapSystem = FindFirstObjectByType<MapSystem>();
            }

            if (mapWorldView == null)
            {
                mapWorldView = FindFirstObjectByType<MapWorldView>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (mapSystem == null || mapWorldView == null || worldCamera == null)
            {
                return;
            }

            if (IsPointerOverUi() || !WasPrimaryPressStarted())
            {
                return;
            }

            if (!TryGetPointerWorldPosition(out Vector3 worldPos))
            {
                return;
            }

            if (!mapWorldView.WorldToGrid(worldPos, out int x, out int y))
            {
                return;
            }

            if (!mapSystem.TryGetTileAt(x, y, out MapTileRuntimeData tile) || tile == null)
            {
                return;
            }

            if (tile.state == TileState.Locked)
            {
                mapSystem.TryUnlockTileAt(x, y);
            }
            else if (tile.state == TileState.Unlocked && !tile.isExploring)
            {
                mapSystem.TryStartExplorationAt(x, y);
            }
        }

        private bool IsPointerOverUi()
        {
            if (UIEventSystem.current == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            return UIEventSystem.current.IsPointerOverGameObject();
#else
            if (Input.touchCount > 0)
            {
                return UIEventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            return UIEventSystem.current.IsPointerOverGameObject();
#endif
        }

        private bool WasPrimaryPressStarted()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                return touch.phase == TouchPhase.Began;
            }

            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool TryGetPointerWorldPosition(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                float depth = -worldCamera.transform.position.z;
                worldPos = worldCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, depth));
                worldPos.z = 0f;
                return true;
            }

            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float depth = -worldCamera.transform.position.z;
                worldPos = worldCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, depth));
                worldPos.z = 0f;
                return true;
            }

            return false;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                float depth = -worldCamera.transform.position.z;
                worldPos = worldCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, depth));
                worldPos.z = 0f;
                return true;
            }

            float z = -worldCamera.transform.position.z;
            Vector3 mousePos = Input.mousePosition;
            worldPos = worldCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, z));
            worldPos.z = 0f;
            return true;
#endif
        }
    }
}
