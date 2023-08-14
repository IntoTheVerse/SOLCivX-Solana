using SimKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    public enum Builds
    { 
        House,
        Weapon,
        Armour,
        Defence
    }
    public TextMeshProUGUI currentBuildText;

    public Button houseButton;
    public Button weaponButton;
    public Button armourButton;
    public Button defenceButton;
    public Transform housesSpawn;
    public Transform weaponsSpawn;
    public Transform armoursSpawn;
    public Transform defencesSpawn;
    public BuildItemManager buildItem;

    private void Start()
    {
        SetCurrentBuild(Builds.House);
        houseButton.onClick.AddListener(() => SetCurrentBuild(Builds.House));
        weaponButton.onClick.AddListener(() => SetCurrentBuild(Builds.Weapon));
        armourButton.onClick.AddListener(() => SetCurrentBuild(Builds.Armour));
        defenceButton.onClick.AddListener(() => SetCurrentBuild(Builds.Defence));
    }

    public void SpawnBuildItem(RTListItem item)
    {
        if (item.ItemName.text.Contains("House")) Instantiate(buildItem, housesSpawn).InitBuildItem(item);
        else if (item.ItemName.text.Contains("Weapon")) Instantiate(buildItem, weaponsSpawn).InitBuildItem(item);
        else if (item.ItemName.text.Contains("Armour")) Instantiate(buildItem, armoursSpawn).InitBuildItem(item);
        else if (item.ItemName.text.Contains("Defence")) Instantiate(buildItem, defencesSpawn).InitBuildItem(item);
    }

    public void SetCurrentBuild(Builds build)
    {
        if (build == Builds.House)
        {
            currentBuildText.text = "House";
            housesSpawn.gameObject.SetActive(true);
            weaponsSpawn.gameObject.SetActive(false);
            armoursSpawn.gameObject.SetActive(false);
            defencesSpawn.gameObject.SetActive(false);
        }
        else if (build == Builds.Weapon)
        { 
            currentBuildText.text = "Weapon";
            housesSpawn.gameObject.SetActive(false);
            weaponsSpawn.gameObject.SetActive(true);
            armoursSpawn.gameObject.SetActive(false);
            defencesSpawn.gameObject.SetActive(false);
        }
        else if (build == Builds.Armour)
        {
            currentBuildText.text = "Armour";
            housesSpawn.gameObject.SetActive(false);
            weaponsSpawn.gameObject.SetActive(false);
            armoursSpawn.gameObject.SetActive(true);
            defencesSpawn.gameObject.SetActive(false);
        }
        else if (build == Builds.Defence)
        { 
            currentBuildText.text = "Defence";
            housesSpawn.gameObject.SetActive(false);
            weaponsSpawn.gameObject.SetActive(false);
            armoursSpawn.gameObject.SetActive(false);
            defencesSpawn.gameObject.SetActive(true);
        }
    }
}