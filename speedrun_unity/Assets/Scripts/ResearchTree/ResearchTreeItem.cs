using System;
using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "Research Tree Item", menuName = "ScriptableObjects/Research Tree Item", order = 4)]
    public class ResearchTreeItem : ScriptableObject
    {
        [Header("Research Tree Item Details")]
        public string RTItemName;
        public string RTItemDescription;
        public Sprite RTItemSprite;
        public ResearchTreeItem[] PreResearchedItemsRequired;
        public int TurnsRequiredToResearch;
        public ResourcesRequiredForResearch[] OtherResourcesRequired;
    }

    [Serializable]
    public struct ResourcesRequiredForResearch
    {
        public Resources Resource;
        public float ResourceAmount;
    }
}