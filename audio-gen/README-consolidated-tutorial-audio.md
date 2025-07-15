# Consolidated Tutorial Audio Generation

This system generates all tutorial audio files for the complete game tutorial experience, covering all tutorial steps with platform-specific instructions.

## Overview

The consolidated tutorial audio system provides step-by-step guidance for the complete tutorial flow:

### Tutorial Steps & Generated Files (12 total)

#### Step 1: Tutorial Start
- **Both Platforms:** "Let's learn how to play"
- **File:** `start.mp3`

#### Step 2: Move Left
- **Desktop:** "Press A or left arrow to move left"
- **Mobile:** "Swipe left to move left"
- **Files:** `move-left-desktop.mp3`, `move-left-mobile.mp3`

#### Step 3: Move Right
- **Desktop:** "Press D or right arrow to move right"
- **Mobile:** "Swipe right to move right"
- **Files:** `move-right-desktop.mp3`, `move-right-mobile.mp3`

#### Step 4: Jump
- **Desktop:** "Press W or up arrow to jump"
- **Mobile:** "Swipe up to jump"
- **Files:** `jump-desktop.mp3`, `jump-mobile.mp3`

#### Step 5: Slide
- **Desktop:** "Press S or down arrow to slide"
- **Mobile:** "Swipe down to slide"
- **Files:** `slide-desktop.mp3`, `slide-mobile.mp3`

#### Step 6: Answer Selection
- **Desktop:** "Click a button or use arrow keys and Enter to select your answer"
- **Mobile:** "Tap a button to select your answer"
- **Files:** `answer-selection-desktop.mp3`, `answer-selection-mobile.mp3`

#### Step 7: Tutorial Complete
- **Both Platforms:** "Congratulations! You've finished the tutorial"
- **File:** `complete.mp3`

## File Location

All files are generated in:
`E:\Crossover\AIProjects\ai-edu-chatbot\game_skeletons\SubwaySurfers\Assets\Sounds\tutorial\`

## Generation

To generate or regenerate all tutorial audio files:

```bash
npm run generate-tutorial
```

## Implementation Notes

- **Consolidated approach**: Single script replaces separate control and popup tutorial generators
- **Platform-specific guidance**: Desktop vs mobile instructions for each action
- **Voice consistency**: Uses the same ElevenLabs voice as other game audio
- **Complete tutorial flow**: Covers entire tutorial sequence from start to finish
- **Smart skipping**: Won't regenerate files that already exist
- **Rate limiting**: Includes 500ms delays between API calls

## Total Files Generated

- 2 shared files (start, complete)
- 10 platform-specific files (5 actions × 2 platforms each)
- **Total: 12 files**

## Example Usage in Game Code

```javascript
// Tutorial state management
class TutorialManager {
  constructor() {
    this.currentStep = 0;
    this.isMobile = detectMobile();
    this.steps = [
      'start',
      'move-left',
      'move-right', 
      'jump',
      'slide',
      'answer-selection',
      'complete'
    ];
  }
  
  playCurrentStepAudio() {
    const step = this.steps[this.currentStep];
    let audioFile;
    
    // Steps that are the same for both platforms
    if (step === 'start' || step === 'complete') {
      audioFile = `${step}.mp3`;
    } else {
      // Platform-specific steps
      const platform = this.isMobile ? 'mobile' : 'desktop';
      audioFile = `${step}-${platform}.mp3`;
    }
    
    playAudio(audioFile);
  }
  
  nextStep() {
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
      this.playCurrentStepAudio();
    }
  }
  
  // Usage examples for specific steps
  playMovementInstructions() {
    const platform = this.isMobile ? 'mobile' : 'desktop';
    
    playAudio(`move-left-${platform}.mp3`);
    // Wait for player to move left...
    
    playAudio(`move-right-${platform}.mp3`);
    // Wait for player to move right...
    
    playAudio(`jump-${platform}.mp3`);
    // Wait for player to jump...
    
    playAudio(`slide-${platform}.mp3`);
    // Wait for player to slide...
  }
}
```

## File Structure

```
E:\Crossover\AIProjects\ai-edu-chatbot\game_skeletons\SubwaySurfers\Assets\Sounds\tutorial\
├── start.mp3
├── move-left-desktop.mp3
├── move-left-mobile.mp3
├── move-right-desktop.mp3
├── move-right-mobile.mp3
├── jump-desktop.mp3
├── jump-mobile.mp3
├── slide-desktop.mp3
├── slide-mobile.mp3
├── answer-selection-desktop.mp3
├── answer-selection-mobile.mp3
└── complete.mp3
```

## Control Mappings

| Action | Desktop Controls | Mobile Controls |
|--------|------------------|-----------------|
| Move Left | A or Left Arrow | Swipe Left |
| Move Right | D or Right Arrow | Swipe Right |
| Jump | W or Up Arrow | Swipe Up |
| Slide | S or Down Arrow | Swipe Down |
| Answer Selection | Click or Arrow Keys + Enter | Tap |

## Integration Benefits

- **Complete tutorial coverage**: All tutorial steps in one cohesive system
- **Platform awareness**: Automatic detection and appropriate instruction delivery
- **Consistent experience**: Same voice and quality across all tutorial audio
- **Easy maintenance**: Single script to manage all tutorial audio generation
- **Scalable**: Easy to add new tutorial steps or modify existing ones 