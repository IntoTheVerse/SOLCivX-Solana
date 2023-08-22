# Solana_Speedrun_SolCivX

<h1 align="center">SOLCivX</h1>

<p align="center">
  <a href="https://youtu.be/IEfSIgAxVZg">
    <img src="https://github.com/memxor/Solana_Speedrun_SolCivX/assets/43913734/2b638b92-ca4c-4a02-9a87-efac4ce4eb4e" alt="Logo" >
  </a>
</p>

**SOLCivX** is a **fully on-chain**  _turn-based civilization strategy_ game about controlling the territory, fighting enemy tribes, discovering new lands and mastering new technologies, and growing your civilization, built on Solana and #OPOS (Only Possible on Solana). 

You take on the role of a Tribal Chief and attempt to build a civilization in a turn-based strategy of competition with the other tribes.

### verify our smart contract deployed to solana devnet
[MMXR4v5L8AtgGeAqQRyz9oCtmQRabQYFZH8bPenGyDE](https://explorer.solana.com/address/MMXR4v5L8AtgGeAqQRyz9oCtmQRabQYFZH8bPenGyDE?cluster=devnet)

  <h6>Built at OPOS by Prasanta B. and Elio Lopes</h6>
  
</p>
  <p>View the project demo on <a href="https://youtu.be/IEfSIgAxVZg">YouTube</a></p>
</p>

### Built on Solana with

1. Anchor/Solana
2. Rust
3. Unity
4. MagicBlock Solana Unity SDK
5. Clockwork
6. Gum Session Keys

### Game Overview
- The game has a 180 second Ambush period. After the Ambush period expires, the player is faced with attacks from the enemy tribe. Therefor, during the 180s ambush period, the player needs to fortify his possessions in his civilisation by upgrading his armor, house, walls, and weaponry. 
- Every move (left, right, up, down) is enabled using A* pathfinding algorithm and expends 1 Energy on chain
- Upgrading your possessions (armor, house, walls, and weaponry) expends fixed number of energy, gold and silver resources depending on the level of the possession (level1, level 2, and level 3)
- Solana Blockchian enables instant Finality and processes every transaction in a fraction of a millisecond, and updates the client, so the game does not need a web2 backend/database at all.
- Every action happening on chain ensures, verifiability and authenticity with every move taken by the player
- This is a game of skills, brain-power and optimisation. You cannot overspend, and underdeliver. You really need to be on your toes when you fortify your possessions, and attack your enemies

### Rust Program description

[Refer to the Rust Program (game-backend) here lib.rs](https://github.com/memxor/Solana_Speedrun_SolCivX/blob/main/speedrun_anchor/programs/speedrun_anchor/src/lib.rs)

- Every action in the game is fully on-chain. Therefore, Gum protocol session keys play a key role in enabling transaction/signature abstraction, so that we do not keep calling the wallet in-game. This solves web3 UX in-game and makes it feel as seamless as a web3 game.
- Solana Unity SDK by MagicBlock, with embedded authentication using web3 auth spins up a non-custodial wallet for the player after signing up with email.
- When a player joins, it creates the user profile and sets the default values, and also sets up the on-chain automated clockwork thread that calls the add energy function every 5 mins.
- Add Gold - When you mine gold, you add gold to your wallet
- Add Silver - When you mine silver, you add silver to your wallet
- Remove Gold - When you upgrade(house, weapon, defense, armour), to spend gold
- Remove Silver - When you upgrade(house, weapon, defense, armour) to spend silver
- Change House Level - When you upgrade a house, the level changes
- Change Weapon Level -  When you upgrade a weapon, the level changes
- Change Defense Level - When you upgrade a defense, the level changes                                 
- Change Armour Level - When you upgrade an armor, the level changes




