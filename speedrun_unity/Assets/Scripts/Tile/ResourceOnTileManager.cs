using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class ResourceOnTileManager : MonoBehaviour
    {
        public Image resourceImage;

        [HideInInspector] public float amount;
        [HideInInspector] public Resources resource;

        public void InitResourceOnTile(Resources resource, float amount)
        {
            resourceImage.sprite = resource.resourceSprite;
            this.resource = resource;
            this.amount = amount;
        }
    }

}