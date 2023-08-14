using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "PlayerActions", menuName = "ScriptableObjects/PlayerActions", order = 6)]
    public class PlayerActions : ScriptableObject
    {
        public Sprite actionSprite;
        public string actionFunctionName;
    }
}