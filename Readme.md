# [Game Title]

**50.033 Midterm Game Jam: The Remix**

- **Team Members:** Foong Jingxuan Janelle
- **Game Title:** Floor is Key
- **Primary Song / Artist:** U Can't Touch This / MC Hammer
- **Remix Track / Artist:**

## 1. Theme Interpretation (25%)

- **Interpretation:** How did you interpret your primary song?  
  I interpreted the song literally as "U can't touch this," where touching forbidden areas results in penalties and lost lives.
- **Implementation:** Which specific gameplay mechanics reflect this interpretation?  
  My inspiration came from "The Floor is Lava," where players must navigate grid tiles while avoiding dynamic red danger tiles to stay alive.

## 2. Gameplay & Player Experience (30%)

- **Objective:** What must the player accomplish? What are the win/lose conditions?  
  Complete the entire song riff within 3 lives and under 60 seconds.
  Win condition: Successfully finish the note sequence before time runs out or lives are depleted.
  Lose condition: Touch 3 danger tiles (lose all 3 lives) or fail to finish within 60 seconds.
- **Core Mechanics:** What are the main gameplay mechanics?

1. Measurement (calculating grid distance and movement path to avoid hazards while hitting targets)
2. Accuracy (precise WASD grid navigation onto target note tiles under timed pressure)

- **Fun Factor:** What makes your game fun? What meaningful choices does the player make?  
  Inspired by Piano Tiles, each valid step on a target tile plays the next note in the track's piano riff. Combining this interactive audio feedback with Floor is Lava grid navigation creates dynamic tension—players must weigh taking direct, high-risk paths to build up their note streak and combo multiplier against taking cautious detours around emerging hazard tiles.

## 3. Remix Integration (20%)

- **Interpretation:** How did you interpret the remix track?
- **Implementation:** What mechanics were added or modified?
- **Interaction:** How does the remix change or interact with the original gameplay?
- **Demonstration:** How can the grader observe the remix mechanic?

## 4. Technical Execution (15%)

- **How to Run:** Instructions to launch the game.
- **Controls:** List all player controls.  
  Standard W, A, S, D keys.  
  W: Move Up  
  A: Move Left  
  S: Move Down  
  D: Move Right
- **Known Bugs / Limitations:** List all known issues, including steps to reproduce significant bugs. State "None" if there are no known issues.

## 5. Game Design & Balance (10%)

- **Challenge:** What makes the game challenging?  
  The strict 60-second time limit coupled with spatial restriction and dynamic danger tile generation near the player's path.
- **Balance:** How do you prevent a single strategy from being overwhelmingly effective?  
- **Demonstration:** What specific gameplay situation should the grader try to evaluate the challenge and balance?  
  Try maintaining a continuous note streak to build a high combo multiplier while navigating toward the next target tile, and observe how dynamic danger tile spawns cut off direct paths and force real-time route adjustments without breaking your combo.

## 6. Credits (Mandatory)

List all external assets, code, tutorials, and AI tools used, with sources. State "None" if not applicable.

- Piano Notes: https://github.com/parisjava/wav-piano-sound
- Sound Effects: https://pixabay.com/sound-effects/film-special-effects-wronganswer-37702/, https://quicksounds.com/sound/29774/brawl-stars-win-winning-sounds 
- Neon UI Kit: https://rjc-dude.itch.io/neon-ui-kit-sampler
- Pacman Game Art: https://www.spriters-resource.com/arcade/pacman/asset/52631/
- Gemini

---

**Instructions:**

- All sections and fields are mandatory.
- Keep responses concise (1–2 sentences per field), except setup instructions, controls, bugs, and credits.
- Describe implemented features, not planned features.
- Ensure all described mechanics can be demonstrated in the submitted game.
