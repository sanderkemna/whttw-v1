/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.Managers
{
    using Opsive.Shared.Editor.Managers;

    /// <summary>
    /// Shows a starting window with useful links.
    /// </summary>
    [OrderedEditorItem("Welcome", 0)]
    public class WelcomeScreenManager : Opsive.Shared.Editor.Managers.WelcomeScreenManager
    {
        protected override string AssetName => Utility.AssetInfo.Name;
        protected override string AssetVersion => Utility.AssetInfo.Version;
        protected override bool AddLargeDocumentationImage => true;

        /// <summary>
        /// Returns the URL for the documentation page.
        /// </summary>
        /// <returns>The URL for the documentation page.</returns>
        protected override string GetDocumentationURL()
        {
            return "https://opsive.com/support/documentation/ultimate-inventory-system/";
        }

        /// <summary>
        /// Returns the URL for the videos page.
        /// </summary>
        /// <returns>The URL for the videos page.</returns>
        protected override string GetVideosURL()
        {
            return "https://opsive.com/videos/?pid=22330";
        }

        /// <summary>
        /// Returns the URL for the asset page.
        /// </summary>
        /// <returns>The URL for the asset page.</returns>
        protected override string GetAssetURL()
        {
            return "https://assetstore.unity.com/packages/slug/166053";
        }
    }
}