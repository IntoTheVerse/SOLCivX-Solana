using System;
using UnityEngine;

namespace SimKit
{
    [Serializable]
    public struct ObjectsOnTile
    {
        public GameObject obj;
        [Range(0, 1)] public float weight;
    }
}