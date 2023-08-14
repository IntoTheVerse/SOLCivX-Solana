using System;
using UnityEngine;

namespace SimKit
{
    [CreateAssetMenu(fileName = "Research Tree", menuName = "ScriptableObjects/Research Tree", order = 3)]
    public class ResearchTree : ScriptableObject
    {
        public string RTName;
        public bool verticalListing;
        public ResearchTreeListAndItems[] ItemsInTree;
    }

    [Serializable]
    public struct ResearchTreeListAndItems
    {
        public int ListNumber;
        public ResearchTreeItem[] Items;
    }
}