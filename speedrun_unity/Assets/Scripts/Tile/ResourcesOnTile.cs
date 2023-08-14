using System;
using UnityEngine;

namespace SimKit
{
    [Serializable]
    public struct ResourcesOnTile
    {
        public Resources resource;
        public float lowestPossibleSpawnAmount;
        public float highestPossibleSpawnAmount;
        [Range(0, 1)] public float probabilityOfSpawning;
    }
}
