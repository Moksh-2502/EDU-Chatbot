using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Handles rendering of progress bars with enhanced visual effects
    /// </summary>
    public class LearningHudProgressRenderer
    {
        private readonly LearningHudStyleManager _styleManager;

        public LearningHudProgressRenderer(LearningHudStyleManager styleManager)
        {
            _styleManager = styleManager;
        }

        public void DrawProgressBar(float fillAmount, Color color)
        {
            var rect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            DrawProgressBar(rect, fillAmount, color);
        }

        public void DrawProgressBar(Rect rect, float fillAmount, Color color)
        {
            // Draw shadow first
            var shadowRect = new Rect(rect.x + _styleManager.ShadowOffset, rect.y + _styleManager.ShadowOffset, rect.width, rect.height);
            var originalColor = GUI.color;
            GUI.color = _styleManager.ShadowColor;
            GUI.Box(shadowRect, "", _styleManager.ProgressBarBackgroundStyle);
            
            // Background with rounded appearance
            GUI.color = Color.white;
            GUI.Box(rect, "", _styleManager.ProgressBarBackgroundStyle);
            
            // Fill with gradient effect
            if (fillAmount > 0)
            {
                var fillRect = new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * fillAmount, rect.height - 6);
                
                // Create gradient fill effect
                GUI.color = color;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                
                // Add highlight on top
                var highlightRect = new Rect(fillRect.x, fillRect.y, fillRect.width, fillRect.height * 0.4f);
                GUI.color = color * 1.3f;
                GUI.DrawTexture(highlightRect, Texture2D.whiteTexture);
            }
            
            // Add percentage text overlay
            GUI.color = _styleManager.TextColor;
            var percentText = $"{fillAmount * 100:F0}%";
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _styleManager.FontSize - 2,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _styleManager.TextColor }
            };
            
            GUI.Label(rect, percentText, textStyle);
            GUI.color = originalColor;
        }

        public void DrawMiniProgressBar(float fillAmount, Color color, float height = 8f)
        {
            var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            
            var originalColor = GUI.color;
            
            // Simple background
            GUI.color = Color.black * 0.3f;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            
            // Fill
            if (fillAmount > 0)
            {
                var fillRect = new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * fillAmount, rect.height - 2);
                GUI.color = color;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }
            
            GUI.color = originalColor;
        }
    }
} 