using System.Collections.Generic;
using UnityEngine;

namespace SimKit
{
    public class TileManager : MonoBehaviour
    {
        [HideInInspector] public static TileManager instance;
        [HideInInspector] public Dictionary<Vector3Int, TileData> tiles = new();

        [Header("Tile Info")]
        [SerializeField] private Tile pointerHighlightAreaSO;
        [SerializeField] private Tile pointerSelectionAreaSO;
        [SerializeField] private Tile FogOfWarSO;
        [SerializeField] private float FogOfWarHeightFromTile;

        private GameObject _pointerSelectionArea = null;
        private GameObject _pointerHighlightArea = null;
        private GameObject _fogOfWarTile = null;
        private Player _currentlySelectedPlayer = null;
        public delegate void InitializePlayer(TileData[] tileDatas);
        public static event InitializePlayer OnInitializePlayer;

        private List<TileData> _currentPlayerPath;
        private TileData _lastCheckedDestination;
        private TileData[] _allTiles;

        private void Awake()
        {
            instance = this;
        }

        public (TileData[], int) GetAllTilesWithLength()
        {
            return (_allTiles, _allTiles.Length);
        }

        public void SetNeighbours(ShapeType shapeType, bool isFlatTopped)
        {
            tiles = new Dictionary<Vector3Int, TileData>();
            TileData[] tileDatas = gameObject.GetComponentsInChildren<TileData>();

            foreach (TileData tileData in tileDatas)
            {
                tiles.Add(tileData.cubeCoordinate, tileData);
            }

            foreach (TileData tileData in tileDatas)
            {
                List<TileData> neighbours = GetNeighbours(tileData);
                tileData.neighbours = neighbours;
            }

            GeneratePointerSelectionArea(shapeType, isFlatTopped);
            GeneratePointerHighlightArea(shapeType, isFlatTopped);
            GenerateFogOfWarTile(shapeType, isFlatTopped);
            GenerateFogOfWar(tileDatas);
            _allTiles = tileDatas;
            OnInitializePlayer?.Invoke(tileDatas);
        }

        private List<TileData> GetNeighbours(TileData tileData)
        {
            List<TileData> neighbours = new();

            Vector3Int[] neighbourCoords = new Vector3Int[]
            {
            new(1, -1, 0),
            new(1, 0, -1),
            new(0, 1, -1),
            new(-1, 1, 0),
            new(-1, 0, 1),
            new(0, -1, 1)
            };

            foreach (Vector3Int neighbourCoord in neighbourCoords)
            {
                Vector3Int tileCoord = tileData.cubeCoordinate;

                if (tiles.TryGetValue(tileCoord + neighbourCoord, out TileData neighbour))
                {
                    neighbours.Add(neighbour);
                }
            }

            return neighbours;
        }

        private void GeneratePointerSelectionArea(ShapeType shapeType, bool isFlatTopped)
        {
            if (_pointerSelectionArea != null) Destroy(_pointerSelectionArea);
            _pointerSelectionArea = new("Pointer Area Selector", typeof(TileRenderer))
            {
                tag = "IgnoredByRendererEditor",
            };
            _pointerSelectionArea.transform.position = new(1000, 1000, 1000);
            TileRenderer renderer = _pointerSelectionArea.GetComponent<TileRenderer>();

            renderer.shapeType = shapeType;
            renderer.isFlatTopped = isFlatTopped;
            renderer.SetValues(pointerSelectionAreaSO.Material, pointerSelectionAreaSO.InnerSize, pointerSelectionAreaSO.OuterSize, pointerSelectionAreaSO.Height);
            renderer.DrawMesh();
        }

