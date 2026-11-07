# Floor is Key

**50.033 Midterm Game Jam: The Remix**

- **Team Members:** Foong Jingxuan Janelle
- **Game Title:** Floor is Key
- **Primary Song / Artist:** U Can't Touch This / MC Hammer
- **Remix Track / Artist:** Toxic / Britney Spears

## 1. Theme Interpretation (25%)

- **Interpretation:** How did you interpret your primary song?  
  I interpreted MC Hammer's "U Can't Touch This" through a literal, spatial game lens: the physical floor space is a dangerous arena where touching forbidden areas or colliding with obstacles yields immediate penalties. The core fantasy revolves around precision avoidance and spatial navigation—making the player feel the high-stakes tension of navigating a field where one wrong step ruins the performance.
  
- **Implementation:** Which specific gameplay mechanics reflect this interpretation?  
  Borrowing mechanics from "The Floor is Lava" combined with interactive musical pitch progression, players control Pac-Man on a 5x5 tile grid. Movement is strictly discrete and grid-aligned. Danger tiles ("Lava") spawn dynamically to block direct traversal paths, while a patrolling "Ghost" enemy actively searches the grid. Stepping on a lava tile or colliding with the ghost immediately subtracts a life, interrupts the combo counter, and plays a disharmonious error sound, reinforcing the core rule that hazard tiles simply cannot be touched.

## 2. Gameplay & Player Experience (30%)

- **Objective:** What must the player accomplish? What are the win/lose conditions?  
  The player's goal is to complete a full musical riff sequence by stepping on designated target note tiles while managing time and survival limits.

  - Win Condition: Clear the entire note sequence in Standard Mode, or successfully complete all escalating memory path chunks (4, 6, and 8 notes) in Recall Mode before the time or life pool is exhausted.

  - Lose Condition: Lose all 3 lives due to step errors (stepping on Lava tiles or getting caught by the Ghost enemy) OR allow the 60-second timer to reach zero.

- **Core Mechanics:** What are the main gameplay mechanics?

  1. Discrete Grid Movement & Timing Cooldown: Movement is mapped strictly to WASD. Stepping onto tiles triggers a move timer cooldown to prevent chaotic spamming, forcing intentional movement.

  2. Dynamic Breadth-First Search (BFS) Path Generation: Every target note spawn generates a fresh layout of neutral tiles and lava hazards. A background BFS algorithm (GridPathfinder.cs) runs continuously to guarantee that a valid, unblocked path always exists between Pac-Man and the target tile.

  3. Dual Gameplay Modes:

      - Standard Mode: Visible, real-time hazard pathing where target notes spawn sequentially alongside patrolling ghosts and hazard tiles.

      - Recall Mode: Memory-based sequence playback where the grid is hidden. Players first listen to and watch an automated preview of the melody path across expanding note chunks, then must recreate the exact tile sequence from memory.

- **Fun Factor:** What makes your game fun? What meaningful choices does the player make?  
  The game creates a tight feedback loop between audio gratification and spatial tension. While players move at their own pace rather than adhering to a strict metronomic beat, hitting each correct cyan tile plays an authentic pitch corresponding to the song's melody sequence (AudioManager.cs), making the player actively construct the track note-by-note.

  Players are constantly faced with high-stakes, real-time decisions:

  - Do I take the fastest direct path toward a target note to extend my combo multiplier before time runs out, or take a wider, safer route around lava tiles and the roaming ghost?

  - Do I risk stepping onto a Toxic Melon to trigger an instant 2-tile forward leap and score boost, even though launching forward into unknown tiles increases the chance of accidentally landing on a lava hazard or directly into the ghost?

## 3. Remix Integration (20%)

- **Interpretation:** How did you interpret the remix track?  
I interpreted Britney Spears' "Toxic" by embodying its thematic essence: an irresistible, seductive temptation that comes with a sudden, unpredictable drawback. Mechanically, this translates into a high-risk / high-reward power-up where grabbing an instant benefit forces a sudden physical leap across the grid.
- **Implementation:** What mechanics were added or modified?  
A special Toxic Melon collectible randomly spawns on available neutral grid tiles during gameplay.

  - The Benefit: Stepping on a Toxic Melon awards an instant +300 score bonus, adds +2 to the active combo multiplier, and triggers a temporary movement speed boost (halving move cooldown).

  - The Toxic Consequence / Leap: It immediately triggers a Toxic Leap (PerformToxicLeap), forcibly propelling Pac-Man an extra step forward in his active movement direction.
