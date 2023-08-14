using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class RTListItem : MonoBehaviour
    {
        public Image ItemImage;
        public TextMeshProUGUI ItemName;
        public TextMeshProUGUI ItemDescription;
        public ResearchOrder researchOrder;
        public Transform RequiredResSpawnTrans;
        public ListItemReqRes RequiredResPrefab;
        public Sprite turns;
        public UILineRenderer lineRendererPrefab;
        public Color defaultColor;
        public Color researchingColor;
        public Color researchedColor;
        public Action onResearchComplete;

        [HideInInspector] public ResearchTreeItem[] requiredResearchedItems;
        [HideInInspector] public int listPos;
        [HideInInspector] public bool isResearched = false;
        [HideInInspector] public Image BGImage;

        private int _turnsLeftToCompleteResearch;
        private string _description;
        private Dictionary<string, float> _resourcesRequired;
        private RTListItem[] _preResReq;
        private Dictionary<RTListItem, List<UILineRenderer>> spawnedLineRenderers = new();
        private Transform _uiLineSpawnTrans;
        private ResearchTreeUIManager _rtUIManager;
        private RectTransform rectTras;
        private bool _resourcesDeducted;
        private ResourcesManager _resourcesManager;
        private List<ListItemReqRes> _listItemReqRes = new();
        private bool _verticalListing;
        private TooltipTrigger _tooltipTrigger;

        private void OnEnable()
        {
            if (_resourcesDeducted) StartCoroutine(ForceUpdateTransform());
        }

        private IEnumerator ForceUpdateTransform()
        {
            RequiredResSpawnTrans.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            RequiredResSpawnTrans.gameObject.SetActive(true);
        }

        public void SetValues(ResearchTreeItem RTI, Dictionary<string, float> reqResInfo, ResearchTreeUIManager rtUIManager, int listPos, bool verticalListing)
        {
            BGImage = GetComponent<Image>();
            GetComponent<Button>().onClick.AddListener(() => OnItemClicked());
            _resourcesManager = FindObjectOfType<ResourcesManager>();
            _tooltipTrigger = GetComponent<TooltipTrigger>();

            _verticalListing = verticalListing;
            ItemImage.sprite = RTI.RTItemSprite;
            ItemName.text = RTI.RTItemName;
            ItemDescription.text = RTI.RTItemDescription;
            _turnsLeftToCompleteResearch = RTI.TurnsRequiredToResearch;
            requiredResearchedItems = RTI.PreResearchedItemsRequired;
            _resourcesRequired = reqResInfo;
            _rtUIManager = rtUIManager;
            _uiLineSpawnTrans = rtUIManager.GetComponentInChildren<ScrollRect>().content.GetChild(0);
            this.listPos = listPos;

            ListItemReqRes resReq = Instantiate(RequiredResPrefab, RequiredResSpawnTrans);
            resReq.SetValues(turns, _turnsLeftToCompleteResearch);
            _listItemReqRes.Add(resReq);

            foreach (var item in _resourcesRequired)
            {
                resReq = Instantiate(RequiredResPrefab, RequiredResSpawnTrans);
                resReq.SetValues(_resourcesManager.GetResourceSprite(item.Key), item.Value);
                _listItemReqRes.Add(resReq);
            }
            FindObjectOfType<BuildMenu>().SpawnBuildItem(this);
            if(RTI.RTItemName.Contains("House") && RTI.RTItemName.Contains("1")) OnResearchCompleted();
            if (RTI.RTItemName.Contains("Defence") && RTI.RTItemName.Contains("1")) OnResearchCompleted();
            SolanaManager.Instance.onPlayerAccountChanged += (account) => OnAccountUpdate(account);
        }

        public void SetRequiredResearches(RTListItem[] reqRes)
        {
            rectTras = GetComponent<RectTransform>();
            _preResReq = reqRes;
            SetLines();
        }

        private void SetLines()
        {
            foreach (var item in _preResReq)
            {
                Vector2 targetPos = transform.position - item.transform.position;
                List<UILineRenderer> lineRenderer = new();
                UILineRenderer renderer = Instantiate(lineRendererPrefab, item.transform);

                if (!_verticalListing)
                {
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(Vector2.zero, new Vector2(targetPos.x - 125 - (rectTras.sizeDelta.x / 2), 0), targetPos.y < 0 ? 45f : -45f);
                    lineRenderer.Add(renderer);

                    renderer = Instantiate(lineRendererPrefab, item.transform);
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(new Vector2(targetPos.x - 125 - (rectTras.sizeDelta.x / 2), 0), new Vector2(targetPos.x - 125 - (rectTras.sizeDelta.x / 2), targetPos.y), targetPos.y < 0 ? -45f : 45f);
                    lineRenderer.Add(renderer);


                    renderer = Instantiate(lineRendererPrefab, item.transform);
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(new Vector2(targetPos.x - 125 - (rectTras.sizeDelta.x / 2), targetPos.y), new Vector2(targetPos.x, targetPos.y), targetPos.y < 0 ? 45f : -45f);
                    lineRenderer.Add(renderer);
                }
                else
                {
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(Vector2.zero, new Vector2(0, targetPos.y + 60 + (rectTras.sizeDelta.y / 2)), targetPos.x < 0 ? 45f : -45f);
                    lineRenderer.Add(renderer);

                    renderer = Instantiate(lineRendererPrefab, item.transform);
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(new Vector2(0, targetPos.y + 60 + (rectTras.sizeDelta.y / 2)), new Vector2(targetPos.x, targetPos.y + 60 + (rectTras.sizeDelta.y / 2)), targetPos.x < 0 ? -45f : 45f);
                    lineRenderer.Add(renderer);


                    renderer = Instantiate(lineRendererPrefab, item.transform);
                    renderer.transform.SetParent(_uiLineSpawnTrans);
                    renderer.DrawLine(new Vector2(targetPos.x, targetPos.y + 60 + (rectTras.sizeDelta.y / 2)), new Vector2(targetPos.x, targetPos.y), targetPos.x < 0 ? 45f : -45f);
                    lineRenderer.Add(renderer);
                }

                spawnedLineRenderers.Add(item, lineRenderer);
            }
        }

        private void OnItemClicked()
        {
            if (isResearched) return;
            if (_rtUIManager.itemsUnderResearch.Count > 0)
            {
                foreach (var item in _rtUIManager.itemsUnderResearch)
                {
                    item.researchOrder.gameObject.SetActive(false);
                    if (!item.isResearched) item.BGImage.color = item.defaultColor;
                }
            }

            _rtUIManager.itemsUnderResearch.Clear();
            _rtUIManager.itemsUnderResearch = GetUnresearchedItems();
            _rtUIManager.itemsUnderResearch.Add(this);

            _rtUIManager.itemsUnderResearch[0].BGImage.color = researchingColor;
            for (int i = 0; i < _rtUIManager.itemsUnderResearch.Count; i++)
            {
                RTListItem item = _rtUIManager.itemsUnderResearch[i];
                item._listItemReqRes[0].Amount.text = $"{item._turnsLeftToCompleteResearch}";
                item.researchOrder.researchOrder.text = $"{i + 1}";
                item.researchOrder.gameObject.SetActive(true);
            }
        }

        public List<RTListItem> GetUnresearchedItems()
        {
            List<RTListItem> items = new();
            foreach (var item in _preResReq)
            {
                if (!item.isResearched)
                {
                    List<RTListItem> parentItems = item.GetUnresearchedItems();
                    parentItems.Add(item);
                    items.AddRange(parentItems);
                }
            }
            return items;
        }

        public void UpdateRenderLinesOfRequiredItems(RTListItem item)
        {
            if (spawnedLineRenderers.TryGetValue(item, out List<UILineRenderer> renderLines))
            {
                foreach (var lineRenderer in renderLines)
                {
                    lineRenderer.color = researchingColor;
                }
            }
        }

        public async Task<bool> OnTick()
        {
            if (!_resourcesDeducted)
            {
                foreach (var item in _resourcesRequired)
                {
                    if (_resourcesManager.GetResource(item.Key) < item.Value) return false;
                }
                _resourcesDeducted = true;

                var tx = new Transaction()
                {
                    FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                    Instructions = new List<TransactionInstruction>(),
                    RecentBlockHash = await Web3.BlockHash()
                };

                foreach (var item in _resourcesRequired)
                {
                    if (item.Key == "Silver")
                    {
                        PublicKey VaultATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(SolanaManager.Instance.VaultPDA, SolanaManager.Instance.silverMint);
                        PublicKey PlayerATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.silverMint);

                        ReduceSilverAccounts accounts = new()
                        {
                            Player = SolanaManager.Instance.userProfilePDA,
                            VaultPda = SolanaManager.Instance.VaultPDA,
                            VaultAta = VaultATA,
                            PlayerAta = PlayerATA,
                            GameToken = SolanaManager.Instance.silverMint,
                            TokenProgram = TokenProgram.ProgramIdKey,
                            AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                            SignerWallet = Web3.Account.PublicKey,
                            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                            SystemProgram = SystemProgram.ProgramIdKey
                        };

                        tx.Add(SpeedrunAnchorProgram.ReduceSilver(accounts, (ulong)item.Value, SolanaManager.Instance.programId));
                    }

                    if (item.Key == "Gold")
                    {
                        PublicKey VaultATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(SolanaManager.Instance.VaultPDA, SolanaManager.Instance.goldMint);
                        PublicKey PlayerATA = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.goldMint);

                        ReduceGoldAccounts accounts = new()
                        {
                            Player = SolanaManager.Instance.userProfilePDA,
                            VaultPda = SolanaManager.Instance.VaultPDA,
                            VaultAta = VaultATA,
                            PlayerAta = PlayerATA,
                            GameToken = SolanaManager.Instance.goldMint,
                            TokenProgram = TokenProgram.ProgramIdKey,
                            AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey,
                            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                            SignerWallet = Web3.Account.PublicKey,
                            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                            SystemProgram = SystemProgram.ProgramIdKey
                        };

                        tx.Add(SpeedrunAnchorProgram.ReduceGold(accounts, (ulong)item.Value, SolanaManager.Instance.programId));
                    }

                    if (item.Key == "Energy")
                    {
                        ReduceEnergyAccounts accounts = new()
                        {
                            Player = SolanaManager.Instance.userProfilePDA,
                            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
                            SystemProgram = SystemProgram.ProgramIdKey
                        };

                        tx.Add(SpeedrunAnchorProgram.ReduceEnergy(accounts, (ulong)item.Value, SolanaManager.Instance.programId));
                    }
                }

                List<Account> signers = new() {SolanaManager.Instance.sessionWallet.Account, Web3.Account};
                tx.Sign(signers);
                
                RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
                Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");

                for (int i = _listItemReqRes.Count - 1; i > 0; i--)
                {
                    Destroy(_listItemReqRes[i].gameObject);
                    _listItemReqRes.RemoveAt(i);
                }
            }

            _turnsLeftToCompleteResearch--;
            if (_turnsLeftToCompleteResearch <= 0)
            {
                OnResearchCompleted();
                return true;
            }
            else
            {
                _tooltipTrigger.SetContentAndHeader($"Turns Required : {_turnsLeftToCompleteResearch}");
                _listItemReqRes[0].Amount.text = $"{_turnsLeftToCompleteResearch}";
                return false;
            }
        }

        private void OnAccountUpdate(PlayerAccount player)
        {
            SolanaManager.Instance.player = player;
            _resourcesManager.UpdateResourceAmount("Gold", SolanaManager.Instance.player.Gold);
            _resourcesManager.UpdateResourceAmount("Silver", SolanaManager.Instance.player.Silver);
            _resourcesManager.UpdateResourceAmount("Energy", SolanaManager.Instance.player.Energy);
        }

        private void OnResearchCompleted()
        {
            isResearched = true;
            onResearchComplete?.Invoke();
            BGImage.color = researchedColor;
            RequiredResSpawnTrans.parent.gameObject.SetActive(false);
            researchOrder.gameObject.SetActive(false);
            _tooltipTrigger.SetContentAndHeader("Research Completed");

            for (int i = 1; i < _rtUIManager.itemsUnderResearch.Count; i++)
            {
                RTListItem item = _rtUIManager.itemsUnderResearch[i];
                item.researchOrder.researchOrder.text = $"{i}";
            }

            foreach (var lineRenderer in spawnedLineRenderers)
            {
                foreach (var line in lineRenderer.Value)
                {
                    line.color = researchedColor;
                }
            }
        }
    }
}