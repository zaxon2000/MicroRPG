using System;

namespace LiberateUI
{
    /// <summary>
    /// Public static delegates to manage MainMenu UI screen visibility changes.
    /// </summary>
    public static class MainMenuUIEvents
    {
        // Show the InventoryScreen
        public static Action InventoryScreenShown;

        // Hide the InventoryScreen
        public static Action InventoryScreenHidden;

        // Show the SettingsScreen
        public static Action SettingsScreenShown;

        // Hide the SettingsScreen
        public static Action SettingsScreenHidden;
    }
}