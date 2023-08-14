using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class NPC : MonoBehaviour, ITurnTakeable, IDamageable
    {
        [HideInInspector] public TileData currentTile;

        [Header("Movement")]
        [SerializeField] private bool snapToTargetWhenHidden;
        [SerializeField] private int movementPerTurn;
        [SerializeField] private Animator animator;

        [Header("Lerp Movement")]
        [SerializeField] private bool lerpToTarget;
        [SerializeField] private float timeToLerpToTarget;

        [Header("Health")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private float maxHealth;

        [Header("Misc")]
        [SerializeField] private GameObject playerModel;

        private List<TileData> _currentPath = new();
        private List<TileData> _pathToTravel = new();
        private float _timePassedSinceLerping;
        private TileData _nextTile;
        private Vector3 _targetPos;
        private Vector3 _initSelfPos;
        private bool _isNewPath;
        private bool _updatePos;
        private bool _isHidden = true;
        private float _health;
        private Player player;
        private bool inRange;
        private bool isDead = false;

        private void Awake()
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
            _health = maxHealth;
            //TileManager.OnInitializePlayer += Instance_OnInitializePlayer;
        }

        public void Instance_OnInitializePlayer(TileData[] tileDatas)
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
            player = FindObjectOfType<Player>();
            GetNewPath();
            CallOnTurn();
        }

        private void GetNewPath()
        {
            _currentPath = PathFinder.FindPath(currentTile, player.currentTile);
            _isNewPath = true;
            Invoke(nameof(GetNewPath), 5);
        }

        public void OnTurn(int currentTurn, out float timeToExecute)
        {
            if (_currentPath.Count <= 0) GetNewPath();
            HandleMovementPath();
            if (_isNewPath) _pathToTravel.RemoveAt(0);
            if (_pathToTravel.Count > 0)
            {
                if(_isHidden) SelfReveal();
                if (snapToTargetWhenHidden && _isHidden)
                {
                    _targetPos = _nextTile.transform.position + new Vector3(0, _nextTile.selfInfo.Height / 2, 0);
                    transform.position = _targetPos;
                    timeToExecute = 0;
                }
                else
                {
                    if (lerpToTarget)
                    {
                        StartCoroutine(TravelPath());
                        timeToExecute = _pathToTravel.Count * timeToLerpToTarget;
                    }
                    else
                    {
                        _targetPos = _nextTile.transform.position + new Vector3(0, _nextTile.selfInfo.Height / 2, 0);
                        transform.position = _targetPos;
                        timeToExecute = 0;
                    }
                }
            }
            else
                timeToExecute = 0;
            _isNewPath = false;
            Invoke(nameof(CallOnTurn), 0.5f);
        }

        private void CallOnTurn()
        {
            OnTurn(0, out float _);
        }

        private IEnumerator TravelPath()
        {
            for (int i = 0; i < _pathToTravel.Count; i++)
            {
                _initSelfPos = transform.position;
                _targetPos = _pathToTravel[i].transform.position + new Vector3(0, _pathToTravel[i].selfInfo.Height / 2, 0);
                _timePassedSinceLerping = 0;
                if (i > 0) _pathToTravel[i - 1] = null;

                _updatePos = true;
                yield return new WaitForSeconds(timeToLerpToTarget);
                _updatePos = false;
            }
            if (!_isHidden) SelfReveal();
            _pathToTravel.Clear();
        }

        private void HandleMovementPath()
        {
            _pathToTravel.Clear();
            if (_currentPath == null || _currentPath.Count <= 0)
            {
                _nextTile = null;
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
            for (int i = 0; i < tilesToRemove; i++)
            {
                _pathToTravel.Add(_currentPath[0]);
                _currentPath.RemoveAt(0);
            }
        }

        private void SelfReveal()
        {
            if (currentTile.fow == null && currentTile.isVisible) _isHidden = false;
            else _isHidden = true;
            playerModel.SetActive(!_isHidden);
        }

        private void Update()
        {
            if (_updatePos)
            {
                animator.SetBool("Walk", true);
                _timePassedSinceLerping += Time.deltaTime;
                transform.position = Vector3.Lerp(_initSelfPos, _targetPos, _timePassedSinceLerping.Remap(0, timeToLerpToTarget, 0, 1));
            }
            else
                animator.SetBool("Walk", false);
        }

        public void TakeDamage(float damage)
        {
            if(isDead) return;
            _health -= damage;
            healthSlider.value = _health;
            if (_health <= 0)
            {
                currentTile.playersOnTop.Remove(gameObject);
                GameManager.instance.RemoveTurnExecution(GetComponent<TurnExecution>());
                player.OnNPCDead(this);
                isDead = true;
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Player>(out Player player))
            {
                inRange = true;
                Attack();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<Player>(out Player player))
            {
                inRange = false;
            }
        }

        private void Attack()
        {
            player.TakeDamage();
            if(inRange) Invoke(nameof(Attack), 7);
        }
    }

}