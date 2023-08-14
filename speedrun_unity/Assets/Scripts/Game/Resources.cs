using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "Resource", menuName = "ScriptableObjects/Resource", order = 2)]
    public class Resources : ScriptableObject
    {
        public string resourceName;
        public Sprite resourceSprite;
        public float startingAmount;
    }
}