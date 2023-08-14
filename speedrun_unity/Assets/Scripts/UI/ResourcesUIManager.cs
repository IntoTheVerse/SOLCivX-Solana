using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class ResourcesUIManager : MonoBehaviour
    {
        [SerializeField] private Image resourceImage;
        [SerializeField] private TextMeshProUGUI resourceName;
        [SerializeField] private TextMeshProUGUI resourceAmount;

        public void SetValues(string name, Sprite sprite, float amount)
        {
            resourceImage.sprite = sprite;
            resourceName.text = name;
            resourceAmount.text = $"{amount}";
        }

        public void UpdateAmount(float amount)
        {
            resourceAmount.text = $"{amount}";
        }
    }
}