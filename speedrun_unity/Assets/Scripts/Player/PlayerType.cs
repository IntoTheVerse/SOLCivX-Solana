using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "PlayerType", menuName = "ScriptableObjects/PlayerType", order = 5)]
    public class PlayerType : ScriptableObject
    {
        public string playerName;
        public string playerDescription;
        public PlayerActions[] playerActions;
    }
}