- **Interaction:** How does the remix change or interact with the original gameplay?  
The Toxic remix disrupts careful, step-by-step path planning. While the double-tile leap and speed boost allow Pac-Man to cover ground faster, the forced extra tile movement can accidentally overshoot safe paths into lava tiles or straight into the ghost if the player does not calculate their leap trajectory accurately.
- **Demonstration:** How can the grader observe the remix mechanic?  
  1. Start a game in Standard Mode.

  2. Traversal on grid tiles has a 30% chance to spawn a Toxic Melon item.

  3. Step onto the Toxic Melon tile.

  4. Notice the instant visual feedback: floating text "+300 COMBO +2!", the onscreen banner "TOXIC OVERDRIVE!", Pac-Man's instant bonus step forward (leap), and the ghost's doubled step frequency.

## 4. Technical Execution (15%)

- **How to Run:**  
  Run the executable file directly.

- **Controls:** List all player controls.  
  Standard W, A, S, D keys and Arrow keys.  
  W / Up Arrow: Move Up  
  A / Left Arrow: Move Left  
  S / Down Arrow: Move Down  
  D / Right Arrow: Move Right

- **Known Bugs / Limitations:**  
None. All pathing, sequence memory clamping, mode transitions, and replay recording routines run smoothly without unhandled exceptions.

## 5. Game Design & Balance (10%)

- **Challenge:** What makes the game challenging?  
  Challenge stems from multi-layered cognitive load:

  1. Time Pressure: The overall 60-second countdown forces constant forward movement.

  2. Spatial Constraints: A 5x5 grid provides tight quarters, especially when 4 tiles are blocked by lava and 1 tile is occupied by a roaming enemy.

  3. Memory & Execution (Recall Mode): In Recall Mode, sequence lengths dynamically escalate from 4 to 6 to 8 notes, requiring memory retention while traversing hidden hazard fields.
- **Balance:** How do you prevent a single strategy from being overwhelmingly effective?  
  - Dynamic Path Validation: Target notes and lava tiles are procedurally generated every step using BFS path validation (GridPathfinder.cs). Players cannot rely on static muscle memory or fixed routes.

  - Enemy AI Avoidance: The ghost algorithm (GridGhost.cs) evaluates surrounding open tiles dynamically while avoiding safe cyan target notes, preventing the ghost from camping on top of the player's objective.
- **Demonstration:** What specific gameplay situation should the grader try to evaluate the challenge and balance?  
  - Test Standard Mode Risk/Reward: Collect a Toxic Melon while positioned close to a row of lava tiles or the ghost. Observe how the instant 2-tile leap and accelerated ghost movement demand instant trajectory planning to avoid landing on a hazard.

  - Test Recall Mode Progression: Select Recall Mode from the Main Menu, choose a preview speed (Slow/Medium/Fast), and play through Chunks 1 and 2. Notice how tile locations hide after the preview phase, requiring memorization of tile coordinates, and verify that Pac-Man safely teleports back to tile 0 of the active chunk upon making an error.

## 6. Credits (Mandatory)

List all external assets, code, tutorials, and AI tools used, with sources. State "None" if not applicable.

- Piano Note Audio Assets:  

  Paris Java Piano WAV Library — https://github.com/parisjava/wav-piano-sound

- Sound Effects:

  - Wrong Step / Error Sound: Pixabay Special Effects — https://pixabay.com/sound-effects/film-special-effects-wronganswer-37702/

  - Level Complete Stinger: QuickSounds Brawl Stars Win FX — https://quicksounds.com/sound/29774/brawl-stars-win-winning-sounds

- UI & Graphics:

  - Neon UI Kit Sampler: RJC Dude on Itch.io — https://rjc-dude.itch.io/neon-ui-kit-sampler

  - Pac-Man Sprite Sheet: The Spriters Resource — https://www.spriters-resource.com/arcade/pacman/asset/52631/

- AI Tools:

  - Gemini

---
