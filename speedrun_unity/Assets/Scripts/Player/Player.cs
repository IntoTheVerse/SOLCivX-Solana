using System;
using System.Collections;
using System.Collections.Generic;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public struct PlayerInformation
    {
        public bool isPlayerOnMiningTile;
        public bool hasAttackedInTurn;
    }

    [RequireComponent(typeof(TurnExecution))]
    public class Player : MonoBehaviour, ISelectable, ITurnTakeable, IActionable
    {
        [HideInInspector] public TileData currentTile;
        [HideInInspector] public int currentArmourLevel;
        [HideInInspector] public int currentWeaponLevel;

        [Header("Player Type")]
        public PlayerType playerType;

        [Header("Indicators")]
        [SerializeField] private GameObject PointsToDestination;
        [SerializeField] private GameObject FinalDestination;
        [SerializeField] private GameObject MinimapIndicator;

        [Header("Movement")]
        [SerializeField] private int movementPerTurn;
        [SerializeField] private Animator animator;

        [Header("Health")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private float maxHealth;

        [Header("Lerp Movement")]
        [SerializeField] private bool lerpToTarget;
        [SerializeField] private float timeToLerpToTarget;

        private PlayerActionsManager _playerActions;
        private PlayerInformation _playerInformation;
        private LineRenderer _lineRenderer;
        private PlayerInfoPopupManager _playerInfoPopupManager;
        private ResourcesManager _resourcesManager;
        private List<TileData> _currentPath = new();
        private List<TileData> _pathToTravel = new();
        private TileData _nextTile;
        private List<GameObject> _spawnedPoints = new();
        private List<GameObject> _spawnedFinalPoints = new();
        private Pooling _pointPooling;
        private Pooling _pointFinalPooling;
        private Vector3 _targetPos;
        private Vector3 _initSelfPos;
        private float _timePassedSinceLerping;
        private float _health;
        private bool _highlited;
        private bool _selected;
        private bool _updatePos;
        private bool _isNewPath;
        private List<NPC> enemies = new();
        private bool canAttack;
        private Dictionary<int, int> _armour = new()
        {
            {1, 7},
            {2, 5},
            {3, 3}
        };

        private Dictionary<int, int> _weapon = new()
        {
            {1, 15},
            {2, 25},
            {3, 40}
        };

        private void Awake()
        {
            _playerInformation = new() { isPlayerOnMiningTile = false, hasAttackedInTurn = false };
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
            _health = maxHealth;
            _lineRenderer = GetComponent<LineRenderer>();
            _resourcesManager = FindObjectOfType<ResourcesManager>();
            _playerActions = FindObjectOfType<PlayerActionsManager>();
            _playerInfoPopupManager = FindObjectOfType<PlayerInfoPopupManager>();
            TileManager.OnInitializePlayer += Instance_OnInitializePlayer;
            _pointPooling = new();
            _pointFinalPooling = new();
            _pointPooling.InitPool(PointsToDestination);
            _pointFinalPooling.InitPool(FinalDestination);
            _playerActions.mineButton.onClick.AddListener(() => Mine());
            _playerActions.attackButton.onClick.AddListener(() => Attack());
        }

        private void Instance_OnInitializePlayer(TileData[] tileDatas)
        {
            TileManager.OnInitializePlayer -= Instance_OnInitializePlayer;
            TileData tile = tileDatas.GetRandom();
            while (!tile.selfInfo.Walkable)
            {
                tile = tileDatas.GetRandom();
            }

            transform.position = tile.transform.position + new Vector3(0, tile.selfInfo.Height / 2, 0);
            currentTile = tile;
            currentTile.playersOnTop.Add(gameObject);
            RevealTile(currentTile);
            UpdatePlayerInformation();
            GetTokensData();
            SolanaManager.Instance.onPlayerAccountChanged += (account) => GetTokensData(account);
        }

        private void RevealTile(TileData tile)
        {
            if (tile.Reveal()) GameManager.instance.AddRevealedTile(tile);
            foreach (TileData neighbour in tile.neighbours)
            {
                if (neighbour.Reveal()) GameManager.instance.AddRevealedTile(neighbour);
            }
        }

        public void OnHighlight()
        {
            _highlited = true;
            MinimapIndicator.SetActive(true);
        }

        public void OnSelect()
        {
            _selected = true;
            MinimapIndicator.SetActive(true);
            TileManager.instance.SetCurrentPlayer(this);
        }

        public void OnDehighlight()
        {
            _highlited = false;
            if (!_highlited && !_selected) MinimapIndicator.SetActive(false);
        }

        public void OnDeselect(bool noOtherSelected = false)
        {
            _selected = false;
            if (!_highlited && !_selected) MinimapIndicator.SetActive(false);
            if (noOtherSelected) TileManager.instance.RemoveCurrentPlayer();
        }

        public void OnTurn(int currentTurn, out float timeToExecute)
        {
            if (SolanaManager.Instance.player.Energy <= 0) 
            { 
                timeToExecute = 0;
                return;
            }
            _playerInformation.hasAttackedInTurn = false;
            HandleMovementPath();
            if (_isNewPath) _pathToTravel.RemoveAt(0);
            if (_pathToTravel.Count > 0)
            {
                RevealTile(currentTile);
                if (lerpToTarget)
                {
                    StartCoroutine(TravelPath());
                    timeToExecute = _pathToTravel.Count * timeToLerpToTarget;
                }
                else
                { 
                    _targetPos = _nextTile.transform.position + new Vector3(0, _nextTile.selfInfo.Height / 2, 0);
                    transform.position = _targetPos;
                    UpdateLinePoints(_currentPath);
                    timeToExecute = 0;
                }
            }
            else
                timeToExecute = 0;
            UpdatePlayerInformation();
            _isNewPath = false;
        }

        private void UpdatePlayerInformation()
        {
            _playerInformation.isPlayerOnMiningTile = currentTile.hasResources;

            if (_playerInformation.isPlayerOnMiningTile) _playerActions.mineButton.interactable = true;
            else _playerActions.mineButton.interactable = false;
        }

        public void SetPath(List<TileData> path)
        {
            _currentPath = path;
            _isNewPath = true;
            UpdateLineRenderer(_currentPath);
            UpdateLinePoints(_currentPath);
        }

        private void HandleMovementPath()
        {
            _pathToTravel.Clear();
            if (_currentPath == null || _currentPath.Count <= 0)
            {
                _nextTile = null;
                if (!GameManager.instance.AutoTick)
                {
                    UpdateLineRenderer(new());
                    UpdateLinePoints(new());
                }
                return;
            }

            int currentlyAvailableMove = movementPerTurn;
            int tilesToRemove = 0;
            for (int i = 0; i < _currentPath.Count; i++)
            {
                currentlyAvailableMove -= _currentPath[i].selfInfo.costOfMovementInTile;
                tilesToRemove++;
                if (currentlyAvailableMove == 0)
                {
                    _nextTile = _currentPath[i];
                    break;
                }
                else if (currentlyAvailableMove < 0)
                {
                    tilesToRemove += _currentPath[i - 1] == currentTile ? 0 : -1;
                    _nextTile = _currentPath[i - 1] == currentTile ? _currentPath[i] : _currentPath[i - 1];
                    break;
                }
                else
                {
                    _nextTile = _currentPath[i];
                }
            }

            if (!_nextTile.selfInfo.Walkable)
            {
                _currentPath.Clear();
                HandleMovementPath();
                return;
            }

            currentTile.playersOnTop.Remove(gameObject);
            currentTile = _nextTile;
            currentTile.playersOnTop.Add(gameObject);
            TileData initialJoin = _currentPath[tilesToRemove - 1];
            for (int i = 0; i < tilesToRemove; i++)
            {
                _pathToTravel.Add(_currentPath[0]);
                _currentPath.RemoveAt(0);
            }

            UpdateLineRenderer(_currentPath, initialJoin);
        }

        public void UpdateLinePoints(List<TileData> path)
        {
            foreach (var item in _spawnedPoints)
            {
                _pointPooling.ReleaseToPool(item);
            }
            _spawnedPoints.Clear();

            foreach (var item in _spawnedFinalPoints)
            {
                _pointFinalPooling.ReleaseToPool(item);
            }
            _spawnedFinalPoints.Clear();

            if (_lineRenderer == null) return;

            int currentCount = 0;
            int currentlyAvailableMove = movementPerTurn;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 tilePos = path[i].transform.position + new Vector3(0, path[i].selfInfo.Height, 0);

                if (i == path.Count - 1)
                {
                    GameObject element = _pointFinalPooling.GetFromPool();
                    _spawnedFinalPoints.Add(element);
                    element.transform.position = tilePos + new Vector3(0, 0.1f, 0);

                }

                currentlyAvailableMove -= path[i].selfInfo.costOfMovementInTile;
                bool canMoveNext = false;
                Vector3 nextPointPos = new();
                if (currentlyAvailableMove == 0)
                {
                    canMoveNext = true;
                    nextPointPos = tilePos + new Vector3(0, 0.1f, 0);
                }
                else if (currentlyAvailableMove < 0)
                {
                    canMoveNext = true;
                    TileData finalTile = path[i - 1] == currentTile ? path[i] : path[i - 1];
                    nextPointPos = finalTile.transform.position + new Vector3(0, finalTile.selfInfo.Height, 0) + new Vector3(0, 0.1f, 0);
                    i--;
                }

                if (canMoveNext)
                {
                    currentlyAvailableMove = movementPerTurn;
                    GameObject element = _pointPooling.GetFromPool();
                    _spawnedPoints.Add(element);
                    element.transform.position = nextPointPos;
                    currentCount++;
                    element.GetComponent<PathPointTextHandler>().SetText($"{currentCount}");
                }
            }
        }

        public void UpdateLineRenderer(List<TileData> path, TileData initialJoin = null)
        {
            List<Vector3> points = new();

            if (lerpToTarget)
            {
                points = SetLerpingLineRendererPoints();
                points.Insert(0, transform.position);
            }

            if (initialJoin != null) points.Add(initialJoin.transform.position + new Vector3(0, initialJoin.selfInfo.Height, 0));

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 tilePos = path[i].transform.position + new Vector3(0, path[i].selfInfo.Height, 0);
                points.Add(tilePos);
            }

            _lineRenderer.positionCount = points.Count;
            _lineRenderer.SetPositions(points.ToArray());
        }

        private IEnumerator TravelPath()
        {
            animator.SetBool("Walk", true);
            for (int i = 0; i < _pathToTravel.Count; i++)
            {
                _initSelfPos = transform.position;
                transform.LookAt(_pathToTravel[i].transform);
                _targetPos = _pathToTravel[i].transform.position + new Vector3(0, _pathToTravel[i].selfInfo.Height / 2, 0);
                _timePassedSinceLerping = 0;
                if (i > 0) _pathToTravel[i - 1] = null;
                UpdateLineRenderer(_currentPath);
                _updatePos = true;
                yield return new WaitForSeconds(timeToLerpToTarget);
                _updatePos = false;
                ReduceEnergy(1);
            }
            _pathToTravel.Clear();
            UpdateLinePoints(_currentPath);
            animator.SetBool("Walk", false);
        }

        private async void ReduceEnergy(int val)
        {
            var tx = new Transaction()
            {
                FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash()
            };

            ReduceEnergyAccounts account = new()
            {
                Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                Player = SolanaManager.Instance.userProfilePDA,
                SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                SystemProgram = SystemProgram.ProgramIdKey
            };

            TransactionInstruction Ix = SpeedrunAnchorProgram.ReduceEnergy(account, (ulong)val, SolanaManager.Instance.programId);
            tx.Add(Ix);

            RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
            Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");
        }

        private List<Vector3> SetLerpingLineRendererPoints()
        {
            List<Vector3> points = new();
            for (int i = 0; i < _pathToTravel.Count; i++)
            {
                if (_pathToTravel[i] != null)
                points.Add(_pathToTravel[i].transform.position + new Vector3(0, _pathToTravel[i].selfInfo.Height, 0));
            }
            return points;
        }

        private void Update()
        {
            if (_updatePos) 
            {
                _lineRenderer.SetPosition(0, transform.position + new Vector3(0, _targetPos.y, 0));
                _timePassedSinceLerping += Time.deltaTime;
                transform.position = Vector3.Lerp(_initSelfPos, _targetPos, _timePassedSinceLerping.Remap(0, timeToLerpToTarget, 0, 1));
            }
        }

        public void TakeDamage()
        {
            _health -= currentArmourLevel == 0 ? 9 : _armour[currentArmourLevel];
            healthSlider.value = _health;
            if (_health <= 0)
            {
                currentTile.playersOnTop.Remove(gameObject);
                GameManager.instance.RemoveTurnExecution(GetComponent<TurnExecution>());
                Destroy(gameObject);
                FindObjectOfType<GameManager>().OnDeath();
                Time.timeScale = 0;
            }
        }

        private void DoDamangeInSelfAndNeighbouringTiles(int v)
        {
            for (int i = 0; i < currentTile.playersOnTop.Count; i++)
            {
                if (currentTile.playersOnTop[i].TryGetComponent(out IDamageable damageable))
                {
                    if((object)damageable != this) damageable.TakeDamage(v);
                }
            }

            foreach (TileData neighbour in currentTile.neighbours)
            {
                for (int i = 0; i < neighbour.playersOnTop.Count; i++)
                {
                    if (neighbour.playersOnTop[i].TryGetComponent(out IDamageable damageable))
                    { 
                        damageable.TakeDamage(v);
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPC npc))
            {
                enemies.Add(npc);
            }

            if(enemies.Count > 0) canAttack = true;
            else canAttack = false;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out NPC npc))
            {
                if(enemies.Contains(npc)) enemies.Remove(npc);
            }

            if (enemies.Count > 0) canAttack = true;
            else canAttack = false;
        }

        public void OnNPCDead(NPC npc)
        {
            enemies.Remove(npc);
        }

        #region PlayerActions
        public async void Mine()
        {
            if (currentTile.hasResources)
            {
                currentTile.hasResources = false;
                currentTile.RemoveResourceUIOnTile();

                string resource = currentTile.spawnedResourceUIOnTile.resource.resourceName;
                PublicKey mint = resource == "Gold" ? SolanaManager.Instance.goldMint : SolanaManager.Instance.silverMint;
                PublicKey VaultATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(SolanaManager.Instance.VaultPDA, mint);
                PublicKey PlayerATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, mint);

                var tx = new Transaction()
                {
                    FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                    Instructions = new List<TransactionInstruction>(),
                    RecentBlockHash = await Web3.BlockHash()
                };

                if (resource == "Gold")
                {
                    AddGoldAccounts goldAccounts = new()
                    {
                        Player = SolanaManager.Instance.userProfilePDA,
                        VaultPda = SolanaManager.Instance.VaultPDA,
                        VaultAta = VaultATA,
                        PlayerAta = PlayerATA,
                        GameToken = SolanaManager.Instance.goldMint,
                        TokenProgram = TokenProgram.ProgramIdKey,
                        AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                        Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                        SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                        SystemProgram = SystemProgram.ProgramIdKey
                    };

                    tx.Add(SpeedrunAnchorProgram.AddGold(goldAccounts, SolanaManager.Instance.programId));
                }
                else
                {
                    AddSilverAccounts silverAccounts = new()
                    {
                        Player = SolanaManager.Instance.userProfilePDA,
                        VaultPda = SolanaManager.Instance.VaultPDA,
                        VaultAta = VaultATA,
                        PlayerAta = PlayerATA,
                        GameToken = SolanaManager.Instance.silverMint,
                        TokenProgram = TokenProgram.ProgramIdKey,
                        AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                        Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                        SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                        SystemProgram = SystemProgram.ProgramIdKey
                    };

                    tx.Add(SpeedrunAnchorProgram.AddSilver(silverAccounts, SolanaManager.Instance.programId));
                }

                RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
                Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");

                UpdatePlayerInformation();
                if (res.WasHttpRequestSuccessful && res.WasRequestSuccessfullyHandled && res.WasSuccessful) ReduceEnergy(1);
            }
        }

        private async void GetTokensData(PlayerAccount account = null)
        {
            if (account == null)
            {
                var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
                PublicKey GoldATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.goldMint);
                PublicKey SilverATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.silverMint);
                RequestResult<ResponseValue<TokenBalance>> goldRes = await rpcClient.GetTokenAccountBalanceAsync(GoldATA);
                RequestResult<ResponseValue<TokenBalance>> silverRes = await rpcClient.GetTokenAccountBalanceAsync(SilverATA);
                SolanaManager.Instance.player.Gold = goldRes.Result.Value.AmountUlong;
                SolanaManager.Instance.player.Silver = silverRes.Result.Value.AmountUlong;
            }
            else
            {
                SolanaManager.Instance.player = account;
            }

            _resourcesManager.UpdateResourceAmount("Gold", SolanaManager.Instance.player.Gold);
            _resourcesManager.UpdateResourceAmount("Silver", SolanaManager.Instance.player.Silver);
            _resourcesManager.UpdateResourceAmount("Energy", SolanaManager.Instance.player.Energy);
        }

        public void Attack()
        {
            animator.SetTrigger("Attack");
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].TakeDamage(currentWeaponLevel == 0 ? 10 : _weapon[currentWeaponLevel]);
            }
        }
        #endregion
    }
}