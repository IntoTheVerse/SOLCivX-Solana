using System;
using System.Text;
using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine.UI;
using TMPro;
using Solana.Unity.Rpc.Types;
using UnityEngine.SceneManagement;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using SpeedrunAnchor.Program;
using SpeedrunAnchor;
using SpeedrunAnchor.Accounts;
using System.Collections.Generic;
using Solana.Unity.Programs.Models;

public class Authentication : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI publicKeyText;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI sessionBalanceText;
    [SerializeField] private GameObject usernamePanel;
    [SerializeField] private GameObject copyButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TextMeshProUGUI usernameDisplay;

    private void OnEnable()
    {
        Web3.OnLogout += OnLogout;
        SolanaManager.Instance.onLoginFinished += OnLoginFinished;
    }

    private void OnDisable() 
    {
        Web3.OnLogout += OnLogout;
    }

    private void Awake()
    {
        loginButton.onClick.AddListener(() => Login());
        logoutButton.onClick.AddListener(() => Logout());
        playButton.onClick.AddListener(() => OnPlay());
        submitButton.onClick.AddListener(() => OnSubmit());
        Web3.OnBalanceChange += (double balance) => {
            balanceText.text = $"Balance: {balance}";
        };

        if (Web3.Account != null) OnLoginFinished();
    }

    private async void Login()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        await Web3.Instance.LoginWeb3Auth(Provider.GOOGLE);
#else
        await Web3.Instance.LoginWalletAdapter();
#endif
    }

    private async void Logout()
    {
        if (await SolanaManager.Instance.sessionWallet.IsSessionTokenInitialized())
        {
            await SolanaManager.Instance.sessionWallet.PrepareLogout();
            SolanaManager.Instance.sessionWallet.Logout();
        }
        Web3.Instance.Logout();
    }

    private async void OnLoginFinished()
    {
        publicKeyText.text = $"Public Key: {Web3.Account.PublicKey}";
        balanceText.text = $"Balance: {await Web3.Instance.WalletBase.GetBalance(commitment: Commitment.Confirmed)}";
        loginButton.gameObject.SetActive(false);
        logoutButton.gameObject.SetActive(true);
        copyButton.SetActive(true);
        Invoke(nameof(UpdateSessionBalance), 2);
        SolanaManager.Instance.onLoginFinished -= OnLoginFinished;
        SolanaManager.Instance.onPlayerAccountChanged += (account) => TryGetUserProfile(account);
        TryGetUserProfile();
    }

    public void CopyPublicKey()
    {
        Web3.Account.PublicKey.ToString().CopyToClipboard();
    }

    private void OnPlay()
    {
        SceneManager.LoadScene("Game");
    }

    public void OnSetupNewAccount()
    {
        usernamePanel.SetActive(true);
    }

    public async void TryGetUserProfile(PlayerAccount account = null)
    {
        if(SceneManager.GetActiveScene().buildIndex != 0) return;
        if (account == null)
        {
            SolanaManager.Instance.onPlayerAccountChanged -= (account) => TryGetUserProfile(account);
            var speedrunClient = new SpeedrunAnchorClient(Web3.Rpc, Web3.WsRpc, SolanaManager.Instance.programId);
            
            AccountResultWrapper<PlayerAccount> res = null;
            try
            {
                res = await speedrunClient.GetPlayerAccountAsync(SolanaManager.Instance.userProfilePDA);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            if (res == null || res.ParsedResult == null) OnSetupNewAccount();
            else
            {
                usernameDisplay.text = res.ParsedResult.Username;
                SolanaManager.Instance.player = res.ParsedResult;
                playButton.gameObject.SetActive(true);
            }
        }
        else
        {
            usernameDisplay.text = account.Username;
            SolanaManager.Instance.player = account;
            playButton.gameObject.SetActive(true);
        }
    }

    private async void OnSubmit()
    {
        submitButton.interactable = false;

        InitializePlayerAccounts playerAccounts = new()
        {
            Player = SolanaManager.Instance.userProfilePDA,
            Signer = Web3.Account,
            SystemProgram = SystemProgram.ProgramIdKey
        };

        TransactionInstruction ixPlayer = SpeedrunAnchorProgram.InitializePlayer(
            playerAccounts, 
            username.text, 
            SolanaManager.Instance.programId
        );


        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        var goldRes = await Web3.Rpc.GetAccountInfoAsync(AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.goldMint));
        if (!goldRes.WasSuccessful || goldRes.Result?.Value == null)
        {
            TransactionInstruction createGoldATAIx = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(Web3.Account.PublicKey, Web3.Account.PublicKey, SolanaManager.Instance.goldMint);
            tx.Add(createGoldATAIx);
        }

        var silverRes = await Web3.Rpc.GetAccountInfoAsync(AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Account.PublicKey, SolanaManager.Instance.silverMint));
        if (!silverRes.WasSuccessful || silverRes.Result?.Value == null)
        {
            TransactionInstruction createSilverATAIx = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(Web3.Account.PublicKey, Web3.Account.PublicKey, SolanaManager.Instance.silverMint);
            tx.Add(createSilverATAIx);
        }

        tx.Add(ixPlayer);

        RequestResult<string> resTx = await Web3.Wallet.SignAndSendTransaction(tx);
        Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(resTx)}");
        usernamePanel.SetActive(false);
        submitButton.interactable = true;
    }

    private async void UpdateSessionBalance()
    {
        sessionBalanceText.text = $"Session Balance: {await SolanaManager.Instance.sessionWallet.GetBalance(commitment: Commitment.Confirmed)}";
    }

    private void OnLogout()
    {
        loginButton.gameObject.SetActive(true);
        logoutButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        publicKeyText.text = "Public Key: Login to see your PublicKey";
        balanceText.text = "Balance: Login to see your Sol Balance";
        sessionBalanceText.text = "Session Balance: Login to see your Session Sol Balance";
        usernameDisplay.text = "";
        copyButton.SetActive(false);
    }
}