        private void GeneratePointerHighlightArea(ShapeType shapeType, bool isFlatTopped)
        {
            if (_pointerHighlightArea != null) Destroy(_pointerHighlightArea);
            _pointerHighlightArea = new("Pointer Area Highlightor", typeof(TileRenderer))
            {
                tag = "IgnoredByRendererEditor",
            };
            _pointerHighlightArea.transform.position = new(1000, 1000, 1000);
            TileRenderer renderer = _pointerHighlightArea.GetComponent<TileRenderer>();

            renderer.shapeType = shapeType;
            renderer.isFlatTopped = isFlatTopped;
            renderer.SetValues(pointerHighlightAreaSO.Material, pointerHighlightAreaSO.InnerSize, pointerHighlightAreaSO.OuterSize, pointerHighlightAreaSO.Height);
            renderer.DrawMesh();
        }

        private void GenerateFogOfWarTile(ShapeType shapeType, bool isFlatTopped)
        {
            if (_fogOfWarTile != null) Destroy(_fogOfWarTile);
            _fogOfWarTile = new("Fog Of War", typeof(TileRenderer))
            {
                tag = "IgnoredByRendererEditor",
            };
            _fogOfWarTile.transform.position = new(1000, 1000, 1000);
            TileRenderer renderer = _fogOfWarTile.GetComponent<TileRenderer>();

            renderer.shapeType = shapeType;
            renderer.isFlatTopped = isFlatTopped;
            renderer.SetValues(FogOfWarSO.Material, FogOfWarSO.InnerSize, FogOfWarSO.OuterSize, FogOfWarSO.Height);
            renderer.DrawMesh();
        }

        private void GenerateFogOfWar(TileData[] tileDatas)
        {
            Mesh tileMesh = _fogOfWarTile.GetComponent<MeshFilter>().mesh;
            foreach (TileData tileData in tileDatas)
            {
                GameObject fow = Instantiate(_fogOfWarTile, transform.GetChild(1));
                fow.name = $"FOW {tileData.offsetCoordinate}";
                fow.transform.position = tileData.transform.position + new Vector3(0, FogOfWarHeightFromTile, 0);
                fow.GetComponent<MeshFilter>().mesh = tileMesh;
                fow.SwapLayer("Tiles");
                tileData.fow = fow;
                tileData.gameObject.SwapLayer("Hidden");
            }
        }

        public void OnHighlightTile(TileData tile)
        {
            _pointerHighlightArea.transform.position = tile.transform.position + new Vector3(0, (pointerHighlightAreaSO.Height / 2) + (tile.selfInfo.Height / 2), 0);
            TryHighlightingPathOfSelectedPlayer(tile);
        }

        public void OnSelectTile(TileData tile)
        {
            _pointerSelectionArea.transform.position = tile.transform.position + new Vector3(0, (pointerSelectionAreaSO.Height / 2) + (tile.selfInfo.Height / 2), 0);
            TryMovingSelectedPlayer();
            _currentlySelectedPlayer = null;
        }

        private void TryHighlightingPathOfSelectedPlayer(TileData destination)
        {
            if (_currentlySelectedPlayer == null) return;
            if (destination.selfInfo.Walkable)
            {
                if (_lastCheckedDestination != destination)
                {
                    _lastCheckedDestination = destination;
                    _currentPlayerPath = PathFinder.FindPath(_currentlySelectedPlayer.currentTile, destination);
                    _currentlySelectedPlayer.UpdateLineRenderer(_currentPlayerPath);
                    _currentlySelectedPlayer.UpdateLinePoints(_currentPlayerPath);
                }
            }
            else
            {
                _lastCheckedDestination = destination;
                _currentlySelectedPlayer.UpdateLineRenderer(new());
                _currentlySelectedPlayer.UpdateLinePoints(new());
            }
        }

        private void TryMovingSelectedPlayer()
        {
            if (_currentlySelectedPlayer == null) return;
            _currentlySelectedPlayer.SetPath(_currentPlayerPath);
        }

        public void SetCurrentPlayer(Player player)
        {
            _currentlySelectedPlayer = player;
        }

        public void RemoveCurrentPlayer()
        {
            _lastCheckedDestination = null;
            _currentlySelectedPlayer.UpdateLineRenderer(new());
            _currentlySelectedPlayer.UpdateLinePoints(new());
            _currentlySelectedPlayer = null;
        }
    }
}