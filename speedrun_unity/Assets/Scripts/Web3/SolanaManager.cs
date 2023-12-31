using System.Collections.Generic;
using UnityEngine;
using System;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using System.Text;
using Frictionless;
using SpeedrunAnchor;
using SolPlay.Scripts.Services;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using Solana.Unity.Rpc.Core.Http;

public class SolanaManager : MonoBehaviour
{
    public static SolanaManager Instance { get; private set; }
    [HideInInspector] public SessionWallet sessionWallet;

    [SerializeField] private string sessionPassword;

    public delegate void OnLoginFinished();
    public delegate void OnLogoutFinished();
    public delegate void OnPlayerAccountChanged(PlayerAccount account);
    public event OnLoginFinished onLoginFinished;
    public event OnLogoutFinished onLogoutFinished;
    public event OnPlayerAccountChanged onPlayerAccountChanged;

    [HideInInspector] public PublicKey programId = new("2sJkpmYD97zezCuFRqYtzfRmDF2F2xnhjtcyNm7zqj7q");
    [HideInInspector] public PublicKey goldMint = new("2jGnShhYzmRM4hx1u4y8tCDjk8yshWiaC86EDqvNSNXT");
    [HideInInspector] public PublicKey silverMint = new("9KRLVyUC4ryLoAijiWewHZiVk1Fv1MGoLVEA4rGSFbuM");
    [HideInInspector] public PublicKey userProfilePDA;
    [HideInInspector] public PublicKey VaultPDA;
    [HideInInspector] public PlayerAccount player;
    private SpeedrunAnchorClient speedrunClient;
    
    private void OnEnable()
    {
        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
    }

    private void OnDisable()
    {
        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnLogin(Account account)
    {
        PublicKey.TryFindProgramAddress(new[]{
            Encoding.UTF8.GetBytes("PLAYER"),
            Web3.Account.PublicKey
            }, programId, out userProfilePDA, out var _);

        PublicKey.TryFindProgramAddress(new[]{
            Encoding.UTF8.GetBytes("Vault")
            }, programId, out VaultPDA, out var _);

        SetupSessionWallet();
        
        speedrunClient = new SpeedrunAnchorClient(Web3.Rpc, Web3.WsRpc, programId);

        Debug.Log("Connect solplay" + Web3.WsRpc.NodeAddress.ToString());
        ServiceFactory.Resolve<SolPlayWebSocketService>().Connect(Web3.WsRpc.NodeAddress.ToString());
    }

    private void OnLogout()
    {
        onLogoutFinished?.Invoke();
    }

    public async void SetupSessionWallet(bool connectWebSocket = true)
    {
        sessionWallet = await SessionWallet.GetSessionWallet(programId, sessionPassword);

        var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
        if (!await sessionWallet.IsSessionTokenInitialized())
        {
            var txSession = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = (await rpcClient.GetLatestBlockHashAsync()).Result.Value.Blockhash
            };

            txSession.Instructions.Add(sessionWallet.CreateSessionIX(true, DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds()));
            txSession.PartialSign(new[] { Web3.Account, sessionWallet.Account });

            var reqRes = await Web3.Wallet.SignAndSendTransaction(txSession, commitment: Commitment.Finalized);
            Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(reqRes)}");
        }

        onLoginFinished?.Invoke();
        if (connectWebSocket) ConnectWebSocket();
    }

    private void ConnectWebSocket()
    {
        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(userProfilePDA, result =>
        {
            var playerData = PlayerAccount.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            onPlayerAccountChanged?.Invoke(playerData);
        });
        InvokeRepeating(nameof(AddEnergy), 60, 60);
    }

    private async void AddEnergy()
    {
        if (player.Energy < 130)
        {
            var tx = new Transaction()
            {
                FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash()
            };

            AddEnergyAccounts energyAccounts = new()
            {
                Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
                Player = userProfilePDA,
                SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA
            };

            tx.Add(SpeedrunAnchorProgram.AddEnergy(energyAccounts, programId));

            RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
            Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");
        }
    }
}