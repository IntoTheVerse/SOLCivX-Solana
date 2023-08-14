using UnityEngine;
using UnityEngine.EventSystems;

namespace SimKit
{
    public class GridLayout : MonoBehaviour
    {
        public static GridLayout Instance;

        [Header("Layout")]
        [SerializeField] private Vector2Int _gridSize;
        [SerializeField] private ShapeType _shapeType;
        [SerializeField] private bool isFlatTopped;

        [Header("Renderer")]
        [SerializeField] public LayerMask _visibilityMask;
        [SerializeField] public Tile[] tileSO;

        [Header("Misc")]
        public ResourceOnTileManager resourceSpawner;

        private Camera _cam;
        private EventSystem _eventSystem;
        private bool _blockUIRaycast = false;
        private ISelectable _prevSelection = null;
        private ISelectable _prevHighlighted = null;

        private void Awake()
        {
            Instance = this;
            _eventSystem = EventSystem.current;
            _cam = Camera.main;
        }

        private void Start()
        {
            LayoutGrid();
            StartCoroutine(GameManager.instance.StartGame());
        }

        private void LayoutGrid()
        {
            DestroyChildren();

            float[] weights = new float[tileSO.Length];
            for (int i = 0; i < tileSO.Length; i++)
            {
                weights[i] = tileSO[i].Weight;
            }

            string[] tileTypes = new string[tileSO.Length];
            for (int i = 0; i < tileSO.Length; i++)
            {
                tileTypes[i] = tileSO[i].TileName;
            }

            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int x = 0; x < _gridSize.x; x++)
                {
                    GameObject tile = new($"Tile {x},{y}", typeof(TileRenderer));
                    tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));

                    int tileIndex = weights.GetRandomWeightedIndex();

                    TileRenderer renderer = tile.GetComponent<TileRenderer>();
                    renderer.shapeType = _shapeType;
                    renderer.isFlatTopped = isFlatTopped;
                    renderer.tileTypes = tileTypes;
                    renderer.currentTileIndex = tileIndex;
                    renderer.SetValues(tileSO[tileIndex].Material, tileSO[tileIndex].InnerSize, tileSO[tileIndex].OuterSize, tileSO[tileIndex].Height);
                    renderer.DrawMesh();

                    tile.AddComponent<MeshCollider>().convex = true;
                    tile.transform.SetParent(transform.GetChild(0), true);

                    TileData tileData = tile.AddComponent<TileData>();
                    tileData.selfInfo = tileSO[tileIndex];
                    tileData.offsetCoordinate = new(x, y);
                    tileData.cubeCoordinate = tileData.offsetCoordinate.OffsetToCube();
                    tileData.resourceSpawner = resourceSpawner;
                    tileData.SetupTile();
                }
            }

            TileManager.instance.SetNeighbours(_shapeType, isFlatTopped);
            FindObjectOfType<CameraController>().SetCameraBounds();
            FindObjectOfType<ResearchTreeManager>().InitResearchTrees();
        }

        private void Update()
        {
            if (_blockUIRaycast && _eventSystem.IsPointerOverGameObject()) return;
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                _prevSelection?.OnDeselect(true);
                _prevSelection = null;
            }
            else
            {
                Vector3 screenPosition = Input.mousePosition;
                screenPosition.z = _cam.nearClipPlane;
                Ray ray = _cam.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _visibilityMask))
                {
                    if (hit.transform.TryGetComponent(out ISelectable target))
                    {
                        if (Input.GetKeyDown(KeyCode.Mouse0))
                        {
                            if (_prevSelection != null && target != _prevSelection) _prevSelection.OnDeselect();
                            target.OnSelect();
                            _prevSelection = target;
                        }
                        else
                        {
                            if (_prevHighlighted != null && target != _prevHighlighted) _prevHighlighted.OnDehighlight();
                            target.OnHighlight();
                            _prevHighlighted = target;
                        }
                    }
                }
            }
        }

        private void DestroyChildren()
        {
            foreach (Transform item in transform.GetChild(0))
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
        }

        public void SetBlockUIRaycast(bool val) => _blockUIRaycast = val;

        private Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
        {
            int column = coordinate.x;
            int row = coordinate.y;
            float width;
            float height;
            float xPosition;
            float yPosition;
            bool shouldOffset;
            float horizontalDistance;
            float verticalDistance;
            float offset;
            float size = tileSO[0].OuterSize;

            if (_shapeType == ShapeType.Hexagon)
            {
                if (!isFlatTopped)
                {
                    shouldOffset = (row % 2) == 0;
                    width = Mathf.Sqrt(3) * size;
                    height = 2f * size;

                    horizontalDistance = width;
                    verticalDistance = height * (3f / 4f);
                    offset = shouldOffset ? width / 2 : 0;

                    xPosition = (column * horizontalDistance) + offset;
                    yPosition = row * verticalDistance;
                }
                else
                {
                    shouldOffset = (column % 2) == 0;
                    width = 2f * size;
                    height = Mathf.Sqrt(3) * size;

                    horizontalDistance = width * (3f / 4f);
                    verticalDistance = height;
                    offset = shouldOffset ? height / 2 : 0;

                    xPosition = column * horizontalDistance;
                    yPosition = (row * verticalDistance) - offset;
                }
            }
            else
            {
                if (!isFlatTopped)
                {
                    shouldOffset = (row % 2) == 0;
                    width = Mathf.Sqrt(4) * size;

                    horizontalDistance = width;
                    verticalDistance = size;
                    offset = shouldOffset ? width / 2 : 0;

                    xPosition = (column * horizontalDistance) + offset;
                    yPosition = row * verticalDistance;
                }
                else
                {
                    width = 1.88f * size;
                    height = Mathf.Sqrt(2) * size;

                    horizontalDistance = width * (3f / 4f);
                    verticalDistance = height;

                    xPosition = column * horizontalDistance;
                    yPosition = (row * verticalDistance);
                }
            }

            return new Vector3(xPosition, 0, yPosition);
        }
    }
}