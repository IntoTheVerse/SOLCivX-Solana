using UnityEngine;

namespace SimKit
{
    public class TooltipManager : MonoBehaviour
    {
        private static TooltipManager instance;
        public Tooltip tooltip;

        public void Awake()
        {
            instance = this;
        }

        public static void Show(string content, string header = "")
        {
            instance.tooltip.SetText(content, header);
            instance.tooltip.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            instance.tooltip.gameObject.SetActive(false);
        }
    }
}