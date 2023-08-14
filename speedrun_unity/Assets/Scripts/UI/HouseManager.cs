using System.Collections.Generic;
using SimKit;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using UnityEngine;

public class HouseManager : MonoBehaviour
{
    public GameObject houseLv1;
    public GameObject houseLv2;
    public GameObject houseLv3;

    public void Awake()
    {
        TileManager.OnInitializePlayer += OnInitialize;
        SolanaManager.Instance.onPlayerAccountChanged += (account) => OnLevelChanged(account);
    }

    private void OnInitialize(TileData[] tileDatas)
    {
        TileManager.OnInitializePlayer -= OnInitialize;
        TileData tile = tileDatas.GetRandom();
        while (!tile.selfInfo.Walkable)
        {
            tile = tileDatas.GetRandom();
        }

        transform.position = tile.transform.position + new Vector3(0, tile.selfInfo.Height / 2, 0);
    }

    public async void SetHouseLevel(int level)
    {
        var tx = new Transaction()
        {
            FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        ChangeHouseLevelAccounts account = new()
        {
            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Player = SolanaManager.Instance.userProfilePDA,
            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
            SystemProgram = SystemProgram.ProgramIdKey
        };

        TransactionInstruction Ix = SpeedrunAnchorProgram.ChangeHouseLevel(account, (ulong)level, SolanaManager.Instance.programId);
        tx.Add(Ix);

        RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
        Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");
    }

    private void OnLevelChanged(PlayerAccount player)
    {
        SolanaManager.Instance.player = player;
        int level = (int)player.HouseLvl;

        if (level == 1)
        {
            houseLv1.SetActive(true);
            houseLv2.SetActive(false);
            houseLv3.SetActive(false);
        }
        else if (level == 2)
        {
            houseLv1.SetActive(false);
            houseLv2.SetActive(true);
            houseLv3.SetActive(false);
        }
        else if (level == 3)
        {
            houseLv1.SetActive(false);
            houseLv2.SetActive(false);
            houseLv3.SetActive(true);
        }
    }
}