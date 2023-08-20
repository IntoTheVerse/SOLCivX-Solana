using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SpeedrunAnchor;
using SpeedrunAnchor.Program;
using SpeedrunAnchor.Errors;
using SpeedrunAnchor.Accounts;

namespace SpeedrunAnchor
{
    namespace Accounts
    {
        public partial class PlayerAccount
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17019182578430687456UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[] { 224, 184, 224, 50, 98, 72, 48, 236 };
            public static string ACCOUNT_DISCRIMINATOR_B58 => "eb62BHK8YZR";
            public string Username { get; set; }

            public PublicKey Authority { get; set; }

            public ulong Energy { get; set; }

            public ulong Xp { get; set; }

            public ulong Gold { get; set; }

            public ulong Silver { get; set; }

            public ulong HouseLvl { get; set; }

            public ulong DefenseLvl { get; set; }

            public ulong ArmourLvl { get; set; }

            public ulong WeaponLvl { get; set; }

            public static PlayerAccount Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerAccount result = new PlayerAccount();
                offset += _data.GetBorshString(offset, out var resultUsername);
                result.Username = resultUsername;
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Energy = _data.GetU64(offset);
                offset += 8;
                result.Xp = _data.GetU64(offset);
                offset += 8;
                result.Gold = _data.GetU64(offset);
                offset += 8;
                result.Silver = _data.GetU64(offset);
                offset += 8;
                result.HouseLvl = _data.GetU64(offset);
                offset += 8;
                result.DefenseLvl = _data.GetU64(offset);
                offset += 8;
                result.ArmourLvl = _data.GetU64(offset);
                offset += 8;
                result.WeaponLvl = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum SpeedrunAnchorErrorKind : uint
        {
            WrongAuthority = 6000U
        }
    }

    public partial class SpeedrunAnchorClient : TransactionalBaseClient<SpeedrunAnchorErrorKind>
    {
        public SpeedrunAnchorClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerAccount>>> GetPlayerAccountsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp> { new Solana.Unity.Rpc.Models.MemCmp { Bytes = PlayerAccount.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 } };
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerAccount>>(res);
            List<PlayerAccount> resultingAccounts = new List<PlayerAccount>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerAccount.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerAccount>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerAccount>> GetPlayerAccountAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful || res.Result?.Value == null)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerAccount>(res);
            var resultingAccount = PlayerAccount.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerAccount>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerAccountAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerAccount> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerAccount parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerAccount.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializePlayerAsync(InitializePlayerAccounts accounts, string username, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.InitializePlayer(accounts, username, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendChangeHouseLevelAsync(ChangeHouseLevelAccounts accounts, ulong to, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ChangeHouseLevel(accounts, to, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendChangeDefenseLevelAsync(ChangeDefenseLevelAccounts accounts, ulong to, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ChangeDefenseLevel(accounts, to, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendChangeArmourLevelAsync(ChangeArmourLevelAccounts accounts, ulong to, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ChangeArmourLevel(accounts, to, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendChangeWeaponLevelAsync(ChangeWeaponLevelAccounts accounts, ulong to, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ChangeWeaponLevel(accounts, to, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAddEnergyAsync(AddEnergyAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.AddEnergy(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendReduceEnergyAsync(ReduceEnergyAccounts accounts, ulong reduceBy, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ReduceEnergy(accounts, reduceBy, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendReduceGoldAsync(ReduceGoldAccounts accounts, ulong amount, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ReduceGold(accounts, amount, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAddGoldAsync(AddGoldAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.AddGold(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendReduceSilverAsync(ReduceSilverAccounts accounts, ulong amount, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.ReduceSilver(accounts, amount, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAddSilverAsync(AddSilverAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SpeedrunAnchorProgram.AddSilver(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<SpeedrunAnchorErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<SpeedrunAnchorErrorKind>> { { 6000U, new ProgramError<SpeedrunAnchorErrorKind>(SpeedrunAnchorErrorKind.WrongAuthority, "Wrong Authority") }, };
        }
    }

    namespace Program
    {
        public class InitializePlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ChangeHouseLevelAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ChangeDefenseLevelAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ChangeArmourLevelAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ChangeWeaponLevelAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddEnergyAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }
        }

        public class ReduceEnergyAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ReduceGoldAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey SignerWallet { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddGoldAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ReduceSilverAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey SignerWallet { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddSilverAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class SpeedrunAnchorProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayer(InitializePlayerAccounts accounts, string username, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9239203753139697999UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(username, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ChangeHouseLevel(ChangeHouseLevelAccounts accounts, ulong to, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8727904970582392213UL, offset);
                offset += 8;
                _data.WriteU64(to, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ChangeDefenseLevel(ChangeDefenseLevelAccounts accounts, ulong to, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14988983167918676390UL, offset);
                offset += 8;
                _data.WriteU64(to, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ChangeArmourLevel(ChangeArmourLevelAccounts accounts, ulong to, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4605718365617978537UL, offset);
                offset += 8;
                _data.WriteU64(to, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ChangeWeaponLevel(ChangeWeaponLevelAccounts accounts, ulong to, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(5081969189455686760UL, offset);
                offset += 8;
                _data.WriteU64(to, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddEnergy(AddEnergyAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(503249688345018944UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ReduceEnergy(ReduceEnergyAccounts accounts, ulong reduceBy, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(680746695999816967UL, offset);
                offset += 8;
                _data.WriteU64(reduceBy, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ReduceGold(ReduceGoldAccounts accounts, ulong amount, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SignerWallet, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14970667489124571913UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddGold(AddGoldAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(6087875905885643852UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ReduceSilver(ReduceSilverAccounts accounts, ulong amount, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SignerWallet, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4624104584731021557UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddSilver(AddSilverAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(747586403045502456UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }
        }
    }
}