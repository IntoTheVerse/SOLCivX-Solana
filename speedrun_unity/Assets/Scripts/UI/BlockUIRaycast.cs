using UnityEngine;
using UnityEngine.EventSystems;

namespace SimKit
{
    [RequireComponent(typeof(EventTrigger))]
    public class BlockUIRaycast : MonoBehaviour
    {
        public void OnPointerEnter()
        {
            GridLayout.Instance.SetBlockUIRaycast(true);
        }

        public void OnPointerExit()
        {
            GridLayout.Instance.SetBlockUIRaycast(false);
        }
    }
}