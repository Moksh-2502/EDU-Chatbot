# Context-Aware Distractor Generation System

## Overview

This system replaces the previous random ±5 distractor generation with an intelligent, educationally-focused approach that creates meaningful incorrect answer choices to help students learn from common multiplication mistakes.

## Architecture

### Core Components

- **IDistractorStrategy**: Interface defining the contract for all distractor generation strategies
- **BaseDistractorStrategy**: Abstract base class with common functionality
- **ContextAwareDistractorGenerator**: Main orchestrator that combines multiple strategies
- **DistractorContext**: Provides context information for strategy decisions
- **DistractorGenerationConfig**: Configuration for strategy weights and settings

### Strategy Pattern Implementation

The system uses the Strategy pattern with three main strategies:

1. **FactorVariationStrategy** (40% default weight)
2. **ArithmeticErrorStrategy** (30% default weight)
3. **TableConfusionStrategy** (20% default weight)

## Distractor Strategies

### 1. FactorVariationStrategy

Generates distractors by varying the factors in multiplication problems.

**Examples:**
- `7×6 = 42` → `7×5=35, 8×6=48, 7×7=49`
- `5×4 = 20` → `5×3=15, 6×4=24, 4×4=16`

**Techniques:**
- Factor variation by ±1, ±2
- Factor doubling/halving
- Square variations
- Common factor mistakes

### 2. ArithmeticErrorStrategy

Simulates common arithmetic errors students make.

**Examples:**
- `7×6 = 42` → `7+6=13, 76, 67`
- `9×4 = 36` → `9+4=13, 94, 49`

**Techniques:**
- Addition instead of multiplication
- Digit concatenation
- Factor confusion (6↔9, 1↔7, 3↔8)
- Off-by-one errors
- Decimal place mistakes

### 3. TableConfusionStrategy

Uses nearby values from multiplication tables and common table patterns.

**Examples:**
- `7×6 = 42` → `6×6=36, 7×5=35, 8×7=56`
- `9×3 = 27` → `9×2=18, 8×3=24, 9×4=36`

**Techniques:**
- Same row/column confusion
- Diagonal table patterns
- Square number confusion
- Fact family mistakes (5s, 9s, 10s)

## Configuration

### DistractorGenerationConfig

```csharp
public class DistractorGenerationConfig
{
    // Strategy Weights (normalized to sum to 1.0)
    public float FactorVariationWeight = 0.4f;
    public float ArithmeticErrorWeight = 0.3f;
    public float TableConfusionWeight = 0.2f;
    public float FallbackRandomWeight = 0.1f;
    
    // Strategy Enable/Disable
    public bool EnableFactorVariation = true;
    public bool EnableArithmeticError = true;
    public bool EnableTableConfusion = true;
    
    // Generation Limits
    public int MaxDistractorsPerStrategy = 2;
    public int MinDistractorValue = 0;
    public int MaxDistractorValue = 144; // 12×12
    
    // Fallback Settings
    public int FallbackRandomRange = 5;
}
```

### Integration with LearningAlgorithmConfig

```csharp
public class LearningAlgorithmConfig
{
    [field: SerializeField]
    public DistractorGenerationConfig DistractorConfig { get; set; } = new DistractorGenerationConfig();
}
```

## Usage

### Basic Usage

```csharp
// Create a fact (7×6)
var fact = new Fact("7x6", 7, 6, "7 × 6 = ?", "7");

// Create context
var context = new DistractorContext(LearningStage.Practice, LearningMode.Practice, 12);

// Generate answer options
var choices = LearningAlgorithmUtils.GenerateContextAwareAnswerOptions(
    fact, 
    42, // correct answer
    context, 
    config.DistractorConfig
);
```

### Advanced Usage

```csharp
// Create custom generator
var generator = new ContextAwareDistractorGenerator(customConfig);

// Generate with full context
var context = new DistractorContext
{
    LearningStage = LearningStage.Assessment,
    LearningMode = LearningMode.Assessment,
    MaxMultiplicationFactor = 12,
    DistractorsNeeded = 3
};

var choices = generator.GenerateAnswerOptions(fact, correctAnswer, context);
```

## How It Works

### Generation Process

1. **Strategy Selection**: Based on configured weights and enabled strategies
2. **Distractor Generation**: Each strategy generates potential distractors
3. **Validation**: Distractors are filtered for validity (range, uniqueness)
4. **Combination**: Strategies are combined based on weights
5. **Fallback**: If insufficient distractors, falls back to random ±5
6. **Shuffling**: Final options are shuffled before returning

### Validation Rules

- Must be within configured min/max range (0-144 by default)
- Must be unique (no duplicates)
- Must not equal the correct answer
- Must be positive integers

### Fallback Mechanism

If strategies don't generate enough distractors:
1. Use fallback random generation (±5 range)
2. If still insufficient, use legacy random method
3. Ensures 4 total options (1 correct + 3 distractors)

## Educational Benefits

### Compared to Random ±5

**Before (Random ±5):**
- `7×6 = 42` → `37, 47, 39` (meaningless)
- No educational value
- Students can't learn from mistakes

**After (Context-Aware):**
- `7×6 = 42` → `35, 48, 13` (factor variation, arithmetic error)
- Reflects actual student mistakes
- Helps identify and address misconceptions

### Learning Outcomes

- **Misconception Identification**: Reveals common error patterns
- **Targeted Practice**: Focuses on specific mistake types
- **Adaptive Difficulty**: Adjusts based on learning stage
- **Educational Validity**: Distractors are plausible and instructive

## Performance Considerations

- **Caching**: Distractor generator is cached for reuse
- **Efficiency**: Strategies are optimized for quick generation
- **Fallback**: Multiple fallback levels ensure consistent performance
- **Validation**: Efficient filtering prevents invalid distractors

## Testing

The system includes comprehensive validation:
- Strategy-specific tests for each distractor type
- Integration tests with `LearningAlgorithmV3`
- Edge case handling (boundary values, special cases)
- Performance benchmarks

## Future Enhancements

Potential improvements:
- **Analytics Integration**: Track distractor effectiveness
- **Machine Learning**: Adapt strategies based on student performance
- **Curriculum Alignment**: Align with specific learning standards
- **Difficulty Progression**: More sophisticated difficulty scaling 