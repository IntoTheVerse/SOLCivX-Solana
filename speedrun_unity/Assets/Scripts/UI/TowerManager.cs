using System.Collections.Generic;
using SimKit;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using SpeedrunAnchor.Accounts;
using SpeedrunAnchor.Program;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public GameObject towerLv1;
    public GameObject towerLv2;
    public GameObject towerLv3;

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

    public async void SetTowerLevel(int level)
    {
        var tx = new Transaction()
        {
            FeePayer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        ChangeDefenseLevelAccounts account = new()
        {
            Signer = SolanaManager.Instance.sessionWallet.Account.PublicKey,
            Player = SolanaManager.Instance.userProfilePDA,
            SessionToken = SolanaManager.Instance.sessionWallet.SessionTokenPDA,
            SystemProgram = SystemProgram.ProgramIdKey
        };

        TransactionInstruction Ix = SpeedrunAnchorProgram.ChangeDefenseLevel(account, (ulong)level, SolanaManager.Instance.programId);
        tx.Add(Ix);

        RequestResult<string> res = await SolanaManager.Instance.sessionWallet.SignAndSendTransaction(tx);
        Debug.Log($"Result: {Newtonsoft.Json.JsonConvert.SerializeObject(res)}");
    }

    private void OnLevelChanged(PlayerAccount player)
    {
        SolanaManager.Instance.player = player;
        int level = (int)player.DefenseLvl;
        
        if (level == 1)
        {
            towerLv1.SetActive(true);
            towerLv2.SetActive(false);
            towerLv3.SetActive(false);
        }
        else if (level == 2)
        {
            towerLv1.SetActive(false);
            towerLv2.SetActive(true);
            towerLv3.SetActive(false);
        }
        else if (level == 3)
        {
            towerLv1.SetActive(false);
            towerLv2.SetActive(false);
            towerLv3.SetActive(true);
        }
    }
}