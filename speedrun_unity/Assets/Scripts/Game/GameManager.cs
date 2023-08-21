using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimKit
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Game Settings")]
        public int totalTurnsAllowed;
        public bool AutoBlurOutOfViewExploredTiles;
        public bool AutoTick;

        [Header("Auto Tick Settings")]
        public float autoTickRate;

        [Header("Misc")]
        public GameObject loading;
        public Button nextTurnButton;
        public NPC enemy;
        public GameObject DeathPanel;
        public Button replay;

        private int _currentTurn = 0;
        private List<TileData> _revealedTiles = new();
        private List<TurnExecution> _turnExecution;
        private List<Player> _player;
        private List<NPC> enemies = new();
        [SerializeField] private TextMeshProUGUI username;
        [SerializeField] private TextMeshProUGUI ambushText;
        private int ambushTime = 120;
        private int ambushCooldown = 180;
        private bool currentlyInAmbush = false;
        private float ambushCounter = 180;
        private int _currentAmbushLevel = 0;
        private TileData[] tiles;

        private void Awake()
        {
            username.text = SolanaManager.Instance.player.Username;
            nextTurnButton.onClick.AddListener(() => OnNextTurnClicked());
            loading.SetActive(true);
            instance = this;
            if (AutoTick)
            {
                GetComponentInChildren<MenuManager>().turnButton.SetActive(false);
                InvokeRepeating(nameof(OnNextTurnClicked), autoTickRate, autoTickRate);
            }
            TileManager.OnInitializePlayer += (val) => { tiles = val; };
            replay.onClick.AddListener(() => OnReplay());
        }

        private void OnReplay()
        {
            SceneManager.LoadScene("Game");
        }

        public void OnDeath()
        {
            StopAmbush();
            DeathPanel.SetActive(true);
        }

        private void Update() 
        {
            if (!currentlyInAmbush)
            {
                ambushCounter -= Time.deltaTime;
                ambushText.text = $"Next Ambush In: {ambushCounter:F2}";
                if (ambushCounter <= 0)
                {
                    ambushCounter = ambushTime;
                    currentlyInAmbush = true;
                    StartAmbush();
                }
            }
            else
            {
                ambushCounter -= Time.deltaTime;
                ambushText.text = $"Ambush Ends In: {ambushCounter:F2}";
                if (ambushCounter <= 0)
                {
                    ambushCounter = ambushCooldown;
                    currentlyInAmbush = false;
                    StopAmbush();
                }
            }
        }

        private void StartAmbush()
        {
            _currentAmbushLevel++;

            for (int i = 0; i < _currentAmbushLevel * 3; i++)
            {
                NPC npc = Instantiate(enemy);
                npc.Instance_OnInitializePlayer(tiles);
                enemies.Add(npc);
            }
        }

        private void StopAmbush()
        { 
            for (int i = 0; i < enemies.Count; i++)
            {
                Destroy(enemies[i]);
            }

            enemies.Clear();
        }

        public void OnNextTurnClicked()
        {
            _currentTurn++;
            nextTurnButton.interactable = false;
            StartCoroutine(TurnExecution());
        }

        private IEnumerator TurnExecution()
        {
            for (int i = 0; i < _turnExecution.Count; i++)
            {
                float waitTime = 0;
                for (int j = 0; j < _turnExecution.Count; j++)
                {
                    if (_turnExecution[j].executionOrder == i) 
                    {
                        _turnExecution[j].GetComponent<ITurnTakeable>().OnTurn(_currentTurn, out float timeToExecute);
                        waitTime = timeToExecute > waitTime ? timeToExecute : waitTime; 
                    }
                }
                yield return new WaitForSeconds(waitTime);
                if (AutoBlurOutOfViewExploredTiles) AutoBlurOoutOfViewExploredTiles();
            }
            OnEndTurn();
        }

        private void OnEndTurn()
        {
            nextTurnButton.interactable = true;
        }

        private void AutoBlurOoutOfViewExploredTiles()
        {
            List<TileData> revealedTiles = new(_revealedTiles);

            foreach (var player in _player)
            {
                player.currentTile.SetInView();
                revealedTiles.Remove(player.currentTile);
                foreach (var neighbour in player.currentTile.neighbours)
                {
                    neighbour.SetInView();
                    revealedTiles.Remove(neighbour);
                }
            }

            foreach (var tile in revealedTiles)
            {
                tile.SetOutOfView();
            }
        }

        public IEnumerator StartGame()
        {
            yield return new WaitForSeconds(1);
            loading.SetActive(false);
            _player = FindObjectsOfType<Player>(true).ToList();
            _turnExecution = FindObjectsOfType<TurnExecution>(true).ToList();
            GridLayout.Instance.SetBlockUIRaycast(false);
            OnNextTurnClicked();
        }

        public void RemoveTurnExecution(TurnExecution turnExecution)
        { 
            _turnExecution.Remove(turnExecution);
        }

        public void AddRevealedTile(TileData tile)
        {
            _revealedTiles.Add(tile);
        }
    }
}