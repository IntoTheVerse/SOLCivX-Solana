using System.Collections.Generic;
using SimKit;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using UnityEngine;

public class ArmourManager : MonoBehaviour
{
    public void Awake()
    {
        SolanaManager.Instance.onPlayerAccountChanged += (account) => OnLevelChanged(account);
    }

    public async void SetArmourLevel(int level)
    {
        var tx = new Transaction()
        {
            FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        ChangeArmourLevelAccounts account = new()
        {
            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Player = SolanaManager.Instance.userProfilePDA,
            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
            SystemProgram = SystemProgram.ProgramIdKey
        };

        TransactionInstruction Ix = SpeedrunAnchorProgram.ChangeArmourLevel(account, (ulong)level, SolanaManager.Instance.programId);
        tx.Add(Ix);

        RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
        Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");
    }

    private void OnLevelChanged(PlayerAccount player)
    {
        SolanaManager.Instance.player = player;
        FindObjectOfType<Player>().currentArmourLevel = (int)player.ArmourLvl;
    }
}