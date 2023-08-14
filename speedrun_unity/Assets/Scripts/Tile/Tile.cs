using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "Tile", menuName = "ScriptableObjects/Tile", order = 1)]
    public class Tile : ScriptableObject
    {
        [Header("Tile Details")]
        public string TileName;
        public float OuterSize;
        public float InnerSize;
        public float Height;
        public Material Material;
        public Material OutOfViewMaterial;
        public bool Walkable;
        public int costOfMovementInTile;
        [Range(0, 1)] public float Weight;

        [Header("Objects On Tile")]
        public int minimunObjectsSpawnOnTile;
        public int maximumObjectsSpawnOnTile;
        public ObjectsOnTile[] objsOnTile;

        [Header("Resources On Tile")]
        public ResourcesOnTile[] resources;
    }
}