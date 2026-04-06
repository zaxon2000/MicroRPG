using System;
using UnityEngine;

namespace LiberateUI
{
    /// <summary>
    /// Public static delegates associated with changing Themes.
    /// This can inform any components listening for seasonal or portrait/landscape theme changes.
    ///
    /// Note: these are "events" in the conceptual sense and not the strict C# sense.
    /// </summary>
    public static class ThemeEvents
    {
        // Event for changing themes (string represents the theme name)
        public static Action<string> ThemeChanged;

        // Event for changing art style (string represents the art style name)
        public static Action<string> ArtStyleChanged;

        // Event triggered for updating a Theme Camera
        public static Action<Camera> CameraUpdated;

    }
}
