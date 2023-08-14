using TMPro;
using UnityEngine;

namespace SimKit
{
    public class PathPointTextHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI pointText;

        public void SetText(string txt)
        {
            pointText.text = txt;
        }
    }
}