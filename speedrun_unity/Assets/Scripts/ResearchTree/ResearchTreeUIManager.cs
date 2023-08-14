using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class ResearchTreeUIManager : MonoBehaviour, ITurnTakeable
    {
        public TextMeshProUGUI RTName;
        public RectTransform RTHorizontalListSpawn;
        public RectTransform RTVerticalListSpawn;
        public ScrollRect ScrollRect;
        public GameObject RTHorizontalListPrefab;
        public RTListItem ListHorizontalItemPrefab;
        public GameObject ListEmptyHorizontalItemPrefab;
        public GameObject RTVerticalListPrefab;
        public RTListItem ListVerticalItemPrefab;
        public GameObject ListEmptyVerticalItemPrefab;

        private ResearchTree RT;
        private List<RTListItem> _spawnedListItems = new();

        [HideInInspector] public List<RTListItem> itemsUnderResearch = new();

        public void InitTree(ResearchTree RT)
        {
            this.RT = RT;
            SetTreeItems();
        }

        private void SetTreeItems()
        {
            ScrollRect.vertical = RT.verticalListing;
            ScrollRect.horizontal = !RT.verticalListing;
            ScrollRect.content = RT.verticalListing ? RTVerticalListSpawn : RTHorizontalListSpawn;
            RTVerticalListSpawn.gameObject.SetActive(RT.verticalListing);
            RTHorizontalListSpawn.gameObject.SetActive(!RT.verticalListing);

            RTName.text = RT.RTName;
            for (int i = 0; i < RT.ItemsInTree.Length; i++)
            {
                ResearchTreeListAndItems rt = RT.ItemsInTree[i];
                Transform list = Instantiate(RT.verticalListing ? RTVerticalListPrefab : RTHorizontalListPrefab, RT.verticalListing ? RTVerticalListSpawn : RTHorizontalListSpawn).transform;
                for (int j = 0; j < rt.Items.Length; j++)
                {
                    ResearchTreeItem item = rt.Items[j];
                    if (item.RTItemName == "Empty")
                        Instantiate(RT.verticalListing ? ListEmptyVerticalItemPrefab : ListEmptyHorizontalItemPrefab, list);
                    else
                    {
                        RTListItem spawnedItem = Instantiate(RT.verticalListing ? ListVerticalItemPrefab : ListHorizontalItemPrefab, list);
                        spawnedItem.gameObject.AddComponent<TooltipTrigger>().SetContentAndHeader(GetItemContent(item.OtherResourcesRequired, item.TurnsRequiredToResearch), "Required Resources");
                        SetItemsInList(spawnedItem, item, i, RT.verticalListing);
                    }
                }
            }

            StartCoroutine(SetReqRes());
        }

        private string GetItemContent(ResourcesRequiredForResearch[] otherResourcesRequired, int turnsRequired)
        {
            string content = $"Turns Required : {turnsRequired} \n"; ;
            for (int i = 0; i < otherResourcesRequired.Length; i++)
            {
                content += $"{otherResourcesRequired[i].Resource.resourceName} : {otherResourcesRequired[i].ResourceAmount} \n";
            }
            return content;
        }

        private void SetItemsInList(RTListItem listItem, ResearchTreeItem itemSO, int listIndex, bool verticalListing)
        {
            Dictionary<string, float> reqResInfo = new();
            foreach (var item in itemSO.OtherResourcesRequired)
            {
                reqResInfo.Add(item.Resource.resourceName.Trim(), item.ResourceAmount);
            }
            _spawnedListItems.Add(listItem);
            listItem.SetValues(itemSO, reqResInfo, this, listIndex, verticalListing);
        }

        private IEnumerator SetReqRes()
        {
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < _spawnedListItems.Count; i++)
            {
                List<RTListItem> items = new();
                for (int j = 0; j < _spawnedListItems[i].requiredResearchedItems.Length; j++)
                {
                    for (int k = 0; k < _spawnedListItems.Count; k++)
                    {
                        if (_spawnedListItems[i].requiredResearchedItems[j].RTItemName == _spawnedListItems[k].ItemName.text)
                        {
                            items.Add(_spawnedListItems[k]);
                        }
                    }
                }
                _spawnedListItems[i].SetRequiredResearches(items.ToArray());
            }

            OnCloseTree();
        }

        public void OnCloseTree()
        {
            gameObject.SetActive(false);
            CameraController.instance.IsInteractable(true);
        }

        private async void UpdateResearchingItems()
        {
            if (itemsUnderResearch.Count > 0)
            {
                bool removeResearchedItem = await itemsUnderResearch[0].OnTick();
                if (removeResearchedItem)
                {
                    foreach (var item in _spawnedListItems)
                    {
                        foreach (var reqRes in item.requiredResearchedItems)
                        {
                            if (reqRes.RTItemName == itemsUnderResearch[0].ItemName.text)
                                item.UpdateRenderLinesOfRequiredItems(itemsUnderResearch[0]);
                        }
                    }
                    itemsUnderResearch.RemoveAt(0);
                    if (itemsUnderResearch.Count > 0) itemsUnderResearch[0].BGImage.color = itemsUnderResearch[0].researchingColor;
                }
            }
        }

        public void OnTurn(int currentTurn, out float timeToExecute)
        {
            UpdateResearchingItems();
            timeToExecute = 0;
        }
    }
}