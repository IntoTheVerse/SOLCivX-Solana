using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    [Serializable]
    public struct ResearchTreeUI
    {
        public Button Connectedbutton;
        public ResearchTree RT;
    }

    public class ResearchTreeManager : MonoBehaviour
    {
        public ResearchTreeUIManager RTUIManager;
        public Transform RTSpawnTrans;
        public ResearchTreeUI[] ResearchTrees;

        public void InitResearchTrees()
        {
            foreach (var RT in ResearchTrees)
            {
                ResearchTreeUIManager manager = Instantiate(RTUIManager, RTSpawnTrans);
                manager.InitTree(RT.RT);
                RT.Connectedbutton.onClick.AddListener(() => { manager.gameObject.SetActive(true); CameraController.instance.IsInteractable(false); });
            }
        }
    }
}