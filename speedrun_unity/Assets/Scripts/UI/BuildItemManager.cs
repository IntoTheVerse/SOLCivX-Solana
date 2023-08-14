using SimKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildItemManager : MonoBehaviour
{
    public Image image;
    public Image bgImage;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;
    public GameObject requiresResearch;
    public Button equipButton;

    public void InitBuildItem(RTListItem item)
    {
        int level = item.ItemName.text.Contains("3") ? 3 : item.ItemName.text.Contains("2") ? 2 : 1;
        bgImage.color = level == 3 ? Color.magenta : level == 2 ? Color.yellow : Color.white;
        image.sprite = item.ItemImage.sprite;
        itemName.text = item.ItemName.text;
        itemDescription.text = item.ItemDescription.text;
        equipButton.interactable = false;
        requiresResearch.SetActive(true);
        item.onResearchComplete += OnResearchComplete;
        if (item.ItemName.text.Contains("House")) equipButton.onClick.AddListener(() => FindObjectOfType<HouseManager>().SetHouseLevel(level));
        else if (item.ItemName.text.Contains("Defence")) equipButton.onClick.AddListener(() => FindObjectOfType<TowerManager>().SetTowerLevel(level));
        else if (item.ItemName.text.Contains("Armour")) equipButton.onClick.AddListener(() => FindObjectOfType<ArmourManager>().SetArmourLevel(level));
        else if (item.ItemName.text.Contains("Weapon")) equipButton.onClick.AddListener(() => FindObjectOfType<WeaponManager>().SetWeaponLevel(level));
    }

    private void OnResearchComplete()
    {
        equipButton.interactable = true;
        requiresResearch.SetActive(false);
    }
}