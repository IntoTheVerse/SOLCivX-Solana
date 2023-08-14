using System.Collections.Generic;
using UnityEngine;

namespace SimKit
{
    public class ResourcesManager : MonoBehaviour
    {
        public Resources[] resources;

        private Dictionary<string, float> resourcesAmount = new();
        private MenuManager menuManager;

        public void Start()
        {
            menuManager = FindObjectOfType<MenuManager>();
            menuManager.SetResources(resources);
            foreach (var resource in resources)
            {
                resourcesAmount.Add(resource.resourceName, resource.startingAmount);
            }
        }

        public void UpdateResourceAmount(string resourceName, float amount)
        {
            if (resourcesAmount.TryGetValue(resourceName, out float _))
            {
                resourcesAmount[resourceName] = amount;
                menuManager.UpdateResource(resourceName, amount);
            }
        }

        public void ReduceResourceAmount(string resourceName, float amount)
        {
            if (resourcesAmount.TryGetValue(resourceName, out float _))
            {
                resourcesAmount[resourceName] -= amount;
                menuManager.UpdateResource(resourceName, resourcesAmount[resourceName]);
            }
        }

        public void AddResourceAmount(string resourceName, float amount)
        {
            if (resourcesAmount.TryGetValue(resourceName, out float _))
            {
                resourcesAmount[resourceName] += amount;
                menuManager.UpdateResource(resourceName, resourcesAmount[resourceName]);
            }
        }

        public float GetResource(string resourceName)
        {
            if (resourcesAmount.TryGetValue(resourceName, out float val))
            {
                return val;
            }

            return 0;
        }

        public Sprite GetResourceSprite(string resourceName)
        {
            Sprite sprite = null;
            foreach (var resource in resources)
            {
                if (resource.resourceName == resourceName)
                    sprite = resource.resourceSprite;
            }
            return sprite;
        }
    }
}