use anchor_lang::prelude::*;
use anchor_lang::solana_program::hash::*;
use anchor_spl::{token::{Transfer, TokenAccount, Token, Mint}, associated_token::AssociatedToken};
use clockwork_sdk::state::Thread;
use anchor_lang::InstructionData;
use anchor_lang::solana_program::{
    instruction::Instruction,
    native_token::LAMPORTS_PER_SOL,
    system_program
};
use gpl_session::{SessionError, SessionToken, session_auth_or, Session};

declare_id!("2sJkpmYD97zezCuFRqYtzfRmDF2F2xnhjtcyNm7zqj7q");

#[program]
pub mod speedrun_anchor 
{
    use super::*;

    pub fn initialize_player(ctx: Context<InitPlayer>, username: String, thread_id: Vec<u8>) -> Result<()> 
    {
        let player = &mut ctx.accounts.player;
        let system_program = &ctx.accounts.system_program;
        let signer = &ctx.accounts.signer;
        let clockwork_program = &ctx.accounts.clockwork_program;
        let thread = &ctx.accounts.thread;
        let thread_authority = &ctx.accounts.thread_authority;

        player.username = username;
        player.authority = signer.key();
        player.house_lvl = 1;
        player.defense_lvl = 1;
        player.armour_lvl = 0;
        player.weapon_lvl = 0;
        player.energy = 130;
        player.xp = 0;
        player.gold = 0;
        player.silver = 0;

        let target_ix = Instruction {
            program_id: ID,
            accounts: crate::accounts::AddEnergy 
            {
                signer: signer.key(),
                player: player.key(),
                thread: thread.key(),
                thread_authority: thread_authority.key(),
            }
            .to_account_metas(Some(true)),
            data: crate::instruction::AddEnergy {}.data(),
        };

        let trigger = clockwork_sdk::state::Trigger::Cron {
            schedule: "*/1 * * * *".into(),
            skippable: false,
        };

        let bump = *ctx.bumps.get("thread_authority").unwrap();
        clockwork_sdk::cpi::thread_create(
            CpiContext::new_with_signer(
                clockwork_program.to_account_info(),
                clockwork_sdk::cpi::ThreadCreate {
                    payer: signer.to_account_info(),
                    system_program: system_program.to_account_info(),
                    thread: thread.to_account_info(),
                    authority: thread_authority.to_account_info(),
                },
                &[&[b"THREAD_AUTHORITY", signer.key().as_ref(), &[bump]]],
            ),
            LAMPORTS_PER_SOL,
            thread_id,     
            vec![target_ix.into()],
            trigger
        )?;

        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn change_house_level(ctx: Context<UpdateLevels>, to: u64) -> Result<()>
    {
        ctx.accounts.player.house_lvl = to;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn change_defense_level(ctx: Context<UpdateLevels>, to: u64) -> Result<()>
    {
        ctx.accounts.player.defense_lvl = to;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn change_armour_level(ctx: Context<UpdateLevels>, to: u64) -> Result<()>
    {
        ctx.accounts.player.armour_lvl = to;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn change_weapon_level(ctx: Context<UpdateLevels>, to: u64) -> Result<()>
    {
        ctx.accounts.player.weapon_lvl = to;
        Ok(())
    }

    pub fn add_energy(ctx: Context<AddEnergy>) -> Result<()>
    {
        let house_level = ctx.accounts.player.house_lvl;
        ctx.accounts.player.energy = ctx.accounts.player.energy + house_level;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn reduce_energy(ctx: Context<UpdateLevels>, reduce_by: u64) -> Result<()>
    {
        ctx.accounts.player.energy = ctx.accounts.player.energy - reduce_by;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn reduce_gold(ctx: Context<ReduceToken>, amount: u64) -> Result<()>
    {
        let transfer_accounts = Transfer {
            from: ctx.accounts.player_ata.to_account_info(),
            to: ctx.accounts.vault_ata.to_account_info(),
            authority: ctx.accounts.signer_wallet.to_account_info(),
        };

        let cpi_ctx = CpiContext::new(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts
        );

        anchor_spl::token::transfer(cpi_ctx, amount)?;

        ctx.accounts.player.gold = ctx.accounts.player.gold - amount;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn add_gold(ctx: Context<AddToken>) -> Result<()>
    {
        let gold_to_add =  get_random_u64(5) + 5;

        let transfer_accounts = Transfer {
            from: ctx.accounts.vault_ata.to_account_info(),
            to: ctx.accounts.player_ata.to_account_info(),
            authority: ctx.accounts.vault_pda.to_account_info(),
        };

        let seeds:&[&[u8]] = &[
            b"Vault",
            &[*ctx.bumps.get("vault_pda").unwrap()]
        ];
        let signer = &[&seeds[..]];

        let cpi_ctx = CpiContext::new_with_signer(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts,
            signer
        );

        anchor_spl::token::transfer(cpi_ctx, gold_to_add)?;

        ctx.accounts.player.gold = ctx.accounts.player.gold + gold_to_add;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn reduce_silver(ctx: Context<ReduceToken>, amount: u64) -> Result<()>
    {
        let transfer_accounts = Transfer {
            from: ctx.accounts.player_ata.to_account_info(),
            to: ctx.accounts.vault_ata.to_account_info(),
            authority: ctx.accounts.signer_wallet.to_account_info(),
        };

        let cpi_ctx = CpiContext::new(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts
        );

        anchor_spl::token::transfer(cpi_ctx, amount)?;

        ctx.accounts.player.silver = ctx.accounts.player.silver - amount;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn add_silver(ctx: Context<AddToken>) -> Result<()>
    {
        let silver_to_add =  get_random_u64(5) + 5;

        let transfer_accounts = Transfer {
            from: ctx.accounts.vault_ata.to_account_info(),
            to: ctx.accounts.player_ata.to_account_info(),
            authority: ctx.accounts.vault_pda.to_account_info(),
        };

        let seeds:&[&[u8]] = &[
            b"Vault",
            &[*ctx.bumps.get("vault_pda").unwrap()]
        ];
        let signer = &[&seeds[..]];

        let cpi_ctx = CpiContext::new_with_signer(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts,
            signer
        );

        anchor_spl::token::transfer(cpi_ctx, silver_to_add)?;

        ctx.accounts.player.silver = ctx.accounts.player.silver + silver_to_add;
        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(username: String, thread_id: Vec<u8>)]
pub struct InitPlayer<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,
    
    #[account(init, payer = signer, seeds = [b"PLAYER", signer.key().as_ref()], bump, space = 108 + username.len())]
    pub player: Account<'info, PlayerAccount>,

    #[account(address = clockwork_sdk::ID)]
    pub clockwork_program: Program<'info, clockwork_sdk::ThreadProgram>,

    /// CHECK: is this the correct account type?
    #[account(mut, address = Thread::pubkey(thread_authority.key(), thread_id))]
    pub thread: AccountInfo<'info>,

    #[account(seeds = [b"THREAD_AUTHORITY", signer.key().as_ref()], bump)]
    pub thread_authority: SystemAccount<'info>,

    #[account(address = system_program::ID)]
    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
pub struct UpdateLevels<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,
    
    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, PlayerAccount>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts)]
pub struct AddEnergy<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,
    
    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, PlayerAccount>,

    #[account(signer, constraint = thread.authority.eq(&thread_authority.key()))]
    pub thread: Account<'info, Thread>,

    #[account(seeds = [b"ENERGY_THREAD".as_ref()], bump)]
    pub thread_authority: SystemAccount<'info>,
}

#[derive(Accounts, Session)]
pub struct AddToken<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, PlayerAccount>,
  
    ///CHECK:
    #[account(seeds=[b"Vault".as_ref()], bump)]
    pub vault_pda: AccountInfo<'info>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = vault_pda)]
    pub vault_ata: Account<'info, TokenAccount>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = player.authority.key())]
    pub player_ata: Account<'info, TokenAccount>,

    pub game_token: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
pub struct ReduceToken<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(mut)]
    pub signer_wallet: Signer<'info>,

    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, PlayerAccount>,
  
    ///CHECK:
    #[account(seeds=[b"Vault".as_ref()], bump)]
    pub vault_pda: AccountInfo<'info>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = vault_pda)]
    pub vault_ata: Account<'info, TokenAccount>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = player.authority.key())]
    pub player_ata: Account<'info, TokenAccount>,

    pub game_token: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>
}

#[account]
pub struct PlayerAccount
{
    pub username: String,
    pub authority: Pubkey,
    pub energy: u64,
    pub xp: u64,
    pub gold: u64,
    pub silver: u64,
    pub house_lvl: u64,
    pub defense_lvl: u64,
    pub armour_lvl: u64,
    pub weapon_lvl: u64,
}

pub fn get_random_u64(max: u64) -> u64 {
    let clock = Clock::get().unwrap();
    let slice = &hash(&clock.slot.to_be_bytes()).to_bytes()[0..8];
    let num: u64 = u64::from_be_bytes(slice.try_into().unwrap());
    let target = num/(u64::MAX/max);
    return target;
}

#[error_code]
pub enum GameErrorCode 
{
    #[msg("Wrong Authority")]
    WrongAuthority,
}