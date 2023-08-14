using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimKit
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string header;
        public string content;

        private IEnumerator delayEnumerator;

        public void SetContentAndHeader(string content, string header = "")
        {
            this.content = content;
            this.header = header;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            delayEnumerator = DelayShow();
            StartCoroutine(delayEnumerator);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopCoroutine(delayEnumerator);
            TooltipManager.Hide();
        }

        private IEnumerator DelayShow()
        {
            yield return new WaitForSeconds(0.5f);
            TooltipManager.Show(content, header);
        }
    }
}