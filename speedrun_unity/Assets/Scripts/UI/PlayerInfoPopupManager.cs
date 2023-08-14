using TMPro;
using UnityEngine;

namespace SimKit
{
    public class PlayerInfoPopupManager : MonoBehaviour
    {
        [Header("Panel Information")]
        public TextMeshProUGUI playerName;
        public TextMeshProUGUI playerDescription;
        public Transform playerActionsSpawnTrans;

        [Header("Lerping")]
        public float lerpTime;

        [Header("Misc")]
        public PlayerActionsUIManager playerActionsUIManager;

        private bool isHidden = true;
        private RectTransform _rect;
        private Vector3 lerpFrom;
        private Vector3 lerpTo;
        private bool lerp;
        private float lerpTimePassed;

        private void Start()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void AssignDataToPanel(PlayerType playerType, IActionable actionable, PlayerInformation playerInfo)
        {
            playerName.text = playerType.playerName;
            playerDescription.text = playerType.playerDescription;

            int itemsRequired = playerType.playerActions.Length - playerActionsSpawnTrans.childCount;

            if (itemsRequired > 0)
            {
                for (int i = 0; i < itemsRequired; i++)
                {
                    Instantiate(playerActionsUIManager, playerActionsSpawnTrans);
                }
            }

            if (itemsRequired < 0)
            {
                for (int i = 0; i < Mathf.Abs(itemsRequired); i++)
                {
                    Destroy(playerActionsSpawnTrans.GetChild(0).gameObject);
                }
            }

            for (int i = 0; i < playerType.playerActions.Length; i++)
            {
                playerActionsSpawnTrans.GetChild(i).GetComponent<PlayerActionsUIManager>().Init(playerType.playerActions[i], actionable, playerInfo);
            }
        }

        private void Update()
        {
            if (lerp)
            {
                lerpTimePassed += Time.deltaTime;
                transform.position = Vector3.Lerp(lerpFrom, lerpTo, lerpTimePassed.Remap(0, lerpTime, 0, 1));
                if (lerpTimePassed >= lerpTime) lerp = false;
            }
        }
    }

}