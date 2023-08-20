use anchor_lang::prelude::*;
use anchor_lang::solana_program::hash::*;
use anchor_spl::{token::{Transfer, TokenAccount, Token, Mint}, associated_token::AssociatedToken};
use gpl_session::{SessionError, SessionToken, session_auth_or, Session};

declare_id!("2sJkpmYD97zezCuFRqYtzfRmDF2F2xnhjtcyNm7zqj7q");

#[program]
pub mod speedrun_anchor 
{
    use super::*;

    pub fn initialize_player(ctx: Context<InitPlayer>, username: String) -> Result<()> 
    {
        let player = &mut ctx.accounts.player;
        let signer = &ctx.accounts.signer;

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

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn add_energy(ctx: Context<AddEnergy>) -> Result<()>
    {
        let house_level = ctx.accounts.player.house_lvl;
        if ctx.accounts.player.energy + house_level > 130 
        {
            ctx.accounts.player.energy = 130;
        }
        else
        {
            ctx.accounts.player.energy = ctx.accounts.player.energy + house_level;
        }
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
#[instruction(username: String)]
pub struct InitPlayer<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,
    
    #[account(init, payer = signer, seeds = [b"PLAYER", signer.key().as_ref()], bump, space = 108 + username.len())]
    pub player: Account<'info, PlayerAccount>,

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

#[derive(Accounts, Session)]
pub struct AddEnergy<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, PlayerAccount>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,
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