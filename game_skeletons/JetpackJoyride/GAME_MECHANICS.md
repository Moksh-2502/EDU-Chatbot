# Endless Runner Educational Game - Game Mechanics

## Overview
This educational endless runner combines fast-paced gameplay with learning objectives. Players navigate a continuously scrolling world while answering educational questions and avoiding obstacles.

## Progression System

### Game Stages
The game has 5 distinct progression stages:
1. **Manual Control** (Novice Navigator): Player must press right arrow to move forward
2. **Auto-Run Initial** (Steady Strider): Player moves automatically at a constant speed
3. **Auto-Run Variable Speed** (Velocity Voyager): Player runs automatically with speed that varies based on answer performance
4. **Expert Runner** (Expert Explorer): Faster automatic movement with more challenging questions
5. **Master Runner** (Master Marathoner): Maximum speed with the hardest questions

### Advancement Requirements
- Players must answer a specific number of questions correctly to advance to the next level:
  - Level 1 → 2: 8 correct answers
  - Level 2 → 3: 13 correct answers
  - Level 3 → 4: 21 correct answers
  - Level 4 → 5: 34 correct answers
  - Level 5 completion: 55 correct answers
- Getting 3+ incorrect answers in succession can cause level regression

## Player Controls

### Basic Movement
- **Right Arrow**: Move forward (in Manual Control stage)
- **Up Arrow**: Jump to avoid obstacles or reach items
- **Right Arrow** (in Auto-Run Variable Speed): Temporarily max out speed

### Movement Characteristics
- **Gravity**: 800 (configurable)
- **Jump Strength**: -400 (configurable)
- **Double Jump**: Available, maximum of 2 jumps
- **Fast Fall**: Available when falling, increases gravity to 3500
- **Manual Mode Speed**: 150 pixels/second
- **Auto-Run Base Speed**: 200 pixels/second
- **Speed Range**: 100-400 pixels/second (adjustable by config)
- **Speed Multipliers**: Different for each level (1.0x, 1.2x, 1.5x, 1.8x, 2.0x)

## Health System

### Health Mechanics
- **Starting Health**: 120 HP
- **Maximum Health**: 120 HP
- **Health Gain from Correct Answer**: +10 HP
- **Health Loss from Wrong Answer**: -15 HP
- **Health Gain from Coin**: +1 HP
- **Health Loss from Spike**: -15 HP
- **Health Loss from Missiles**: 10-15 HP (depending on missile type)
- **Health Boost from Missile Score**: +10 HP (when missile score reaches 1.0)

### Game Over
- Health reaching 0 triggers game over
- Final score is saved to leaderboard

## Answer System

### Physical Answer Obstacles
- Answer choices appear as physical obstacles in the game
- Answers spawn to the right of the screen and move toward the player
- Player must collide with the answer they believe is correct

### Answer Positioning
- **Y-Offset**: 220 pixels from player's position (configurable)
- **Spacing**: Random between 450-700 pixels between answer options (configurable)
- **Spawn Delay**: 500ms after previous answers clear (configurable)

### Answer Feedback
- **Visual Feedback**: Text showing health change
- **Color Coding**: Green for correct, red for incorrect
- **Sound Effects**: Different sounds for correct/incorrect answers
- **UI Feedback**: Message showing correct answer if player chose wrong

## Question System

### Question Generation
- Questions come from the FluencyProvider
- Difficulty scales with progression level
- Questions appear at intervals defined by configuration:
  - Manual Control: Every 10 seconds
  - Auto-Run Initial: Every 8 seconds
  - Auto-Run Variable Speed: Every 7 seconds
  - Expert Runner: Every 6 seconds
  - Master Runner: Every 5 seconds

### Question Display
- Current question displays at the top of the screen
- Multiple choice answers appear as physical obstacles
- If API fails, fallback questions are used

## Obstacles and Items

### Spikes
- Drop from birds
- Deal 15 damage on contact
- Created every 5 seconds if enabled

### Coins
- Drop from birds
- Provide +1 HP when collected
- Increase score by 1
- Created every 1 second if enabled

### Missiles
- Travel horizontally toward player
- Two types: high missiles (15 damage) and low missiles (10 damage)
- Can be jumped on to gain missile score
- Frequency decreases in higher levels
- Speed increases in higher levels:
  - Manual Control: Speed 2
  - Auto-Run Initial: Speed 3
  - Auto-Run Variable Speed: Speed 4
  - Expert Runner: Speed 5
  - Master Runner: Speed 6

### Birds
- Fly across screen at configurable speed (-100)
- Created every 2.5-5 seconds
- Drop coins and spikes occasionally

## Environment

### Parallax Scrolling
- **Mountains**: Move at 25% of game speed
- **Plateau**: Move at 50% of game speed
- **Ground**: Moves at 100% of game speed

### Speed Transitions
- Gradual speed increase when transitioning from manual to auto-run (2000ms duration)
- Speed updated in real-time based on player performance in variable speed mode

## Configuration

All game mechanics are configurable through the GameConfig system, allowing for easy adjustment of:
- Feature toggles (birds, rockets, coin drops, answer obstacles)
- Speed settings and progression thresholds
- Health values and penalties
- Question intervals
- Obstacle damage values
- Item creation frequencies
- Player physics properties

## Special Mechanics

### Speed-Up Feature
- In Auto-Run Variable Speed mode, pressing right arrow maxes out speed
- Plays special sound effect
- Useful for advanced players to challenge themselves

### Health Refill on Level Change
- Health refills to maximum when changing progression levels
- Provides fresh start at each new difficulty level

### Missile Score System
- Jumping on missiles grants partial missile score (0.25-0.5)
- When total missile score reaches 1.0, player gains health boost
- Resets after each boost 