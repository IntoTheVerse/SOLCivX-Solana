using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class PlayerActionsUIManager : MonoBehaviour
    {
        public Image actionImage;
        public Button actionButton;

        public void Init(PlayerActions action, IActionable actionable, PlayerInformation playerInfo)
        {
            actionImage.sprite = action.actionSprite;
            actionButton.onClick.RemoveAllListeners();

            if (action.actionFunctionName == "Mine") actionButton.interactable = playerInfo.isPlayerOnMiningTile;
            else if (action.actionFunctionName == "Attack") actionButton.interactable = !playerInfo.hasAttackedInTurn;
            else actionButton.interactable = true;


            if (action.actionFunctionName == "Mine") actionButton.onClick.AddListener(() => actionable.Mine());
            else if (action.actionFunctionName == "Attack") actionButton.onClick.AddListener(() => actionable.Attack());
        }
    }
}