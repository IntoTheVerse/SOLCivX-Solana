using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class Tooltip : MonoBehaviour
    {
        public RectTransform canvasRect;
        public TextMeshProUGUI header;
        public TextMeshProUGUI content;
        public LayoutElement layoutElement;
        public int characterWrapLimit;

        private RectTransform _rectTransform;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetText(string contentText, string headerText = "")
        {
            if (string.IsNullOrEmpty(headerText))
            {
                header.gameObject.SetActive(false);
            }
            else
            {
                header.gameObject.SetActive(true);
                header.text = headerText;
            }

            content.text = contentText;

            int headerLength = header.text.Length;
            int contentLength = content.text.Length;

            layoutElement.enabled = (headerLength > characterWrapLimit || contentLength > characterWrapLimit);
        }

        private void Update()
        {
            Vector2 position = Input.mousePosition;

            if (position.x + _rectTransform.rect.width > canvasRect.rect.width)
                position.x = canvasRect.rect.width - _rectTransform.rect.width;

            if (position.y + _rectTransform.rect.height > canvasRect.rect.height)
                position.y = canvasRect.rect.height - _rectTransform.rect.height;

            transform.position = position;
        }
    }
}