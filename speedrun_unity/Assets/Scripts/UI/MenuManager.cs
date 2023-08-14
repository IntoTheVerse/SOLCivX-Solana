using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimKit
{
    public class MenuManager : MonoBehaviour, ITurnTakeable
    {
        public GameObject turnButton;
        public GameObject settings;
        public GameObject menu;
        public GameObject options;
        public TextMeshProUGUI turns;
        public ResourcesUIManager resourcesPrefab;
        public Transform resourcesSpawnTrans;

        private InputManager _inputManager;
        private Dictionary<string, ResourcesUIManager> uiResources = new();

        private void Awake()
        {
            _inputManager = new InputManager();
        }

        public void OnTurn(int currentTurn, out float timeToExecute)
        {
            turns.text = $"Turn {currentTurn}/{GameManager.instance.totalTurnsAllowed}";
            timeToExecute = 0;
        }

        private void OnEnable()
        {
            _inputManager.UI.Enable();
            _inputManager.UI.ESC.started += _ => OnEscape();

        }

        private void OnDisable()
        {
            _inputManager.UI.Disable();
            _inputManager.UI.ESC.started -= _ => OnEscape();
        }

        public void OnEscape()
        {
            settings.SetActive(!settings.activeInHierarchy);
            menu.SetActive(true);
            options.SetActive(false);
        }

        public void OnSettingsButtonClicked()
        {
            settings.SetActive(true);
            menu.SetActive(true);
            options.SetActive(false);
        }

        public void OnReturnToGame()
        {
            settings.SetActive(false);
        }

        public void OnExitToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OnOptions()
        {
            menu.SetActive(false);
            options.SetActive(true);
        }

        public void OnReturnToSettigns()
        {
            menu.SetActive(true);
            options.SetActive(false);
        }

        public void SetResources(Resources[] resources)
        {
            foreach (var resource in resources)
            {
                ResourcesUIManager manager = Instantiate(resourcesPrefab, resourcesSpawnTrans);
                manager.gameObject.AddComponent<TooltipTrigger>().SetContentAndHeader(resource.resourceName);
                manager.SetValues(resource.resourceName, resource.resourceSprite, resource.startingAmount);
                uiResources.Add(resource.resourceName, manager);
            }
        }

        public void UpdateResource(string resourceName, float amount)
        {
            if (uiResources.TryGetValue(resourceName, out ResourcesUIManager manager))
            {
                manager.UpdateAmount(amount);
            }
        }
    }
}