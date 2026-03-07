using System;
using System.Reflection;
using _patcher.Graphics;
using _patcher.Rosu;

namespace _patcher.Play
{
    /// <summary>
    /// Performance class.
    /// </summary>
    internal class Performance
    {
        private readonly Calculator _ppCalculator;
        private readonly pSpriteText[] _ppSpriteTexts;
        private int currentPP;
        private int previousPP;
        private int lastCombo;

        public Performance(object beatmap, MethodInfo beatmapStream, uint mods, pSpriteText[] spriteTexts)
            : this(beatmap, beatmapStream, mods)
        {
            _ppSpriteTexts = spriteTexts;
        }

        public Performance(object beatmap, MethodInfo beatmapStream, uint mods)
        {
            _ppCalculator = new Calculator(beatmap, beatmapStream, mods);
        }

        public void UpdatePerformance(object score, float accuracy, int totalHits, int maxCombo, int playMode)
        {
            if (lastCombo != totalHits)
            {
                currentPP = (int)Math.Round(_ppCalculator.CalculateScore(score, accuracy, totalHits, maxCombo, playMode));
                lastCombo = totalHits;
            }

            if (previousPP != currentPP && _ppSpriteTexts != null)
                foreach (var spriteText in _ppSpriteTexts)
                    spriteText.Text = currentPP.ToString();

            previousPP = currentPP;
        }
    }
}
