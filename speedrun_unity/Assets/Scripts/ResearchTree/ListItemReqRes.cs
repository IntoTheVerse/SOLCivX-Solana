using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class ListItemReqRes : MonoBehaviour
    {
        public Image Img;
        public TextMeshProUGUI Amount;

        public void SetValues(Sprite sprite, float amount)
        {
            Img.sprite = sprite;
            Amount.text = $"{amount}";
        }
    }
}