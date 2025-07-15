using UnityEngine;
using FluencySDK;
namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Manages all visual styling, colors, and textures for the Learning HUD
    /// </summary>
    public class LearningHudStyleManager
    {
        // Visual Settings
        public float CornerRadius { get; set; } = 8f;
        public float ShadowOffset { get; set; } = 2f;
        public float ContentPadding { get; set; } = 15f;
        public int FontSize { get; set; } = 14;
        public int HeaderFontSize { get; set; } = 16;

        // Enhanced Color Palette
        public Color BackgroundColor { get; } = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        public Color PanelColor { get; } = new Color(0.15f, 0.16f, 0.21f, 0.98f);
        public Color PanelHighlightColor { get; } = new Color(0.2f, 0.22f, 0.28f, 1f);
        public Color HeaderColor { get; } = new Color(0.3f, 0.8f, 1f, 1f);
        public Color SubHeaderColor { get; } = new Color(0.7f, 0.9f, 1f, 1f);
        public Color TextColor { get; } = new Color(0.9f, 0.9f, 0.95f, 1f);
        public Color TextSecondaryColor { get; } = new Color(0.7f, 0.7f, 0.8f, 1f);
        public Color ButtonColor { get; } = new Color(0.25f, 0.28f, 0.35f, 0.9f);
        public Color ButtonHoverColor { get; } = new Color(0.35f, 0.38f, 0.45f, 0.95f);
        public Color SelectedButtonColor { get; } = new Color(0.2f, 0.5f, 0.8f, 0.9f);
        public Color SelectedButtonHoverColor { get; } = new Color(0.3f, 0.6f, 0.9f, 0.95f);
        public Color ShadowColor { get; } = new Color(0, 0, 0, 0.3f);

        // Progress Colors
        public Color ProgressExcellentColor { get; } = new Color(0.2f, 0.8f, 0.3f, 1f);
        public Color ProgressGoodColor { get; } = new Color(0.5f, 0.8f, 0.2f, 1f);
        public Color ProgressOkayColor { get; } = new Color(0.9f, 0.7f, 0.2f, 1f);
        public Color ProgressPoorColor { get; } = new Color(0.9f, 0.3f, 0.2f, 1f);
        public Color ProgressUnknownColor { get; } = new Color(0.5f, 0.5f, 0.6f, 1f);

        // GUI Styles
        public GUIStyle BoxStyle { get; private set; }
        public GUIStyle PanelStyle { get; private set; }
        public GUIStyle HeaderStyle { get; private set; }
        public GUIStyle SubHeaderStyle { get; private set; }
        public GUIStyle LabelStyle { get; private set; }
        public GUIStyle ButtonStyle { get; private set; }
        public GUIStyle SelectedButtonStyle { get; private set; }
        public GUIStyle ProgressBarStyle { get; private set; }
        public GUIStyle ProgressBarBackgroundStyle { get; private set; }
        public GUIStyle ScrollViewStyle { get; private set; }
        public GUIStyle TableHeaderStyle { get; private set; }
        public GUIStyle TableCellStyle { get; private set; }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;

            CreateStyles();
            _initialized = true;
        }

        private void CreateStyles()
        {
            // Main background box style
            BoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradientTexture(BackgroundColor, BackgroundColor * 1.1f, 64, 64) },
                padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)ContentPadding),
                margin = new RectOffset(0, 0, 0, 0)
            };

            // Panel style with subtle gradient
            PanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradientTexture(PanelColor, PanelHighlightColor, 32, 32) },
                padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)ContentPadding),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Enhanced header style
            HeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = HeaderFontSize + 2,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HeaderColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 5, 8)
            };

            // Sub-header style
            SubHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = HeaderFontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SubHeaderColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 3, 5)
            };

            // Enhanced label style
            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                normal = { textColor = TextColor },
                wordWrap = true,
                margin = new RectOffset(8, 0, 3, 3),
                padding = new RectOffset(2, 2, 2, 2)
            };

            // Enhanced button style with gradients
            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = FontSize,
                fontStyle = FontStyle.Normal,
                normal = { 
                    textColor = TextColor, 
                    background = MakeGradientTexture(ButtonColor, ButtonColor * 1.15f, 32, 32) 
                },
                hover = { 
                    textColor = Color.white, 
                    background = MakeGradientTexture(ButtonHoverColor, ButtonHoverColor * 1.15f, 32, 32) 
                },
                active = { 
                    textColor = Color.white, 
                    background = MakeGradientTexture(ButtonColor * 0.8f, ButtonColor * 0.9f, 32, 32) 
                },
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(2, 2, 2, 2),
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            // Enhanced selected button style
            SelectedButtonStyle = new GUIStyle(ButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { 
                    textColor = Color.white, 
                    background = MakeGradientTexture(SelectedButtonColor, SelectedButtonHoverColor, 32, 32) 
                },
                hover = { 
                    textColor = Color.white, 
                    background = MakeGradientTexture(SelectedButtonHoverColor, SelectedButtonHoverColor * 1.1f, 32, 32) 
                }
            };

            // Enhanced progress bar background
            ProgressBarBackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradientTexture(Color.black * 0.3f, Color.black * 0.5f, 16, 16) },
                padding = new RectOffset(2, 2, 2, 2)
            };

            // Progress bar fill style
            ProgressBarStyle = new GUIStyle()
            {
                normal = { background = Texture2D.whiteTexture }
            };

            // Scroll view style
            ScrollViewStyle = new GUIStyle(GUI.skin.scrollView)
            {
                normal = { background = MakeColorTexture(Color.clear) }
            };

            // Table header style - bold and slightly larger than table cells
            TableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = SubHeaderColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 2, 2)
            };

            // Table cell style - smaller than regular labels
            TableCellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize - 1,
                fontStyle = FontStyle.Normal,
                normal = { textColor = TextColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 3, 3),
                margin = new RectOffset(0, 0, 1, 1),
                wordWrap = true
            };
        }

        public Color GetProgressColor(float progressPercent)
        {
            if (progressPercent >= 85f) return ProgressExcellentColor;
            if (progressPercent >= 70f) return ProgressGoodColor;
            if (progressPercent >= 50f) return ProgressOkayColor;
            if (progressPercent > 0f) return ProgressPoorColor;
            return ProgressUnknownColor;
        }

        public Color GetStageColor(LearningStage stage)
        {
            switch (stage.Type)
            {
                case LearningStageType.Mastered:
                    return ProgressExcellentColor;
                case LearningStageType.Review:
                case LearningStageType.Repetition:
                    return ProgressGoodColor;
                case LearningStageType.Practice:
                case LearningStageType.Assessment:
                    return ProgressOkayColor;
                case LearningStageType.Grounding:
                    return ProgressPoorColor;
                default:
                    return ProgressUnknownColor;
            }
        }

        public string GetProgressIcon(float progressPercent)
        {
            if (progressPercent >= 85f) return "🌟"; // Excellent
            if (progressPercent >= 70f) return "✅"; // Good
            if (progressPercent >= 50f) return "⚡"; // Okay
            if (progressPercent > 0f) return "🔄"; // Poor/In Progress
            return "⭕"; // Unknown/Not started
        }

        private Texture2D MakeColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Texture2D MakeGradientTexture(Color startColor, Color endColor, int width, int height)
        {
            var texture = new Texture2D(width, height);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float t = (float)y / (height - 1);
                    Color color = Color.Lerp(startColor, endColor, t);
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            return texture;
        }

        private Texture2D MakeRoundedTexture(Color color, int width, int height, int cornerRadius)
        {
            var texture = new Texture2D(width, height);
            var clearColor = Color.clear;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Calculate distance from corners
                    bool isInCorner = false;
                    
                    // Top-left corner
                    if (x < cornerRadius && y < cornerRadius)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                        isInCorner = dist > cornerRadius;
                    }
                    // Top-right corner
                    else if (x >= width - cornerRadius && y < cornerRadius)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, cornerRadius));
                        isInCorner = dist > cornerRadius;
                    }
                    // Bottom-left corner
                    else if (x < cornerRadius && y >= height - cornerRadius)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, height - cornerRadius - 1));
                        isInCorner = dist > cornerRadius;
                    }
                    // Bottom-right corner
                    else if (x >= width - cornerRadius && y >= height - cornerRadius)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, height - cornerRadius - 1));
                        isInCorner = dist > cornerRadius;
                    }
                    
                    texture.SetPixel(x, y, isInCorner ? clearColor : color);
                }
            }
            
            texture.Apply();
            return texture;
        }
    }
} 