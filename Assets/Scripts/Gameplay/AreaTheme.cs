using System;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// One visual area inside the same endless run. Themes change the sky,
    /// backdrop tint and ground tint. They never load a different scene.
    /// </summary>
    [Serializable]
    public class AreaTheme
    {
        [Tooltip("Shown under NEW AREA. Keep this short.")]
        public string displayName = "GREEN HILLS";

        [Tooltip("How long this area lasts before the next one, in seconds of play.")]
        public float durationSeconds = 60f;

        [Tooltip("Camera clear colour. This is the sky.")]
        public Color cameraColor = new Color(0.45f, 0.75f, 0.95f, 1f);

        [Tooltip("Multiplied onto the existing parallax sprites.")]
        public Color backgroundTint = Color.white;

        [Tooltip("Multiplied onto terrain sprites. Keep this bright enough to read.")]
        public Color groundTint = Color.white;

        /// <summary>
        /// True when Unity created a blank array slot (empty name, zero duration,
        /// black or clear camera colour). Field initializers are not applied to those slots.
        /// </summary>
        public bool IsUnconfigured()
        {
            bool noName = string.IsNullOrWhiteSpace(displayName);
            bool noDuration = durationSeconds <= 0.001f;
            bool noCamera = cameraColor.maxColorComponent <= 0.02f;
            return noName && noDuration && noCamera;
        }

        public void CopyFrom(AreaTheme source)
        {
            if (source == null)
            {
                return;
            }

            displayName = source.displayName;
            durationSeconds = source.durationSeconds;
            cameraColor = source.cameraColor;
            backgroundTint = source.backgroundTint;
            groundTint = source.groundTint;
        }
    }
}
