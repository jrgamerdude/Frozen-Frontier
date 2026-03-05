using FrozenFrontier.Data;
using UnityEngine;
using UIEventSystem = UnityEngine.EventSystems.EventSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FrozenFrontier.Systems
{
    public class BaseGridInput : MonoBehaviour
    {
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Color validPlacementColor = new Color(0.2f, 0.85f, 0.45f, 0.35f);
        [SerializeField] private Color invalidPlacementColor = new Color(0.9f, 0.25f, 0.2f, 0.35f);
        [SerializeField] private int hoverSortingOrder = 250;

        private SpriteRenderer hoverRenderer;
        private Sprite squareSprite;

        private void Awake()
        {
            if (buildingSystem == null)
            {
                buildingSystem = FindFirstObjectByType<BuildingSystem>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            EnsureHoverRenderer();
            SetHoverVisible(false);
        }

        private void OnEnable()
        {
            if (buildingSystem != null)
            {
                buildingSystem.PlacementModeChanged += OnPlacementModeChanged;
            }
        }

        private void OnDisable()
        {
            if (buildingSystem != null)
            {
                buildingSystem.PlacementModeChanged -= OnPlacementModeChanged;
            }

            SetHoverVisible(false);
        }

        private void Update()
        {
            if (buildingSystem == null || worldCamera == null)
            {
                return;
            }

            if (!buildingSystem.IsPlacementMode)
            {
                SetHoverVisible(false);
                return;
            }

            if (WasCancelPressed())
            {
                buildingSystem.CancelPlacement();
                return;
            }

            bool pointerOverUi = IsPointerOverUi();
            if (pointerOverUi)
            {
                SetHoverVisible(false);
                return;
            }

            if (!TryGetPointerWorld(out Vector3 worldPos))
            {
                SetHoverVisible(false);
                return;
            }

            if (!buildingSystem.WorldToGrid(worldPos, out int x, out int y))
            {
                SetHoverVisible(false);
                return;
            }

            bool canPlace = buildingSystem.CanPlacePendingAtGrid(x, y, out _);
            UpdateHover(x, y, canPlace);

            if (WasPlacePressed())
            {
                buildingSystem.TryPlacePendingAtGrid(x, y);
            }
        }

        private void OnPlacementModeChanged(BuildingDef _)
        {
            if (buildingSystem != null && buildingSystem.IsPlacementMode)
            {
                EnsureHoverRenderer();
            }
            else
            {
                SetHoverVisible(false);
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

        private bool TryGetPointerWorld(out Vector3 worldPos)
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
                float z = -worldCamera.transform.position.z;
                worldPos = worldCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, z));
                worldPos.z = 0f;
                return true;
            }

            return false;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector3 touchPos = touch.position;
                float depth = -worldCamera.transform.position.z;
                worldPos = worldCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, depth));
                worldPos.z = 0f;
                return true;
            }

            Vector3 mousePos = Input.mousePosition;
            float legacyZ = -worldCamera.transform.position.z;
            worldPos = worldCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, legacyZ));
            worldPos.z = 0f;
            return true;
#endif
        }

        private bool WasPlacePressed()
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

        private bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            return rightClick || escape;
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#endif
        }

        private void EnsureHoverRenderer()
        {
            if (hoverRenderer != null)
            {
                return;
            }

            if (squareSprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                squareSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width);
            }

            GameObject hover = new GameObject("PlacementHover");
            Transform parent = buildingSystem != null && buildingSystem.BaseAreaRoot != null
                ? buildingSystem.BaseAreaRoot
                : transform;
            hover.transform.SetParent(parent, false);
            hoverRenderer = hover.AddComponent<SpriteRenderer>();
            hoverRenderer.sprite = squareSprite;
            hoverRenderer.sortingOrder = hoverSortingOrder;
        }

        private void UpdateHover(int x, int y, bool valid)
        {
            EnsureHoverRenderer();
            if (hoverRenderer == null || buildingSystem == null)
            {
                return;
            }

            Vector2Int size = buildingSystem.GetPendingSize();
            hoverRenderer.color = valid ? validPlacementColor : invalidPlacementColor;
            hoverRenderer.transform.position = buildingSystem.GridToWorldCenter(x, y, size);
            hoverRenderer.transform.localScale = new Vector3(
                Mathf.Max(1, size.x) * buildingSystem.CellWorldSize * 0.96f,
                Mathf.Max(1, size.y) * buildingSystem.CellWorldSize * 0.96f,
                1f);
            SetHoverVisible(true);
        }

        private void SetHoverVisible(bool visible)
        {
            if (hoverRenderer != null)
            {
                hoverRenderer.enabled = visible;
            }
        }
    }
}
