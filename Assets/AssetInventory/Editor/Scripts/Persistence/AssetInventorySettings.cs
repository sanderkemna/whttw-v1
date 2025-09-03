﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetInventorySettings
    {
        public static class Size
        {
            public const long KB = 1L << 10; // 1024
            public const long MB = 1L << 20; // 1 048 576
            public const long GB = 1L << 30; // 1 073 741 824
        }

        private const int LOG_MEDIA_DOWNLOADS = 1;
        private const int LOG_IMAGE_RESIZING = 2;
        private const int LOG_AUDIO_PARSING = 4;
        private const int LOG_PACKAGE_PARSING = 8;
        private const int LOG_CUSTOM_ACTION = 16;

        public int version = UpgradeUtil.CURRENT_CONFIG_VERSION;
        public int searchType;
        public int searchField;
        public bool searchAICaptions = true;
        public bool searchPackageNames;
        public int sortField;
        public bool sortDescending;
        public int maxResults = 5;
        public int maxResultsLimit = 10000;
        public int maxInMemoryResults = 50000;
        public int timeout = 20;
        public int tileText; // 0 - intelligent
        public bool autoPlayAudio = true;
        public int autoCalculateDependencies = 1; // 0 - none, 1 - all, 2 - only simple, no fbx
        public bool allowCrossPackageDependencies = true;
        public bool loopAudio;
        public bool pingSelected = true;
        public bool pingImported = true;
        public int doubleClickBehavior = 1; // 0 = none, 1 = import, 2 = open
        public bool groupLists = true;
        public bool keepAutoDownloads;
        public bool limitAutoDownloads;
        public int downloadLimit = 500;
        public bool searchAutomatically = true;
        public bool searchWithoutInput = true;
        public bool searchSubPackages;
        public bool extractSingleFiles;
        public int previewVisibility;
        public int searchTileSize = 128;
        public float searchTileAspectRatio = 1f;
        public float searchDelay = 0.5f;
        public float inMemorySearchDelay = 0.1f;
        public float hueRange = 10f;
        public int animationGrid = 4;
        public float animationSpeed = 0.1f;
        public bool excludePreviewExtensions;
        public string excludedPreviewExtensions;
        public bool excludeExtensions = true;
        public string excludedExtensions = "asset;json;txt;cs;md;uss;asmdef;uxml;editorconfig;signature;yml;cginc;gitattributes;release;collabignore;suo";
        public bool showExtensionsList;
        public bool keepExtractedOnAudio = true;
        public bool disableDragDrop;

        public int workspace;
        public bool wsSearchWithoutInput;
        public bool wsSavedSearchInMemory = true;

        public float rowHeightMultiplier = 1.1f;
        public int previewChunkSize = 20;
        public int previewSize = 128;
        public int mediaHeight = 350;
        public int mediaThumbnailWidth = 120;
        public int mediaThumbnailHeight = 75;
        public int currency; // 0 - EUR, 1 - USD, 2 - CYN
        public int packageTileSize = 150;
        public int noPackageTileTextBelow = 110;
        public int tagListHeight = 250;
        public int tileMargin = 2;
        public bool enlargeTiles = true;
        public bool centerTiles;
        public int[] visiblePackageTreeColumns;

        public bool showSearchSideBar = true;
        public bool expandPackageDetails;
        public bool alwaysShowPackageDetails;
        public bool showPreviews = true;
        public bool showIndexingSettings;
        public bool showFolderSettings;
        public bool showAMSettings;
        public bool showImportSettings;
        public bool showBackupSettings;
        public bool showAISettings;
        public bool showUISettings;
        public bool showLocationSettings;
        public bool showPreviewSettings;
        public bool showAdvancedSettings;
        public bool showHints = true;
        public int packageViewMode; // 0 = list, 1 = grid
        public bool searchPackageDescriptions;
        public bool showPackageStatsDetails;
        public bool onlyInProject;
        public bool projectDetailTabs = true;

        public bool excludeHidden = true;
        public int assetStoreRefreshCycle = 3; // days
        public int assetCacheLocationType; // 0 = auto, 1 = custom
        public string assetCacheLocation;
        public int packageCacheLocationType; // 0 = auto, 1 = custom
        public string packageCacheLocation;
        public bool gatherExtendedMetadata = true;
        public bool extractPreviews = true;
        public bool extractAudioColors;
        public bool excludeByDefault;
        public bool extractByDefault;
        public bool captionByDefault;
        public bool convertToPipeline;
        public bool scanFBXDependencies = true;
        public bool indexSubPackages = true;
        public bool indexAssetPackageContents = true;
        public bool verifyPreviews = true;
        public bool showIconsForMissingPreviews = true;
        public bool importPackageKeywordsAsTags;
        public string customStorageLocation;
        public bool showCustomPackageUpdates;
        public bool showIndirectPackageUpdates;
        public bool removeUnresolveableDBFiles;

        public bool logAICaptions;
        public bool aiForPrefabs = true;
        public bool aiForModels;
        public bool aiForImages;
        public int aiBackend = 1; // 0 - blip, 1 = ollama
        public bool aiContinueOnEmpty;
        public int aiPause;
        public int aiMinSize = 32; // minimum size for AI processing, in pixels, upscales otherwise
        public int aiMaxCaptionLength = 200; // some model outputs are extremely long and cause crashes

        public string ollamaModel = "qwen2.5vl";
        public string ollamaPrompt;

        public int blipType; // 0 - small, 1 = large
        public int blipChunkSize = 1;
        public bool blipUseGPU;
        public string blipPath;

        public bool upscalePreviews = true;
        public bool upscaleLossless = true;
        public int upscaleSize = 256;

        public bool hideAdvanced = true;
        public bool useCooldown = true;
        public int cooldownInterval = 20; // minutes
        public int cooldownDuration = 20; // seconds
        public int reportingBatchSize = 500;
        public long memoryLimit = 10 * Size.GB; // every X gigabytes
        public bool limitCacheSize = true;
        public int cacheLimit = 60; // in gigabyte
        public int massOpenWarnThreshold = 7;
        public int logAreas = LOG_IMAGE_RESIZING | LOG_AUDIO_PARSING | LOG_MEDIA_DOWNLOADS | LOG_PACKAGE_PARSING | LOG_CUSTOM_ACTION;
        public int dbOptimizationPeriod = 30; // days
        public int dbOptimizationReminderPeriod = 1; // days
        public string dbJournalMode = "WAL"; // DELETE is an alternative for better compatibility while WAL is faster
        public bool askedForAffiliateLinks;
        public bool useAffiliateLinks;

        public bool backupByDefault;
        public bool onlyLatestPatchVersion = true;
        public int backupsPerAsset = 5;
        public string backupFolder;
        public string cacheFolder;
        public string previewFolder;
        public string exportFolder;
        public string exportFolder2;
        public string exportFolder3;
        public TemplateExportSettings templateExportSettings = new TemplateExportSettings();

        public int importStructure = 1;
        public int importDestination = 2;
        public string importFolder = "Assets/ThirdParty";
        public bool removeLODs;

        public int assetSorting;
        public bool sortAssetsDescending;
        public int assetGrouping;
        public int assetDeprecation;
        public int assetSRPs;
        public int packagesListing = 1; // only assets per default
        public int maxConcurrentUnityRequests = 10;
        public int observationSpeed = 5;
        public bool autoRefreshMetadata = true;
        public int metadataTimeout = 12; // in hours
        public bool autoStopObservation = true;
        public int observationTimeout = 10; // in seconds

        // non-preferences for convenience
        public int tab;
        public ulong statsImports;

        public List<UpdateActionStates> actionStates = new List<UpdateActionStates>();
        public List<FolderSpec> folders = new List<FolderSpec>();

        // log helpers
        public bool LogMediaDownloads => (logAreas & LOG_MEDIA_DOWNLOADS) != 0;
        public bool LogImageExtraction => (logAreas & LOG_IMAGE_RESIZING) != 0;
        public bool LogAudioParsing => (logAreas & LOG_AUDIO_PARSING) != 0;
        public bool LogPackageParsing => (logAreas & LOG_PACKAGE_PARSING) != 0;
        public bool LogCustomActions => (logAreas & LOG_CUSTOM_ACTION) != 0;

        // UI customization
        public List<UISection> uiSections = new List<UISection>();
        public HashSet<string> advancedUI;

        // outdated
        public List<OutdatedSavedSearch> searches = new List<OutdatedSavedSearch>();

        public AssetInventorySettings()
        {
            ResetAdvancedUI();
        }

        public void InitUISections()
        {
            if (uiSections == null) uiSections = new List<UISection>();
            if (!uiSections.Any(uis => uis.name == "package"))
            {
                uiSections.Add(new UISection {name = "package", sections = new List<string> {"PackageData", "TabbedDetails", "Media", "Description", "ReleaseNotes", "Dependencies"}});
            }
        }

        public UISection GetSection(string name)
        {
            InitUISections(); // ensure sections are always initialized

            return uiSections.FirstOrDefault(s => s.name == name);
        }

        public void ResetAdvancedUI()
        {
            // list of UI elements that should be hidden by default
            advancedUI = new HashSet<string>
            {
                "settings.actions.clearcache",
                "settings.actions.cleardb",
                "settings.actions.resetconfig",
                "settings.actions.resetuiconfig",
                "settings.actions.closedb",
                "settings.actions.openassetcache",
                "settings.actions.openpackagecache",
                "settings.actions.dblocation",
                "package.category",
                "package.childcount",
                "package.exclude",
                "package.extract",
                "package.indexedfiles",
                "package.metadata",
                "package.price",
                "package.purchasedate",
                "package.releasedate",
                "package.srps",
                "package.unityversions",
                "package.actions.layout",
                "package.actions.openinpackagemanager",
                "package.actions.reindexnextrun",
                "package.actions.recreatemissingpreviews",
                "package.actions.recreateimagepreviews",
                "package.actions.recreateallpreviews",
                "package.actions.delete",
                "package.actions.openlocation",
                "package.actions.refreshmetadata",
                "package.actions.export",
                "package.actions.deletefile",
                "package.actions.nameonly",
                "package.actions.reindexnow",
                "package.actions.removeassetstoreconnection",
                "asset.actions.openexplorer",
                "asset.actions.delete",
                "asset.bulk.actions.export",
                "asset.bulk.actions.delete",
                "asset.bulk.actions.openexplorer",
                "package.bulk.actions.refreshmetadata",
                "package.bulk.actions.delete",
                "package.bulk.actions.deletefile",
                "package.bulk.actions.openlocation",
                "search.actions.tilesize",
                "search.actions.sidebar"
            };
        }
    }
}