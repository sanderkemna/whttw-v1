﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if !ASSET_INVENTORY_NOAUDIO
using JD.EditorAudioUtils;
#endif
using SQLite;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#if UNITY_2021_3_OR_NEWER && !USE_TUTORIALS
using UnityEditor.PackageManager;
#endif
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public partial class IndexUI
    {
        private const string PACKAGE_TAG_JOIN_CLAUSE = "inner join TagAssignment as tap on (Asset.Id = tap.TargetId and tap.TagTarget = 0)";
        private const string FILE_TAG_JOIN_CLAUSE = "inner join TagAssignment as taf on (AssetFile.Id = taf.TargetId and taf.TagTarget = 1)";
        private const float DRAG_THRESHOLD = 5f; // pixels
        private const float DRAG_DELAY = 0.5f; // seconds

        private enum InMemoryModeState
        {
            None,
            Init,
            Active
        }

        // customizable interaction modes, search mode will only show search tab contents and no actions except "Select"
        public bool searchMode;

        // will show additional workspace layer
        public bool workspaceMode;

        // special mode that will return accompanying textures to the selected one, trying to identify normal, metallic etc. 
        public bool textureMode;

        // will hide right-side inspector pane
        public bool hideDetailsPane;
        public bool hideMainNavigation;

        // will not select items in the project window upon selection
        public bool disablePings;

        // will cause clicking on a grid tile to return the selection to the caller and close the window
        public bool instantSelection;

        // locks the search to a specific type, e.g. "Prefabs" 
        public string fixedSearchType;

        // event handler during search mode
        protected Action<string> searchModeCallback;
        protected Action<Dictionary<string, string>> searchModeTextureCallback;

        private List<AssetInfo> _files;
        private IEnumerable<AssetInfo> _filteredFiles;

        private GridControl SGrid
        {
            get
            {
                if (_sgrid == null)
                {
                    _sgrid = new GridControl();
                    _sgrid.OnDoubleClick += OnSearchDoubleClick;
                }
                return _sgrid;
            }
        }
        private GridControl _sgrid;

        private InMemoryModeState _inMemoryMode = InMemoryModeState.None;
        private string _searchPhrase;
        private string _searchPhraseInMemory;
        private string _searchWidth;
        private string _searchHeight;
        private string _searchLength;
        private string _searchSize;
        private bool _checkMaxWidth;
        private bool _checkMaxHeight;
        private bool _checkMaxLength;
        private bool _checkMaxSize;
        private int _selectedPublisher;
        private int _selectedCategory;
        private int _selectedExpertSearchField;
        private int _selectedAsset;
        private int _selectedPackageTypes = 1;
        private int _selectedPackageSRPs;
        private int _selectedImageType;
        private int _selectedPackageTag;
        private int _selectedFileTag;
        private int _selectedMaintenance;
        private int _selectedColorOption;
        private Color _selectedColor;

        private Vector2 _searchScrollPos;
        private Vector2 _inspectorScrollPos;

        private int _resultCount;
        private int _originalResultCount;
        private int _curPage = 1;
        private int _pageCount;

        private CancellationTokenSource _textureLoading;
        private CancellationTokenSource _textureLoading2;
        private CancellationTokenSource _textureLoading3;
        private CancellationTokenSource _extraction;

        private AssetInfo _selectedEntry;
        private Workspace _selectedWorkspace;

        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;

        private int _searchInspectorTab;
        private float _nextSearchTime;
        private Rect _pageButtonRect;
        private Rect _querySamplesButtonRect;
        private Rect _wsButtonRect;
        private DateTime _lastTileSizeChange;
        private string _searchError;
        private bool _searchDone;
        private bool _lockSelection;
        private string _curOperation;
        private int _fixedSearchTypeIdx;
        private bool _draggingPossible;
        private bool _dragging;
        private Vector2 _dragStartPosition;
        private float _dragStartTime;
        private bool _keepSearchResultPage = true;
        private readonly Dictionary<string, Tuple<int, Color>> _assetFileBulkTags = new Dictionary<string, Tuple<int, Color>>();
        private Texture2D _animTexture;
        private List<Rect> _animFrames;
        private int _curAnimFrame;
        private float _nextAnimTime;
        private int _assetFileAMProjectCount;
        private int _assetFileAMCollectionCount;

        // Track the currently active saved search
        private int _activeSavedSearchId = -1;

        private List<SavedSearch> Searches
        {
            get
            {
                if (_searches == null || !_searchesLoaded)
                {
                    _searches = DBAdapter.DB.Table<SavedSearch>().ToList();
                    _searchesLoaded = true;
                }
                return _searches;
            }
        }
        private List<SavedSearch> _searches;
        private bool _searchesLoaded;

        private List<Workspace> Workspaces
        {
            get
            {
                if (_workspaces == null || !_workspacesLoaded)
                {
                    _workspaces = DBAdapter.DB.Table<Workspace>().ToList();
                    _workspacesLoaded = true;
                }
                return _workspaces;
            }
        }
        private List<Workspace> _workspaces;
        private bool _workspacesLoaded;

        private void InitWorkspace()
        {
            if (!workspaceMode || AI.Config.workspace <= 0)
            {
                _selectedWorkspace = null;
                return;
            }
            SetWorkspace(Workspaces.FirstOrDefault(ws => ws.Id == AI.Config.workspace));
        }

        private void SetWorkspace(Workspace ws)
        {
            _selectedWorkspace = ws;
            List<WorkspaceSearch> searches = _selectedWorkspace?.LoadSearches();
            if (searches == null || searches.Count == 0)
            {
                // deactivate current in-memory mode if no searches are available
                _inMemoryMode = InMemoryModeState.None;
                _searchPhrase = "";
                _requireSearchUpdate = true;
            }

            int oldWorkspace = AI.Config.workspace;
            AI.Config.workspace = ws == null ? 0 : ws.Id;
            if (oldWorkspace != AI.Config.workspace) AI.SaveConfig();
        }

        protected void SetInitialSearch(string searchPhrase)
        {
            _searchPhrase = searchPhrase;
        }

        private void OnSearchDoubleClick(AssetInfo obj)
        {
            if ((searchMode || AI.Config.doubleClickBehavior > 0) && _selectedEntry != null)
            {
                if (searchMode)
                {
                    ExecuteSingleAction();
                }
                else
                {
                    if ((AI.Config.doubleClickBehavior == 1 && !Event.current.alt) || (AI.Config.doubleClickBehavior == 2 && Event.current.alt))
                    {
                        _ = PerformCopyTo(_selectedEntry, _importFolder);
                    }
                    else
                    {
                        Open(_selectedEntry);
                    }
                }
            }
        }

        private void RecreatePreviewEditor()
        {
            Object previewObject = _selectedEntry.InProject ? AssetDatabase.LoadAssetAtPath<Object>(_selectedEntry.ProjectPath) : null;
            if (_previewEditor != null)
            {
                DestroyImmediate(_previewEditor);
                _previewEditor = null;
            }

            if (previewObject != null)
            {
                _previewEditor = Editor.CreateEditor(previewObject);
            }
        }

        private void DrawSearchTab()
        {
            if (_packageFileCount == 0)
            {
                _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, false, false);
                bool canStillSearch = AI.Actions.ActionsInProgress || _packageCount == 0 || AI.Config.indexAssetPackageContents;
                if (canStillSearch)
                {
                    EditorGUILayout.HelpBox("The search index needs to be initialized. Start it right from here or go to the Settings tab to configure the details.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The search is only available if package contents was indexed.", MessageType.Info);
                }

                EditorGUILayout.Space(30);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(250), GUILayout.MaxHeight(250));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (canStillSearch)
                {
                    EditorGUILayout.Space(30);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    EditorGUI.BeginDisabledGroup(AI.Actions.ActionsInProgress);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AI.Actions.ActionsInProgress ? "Indexing..." : "Start Indexing", GUILayout.Height(UIStyles.BIG_BUTTON_HEIGHT), GUILayout.MaxWidth(400))) PerformFullUpdate();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    if (!AI.Actions.ActionsInProgress)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Settings...", GUILayout.ExpandWidth(false))) SetupWizardUI.ShowWindow();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

#if UNITY_2021_3_OR_NEWER && !USE_TUTORIALS
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Install Getting-Started Tutorials", GUILayout.ExpandWidth(false)))
                    {
                        Client.Add($"com.unity.learn.iet-framework@{AI.TUTORIALS_VERSION}");
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
#endif
                    EditorGUI.EndDisabledGroup();
                    if (AI.Actions.ActionsInProgress)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Index results will appear here automatically once available. To see the detailed progress go to the Settings tab.", EditorStyles.centeredGreyMiniLabel);
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("Since the search index is shared across Unity projects it is highly recommended to perform initial indexing from an empty project on a new Unity version and if possible on an SSD drive for better performance.", MessageType.Warning);
                }
                GUILayout.EndScrollView();
            }
            else if (_lockSelection)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Making asset available in project...", UIStyles.centerLabel);
                EditorGUILayout.LabelField("This can take a while depending on the size of the source package.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(30);
                EditorGUILayout.LabelField(_curOperation, EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                bool dirty = false;

                // saved searches bar
                if (Searches.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Space(5);

                    // Calculate available width for wrapping
                    float availableWidth = position.width - 50; // Account for margins + workspace dropdown
                    float currentX = 0;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    // Get the searches to display, respecting workspace order if in workspace mode
                    IEnumerable<SavedSearch> searchesToDisplay;
                    if (workspaceMode && _selectedWorkspace != null && _selectedWorkspace.Searches != null)
                    {
                        // In workspace mode, use the order from _selectedWorkspace.Searches
                        searchesToDisplay = _selectedWorkspace.Searches
                            .OrderBy(ws => ws.OrderIdx)
                            .Select(ws => Searches.FirstOrDefault(s => s.Id == ws.SavedSearchId))
                            .Where(s => s != null);
                    }
                    else
                    {
                        // Normal mode, use all searches
                        searchesToDisplay = Searches;
                    }

                    foreach (SavedSearch search in searchesToDisplay)
                    {
                        Color oldCol = GUI.backgroundColor;

                        float buttonHeight = EditorStyles.miniButton.CalcSize(GUIContent.none).y;
                        Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
                        EditorGUIUtility.SetIconSize(new Vector2(buttonHeight, buttonHeight));

                        // Check if this search is currently active
                        bool isActive = search.Id == _activeSavedSearchId;
                        if (isActive && _activeSavedSearchId == -1)
                        {
                            _activeSavedSearchId = search.Id;
                        }

                        // Apply search color
                        if (ColorUtility.TryParseHtmlString($"#{search.Color}", out Color color))
                        {
                            GUI.backgroundColor = color;
                        }

                        bool searchIsActive = search.Id == _activeSavedSearchId;

                        GUIContent content;
                        if (string.IsNullOrWhiteSpace(search.Name))
                        {
                            content = EditorGUIUtility.IconContent(search.Icon, "|" + search.SearchPhrase);
                        }
                        else if (string.IsNullOrWhiteSpace(search.Icon))
                        {
                            content = UIStyles.Content(search.Name, search.SearchPhrase);
                        }
                        else
                        {
                            content = UIStyles.Content(search.Name, EditorGUIUtility.IconContent(search.Icon, "|" + search.SearchPhrase).image, search.SearchPhrase);
                        }

                        // Calculate button width based on content
                        Vector2 contentSize = EditorStyles.miniButton.CalcSize(content);
                        EditorGUIUtility.SetIconSize(oldIconSize);
                        float buttonWidth = contentSize.x + 20; // Add padding
                        float settingsButtonWidth = 20;
                        float totalWidth = buttonWidth + settingsButtonWidth + 5; // 5 for spacing between buttons

                        // Check if we need to wrap to next line
                        if (currentX + totalWidth > availableWidth && currentX > 0)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            currentX = 10; // Reset to margin
                        }

                        if (GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.Width(buttonWidth)))
                        {
                            if (workspaceMode && AI.Config.wsSavedSearchInMemory)
                            {
                                _inMemoryMode = InMemoryModeState.Init;
                            }
                            LoadSearch(search);
                        }

                        if (GUILayout.Button(EditorGUIUtility.IconContent("icon dropdown", "|Settings"), EditorStyles.miniButtonRight, GUILayout.Width(settingsButtonWidth)))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Edit..."), false, () =>
                            {
                                SavedSearchUI savedSearchUI = SavedSearchUI.ShowWindow();
                                savedSearchUI.Init(search);
                            });
                            // menu.AddItem(new GUIContent("Override with current search"), false, () => { });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Delete"), false, () =>
                            {
                                if (!EditorUtility.DisplayDialog("Confirm", $"Do you really want to delete the saved search '{search.Name}'?", "Yes", "No")) return;

                                DBAdapter.DB.Delete(search);
                                Searches.Remove(search);
                                DBAdapter.DB.Execute("delete from WorkspaceSearch where SavedSearchId = ?", search.Id);
                                _selectedWorkspace?.LoadSearches();
                            });
                            menu.ShowAsContext();
                        }
                        GUI.backgroundColor = oldCol;

                        // Draw glowing border for active search after both buttons are drawn
                        if (searchIsActive && Event.current.type == EventType.Repaint)
                        {
                            Rect mainButtonRect = GUILayoutUtility.GetLastRect();
                            // Get the rect of the main button (we need to calculate it since we only have the dropdown rect)
                            Rect mainButtonRectCalculated = new Rect(mainButtonRect.x - buttonWidth, mainButtonRect.y, buttonWidth, mainButtonRect.height);

                            // Calculate the combined rect that encompasses both buttons
                            Rect combinedRect = new Rect(
                                mainButtonRectCalculated.x - 3,
                                mainButtonRectCalculated.y - 3,
                                mainButtonRectCalculated.width + mainButtonRect.width + 6,
                                mainButtonRectCalculated.height + 6
                            );

                            Color borderColor = Color.white;
                            Color oldColor = GUI.color;
                            GUI.color = borderColor;

                            // Top border
                            GUI.Box(new Rect(combinedRect.x, combinedRect.y, combinedRect.width, 4), "");
                            // Bottom border
                            GUI.Box(new Rect(combinedRect.x, combinedRect.y + combinedRect.height - 4, combinedRect.width, 4), "");
                            // Left border
                            GUI.Box(new Rect(combinedRect.x, combinedRect.y, 4, combinedRect.height), "");
                            // Right border
                            GUI.Box(new Rect(combinedRect.x + combinedRect.width - 4, combinedRect.y, 4, combinedRect.height), "");

                            GUI.color = oldColor;
                        }

                        GUILayout.Space(5);
                        currentX += totalWidth;
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    if (workspaceMode)
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                        GUILayout.Space(5);
                        if (GUILayout.Button(EditorGUIUtility.IconContent("icon dropdown", "|Workspaces"), EditorStyles.miniButton, GUILayout.Width(28)))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("-No Workspace-"), _selectedWorkspace == null, () => SetWorkspace(null));
                            if (Workspaces.Count > 0)
                            {
                                menu.AddSeparator("");
                                foreach (Workspace ws in Workspaces)
                                {
                                    menu.AddItem(new GUIContent(ws.Name), _selectedWorkspace != null && _selectedWorkspace.Id == ws.Id, () => SetWorkspace(ws));
                                }
                            }
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("New..."), false, () =>
                            {
                                NameUI nameUI = new NameUI();
                                nameUI.Init("My Workspace", SaveWorkspace);
                                PopupWindow.Show(_wsButtonRect, nameUI);
                            });
                            if (_selectedWorkspace != null)
                            {
                                menu.AddItem(new GUIContent("Edit..."), false, () =>
                                {
                                    WorkspaceUI workspaceUI = WorkspaceUI.ShowWindow();
                                    workspaceUI.Init(_selectedWorkspace);
                                });
                                menu.AddItem(new GUIContent("Delete"), false, () =>
                                {
                                    if (!EditorUtility.DisplayDialog("Confirm", $"Do you really want to delete workspace '{_selectedWorkspace.Name}'?", "Yes", "No")) return;

                                    Workspaces.Remove(_selectedWorkspace);
                                    DBAdapter.DB.Execute("delete from WorkspaceSearch where WorkspaceId = ?", _selectedWorkspace.Id);
                                    DBAdapter.DB.Delete(_selectedWorkspace);
                                    SetWorkspace(null);
                                });
                            }
                            menu.ShowAsContext();
                        }
                        if (Event.current.type == EventType.Repaint) _wsButtonRect = GUILayoutUtility.GetLastRect();
                        GUILayout.EndVertical();
                        GUILayout.Space(2);
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }

                // search bar
                GUILayout.BeginHorizontal();
                if (_inMemoryMode == InMemoryModeState.None)
                {
                    EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                    EditorGUIUtility.labelWidth = 60;
                    EditorGUI.BeginChangeCheck();
                    _searchPhrase = SearchField.OnGUI(_searchPhrase, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // delay search to allow fast typing
                        _nextSearchTime = Time.realtimeSinceStartup + AI.Config.searchDelay;
                        // Clear active saved search when manually changing search phrase
                        _activeSavedSearchId = -1;
                    }
                    else if (_nextSearchTime > 0 && Time.realtimeSinceStartup > _nextSearchTime)
                    {
                        _nextSearchTime = 0;
                        if (AI.Config.searchAutomatically && !_searchPhrase.StartsWith("=")) dirty = true;
                    }
                    if (_allowLogic && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                    {
                        PerformSearch();
                    }
                    if (!AI.Config.searchAutomatically)
                    {
                        if (GUILayout.Button("Go", GUILayout.Width(30))) PerformSearch();
                    }

                    if (_searchPhrase != null && _searchPhrase.StartsWith("="))
                    {
                        EditorGUI.BeginChangeCheck();
                        GUILayout.Space(2);
                        _selectedExpertSearchField = EditorGUILayout.Popup(_selectedExpertSearchField, _expertSearchFields, GUILayout.Width(90));
                        if (EditorGUI.EndChangeCheck())
                        {
                            string field = _expertSearchFields[_selectedExpertSearchField];
                            if (!string.IsNullOrEmpty(field) && !field.StartsWith("-"))
                            {
                                _searchPhrase += field.Replace('/', '.');
                                SearchField.SetFocus();
                            }
                            _selectedExpertSearchField = 0;
                        }
                    }
                    UILine("search.actions.assistant", () =>
                    {
                        if (GUILayout.Button(UIStyles.Content("?", "Show example searches"), GUILayout.Width(20)))
                        {
                            AdvancedSearchUI searchUI = new AdvancedSearchUI();
                            searchUI.Init((searchPhrase, searchType) =>
                            {
                                _searchPhrase = searchPhrase;
                                if (searchType == null)
                                {
                                    AI.Config.searchType = 0;
                                }
                                else
                                {
                                    int typeIdx = Array.IndexOf(_types, searchType);
                                    if (typeIdx >= 0) AI.Config.searchType = typeIdx;
                                }
                                _requireSearchUpdate = true;
                            });
                            PopupWindow.Show(_querySamplesButtonRect, searchUI);
                        }
                        if (Event.current.type == EventType.Repaint) _querySamplesButtonRect = GUILayoutUtility.GetLastRect();
                    });
                    if (_fixedSearchTypeIdx < 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        GUILayout.Space(2);
                        AI.Config.searchType = EditorGUILayout.Popup(AI.Config.searchType, _types, GUILayout.ExpandWidth(false), GUILayout.MinWidth(85));
                        if (EditorGUI.EndChangeCheck())
                        {
                            AI.SaveConfig();
                            dirty = true;
                            // Clear active saved search when search type changes
                            _activeSavedSearchId = -1;
                        }
                    }
                    UIBlock("asset.actions.savedsearches", () =>
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_saveas", "|Save current search to quickly pull up the results later again"), EditorStyles.miniButton))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(string.IsNullOrEmpty(_searchPhrase) ? "My Search" : _searchPhrase, SaveSearch);
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }
                        GUILayout.Space(2);
                    });
                    ShowInMemoryButton();
                }
                else
                {
                    UIBlock("asset.hints.inmemoryactive", () =>
                    {
                        EditorGUILayout.HelpBox($"In-Memory search is active. The {_originalResultCount:N0} results of the initial search are now the foundation for any subsequent, much faster, search.", MessageType.Info);
                    });
                }

                GUILayout.EndHorizontal();

                if (_inMemoryMode != InMemoryModeState.None)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Refine Search:", GUILayout.Width(90));
                    EditorGUI.BeginChangeCheck();
                    _searchPhraseInMemory = SearchField.OnGUI(_searchPhraseInMemory, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // delay search to allow fast typing
                        _nextSearchTime = Time.realtimeSinceStartup + AI.Config.inMemorySearchDelay;
                    }
                    else if (_nextSearchTime > 0 && Time.realtimeSinceStartup > _nextSearchTime)
                    {
                        _nextSearchTime = 0;
                        UpdateFilteredFiles();
                    }
                    if (_allowLogic && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                    {
                        UpdateFilteredFiles();
                    }
                    ShowInMemoryButton();
                    GUILayout.EndHorizontal();
                }

                // error display
                if (!string.IsNullOrEmpty(_searchError))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(90);
                    EditorGUILayout.LabelField($"Error: {_searchError}", UIStyles.ColoredText(Color.red));
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();

                // result
                if (SGrid == null || (SGrid.contents != null && SGrid.contents.Length > 0 && _files == null)) PerformSearch(); // happens during recompilation
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                // assets
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                bool isAudio = AI.IsFileType(_selectedEntry?.Path, AI.AssetGroup.Audio);
                if (SGrid.contents != null && SGrid.contents.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, false, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO: implement paged endless scrolling, needs some pixel calculations though
                        // if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
                        // _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this);
                    }

                    // draw contents
                    EditorGUI.BeginChangeCheck();

                    int inspectorCount = (hideDetailsPane || !AI.Config.showSearchSideBar) ? 0 : 1;
                    SGrid.Draw(position.width, inspectorCount, AI.Config.searchTileSize, AI.Config.searchTileAspectRatio, UIStyles.searchTile, UIStyles.selectedSearchTile);

                    if (EditorGUI.EndChangeCheck() || (_allowLogic && _searchDone))
                    {
                        // interactions
                        SGrid.HandleMouseClicks();
                        SGrid.LimitSelection(_filteredFiles.Count());
                        _selectedEntry = _filteredFiles.ElementAt(SGrid.selectionTile);
                        _requireSearchSelectionUpdate = true;
                        DisposeAnimTexture();

                        // Used event is thrown if user manually selected the entry
                        _searchSelectionChangedManually = Event.current.type == EventType.Used;
                    }
                    GUILayout.EndScrollView();

                    // navigation
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();

                    UILine("search.actions.tilesize", () =>
                    {
                        EditorGUI.BeginChangeCheck();
                        AI.Config.searchTileSize = EditorGUILayout.IntSlider(AI.Config.searchTileSize, 50, 300, GUILayout.Width(150));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _lastTileSizeChange = DateTime.Now;
                            AI.SaveConfig();
                        }
                    });

                    GUILayout.FlexibleSpace();
                    if (_pageCount > 1)
                    {
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp) SetPage(1);
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown) SetPage(_pageCount);

                        EditorGUI.BeginDisabledGroup(_curPage <= 1);
                        if ((_searchInspectorTab != 2 && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow) ||
                            GUILayout.Button("<", GUILayout.ExpandWidth(false))) SetPage(_curPage - 1);
                        EditorGUI.EndDisabledGroup();

                        if (EditorGUILayout.DropdownButton(UIStyles.Content($"Page {_curPage:N0}/{_pageCount:N0}", $"{_resultCount:N0} results in total"), FocusType.Keyboard, UIStyles.centerPopup, GUILayout.MinWidth(100)))
                        {
                            DropDownUI pageUI = new DropDownUI();
                            pageUI.Init(1, _pageCount, _curPage, "Page ", null, SetPage);
                            PopupWindow.Show(_pageButtonRect, pageUI);
                        }
                        if (Event.current.type == EventType.Repaint) _pageButtonRect = GUILayoutUtility.GetLastRect();

                        EditorGUI.BeginDisabledGroup(_curPage >= _pageCount);
                        if ((_searchInspectorTab != 2 && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow) ||
                            GUILayout.Button(">", GUILayout.ExpandWidth(false))) SetPage(_curPage + 1);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{_resultCount:N0} results", UIStyles.centerLabel, GUILayout.ExpandWidth(true));
                    }
                    GUILayout.FlexibleSpace();

                    if (!hideDetailsPane && !searchMode)
                    {
                        UILine("search.actions.sidebar", () =>
                        {
                            if (GUILayout.Button(EditorGUIUtility.IconContent("d_unityeditor.hierarchywindow", "|Show/Hide Details Inspector"), EditorStyles.miniButtonRight))
                            {
                                AI.Config.showSearchSideBar = !AI.Config.showSearchSideBar;
                                AI.SaveConfig();
                            }
                        });
                    }

                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                else
                {
                    if (!_lockSelection) _selectedEntry = null;
                    if (!SearchWithoutInput() && !IsSearchFilterActive() && string.IsNullOrWhiteSpace(_searchPhrase))
                    {
                        GUILayout.Label("Enter search phrase to start searching", EditorStyles.centeredGreyMiniLabel, GUILayout.MinHeight(AI.Config.searchTileSize));
                    }
                    else
                    {
                        GUILayout.Label("No matching results", UIStyles.whiteCenter, GUILayout.MinHeight(AI.Config.searchTileSize));

                        bool isIndexing = AI.Actions.ActionsInProgress;
                        bool hasHiddenExtensions = AI.Config.searchType == 0 && !string.IsNullOrWhiteSpace(AI.Config.excludedExtensions);
                        bool hasHiddenPreviews = AI.Config.previewVisibility > 0;
                        if (isIndexing || hasHiddenExtensions || hasHiddenPreviews)
                        {
                            GUILayout.Label("Search result is potentially limited", EditorStyles.centeredGreyMiniLabel);
                            if (isIndexing) GUILayout.Label("Index is currently being updated", EditorStyles.centeredGreyMiniLabel);
                            if (hasHiddenExtensions)
                            {
                                EditorGUILayout.Space();
                                GUILayout.Label($"Hidden extensions: {AI.Config.excludedExtensions}", EditorStyles.centeredGreyMiniLabel);
                                GUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Ignore Once", GUILayout.Width(100))) PerformSearch(false, true);
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }
                            if (hasHiddenPreviews) GUILayout.Label("Results depend on preview availability", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.Space();
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                // inspector
                if (!hideDetailsPane && AI.Config.showSearchSideBar)
                {
                    EditorGUILayout.Space();

                    int labelWidth = 95;
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal();
                    List<string> strings = new List<string>
                    {
                        "Inspector",
                        "Filters" + (IsSearchFilterActive() ? "*" : "")
                    };
                    _searchInspectorTab = GUILayout.Toolbar(_searchInspectorTab, strings.ToArray());
                    UIBlock("search.actions.settings", () =>
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Manage View"), EditorStyles.miniButton, GUILayout.ExpandWidth(false), GUILayout.Height(18)))
                        {
                            _searchInspectorTab = -1;
                        }
                        GUILayout.Space(2);
                    });
                    GUILayout.EndHorizontal();

                    switch (_searchInspectorTab)
                    {
                        case -1:
                            GUILayout.BeginVertical(GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                            EditorGUILayout.Space();

                            _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                            EditorGUI.BeginChangeCheck();

                            int width = 135;

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Search In", "Field to use for finding assets when doing plain searches and no expert search."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.searchField = EditorGUILayout.Popup(AI.Config.searchField, _searchFields);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.searchPackageNames = EditorGUILayout.ToggleLeft(UIStyles.Content("Package Name", "Search also in package names for hits."), AI.Config.searchPackageNames);
                            GUILayout.EndHorizontal();

                            if (AI.Actions.CreateAICaptions)
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(width));
                                AI.Config.searchAICaptions = EditorGUILayout.ToggleLeft(UIStyles.Content("AI Captions", "Search also in AI captions for hits."), AI.Config.searchAICaptions);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Sort by", "Specify the sort order. Unsorted will result in the fastest experience."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.sortField = EditorGUILayout.Popup(AI.Config.sortField, _sortFields);
                            if (GUILayout.Button(AI.Config.sortDescending ? UIStyles.Content("˅", "Descending") : UIStyles.Content("˄", "Ascending"), GUILayout.Width(17)))
                            {
                                AI.Config.sortDescending = !AI.Config.sortDescending;
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Results", $"Maximum number of results to show. A (configurable) hard limit of {AI.Config.maxResultsLimit} will be enforced to keep Unity responsive."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.maxResults = EditorGUILayout.Popup(AI.Config.maxResults, _resultSizes);
                            GUILayout.EndHorizontal();

                            if (ShowAdvanced())
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(UIStyles.Content("In-Memory Results", "Maximum number of results to show when high-speed mode is active. The higher this value the more results you can browse but the more memory will also be consumed."), EditorStyles.boldLabel, GUILayout.Width(width));
                                AI.Config.maxInMemoryResults = EditorGUILayout.DelayedIntField(AI.Config.maxInMemoryResults);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Hide Extensions", "File extensions to hide from search results when searching for all file types, e.g. asset;json;txt. These will still be indexed."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.excludeExtensions = EditorGUILayout.Toggle(AI.Config.excludeExtensions, GUILayout.Width(16));
                            if (AI.Config.excludeExtensions)
                            {
                                AI.Config.excludedExtensions = EditorGUILayout.DelayedTextField(AI.Config.excludedExtensions);
                            }
                            GUILayout.EndHorizontal();

                            if (EditorGUI.EndChangeCheck())
                            {
                                dirty = true;
                                _curPage = 1;
                                AI.SaveConfig();
                            }

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Tile Text", "Text to be shown on the tile"), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.tileText = EditorGUILayout.Popup(AI.Config.tileText, _tileTitle);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                            {
                                dirty = true;
                                AI.SaveConfig();
                            }

                            EditorGUILayout.Space();

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Search While Typing", "Will search immediately while typing and update results constantly."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.searchAutomatically = EditorGUILayout.Toggle(AI.Config.searchAutomatically);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck()) AI.SaveConfig();

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Search Without Input", "Will always show search results also when no keywords or filters are set."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.searchWithoutInput = EditorGUILayout.Toggle(AI.Config.searchWithoutInput);
                            GUILayout.EndHorizontal();

                            if (ShowAdvanced())
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(UIStyles.Content("Sub-Packages", "Will search through sub-packages as well if a filter is set for a specific package."), EditorStyles.boldLabel, GUILayout.Width(width));
                                AI.Config.searchSubPackages = EditorGUILayout.Toggle(AI.Config.searchSubPackages);
                                GUILayout.EndHorizontal();
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                dirty = true;
                                AI.SaveConfig();
                            }

                            EditorGUI.BeginChangeCheck();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Auto-Play Audio", "Will automatically extract unity packages to play the sound file if they were not extracted yet. This is the most convenient option but will require sufficient hard disk space."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.autoPlayAudio = EditorGUILayout.Toggle(AI.Config.autoPlayAudio);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Ping Selected", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.pingSelected = EditorGUILayout.Toggle(AI.Config.pingSelected);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Ping Imported", "Highlight items in the Unity project tree after import."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.pingImported = EditorGUILayout.Toggle(AI.Config.pingImported);
                            GUILayout.EndHorizontal();

                            if (EditorGUI.EndChangeCheck()) AI.SaveConfig();

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Group Lists", "Add a second level hierarchy to dropdowns if they become too long to scroll."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.groupLists = EditorGUILayout.Toggle(AI.Config.groupLists);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                            {
                                AI.SaveConfig();
                                ReloadLookups();
                            }

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Double-Click Action", "Define what should happen when double-clicking on search results. Holding ALT will trigger the not selected alternative action."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.doubleClickBehavior = EditorGUILayout.Popup(AI.Config.doubleClickBehavior, _doubleClickOptions);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck()) AI.SaveConfig();

                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Dependency Calc", "Can automatically calculate dependencies for assets that are already extracted."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.autoCalculateDependencies = EditorGUILayout.Popup(AI.Config.autoCalculateDependencies, _dependencyOptions);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Previews", "Optionally restricts search results to those with either preview images available or not."), EditorStyles.boldLabel, GUILayout.Width(width));
                            AI.Config.previewVisibility = EditorGUILayout.Popup(AI.Config.previewVisibility, _previewOptions);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                            {
                                dirty = true;
                                AI.SaveConfig();
                            }

                            if (ShowAdvanced())
                            {
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField("UI", EditorStyles.largeLabel);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(UIStyles.Content("Tile Aspect Ratio", "Adjusts the height of the tiles."), EditorStyles.boldLabel, GUILayout.Width(width));
                                AI.Config.searchTileAspectRatio = EditorGUILayout.Slider(AI.Config.searchTileAspectRatio, 0.3f, 3f);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(UIStyles.Content("Tile Margins", "Adjusts the space between tiles."), EditorStyles.boldLabel, GUILayout.Width(width));
                                AI.Config.tileMargin = EditorGUILayout.IntSlider(AI.Config.tileMargin, -3, 30);
                                GUILayout.EndHorizontal();
                                if (EditorGUI.EndChangeCheck())
                                {
                                    _lastTileSizeChange = DateTime.Now;
                                    AI.SaveConfig();
                                }
                            }

                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                            break;

                        case 0:
                            if (SGrid.selectionCount <= 1)
                            {
                                GUILayout.BeginVertical(GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                                EditorGUILayout.Space();
                                _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                                if (_selectedEntry == null || string.IsNullOrEmpty(_selectedEntry.SafeName))
                                {
                                    // will happen after script reload
                                    EditorGUILayout.HelpBox("Select an asset for details", MessageType.Info);
                                }
                                else
                                {
                                    EditorGUILayout.LabelField("File", EditorStyles.largeLabel);
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(UIStyles.Content("Name", $"Internal Id: {_selectedEntry.Id}\nPreview State: {_selectedEntry.PreviewState.ToString()}\nGuid: {_selectedEntry.Guid}"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                    if (_selectedEntry.AssetSource == Asset.Source.AssetManager)
                                    {
                                        if (GUILayout.Button(UIStyles.Content(Path.GetFileName(_selectedEntry.GetPath(true))), UIStyles.wrappedLinkLabel, GUILayout.ExpandWidth(true)))
                                        {
                                            Application.OpenURL(_selectedEntry.GetAMAssetUrl());
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField(UIStyles.Content(Path.GetFileName(_selectedEntry.GetPath(true)), _selectedEntry.GetPath(true)), EditorStyles.wordWrappedLabel);
                                    }
                                    GUILayout.EndHorizontal();
                                    if (_selectedEntry.AssetSource == Asset.Source.Directory) UIBlock("asset.location", () => GUILabelWithText("Location", $"{Path.GetDirectoryName(_selectedEntry.GetPath(true))}", 95, null, true));
                                    if (!string.IsNullOrWhiteSpace(_selectedEntry.FileStatus)) UIBlock("asset.status", () => GUILabelWithText("Status", $"{_selectedEntry.FileStatus}"));
                                    UIBlock("asset.size", () => GUILabelWithText("Size", EditorUtility.FormatBytes(_selectedEntry.Size)));
                                    if (_selectedEntry.Width > 0) UIBlock("asset.dimensions", () => GUILabelWithText("Dimensions", $"{_selectedEntry.Width}x{_selectedEntry.Height} px"));
                                    if (_selectedEntry.Length > 0) UIBlock("asset.length", () => GUILabelWithText("Length", $"{_selectedEntry.Length:N2} seconds"));
                                    if (ShowAdvanced() || _selectedEntry.InProject) GUILabelWithText("In Project", _selectedEntry.InProject ? "Yes" : "No");
                                    if (_selectedEntry.IsDownloaded || _selectedEntry.IsMaterialized)
                                    {
                                        bool needsDependencyScan = false;
                                        if (_selectedEntry.AssetSource == Asset.Source.AssetManager || DependencyAnalysis.NeedsScan(_selectedEntry.Type))
                                        {
                                            UIBlock("asset.dependencies", () =>
                                            {
                                                switch (_selectedEntry.DependencyState)
                                                {
                                                    case AssetInfo.DependencyStateOptions.Unknown:
                                                        needsDependencyScan = true;
                                                        GUILayout.BeginHorizontal();
                                                        EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                                        EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                                        if (GUILayout.Button("Calculate", GUILayout.ExpandWidth(false)))
                                                        {
                                                            // must run in same thread
                                                            _ = CalculateDependencies(_selectedEntry);
                                                        }
                                                        EditorGUI.EndDisabledGroup();
                                                        GUILayout.EndHorizontal();
                                                        break;

                                                    case AssetInfo.DependencyStateOptions.Calculating:
                                                        GUILabelWithText("Dependencies", "Calculating...");
                                                        break;

                                                    case AssetInfo.DependencyStateOptions.NotPossible:
                                                        GUILabelWithText("Dependencies", "Cannot determine (binary)");
                                                        break;

                                                    case AssetInfo.DependencyStateOptions.Failed:
                                                        GUILabelWithText("Dependencies", "Failed to determine");
                                                        break;

                                                    case AssetInfo.DependencyStateOptions.Done:
                                                        GUILayout.BeginHorizontal();
                                                        if (ShowAdvanced())
                                                        {
                                                            string scriptDeps = _selectedEntry.ScriptDependencies?.Count > 0 ? $" + {_selectedEntry.ScriptDependencies?.Count} scripts" : string.Empty;
                                                            GUILabelWithText("Dependencies", $"{_selectedEntry.MediaDependencies?.Count}{scriptDeps} ({EditorUtility.FormatBytes(_selectedEntry.DependencySize)})");
                                                        }
                                                        else
                                                        {
                                                            GUILabelWithText("Dependencies", $"{_selectedEntry.Dependencies?.Count}");
                                                        }
                                                        if (_selectedEntry.Dependencies.Count > 0 && GUILayout.Button("Show..."))
                                                        {
                                                            DependenciesUI depUI = DependenciesUI.ShowWindow();
                                                            depUI.Init(_selectedEntry);
                                                        }
                                                        GUILayout.EndHorizontal();
                                                        break;
                                                }
                                            });
                                        }

                                        if (!searchMode)
                                        {
                                            if (!_selectedEntry.InProject && string.IsNullOrEmpty(_importFolder))
                                            {
                                                EditorGUILayout.Space();
                                                EditorGUILayout.LabelField("Select a folder in Project View for import options", EditorStyles.centeredGreyMiniLabel);
                                                EditorGUI.BeginDisabledGroup(true);
                                                GUILayout.Button("Import File");
                                                EditorGUI.EndDisabledGroup();
                                            }
                                            else
                                            {
                                                if (ShowAdvanced())
                                                {
                                                    EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                                    if ((!_selectedEntry.InProject || ShowAdvanced()) && !string.IsNullOrEmpty(_importFolder))
                                                    {
                                                        string command = _selectedEntry.InProject ? "Reimport" : "Import";
                                                        GUILabelWithText($"{command} To", _importFolder, 95, null, true);
                                                        EditorGUILayout.Space();
                                                        if (needsDependencyScan)
                                                        {
                                                            EditorGUILayout.LabelField("Dependency scan needed to determine import options.", EditorStyles.centeredGreyMiniLabel);
                                                            EditorGUI.BeginDisabledGroup(true);
                                                            GUILayout.Button("Import File");
                                                            EditorGUI.EndDisabledGroup();
                                                        }
                                                        else
                                                        {
                                                            if (GUILayout.Button($"{command} File" + (_selectedEntry.DependencySize > 0 ? " Only" : ""))) CopyTo(_selectedEntry, _importFolder, false, false, true, false, _selectedEntry.InProject);
                                                            if (_selectedEntry.DependencySize > 0 && DependencyAnalysis.NeedsScan(_selectedEntry.Type))
                                                            {
                                                                if (GUILayout.Button($"{command} With Dependencies")) CopyTo(_selectedEntry, _importFolder, true, false, true, false, _selectedEntry.InProject);
                                                                if (_selectedEntry.ScriptDependencies.Count > 0)
                                                                {
                                                                    if (GUILayout.Button($"{command} With Dependencies + Scripts")) CopyTo(_selectedEntry, _importFolder, true, true, true, false, _selectedEntry.InProject);
                                                                }

                                                                EditorGUILayout.Space();
                                                            }
                                                        }
                                                    }
                                                    EditorGUI.EndDisabledGroup();
                                                }
                                                else
                                                {
                                                    EditorGUILayout.Space();
                                                    if (!_selectedEntry.InProject)
                                                    {
                                                        EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                                        if (GUILayout.Button("Import")) CopyTo(_selectedEntry, _importFolder, true);
                                                        EditorGUI.EndDisabledGroup();
                                                    }
                                                }
                                            }
                                        }

#if !ASSET_INVENTORY_NOAUDIO
                                        if (isAudio)
                                        {
                                            UIBlock("asset.actions.audiopreview", () =>
                                            {
                                                bool isPreviewClipPlaying = EditorAudioUtility.IsPreviewClipPlaying();

                                                GUILayout.BeginHorizontal();
                                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_PlayButton", "|Play"), GUILayout.Width(40))) PlayAudio(_selectedEntry);
                                                EditorGUI.BeginDisabledGroup(!isPreviewClipPlaying);
                                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreMatQuad", "|Stop"), GUILayout.Width(40))) EditorAudioUtility.StopAllPreviewClips();
                                                EditorGUI.EndDisabledGroup();
                                                EditorGUILayout.Space();
                                                EditorGUI.BeginChangeCheck();
                                                AI.Config.autoPlayAudio = GUILayout.Toggle(AI.Config.autoPlayAudio, "Auto-Play");
                                                AI.Config.loopAudio = GUILayout.Toggle(AI.Config.loopAudio, "Loop");
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    AI.SaveConfig();
                                                    if (AI.Config.autoPlayAudio) PlayAudio(_selectedEntry);
                                                }
                                                GUILayout.EndHorizontal();

                                                // scrubbing (Unity 2020.1+)
                                                if (isPreviewClipPlaying && EditorAudioUtility.LastPlayedPreviewClip != null)
                                                {
                                                    AudioClip currentClip = EditorAudioUtility.LastPlayedPreviewClip;
                                                    EditorGUI.BeginChangeCheck();
                                                    float newVal = EditorGUILayout.Slider(EditorAudioUtility.GetPreviewClipPosition(), 0, currentClip.length);
                                                    if (EditorGUI.EndChangeCheck())
                                                    {
                                                        AI.StopAudio();
                                                        EditorAudioUtility.PlayPreviewClip(currentClip, Mathf.RoundToInt(currentClip.samples * newVal / currentClip.length), false);
                                                    }
                                                }
                                                EditorGUILayout.Space();
                                            });
                                        }
#endif

                                        if (_selectedEntry.InProject && !AI.Config.pingSelected)
                                        {
                                            UIBlock("asset.actions.ping", () =>
                                            {
                                                if (GUILayout.Button("Ping")) PingAsset(_selectedEntry);
                                            });
                                        }

                                        if (!searchMode)
                                        {
                                            EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                            UIBlock("asset.actions.open", () =>
                                            {
                                                if (GUILayout.Button(UIStyles.Content("Open", "Open the file with the assigned system application")))
                                                {
                                                    Open(_selectedEntry);
                                                }
                                            });
                                            UIBlock("asset.actions.openexplorer", () =>
                                            {
                                                if (GUILayout.Button(Application.platform == RuntimePlatform.OSXEditor ? "Show in Finder" : "Show in Explorer"))
                                                {
                                                    OpenExplorer(_selectedEntry);
                                                }
                                            });
                                            UIBlock("asset.actions.recreatepreview", () =>
                                            {
                                                if (((ShowAdvanced()
                                                        || _selectedEntry.PreviewState == AssetFile.PreviewOptions.Error
                                                        || _selectedEntry.PreviewState == AssetFile.PreviewOptions.None
                                                        || _selectedEntry.PreviewState == AssetFile.PreviewOptions.Redo
                                                        || _selectedEntry.PreviewState == AssetFile.PreviewOptions.RedoMissing))
                                                    && PreviewManager.IsPreviewable(_selectedEntry.FileName, true, _selectedEntry)
                                                    && GUILayout.Button("Recreate Preview"))
                                                {
                                                    RecreatePreviews(new List<AssetInfo> {_selectedEntry});
                                                }
                                            });
                                            UIBlock("asset.actions.recreateaicaption", () =>
                                            {
                                                if (AI.Actions.CreateAICaptions
                                                    && (ShowAdvanced() || string.IsNullOrWhiteSpace(_selectedEntry.AICaption))
                                                    && GUILayout.Button(string.IsNullOrWhiteSpace(_selectedEntry.AICaption) ? "Create AI Caption" : "Recreate AI Caption"))
                                                {
                                                    RecreateAICaptions(new List<AssetInfo> {_selectedEntry});
                                                }
                                            });
                                            EditorGUI.EndDisabledGroup();

#if USE_ASSET_MANAGER && USE_CLOUD_IDENTITY
                                            if (AI.Actions.IndexAssetManager)
                                            {
                                                EditorGUI.BeginDisabledGroup(CloudAssetManagement.IsBusy);
                                                EditorGUILayout.Space();
                                                if (_selectedEntry.AssetSource == Asset.Source.AssetManager)
                                                {
                                                    if (_selectedEntry.ParentInfo == null)
                                                    {
                                                        if (GUILayout.Button(UIStyles.Content("Delete from Project", "Delete the file from the Asset Manager project.")))
                                                        {
                                                            DeleteAssetsFromProject(new List<AssetInfo> {_selectedEntry});
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (GUILayout.Button(UIStyles.Content("Remove from Collection", "Remove the file from the Asset Manager collection.")))
                                                        {
                                                            RemoveAssetsFromCollection(new List<AssetInfo> {_selectedEntry});
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (GUILayout.Button("Upload to Asset Manager..."))
                                                    {
                                                        ProjectSelectionUI projectUI = new ProjectSelectionUI();
                                                        projectUI.Init(project =>
                                                        {
                                                            AddAssetsToProject(project, new List<AssetInfo> {_selectedEntry});
                                                        });
                                                        projectUI.SetAssets(_assets);
                                                        PopupWindow.Show(_amUploadButtonRect, projectUI);
                                                    }
                                                    if (Event.current.type == EventType.Repaint) _amUploadButtonRect = GUILayoutUtility.GetLastRect();
                                                }
                                                EditorGUI.EndDisabledGroup();
                                            }
#endif

                                            UIBlock("asset.actions.delete", () =>
                                            {
                                                EditorGUILayout.Space();
                                                if (GUILayout.Button(UIStyles.Content("Delete from Index", "Will delete the indexed file from the database. The package will need to be reindexed in order for it to appear again.")))
                                                {
                                                    DeleteFromIndex(_selectedEntry);
                                                }
                                            });
                                        }
                                        if (!_selectedEntry.IsMaterialized && !_blockingInProgress)
                                        {
                                            UIBlock("asset.actions.extraction", () =>
                                            {
                                                if (_selectedEntry.AssetSource == Asset.Source.AssetManager)
                                                {
                                                    EditorGUILayout.LabelField($"{EditorUtility.FormatBytes(_selectedEntry.Size)} will be downloaded first", EditorStyles.centeredGreyMiniLabel);
                                                }
                                                else
                                                {
                                                    EditorGUILayout.LabelField($"{EditorUtility.FormatBytes(_selectedEntry.GetRoot().PackageSize)} will be extracted first", EditorStyles.centeredGreyMiniLabel);
                                                }
                                            });
                                        }
                                    }
                                    else if (_selectedEntry.IsLocationUnmappedRelative())
                                    {
                                        EditorGUILayout.HelpBox("The location of this package is stored relative and no mapping has been done yet for this system in the settings: " + _selectedEntry.Location, MessageType.Info);
                                    }

                                    if (_blockingInProgress)
                                    {
                                        EditorGUI.EndDisabledGroup();
                                        EditorGUILayout.BeginHorizontal();
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField("Working...", EditorStyles.miniLabel, GUILayout.Width(55));
                                        if (_extraction != null)
                                        {
                                            EditorGUI.BeginDisabledGroup(_extraction.IsCancellationRequested);
                                            if (GUILayout.Button(UIStyles.Content("x", "Cancel Activity"), EditorStyles.miniButton))
                                            {
                                                _extraction?.Cancel();
                                            }
                                            EditorGUI.EndDisabledGroup();
                                        }
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.EndHorizontal();
                                        EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                    }

                                    if (!string.IsNullOrWhiteSpace(_selectedEntry.AICaption))
                                    {
                                        EditorGUILayout.LabelField(_selectedEntry.AICaption, EditorStyles.wordWrappedLabel);
                                    }

                                    UIBlock("asset.actions.tag", () =>
                                    {
                                        // tags
                                        DrawAddFileTag(new List<AssetInfo> {_selectedEntry});

                                        if (_selectedEntry.AssetTags != null && _selectedEntry.AssetTags.Count > 0)
                                        {
                                            float x = 0f;
                                            foreach (TagInfo tagInfo in _selectedEntry.AssetTags)
                                            {
                                                x = CalcTagSize(x, tagInfo.Name);
                                                UIStyles.DrawTag(tagInfo, () =>
                                                {
                                                    Tagging.RemoveAssignment(_selectedEntry, tagInfo, true, true);
                                                    _requireAssetTreeRebuild = true;
                                                    _requireSearchUpdate = true;
                                                });
                                            }
                                        }
                                        GUILayout.EndHorizontal();
                                    });

                                    EditorGUILayout.Space();

                                    // render preview if available
                                    if (_previewEditor != null)
                                    {
                                        if (_previewEditor.HasPreviewGUI())
                                        {
                                            AI.Config.showPreviews = EditorGUILayout.BeginFoldoutHeaderGroup(AI.Config.showPreviews, "Preview");
                                            if (AI.Config.showPreviews)
                                            {
                                                // Allocate space for the preview
                                                Rect previewRect = GUILayoutUtility.GetRect(AI.Config.previewSize, AI.Config.previewSize, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                                                _previewEditor.OnPreviewGUI(previewRect, EditorStyles.whiteLabel);
                                            }
                                            EditorGUILayout.EndFoldoutHeaderGroup();
                                        }
                                    }
                                    EditorGUILayout.Space();

                                    DrawPackageInfo(_selectedEntry, false, !searchMode, false);
                                }

                                GUILayout.EndScrollView();
                                GUILayout.EndVertical();
                            }
                            else
                            {
                                GUILayout.BeginVertical(GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField("Bulk Actions", EditorStyles.largeLabel);
                                _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                                UIBlock("asset.bulk.count", () => GUILabelWithText("Selected", $"{SGrid.selectionCount:N0}"));
                                UIBlock("asset.bulk.packages", () => GUILabelWithText("Packages", $"{SGrid.selectionPackageCount:N0}"));
                                UIBlock("asset.bulk.size", () => GUILabelWithText("Size", EditorUtility.FormatBytes(SGrid.selectionSize)));

                                int inProject = SGrid.selectionItems.Count(item => item.InProject);
                                UIBlock("asset.bulk.inproject", () =>
                                {
                                    GUILabelWithText("In Project", $"{inProject:N0}/{SGrid.selectionCount:N0}");
                                });

                                EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                if (!searchMode && !string.IsNullOrEmpty(_importFolder))
                                {
                                    if (inProject < SGrid.selectionCount)
                                    {
                                        UIBlock("asset.bulk.actions.import", () =>
                                        {
                                            string command = "Import";
                                            if (inProject > 0) command += $" {SGrid.selectionCount - inProject} Remaining";

                                            GUILabelWithText("Import To", _importFolder, 95, null, true);
                                            EditorGUILayout.Space();
                                            if (GUILayout.Button($"{command} Files")) ImportBulkFiles(SGrid.selectionItems);
                                        });
                                    }
                                }

                                if (!searchMode)
                                {
                                    UIBlock("asset.bulk.actions.open", () =>
                                    {
                                        if (GUILayout.Button(UIStyles.Content("Open", "Open the files with the assigned system application")))
                                        {
                                            bool show = true;
                                            if (SGrid.selectionItems.Count > AI.Config.massOpenWarnThreshold)
                                            {
                                                show = EditorUtility.DisplayDialog("Open Files", $"You are about to open {SGrid.selectionItems.Count} files. This may take a while and will open a lot of windows.\n\nDo you want to continue?", "Continue", "Cancel");
                                            }
                                            if (show) SGrid.selectionItems.ForEach(Open);
                                        }
                                    });
                                    UIBlock("asset.bulk.actions.openexplorer", () =>
                                    {
                                        if (GUILayout.Button(Application.platform == RuntimePlatform.OSXEditor ? "Show in Finder" : "Show in Explorer"))
                                        {
                                            bool show = true;
                                            if (SGrid.selectionItems.Count > AI.Config.massOpenWarnThreshold)
                                            {
                                                show = EditorUtility.DisplayDialog("Show Files", $"You are about to open {SGrid.selectionItems.Count} locations. This may take a while and will open a lot of windows.\n\nDo you want to continue?", "Continue", "Cancel");
                                            }
                                            if (show) SGrid.selectionItems.ForEach(OpenExplorer);
                                        }
                                    });
                                    UIBlock("asset.bulk.actions.recreatepreviews", () =>
                                    {
                                        EditorGUI.BeginDisabledGroup(_blockingInProgress);
                                        if (GUILayout.Button("Recreate Previews")) RecreatePreviews(SGrid.selectionItems);
                                        EditorGUI.EndDisabledGroup();
                                    });
                                    UIBlock("asset.bulk.actions.recreateaicaptions", () =>
                                    {
                                        if (ShowAdvanced() && AI.Actions.CreateAICaptions && GUILayout.Button("Recreate AI Captions"))
                                        {
                                            RecreateAICaptions(SGrid.selectionItems);
                                        }
                                    });

#if USE_ASSET_MANAGER && USE_CLOUD_IDENTITY
                            if (AI.Actions.IndexAssetManager)
                            {
                                EditorGUI.BeginDisabledGroup(CloudAssetManagement.IsBusy);
                                EditorGUILayout.Space();
                                if (_assetFileAMProjectCount + _assetFileAMCollectionCount > 0)
                                {
                                    if (_assetFileAMProjectCount > 0)
                                    {
                                        if (GUILayout.Button(UIStyles.Content("Delete from Project", "Delete the files from the Asset Manager project.")))
                                        {
                                            DeleteAssetsFromProject(SGrid.selectionItems);
                                        }
                                    }
                                    if (_assetFileAMCollectionCount > 0)
                                    {
                                        if (GUILayout.Button(UIStyles.Content("Remove from Collection", "Remove the files from the Asset Manager collection.")))
                                        {
                                            RemoveAssetsFromCollection(SGrid.selectionItems);
                                        }
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Upload to Asset Manager..."))
                                    {
                                        ProjectSelectionUI projectUI = new ProjectSelectionUI();
                                        projectUI.Init(project =>
                                        {
                                            AddAssetsToProject(project, SGrid.selectionItems);
                                        });
                                        projectUI.SetAssets(_assets);
                                        PopupWindow.Show(_amUploadButtonRect, projectUI);
                                    }
                                    if (Event.current.type == EventType.Repaint) _amUploadButtonRect = GUILayoutUtility.GetLastRect();
                                }
                                EditorGUI.EndDisabledGroup();
                            }
#endif
                                    UIBlock("asset.bulk.actions.export", () =>
                                    {
                                        if (GUILayout.Button("Export Files..."))
                                        {
                                            ExportUI exportUI = ExportUI.ShowWindow();
                                            exportUI.Init(SGrid.selectionItems, true, 2);
                                        }
                                    });
                                    UIBlock("asset.bulk.actions.delete", () =>
                                    {
                                        EditorGUILayout.Space();
                                        if (GUILayout.Button(UIStyles.Content("Delete from Index", "Will delete the indexed files from the database. The package will need to be reindexed in order for it to appear again.")))
                                        {
                                            SGrid.selectionItems.ForEach(DeleteFromIndex);
                                        }
                                    });
                                }
                                EditorGUI.EndDisabledGroup();
                                if (_blockingInProgress) EditorGUILayout.LabelField("Operation in progress...", UIStyles.centeredWhiteMiniLabel);

                                UIBlock("asset.bulk.actions.tag", () =>
                                {
                                    // tags
                                    DrawAddFileTag(SGrid.selectionItems);

                                    float x = 0f;
                                    List<string> toRemove = new List<string>();
                                    foreach (KeyValuePair<string, Tuple<int, Color>> bulkTag in _assetFileBulkTags)
                                    {
                                        string tagName = $"{bulkTag.Key} ({bulkTag.Value.Item1})";
                                        x = CalcTagSize(x, tagName);
                                        UIStyles.DrawTag(tagName, bulkTag.Value.Item2, () =>
                                        {
                                            Tagging.RemoveAssetAssignments(SGrid.selectionItems, bulkTag.Key, true);
                                            toRemove.Add(bulkTag.Key);
                                        }, UIStyles.TagStyle.Remove);
                                    }
                                    toRemove.ForEach(key => _assetFileBulkTags.Remove(key));
                                    GUILayout.EndHorizontal();
                                });

                                GUILayout.EndScrollView();
                                GUILayout.EndVertical();
                            }
                            break;

                        case 1:
                            EditorGUI.BeginDisabledGroup(_inMemoryMode != InMemoryModeState.None);
                            GUILayout.BeginVertical(GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                            EditorGUILayout.Space();

                            _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

                            EditorGUI.BeginChangeCheck();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Package Tag", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedPackageTag = EditorGUILayout.Popup(_selectedPackageTag, _tagNames, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("File Tag", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedFileTag = EditorGUILayout.Popup(_selectedFileTag, _tagNames, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Package", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedAsset = EditorGUILayout.Popup(_selectedAsset, _assetNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Publisher", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedPublisher = EditorGUILayout.Popup(_selectedPublisher, _publisherNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedCategory = EditorGUILayout.Popup(_selectedCategory, _categoryNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                            GUILayout.EndHorizontal();

                            if (IsFilterApplicable("ImageType"))
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Image Type", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                _selectedImageType = EditorGUILayout.Popup(_selectedImageType, _imageTypeOptions, GUILayout.ExpandWidth(true));
                                GUILayout.EndHorizontal();
                            }

                            if (IsFilterApplicable("Width"))
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Width", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                if (GUILayout.Button(_checkMaxWidth ? "<=" : ">=", GUILayout.Width(25))) _checkMaxWidth = !_checkMaxWidth;
                                _searchWidth = EditorGUILayout.DelayedTextField(_searchWidth, GUILayout.Width(58));
                                EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                                GUILayout.EndHorizontal();
                            }

                            if (IsFilterApplicable("Height"))
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Height", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                if (GUILayout.Button(_checkMaxHeight ? "<=" : ">=", GUILayout.Width(25))) _checkMaxHeight = !_checkMaxHeight;
                                _searchHeight = EditorGUILayout.DelayedTextField(_searchHeight, GUILayout.Width(58));
                                EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                                GUILayout.EndHorizontal();
                            }

                            if (IsFilterApplicable("Length"))
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Length", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                if (GUILayout.Button(_checkMaxLength ? "<=" : ">=", GUILayout.Width(25))) _checkMaxLength = !_checkMaxLength;
                                _searchLength = EditorGUILayout.DelayedTextField(_searchLength, GUILayout.Width(58));
                                EditorGUILayout.LabelField("sec", EditorStyles.miniLabel);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("File Size", "File size in kilobytes"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            if (GUILayout.Button(_checkMaxSize ? "<=" : ">=", GUILayout.Width(25))) _checkMaxSize = !_checkMaxSize;
                            _searchSize = EditorGUILayout.DelayedTextField(_searchSize, GUILayout.Width(58));
                            EditorGUILayout.LabelField("kb", EditorStyles.miniLabel);
                            GUILayout.EndHorizontal();

                            if (AI.Actions.ExtractColors)
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                _selectedColorOption = EditorGUILayout.Popup(_selectedColorOption, _colorOptions, GUILayout.Width(labelWidth + 2));
                                if (_selectedColorOption > 0) _selectedColor = EditorGUILayout.ColorField(_selectedColor);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            _selectedPackageTypes = EditorGUILayout.Popup(_selectedPackageTypes, _packageListingOptions, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("SRPs", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
                            _selectedPackageSRPs = EditorGUILayout.Popup(_selectedPackageSRPs, _srpOptions, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();

                            if (EditorGUI.EndChangeCheck())
                            {
                                dirty = true;
                                // Clear active saved search when filters change
                                _activeSavedSearchId = -1;
                            }

                            EditorGUILayout.Space();
                            if (IsSearchFilterActive() && GUILayout.Button("Reset Filters"))
                            {
                                ResetSearch(true, false);
                                _requireSearchUpdate = true;
                            }

                            GUILayout.EndScrollView();
                            GUILayout.FlexibleSpace();
                            if (AI.DEBUG_MODE && GUILayout.Button("Reload Lookups")) ReloadLookups();

                            GUILayout.EndVertical();
                            EditorGUI.EndDisabledGroup();
                            break;

                    }
                    if (searchMode)
                    {
                        if (!_selectedEntry.InProject && string.IsNullOrEmpty(_importFolder))
                        {
                            EditorGUILayout.HelpBox("Select a folder in the Project View to import to", MessageType.Warning);
                        }
                        else
                        {
                            if (GUILayout.Button("Select", GUILayout.Height(40))) ExecuteSingleAction();
                        }
                    }
                    else
                    {
                        if (!ShowAdvanced() && AI.Config.showHints)
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField("Hold down CTRL for additional options.", EditorStyles.centeredGreyMiniLabel);
                        }
                    }

                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                SGrid.HandleKeyboardCommands();
                HandleTagShortcuts();

                if (dirty)
                {
                    _requireSearchUpdate = true;
                    _keepSearchResultPage = false;
                }
                EditorGUIUtility.labelWidth = 0;
            }
        }

        private void ShowInMemoryButton()
        {
            UIBlock("asset.actions.inmemorymode", () =>
            {
                EditorGUI.BeginDisabledGroup(_resultCount <= 0);
                EditorGUI.BeginChangeCheck();
                bool inMemory = _inMemoryMode != InMemoryModeState.None;
                inMemory = GUILayout.Toggle(inMemory, EditorGUIUtility.IconContent("d_lighting", "|High-Speed Mode: Load all current results into memory for extremely fast sub-searches."), EditorStyles.miniButton, GUILayout.Width(28), GUILayout.ExpandHeight(true));
                if (EditorGUI.EndChangeCheck())
                {
                    _inMemoryMode = inMemory ? InMemoryModeState.Init : InMemoryModeState.None;
                    _requireSearchUpdate = true;
                    _searchPhraseInMemory = "";

                    RefreshSearchField();
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(2);
            });
        }

        private static void RefreshSearchField()
        {
            // force IMGUI to drop its TextEditor cache
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
            EditorGUIUtility.editingTextField = false;
        }

        private bool SearchWithoutInput()
        {
            return workspaceMode ? AI.Config.wsSearchWithoutInput : AI.Config.searchWithoutInput;
        }

        private void HandleSearchSelectionChanged()
        {
            if (AI.DEBUG_MODE) Debug.LogWarning("HandleSearchSelectionChanged");

            _requireSearchSelectionUpdate = false;
            _selectionHandlerAdded = false;
            EditorApplication.delayCall -= HandleSearchSelectionChanged;

            AI.StopAudio();
            DisposeAnimTexture();
            bool isAudio = AI.IsFileType(_selectedEntry?.Path, AI.AssetGroup.Audio);
            if (_selectedEntry != null)
            {
                _selectedEntry.Refresh();
                AI.ResolveChildren(new List<AssetInfo> {_selectedEntry}, _assets);
                AI.GetObserver().SetPrioritized(new List<AssetInfo> {_selectedEntry});
                _selectedEntry.PackageDownloader.RefreshState();

                _selectedEntry.CheckIfInProject();
                _selectedEntry.IsMaterialized = AI.IsMaterialized(_selectedEntry.ToAsset(), _selectedEntry);
                _ = AssetUtils.LoadPackageTexture(_selectedEntry);
                LoadAnimTexture(_selectedEntry);

                if (AI.Config.autoCalculateDependencies == 1)
                {
                    // if entry is already materialized calculate dependencies immediately
                    if (!_blockingInProgress &&
                        _selectedEntry.DependencyState == AssetInfo.DependencyStateOptions.Unknown &&
                        _selectedEntry.IsMaterialized &&
                        DependencyAnalysis.NeedsScan(_selectedEntry.Type))
                    {
                        // must run in same thread
                        _ = CalculateDependencies(_selectedEntry);
                    }
                }

                RecreatePreviewEditor();

                if (!_searchDone && AI.Config.pingSelected && _selectedEntry.InProject) PingAsset(_selectedEntry);
            }
            _searchDone = false;

            if (_searchSelectionChangedManually)
            {
                _searchSelectionChangedManually = false;
                _searchInspectorTab = 0;
                if (instantSelection)
                {
                    ExecuteSingleAction();
                }
                else if (AI.Config.autoPlayAudio && isAudio) PlayAudio(_selectedEntry);
            }
        }

        private bool IsSearchFilterActive()
        {
            return _selectedPackageTag > 0
                || _selectedFileTag > 0
                || _selectedAsset > 0
                || _selectedPublisher > 0
                || _selectedCategory > 0
                || _selectedImageType > 0
                || _selectedColorOption > 0
                || _selectedPackageTypes != 1
                || _selectedPackageSRPs > 0
                || !string.IsNullOrEmpty(_searchWidth)
                || !string.IsNullOrEmpty(_searchHeight)
                || !string.IsNullOrEmpty(_searchLength)
                || !string.IsNullOrEmpty(_searchSize);
        }

        private async void ImportBulkFiles(List<AssetInfo> items)
        {
            _blockingInProgress = true;
            foreach (AssetInfo info in items)
            {
                // must be done consecutively to avoid IO conflicts
                await AI.CopyTo(info, _importFolder, true);
            }
            _blockingInProgress = false;
        }

        private void DrawAddFileTag(List<AssetInfo> assets)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(UIStyles.Content("Add Tag..."), GUILayout.Width(70)))
            {
                TagSelectionUI tagUI = new TagSelectionUI();
                tagUI.Init(TagAssignment.Target.Asset, CalculateSearchBulkSelection);
                tagUI.SetAssets(assets);
                PopupWindow.Show(_tag2ButtonRect, tagUI);
            }
            if (Event.current.type == EventType.Repaint) _tag2ButtonRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(15);
        }

        private async void ExecuteSingleAction()
        {
            if (_selectedEntry == null) return;
            if (!_selectedEntry.InProject && string.IsNullOrEmpty(_importFolder))
            {
                EditorUtility.DisplayDialog("Missing Target", "Select a target folder in the Project View first to proceed.", "OK");
                return;
            }

            List<AssetInfo> files = new List<AssetInfo>();
            Dictionary<string, AssetInfo> identifiedTextures = null;
            if (textureMode)
            {
                identifiedTextures = IdentifyTextures(_selectedEntry);
                files.AddRange(identifiedTextures.Values); // TODO: one file will be duplicate, not an issue but will save time to eliminate it
            }
            else
            {
                files.Add(_selectedEntry);
            }

            foreach (AssetInfo info in files)
            {
                info.CheckIfInProject();
                if (!info.InProject)
                {
                    _blockingInProgress = true;
                    _lockSelection = true;

                    // download on-demand
                    if (!info.IsDownloaded)
                    {
                        if (info.IsAbandoned)
                        {
                            Debug.LogError($"Cannot download {info.GetDisplayName()} as it is an abandoned package.");
                            _lockSelection = false;
                            return;
                        }

                        AI.GetObserver().Attach(info);
                        if (info.PackageDownloader.IsDownloadSupported())
                        {
                            _curOperation = $"Downloading {info.GetDisplayName()}...";
                            info.PackageDownloader.Download();
                            do
                            {
                                await Task.Delay(200);

                                info.PackageDownloader.RefreshState();
                                float progress = info.PackageDownloader.GetState().progress * 100f;
                                _curOperation = $"Downloading {info.GetDisplayName()}: {progress:N0}%...";
                            } while (info.IsDownloading());
                            await Task.Delay(3000); // ensure all file operations have finished, can otherwise lead to issues
                            info.Refresh();
                        }
                    }

                    _curOperation = $"Extracting & Importing '{info.FileName}'...";
                    await AI.CopyTo(info, _importFolder, true);
                    _blockingInProgress = false;

                    if (!info.InProject)
                    {
                        Debug.LogError("The file could not be materialized into the project.");
                        _lockSelection = false;
                        return;
                    }
                }
            }

            Close();
            AI.StopAudio();

            if (textureMode)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (KeyValuePair<string, AssetInfo> file in identifiedTextures)
                {
                    result.Add(file.Key, file.Value.ProjectPath);
                }
                searchModeTextureCallback?.Invoke(result);
            }
            else
            {
                searchModeCallback?.Invoke(_selectedEntry.ProjectPath);
            }
            _lockSelection = false;
        }

        private Dictionary<string, AssetInfo> IdentifyTextures(AssetInfo info)
        {
            TextureNameSuggester tns = new TextureNameSuggester();
            Dictionary<string, string> files = tns.SuggestFileNames(info.Path, path =>
            {
                string sep = info.Path.Contains("/") ? "/" : "\\";
                string toCheck = info.Path.Substring(0, info.Path.LastIndexOf(sep) + 1) + Path.GetFileName(path);
                AssetInfo ai = AI.GetAssetByPath(toCheck, info.ToAsset());
                return ai?.Path; // capitalization could be different from actual validation request, so use result
            });

            Dictionary<string, AssetInfo> result = new Dictionary<string, AssetInfo>();
            foreach (KeyValuePair<string, string> file in files)
            {
                AssetInfo ai = AI.GetAssetByPath(file.Value, info.ToAsset());
                if (ai != null) result.Add(file.Key, ai);
            }
            return result;
        }

        private void DeleteFromIndex(AssetInfo info)
        {
            AI.ForgetAssetFile(info);
            _requireSearchUpdate = true;
        }

        private async void RecreatePreviews(List<AssetInfo> infos)
        {
            _blockingInProgress = true;

            PreviewPipeline previewPipeline = new PreviewPipeline();
            AI.Actions.RegisterRunningAction(ActionHandler.ACTION_PREVIEWS_RECREATE, previewPipeline, "Recreating previews");
            if (await previewPipeline.RecreatePreviews(infos, false, req =>
                {
                    if (infos.Count > 1) return;
                    if (req == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Preview could not be created.", "OK");
                    }
                    else if (req.IncompatiblePipeline)
                    {
                        EditorUtility.DisplayDialog("Pipeline Error", "Preview could not be created. The item is incompatible to the currently used render pipeline.", "OK");
                    }
                }) > 0) _requireSearchUpdate = true;
            previewPipeline.FinishProgress();

            _blockingInProgress = false;
        }

        private async void RecreateAICaptions(List<AssetInfo> infos)
        {
            _blockingInProgress = true;

            CaptionCreator captionCreator = new CaptionCreator();
            AI.Actions.RegisterRunningAction(ActionHandler.ACTION_AI_CAPTIONS, captionCreator, "Creating selective AI captions");
            await captionCreator.Run(infos);
            captionCreator.FinishProgress();

            _requireSearchUpdate = true;
            _blockingInProgress = false;
        }

        private void LoadSearch(SavedSearch search)
        {
            _searchPhrase = search.SearchPhrase;
            _selectedPackageTypes = search.PackageTypes;
            _selectedPackageSRPs = search.PackageSrPs;
            _selectedImageType = search.ImageType;
            _selectedColorOption = search.ColorOption;
            _selectedColor = ImageUtils.FromHex(search.SearchColor);
            _searchWidth = search.Width;
            _searchHeight = search.Height;
            _searchLength = search.Length;
            _searchSize = search.Size;
            _checkMaxWidth = search.CheckMaxWidth;
            _checkMaxHeight = search.CheckMaxHeight;
            _checkMaxLength = search.CheckMaxLength;
            _checkMaxSize = search.CheckMaxSize;

            AI.Config.searchType = string.IsNullOrWhiteSpace(search.Type) ? 0 : Mathf.Max(0, Array.FindIndex(_types, s => s == search.Type || s.EndsWith($"/{search.Type}")));
            _selectedPublisher = string.IsNullOrWhiteSpace(search.Publisher) ? 0 : Mathf.Max(0, Array.FindIndex(_publisherNames, s => s == search.Publisher || s.EndsWith($"/{search.Publisher}")));
            _selectedAsset = string.IsNullOrWhiteSpace(search.Package) ? 0 : Mathf.Max(0, Array.FindIndex(_assetNames, s => s == search.Package || s.EndsWith($"/{search.Package}")));
            _selectedCategory = string.IsNullOrWhiteSpace(search.Category) ? 0 : Mathf.Max(0, Array.FindIndex(_categoryNames, s => s == search.Category || s.EndsWith($"/{search.Category}")));
            _selectedPackageTag = string.IsNullOrWhiteSpace(search.PackageTag) ? 0 : Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.PackageTag || s.EndsWith($"/{search.PackageTag}")));
            _selectedFileTag = string.IsNullOrWhiteSpace(search.FileTag) ? 0 : Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.FileTag || s.EndsWith($"/{search.FileTag}")));

            _activeSavedSearchId = search.Id;
            _requireSearchUpdate = true;
            RefreshSearchField();
        }

        private void SaveSearch(string value)
        {
            SavedSearch search = new SavedSearch();
            search.Name = value;
            search.SearchPhrase = _searchPhrase;
            search.PackageTypes = _selectedPackageTypes;
            search.PackageSrPs = _selectedPackageSRPs;
            search.ImageType = _selectedImageType;
            search.ColorOption = _selectedColorOption;
            search.SearchColor = "#" + ColorUtility.ToHtmlStringRGB(_selectedColor);
            search.Width = _searchWidth;
            search.Height = _searchHeight;
            search.Length = _searchLength;
            search.Size = _searchSize;
            search.CheckMaxWidth = _checkMaxWidth;
            search.CheckMaxHeight = _checkMaxHeight;
            search.CheckMaxLength = _checkMaxLength;
            search.CheckMaxSize = _checkMaxSize;
            search.Color = ColorUtility.ToHtmlStringRGB(Random.ColorHSV());

            if (AI.Config.searchType > 0 && _types.Length > AI.Config.searchType)
            {
                search.Type = _types[AI.Config.searchType].Split('/').LastOrDefault();
            }

            if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
            {
                search.Publisher = _publisherNames[_selectedPublisher].Split('/').LastOrDefault();
            }

            if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
            {
                search.Package = _assetNames[_selectedAsset].Split('/').LastOrDefault();
            }

            if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
            {
                search.Category = _categoryNames[_selectedCategory].Split('/').LastOrDefault();
            }

            if (_selectedPackageTag > 0 && _tagNames.Length > _selectedPackageTag)
            {
                search.PackageTag = _tagNames[_selectedPackageTag].Split('/').LastOrDefault();
            }

            if (_selectedFileTag > 0 && _tagNames.Length > _selectedFileTag)
            {
                search.FileTag = _tagNames[_selectedFileTag].Split('/').LastOrDefault();
            }

            DBAdapter.DB.Insert(search);
            Searches.Add(search);
            _activeSavedSearchId = search.Id;

            // add to current workspace as well
            if (_selectedWorkspace != null)
            {
                WorkspaceSearch wsSearch = new WorkspaceSearch
                {
                    WorkspaceId = _selectedWorkspace.Id,
                    SavedSearchId = search.Id,
                    OrderIdx = _selectedWorkspace.Searches.Count
                };
                DBAdapter.DB.Insert(wsSearch);
                _selectedWorkspace.Searches.Add(wsSearch);
            }
        }

        private void SaveWorkspace(string value)
        {
            Workspace ws = new Workspace();
            ws.Name = value;

            DBAdapter.DB.Insert(ws);
            Workspaces.Add(ws);

            WorkspaceUI workspaceUI = WorkspaceUI.ShowWindow();
            workspaceUI.Init(ws);
        }

        private async void PlayAudio(AssetInfo info)
        {
            // play instantly if no extraction is required
            if (_blockingInProgress)
            {
                if (AI.IsMaterialized(info.ToAsset(), info)) await AI.PlayAudio(info);
                return;
            }

            await AI.PlayAudio(info, InitBlockingToken());
            DisposeBlocking();
        }

        private async void PingAsset(AssetInfo info)
        {
            if (disablePings) return;

            // requires pauses in-between to allow editor to catch up
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            await Task.Yield();

            Selection.activeObject = null;
            await Task.Yield();

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(info.ProjectPath);
            if (Selection.activeObject == null) info.ProjectPath = null; // probably got deleted again
        }

        private async Task CalculateDependencies(AssetInfo info)
        {
            await AI.CalculateDependencies(info, InitBlockingToken());
            DisposeBlocking();
        }

        private async void Open(AssetInfo info)
        {
            if (!info.IsDownloaded && !info.IsMaterialized) return;

            _blockingInProgress = true;
            string targetPath;
            if (info.InProject)
            {
                targetPath = info.ProjectPath;
            }
            else
            {
                targetPath = await AI.EnsureMaterializedAsset(info);
                if (info.Id == 0) _requireSearchUpdate = true; // was deleted
            }

            if (targetPath != null) EditorUtility.OpenWithDefaultApp(targetPath);
            _blockingInProgress = false;
        }

        private async void OpenExplorer(AssetInfo info)
        {
            if (!info.IsDownloaded && !info.IsMaterialized) return;

            _blockingInProgress = true;
            string targetPath;
            if (info.InProject)
            {
                targetPath = info.ProjectPath;
            }
            else
            {
                targetPath = await AI.EnsureMaterializedAsset(info);
                if (info.Id == 0) _requireSearchUpdate = true; // was deleted
            }

            if (targetPath != null) EditorUtility.RevealInFinder(IOUtils.ToShortPath(targetPath));
            _blockingInProgress = false;
        }

        private async void CopyTo(AssetInfo info, string targetFolder, bool withDependencies = false, bool withScripts = false, bool autoPing = true, bool fromDragDrop = false, bool reimport = false)
        {
            _blockingInProgress = true;

            string mainFile = await AI.CopyTo(info, targetFolder, withDependencies, withScripts, fromDragDrop, false, reimport);
            if (autoPing && mainFile != null)
            {
                if (AI.Config.pingImported) PingAsset(new AssetInfo().WithProjectPath(mainFile));
                if (AI.Config.statsImports == 5) ShowInterstitial();
            }

            _blockingInProgress = false;
        }

        private void SetPage(int newPage)
        {
            SetPage(newPage, false);
        }

        private void SetPage(int newPage, bool ignoreExcludedExtensions)
        {
            newPage = Mathf.Clamp(newPage, 1, _pageCount);
            if (newPage != _curPage)
            {
                _curPage = newPage;
                SGrid.DeselectAll();
                _searchScrollPos = Vector2.zero;
                if (_curPage > 0)
                {
                    if (_inMemoryMode == InMemoryModeState.Active)
                    {
                        UpdateFilteredFiles();

                        _selectedEntry = _filteredFiles.ElementAt(SGrid.selectionTile);
                        _requireSearchSelectionUpdate = true;
                        DisposeAnimTexture();
                    }
                    else
                    {
                        PerformSearch(true, ignoreExcludedExtensions);
                    }
                }
            }
        }

        private void UpdateFilteredFiles()
        {
            StopSearchPreviewLoading();

            _filteredFiles = _files;
            if (_inMemoryMode != InMemoryModeState.None)
            {
                int maxResults = GetMaxResults();

                // apply search criteria
                if (!string.IsNullOrWhiteSpace(_searchPhraseInMemory))
                {
                    List<Func<AssetInfo, string>> selectors = new List<Func<AssetInfo, string>>();
                    switch (AI.Config.searchField)
                    {
                        case 0:
                            selectors.Add(a => a.Path);
                            break;

                        case 1:
                            selectors.Add(a => a.FileName);
                            break;
                    }
                    if (AI.Config.searchAICaptions && AI.Actions.CreateAICaptions) selectors.Add(a => a.AICaption);
                    if (AI.Config.searchPackageNames) selectors.Add(a => a.DisplayName);

                    if (_searchPhraseInMemory.StartsWith("~"))
                    {
                        string term = _searchPhraseInMemory.Substring(1);
                        _filteredFiles = _filteredFiles
                            .Where(a => selectors.Any(sel => sel(a)?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
                    }
                    else
                    {
                        string[] fuzzyWords = _searchPhraseInMemory
                            .Split(' ')
                            .Select(w => w.Trim())
                            .Where(w => !string.IsNullOrWhiteSpace(w))
                            .ToArray();

                        foreach (string word in fuzzyWords)
                        {
                            bool isNeg = word.StartsWith("-");
                            string term = isNeg || word.StartsWith("+") ? word.Substring(1) : word;
                            if (string.IsNullOrWhiteSpace(term)) continue;
                            if (isNeg)
                            {
                                _filteredFiles = _filteredFiles
                                    .Where(a => selectors.All(sel => sel(a)?.Contains(term, StringComparison.OrdinalIgnoreCase) == false));
                            }
                            else
                            {
                                _filteredFiles = _filteredFiles
                                    .Where(a => selectors.Any(sel => sel(a)?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
                            }
                        }
                    }
                }

                // new pagination
                _resultCount = _filteredFiles.Count();
                _pageCount = AssetUtils.GetPageCount(_resultCount, GetMaxResults());
                if (_curPage > _pageCount) _curPage = 1;

                _filteredFiles = _filteredFiles.Skip((_curPage - 1) * maxResults).Take(maxResults);
            }
            else
            {
                _pageCount = AssetUtils.GetPageCount(_resultCount, GetMaxResults());
            }

            DisposeSearchResultTextures();
            SGrid.contents = _filteredFiles.Select(file =>
            {
                string text = "";
                int tileTextToUse = AI.Config.tileText;
                if (tileTextToUse == 5 && string.IsNullOrEmpty(file.AICaption))
                {
                    tileTextToUse = 3;
                }
                if (tileTextToUse == 0) // intelligent
                {
                    if (AI.Config.searchTileSize < 70)
                    {
                        tileTextToUse = 6;
                    }
                    else if (AI.Config.searchTileSize < 90)
                    {
                        tileTextToUse = 4;
                    }
                    else if (AI.Config.searchTileSize < 150)
                    {
                        tileTextToUse = 3;
                    }
                    else
                    {
                        tileTextToUse = 2;
                    }
                }
                switch (tileTextToUse)
                {
                    case 2:
                        text = file.ShortPath;
                        break;

                    case 3:
                        text = file.FileName;
                        break;

                    case 4:
                        text = Path.GetFileNameWithoutExtension(file.FileName);
                        break;

                    case 5:
                        text = file.AICaption;
                        break;

                }
                text = text == null ? "" : text.Replace('/', Path.DirectorySeparatorChar);

                return new GUIContent(text);
            }).ToArray();

            SGrid.enlargeTiles = AI.Config.enlargeTiles;
            SGrid.centerTiles = AI.Config.centerTiles;
            SGrid.Init(_assets, _filteredFiles, CalculateSearchBulkSelection);

            UpdateSearchPreviews();
        }

        private bool IsFilterApplicable(string filterName)
        {
            string searchType = GetRawSearchType();
            if (searchType == null) return true;
            if (AI.FilterRestriction.TryGetValue(filterName, out string[] restrictions))
            {
                return restrictions.Contains(searchType);
            }

            return true;
        }

        private string GetRawSearchType()
        {
            int searchType = _fixedSearchTypeIdx >= 0 ? _fixedSearchTypeIdx : AI.Config.searchType;
            return searchType > 0 && _types.Length > searchType ? _types[searchType] : null;
        }

        private int GetMaxResults()
        {
            string selectedSize = _resultSizes[AI.Config.maxResults];
            int.TryParse(selectedSize, out int maxResults);
            if (maxResults <= 0 || maxResults > AI.Config.maxResultsLimit) maxResults = AI.Config.maxResultsLimit;

            return maxResults;
        }

        private void PerformSearch(bool keepPage = false, bool ignoreExcludedExtensions = false)
        {
            if (AI.DEBUG_MODE) Debug.LogWarning("Perform Search");

            _requireSearchUpdate = false;
            _searchHandlerAdded = false;
            _keepSearchResultPage = true;
            StopSearchPreviewLoading();

            // check if something was searched for actually, good for reducing initial load time if user is not interested in seeing full catalog
            if (!SearchWithoutInput())
            {
                if (!IsSearchFilterActive() && string.IsNullOrWhiteSpace(_searchPhrase))
                {
                    _resultCount = 0;
                    _pageCount = 0;
                    _curPage = 1;
                    _filteredFiles = new List<AssetInfo>();
                    SGrid.contents = Array.Empty<GUIContent>();
                    return;
                }
            }

            int lastCount = _resultCount; // a bit of a heuristic but works great and is very performant
            int maxResults = GetMaxResults();
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            string packageTagJoin = "";
            string fileTagJoin = "";
            string computedFields = "";
            string lastWhere = null;
            string phrase = _searchPhrase;

            wheres.Add("Asset.Exclude=0");

            // parse tokens 
            List<string> parsedPackageTags = new List<string>();
            phrase = StringUtils.ExtractTokens(phrase, "pt", parsedPackageTags);
            if (parsedPackageTags.Count > 0)
            {
                List<string> conditions = new List<string>();
                foreach (string tag in parsedPackageTags)
                {
                    conditions.Add("tap.TagId = ?");
                    args.Add(_tags.FirstOrDefault(t => t.Name.ToLowerInvariant() == tag.ToLowerInvariant())?.Id);
                }
                wheres.Add("(" + string.Join(" OR ", conditions) + ")");
                packageTagJoin = PACKAGE_TAG_JOIN_CLAUSE;
            }

            List<string> parsedFileTags = new List<string>();
            phrase = StringUtils.ExtractTokens(phrase, "ft", parsedFileTags);
            if (parsedFileTags.Count > 0)
            {
                List<string> conditions = new List<string>();
                foreach (string tag in parsedFileTags)
                {
                    conditions.Add("taf.TagId = ?");
                    args.Add(_tags.FirstOrDefault(t => t.Name.ToLowerInvariant() == tag.ToLowerInvariant())?.Id);
                }
                wheres.Add("(" + string.Join(" OR ", conditions) + ")");
                fileTagJoin = FILE_TAG_JOIN_CLAUSE;
            }

            // numerical conditions first
            switch (_selectedPackageSRPs)
            {
                case 2:
                    wheres.Add("Asset.BIRPCompatible=1");
                    break;

                case 3:
                    wheres.Add("Asset.URPCompatible=1");
                    break;

                case 4:
                    wheres.Add("Asset.HDRPCompatible=1");
                    break;
            }

            if (IsFilterApplicable("Width") && !string.IsNullOrWhiteSpace(_searchWidth))
            {
                if (int.TryParse(_searchWidth, out int width) && width > 0)
                {
                    string widthComp = _checkMaxWidth ? "<=" : ">=";
                    wheres.Add($"AssetFile.Width > 0 and AssetFile.Width {widthComp} ?");
                    args.Add(width);
                }
            }

            if (IsFilterApplicable("Height") && !string.IsNullOrWhiteSpace(_searchHeight))
            {
                if (int.TryParse(_searchHeight, out int height) && height > 0)
                {
                    string heightComp = _checkMaxHeight ? "<=" : ">=";
                    wheres.Add($"AssetFile.Height > 0 and AssetFile.Height {heightComp} ?");
                    args.Add(height);
                }
            }

            if (IsFilterApplicable("Length") && !string.IsNullOrWhiteSpace(_searchLength))
            {
                if (float.TryParse(_searchLength, out float length) && length > 0)
                {
                    string lengthComp = _checkMaxLength ? "<=" : ">=";
                    wheres.Add($"AssetFile.Length > 0 and AssetFile.Length {lengthComp} ?");
                    args.Add(length);
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchSize))
            {
                if (int.TryParse(_searchSize, out int size) && size > 0)
                {
                    string sizeComp = _checkMaxSize ? "<=" : ">=";
                    wheres.Add($"AssetFile.Size > 0 and AssetFile.Size {sizeComp} ?");
                    args.Add(size * 1024);
                }
            }

            if (parsedPackageTags.Count == 0) // inline tags override this filter
            {
                if (_selectedPackageTag == 1)
                {
                    wheres.Add("not exists (select tap.Id from TagAssignment as tap where Asset.Id = tap.TargetId and tap.TagTarget = 0)");
                }
                else if (_selectedPackageTag > 1 && _tagNames.Length > _selectedPackageTag)
                {
                    string[] arr = _tagNames[_selectedPackageTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("tap.TagId = ?");
                    args.Add(_tags.FirstOrDefault(t => t.Name == tag)?.Id);

                    packageTagJoin = PACKAGE_TAG_JOIN_CLAUSE;
                }
            }

            if (parsedFileTags.Count == 0) // inline tags override this filter
            {
                if (_selectedFileTag == 1)
                {
                    wheres.Add("not exists (select taf.Id from TagAssignment as taf where AssetFile.Id = taf.TargetId and taf.TagTarget = 1)");
                }
                else if (_selectedFileTag > 1 && _tagNames.Length > _selectedFileTag)
                {
                    string[] arr = _tagNames[_selectedFileTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("taf.TagId = ?");
                    args.Add(_tags.FirstOrDefault(t => t.Name == tag)?.Id);

                    fileTagJoin = FILE_TAG_JOIN_CLAUSE;
                }
            }

            switch (_selectedPackageTypes)
            {
                case 1:
                    wheres.Add("Asset.AssetSource != ?");
                    args.Add(Asset.Source.RegistryPackage);
                    break;

                case 2:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.AssetStorePackage);
                    break;

                case 3:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.RegistryPackage);
                    break;

                case 4:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.CustomPackage);
                    break;

                case 5:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.Directory);
                    break;

                case 6:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.Archive);
                    break;

                case 7:
                    wheres.Add("Asset.AssetSource = ?");
                    args.Add(Asset.Source.AssetManager);
                    break;

            }

            if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
            {
                string[] arr = _publisherNames[_selectedPublisher].Split('/');
                string publisher = arr[arr.Length - 1];
                wheres.Add("Asset.SafePublisher = ?");
                args.Add($"{publisher}");
            }

            if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
            {
                string[] arr = _assetNames[_selectedAsset].Split('/');
                string asset = arr[arr.Length - 1];
                if (asset.LastIndexOf('[') > 0)
                {
                    string assetId = asset.Substring(asset.LastIndexOf('[') + 1);
                    assetId = assetId.Substring(0, assetId.Length - 1);
                    if (AI.Config.searchSubPackages)
                    {
                        wheres.Add("(Asset.Id = ? or Asset.ParentId = ?)");
                        args.Add(int.Parse(assetId));
                        args.Add(int.Parse(assetId));
                    }
                    else
                    {
                        wheres.Add("Asset.Id = ?");
                        args.Add(int.Parse(assetId));
                    }
                }
                else
                {
                    wheres.Add("Asset.SafeName = ?");
                    args.Add($"{asset}");
                }
            }

            if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
            {
                wheres.Add("Asset.DisplayCategory = ?");
                args.Add(_categoryNames[_selectedCategory]);
            }

            if (_selectedColorOption > 0)
            {
                wheres.Add("AssetFile.Hue >= ?");
                wheres.Add("AssetFile.Hue <= ?");
                args.Add(_selectedColor.ToHue() - AI.Config.hueRange / 2f);
                args.Add(_selectedColor.ToHue() + AI.Config.hueRange / 2f);
            }

            if (IsFilterApplicable("ImageType") && _selectedImageType > 0)
            {
                computedFields = ", CASE WHEN INSTR(AssetFile.FileName, '.') > 0 THEN Lower(SUBSTR(AssetFile.FileName, 1, INSTR(AssetFile.FileName, '.') - 1)) ELSE Lower(AssetFile.FileName) END AS FileNameWithoutExtension";
                string[] patterns = TextureNameSuggester.suffixPatterns[_imageTypeOptions[_selectedImageType].ToLowerInvariant()];

                // concatenate all patterns manually into an or condition and add as a single where
                List<string> patternWheres = new List<string>();
                foreach (string pattern in patterns)
                {
                    if (string.IsNullOrWhiteSpace(pattern)) continue;

                    patternWheres.Add("FileNameWithoutExtension like ? ESCAPE '\\'");
                    args.Add("%" + pattern.Replace("_", "\\_"));
                }
                wheres.Add("(" + string.Join(" or ", patternWheres) + ")");
            }

            if (!string.IsNullOrWhiteSpace(_searchPhrase))
            {
                List<string> searchFields = new List<string>();

                switch (AI.Config.searchField)
                {
                    case 0:
                        searchFields.Add("AssetFile.Path");
                        break;

                    case 1:
                        searchFields.Add("AssetFile.FileName");
                        break;

                }
                if (AI.Config.searchAICaptions && AI.Actions.CreateAICaptions) searchFields.Add("AssetFile.AICaption");
                if (AI.Config.searchPackageNames) searchFields.Add("Asset.DisplayName");

                // check for sqlite escaping requirements
                string escape = "";
                if (phrase.Contains("_"))
                {
                    if (!phrase.StartsWith("=")) phrase = phrase.Replace("_", "\\_");
                    escape = "ESCAPE '\\'";
                }

                if (phrase.StartsWith("=")) // expert mode
                {
                    if (phrase.Length > 1)
                    {
                        phrase = StringUtils.EscapeSQL(phrase);
                        lastWhere = phrase.Substring(1);
                    }
                }
                else if (phrase.StartsWith("~")) // exact mode
                {
                    string term = phrase.Substring(1);
                    List<string> conditions = new List<string>();
                    searchFields.ForEach(s =>
                    {
                        conditions.Add($"COALESCE({s}, '') like ? {escape}");
                        args.Add($"%{term}%");
                    });
                    wheres.Add("(" + string.Join(" OR ", conditions) + ")");
                }
                else
                {
                    string[] fuzzyWords = phrase
                        .Split(' ')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToArray();
                    foreach (string fuzzyWord in fuzzyWords)
                    {
                        if (fuzzyWord.StartsWith("-"))
                        {
                            List<string> conditions = new List<string>();
                            searchFields.ForEach(s =>
                            {
                                conditions.Add($"COALESCE({s}, '') not like ? {escape}");
                                args.Add($"%{fuzzyWord.Substring(1)}%");
                            });
                            wheres.Add("(" + string.Join(" AND ", conditions) + ")");
                        }
                        else
                        {
                            string term = fuzzyWord;
                            if (term.StartsWith("+")) term = term.Substring(1);
                            List<string> conditions = new List<string>();
                            searchFields.ForEach(s =>
                            {
                                conditions.Add($"COALESCE({s}, '') like ? {escape}");
                                args.Add($"%{term}%");
                            });
                            wheres.Add("(" + string.Join(" OR ", conditions) + ")");
                        }
                    }
                }
            }

            string rawType = GetRawSearchType();
            if (rawType != null)
            {
                string[] type = rawType.Split('/');
                if (type.Length > 1)
                {
                    wheres.Add("AssetFile.Type = ?");
                    args.Add(type.Last());
                }
                else
                {
                    if (Enum.TryParse(rawType, out AI.AssetGroup assetGroup))
                    {
                        if (AI.TypeGroups.TryGetValue(assetGroup, out string[] group))
                        {
                            // optimize SQL slightly for cases where only one type is checked
                            if (group.Length == 1)
                            {
                                wheres.Add("AssetFile.Type = ?");
                                args.Add(group[0]);
                            }
                            else
                            {
                                // sqlite does not support binding lists, parameters must be spelled out
                                List<string> paramCount = new List<string>();
                                foreach (string t in group)
                                {
                                    paramCount.Add("?");
                                    args.Add(t);
                                }

                                wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");
                            }
                        }
                    }
                }
            }

            if (!ignoreExcludedExtensions && AI.Config.excludeExtensions && AI.Config.searchType == 0 && !string.IsNullOrWhiteSpace(AI.Config.excludedExtensions))
            {
                string[] extensions = AI.Config.excludedExtensions.Split(';');
                List<string> paramCount = new List<string>();
                foreach (string ext in extensions)
                {
                    paramCount.Add("?");
                    args.Add(ext.Trim());
                }

                wheres.Add("AssetFile.Type not in (" + string.Join(",", paramCount) + ")");
            }

            switch (AI.Config.previewVisibility)
            {
                case 2:
                    wheres.Add("AssetFile.PreviewState in (1, 2, 3)");
                    break;

                case 3:
                    wheres.Add("AssetFile.PreviewState not in (1, 2, 3)");
                    break;
            }

            // ordering, can only be done on DB side since post-processing results would only work on the paged results which is incorrect
            string orderBy = "order by ";
            switch (AI.Config.sortField)
            {
                case 0:
                    orderBy += "AssetFile.Path";
                    break;

                case 1:
                    orderBy += "AssetFile.FileName";
                    break;

                case 2:
                    orderBy += "AssetFile.Size";
                    break;

                case 3:
                    orderBy += "AssetFile.Type";
                    break;

                case 4:
                    orderBy += "AssetFile.Length";
                    break;

                case 5:
                    orderBy += "AssetFile.Width";
                    break;

                case 6:
                    orderBy += "AssetFile.Height";
                    break;

                case 7:
                    orderBy += "AssetFile.Hue";
                    wheres.Add("AssetFile.Hue >=0");
                    break;

                case 8:
                    orderBy += "Asset.DisplayCategory";
                    break;

                case 9:
                    orderBy += "Asset.LastRelease";
                    break;

                case 10:
                    orderBy += "Asset.AssetRating";
                    break;

                case 11:
                    orderBy += "Asset.RatingCount";
                    break;

                default:
                    orderBy = "";
                    break;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy += " COLLATE NOCASE";
                if (AI.Config.sortDescending) orderBy += " desc";
                orderBy += ", AssetFile.Path"; // always sort by path in case of equality of first level sorting
            }
            if (!string.IsNullOrEmpty(lastWhere)) wheres.Add(lastWhere);

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            string baseQuery = $"from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId {packageTagJoin} {fileTagJoin} {where}";
            string countQuery = $"select count(*){computedFields} {baseQuery}";
            string dataQuery = $"select *, AssetFile.Id as Id{computedFields} {baseQuery} {orderBy}";
            if (maxResults > 0 && _inMemoryMode == InMemoryModeState.None) dataQuery += $" limit {maxResults} offset {(_curPage - 1) * maxResults}";
            try
            {
                _searchError = null;
                _resultCount = DBAdapter.DB.ExecuteScalar<int>($"{countQuery}", args.ToArray());
                _originalResultCount = _resultCount; // store original result count for later use

                // restrict result set if in memory mode is active and it exceeds the limit to ensure not the whole database is accidentally loaded into memory
                if (maxResults > 0 && _inMemoryMode != InMemoryModeState.None && _resultCount > AI.Config.maxInMemoryResults)
                {
                    _inMemoryMode = InMemoryModeState.None;
                    dataQuery += $" limit {maxResults} offset {(_curPage - 1) * maxResults}";
                    EditorUtility.DisplayDialog("Search Result Limit Exceeded",
                        $"There are more than {AI.Config.maxInMemoryResults:N0} search results (configured in search settings). In-Memory mode was therefore disabled again.",
                        "OK");
                }

                _files = DBAdapter.DB.Query<AssetInfo>($"{dataQuery}", args.ToArray());
            }
            catch (SQLiteException e)
            {
                _searchError = e.Message;
            }
            AI.ResolveParents(_files, _assets);
            if (_inMemoryMode == InMemoryModeState.Init) _inMemoryMode = InMemoryModeState.Active;

            // pagination
            UpdateFilteredFiles();

            if (!keepPage && lastCount != _resultCount)
            {
                SetPage(1, ignoreExcludedExtensions);
            }
            else
            {
                SetPage(_curPage, ignoreExcludedExtensions);
            }

            _searchDone = true;
        }

        private void StopSearchPreviewLoading()
        {
            _textureLoading?.Cancel();
            _textureLoading = new CancellationTokenSource();
        }

        private void UpdateSearchPreviews()
        {
            StopSearchPreviewLoading();
            LoadTextures(false, _textureLoading.Token); // TODO: should be true once pages endless scrolling is supported
        }

        private async void LoadAnimTexture(AssetInfo info)
        {
            _curAnimFrame = 1;
            _animTexture = null;

            string animPreviewFile = info.GetPreviewFile(AI.GetPreviewFolder(), true);
            if (!File.Exists(animPreviewFile)) return;

            _animTexture = await AssetUtils.LoadLocalTexture(animPreviewFile, false, (AI.Config.upscalePreviews && !AI.Config.upscaleLossless) ? AI.Config.upscaleSize : 0);
            _animFrames = CreateUVs(AI.Config.animationGrid, AI.Config.animationGrid);
        }

        private List<Rect> CreateUVs(int columns, int rows)
        {
            List<Rect> rects = new List<Rect>();

            float frameWidth = 1f / columns;
            float frameHeight = 1f / rows;

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < columns; x++)
                {
                    Rect rect = new Rect(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                    rects.Add(rect);
                }
            }

            return rects;
        }

        private Texture2D ExtractFrame(Texture2D sourceTexture, Rect uvRect)
        {
            int x = Mathf.RoundToInt(uvRect.x * sourceTexture.width);
            int y = Mathf.RoundToInt(uvRect.y * sourceTexture.height);
            int width = Mathf.RoundToInt(uvRect.width * sourceTexture.width);
            int height = Mathf.RoundToInt(uvRect.height * sourceTexture.height);

            // Flip the y-coordinate because Unity's texture origin is at the bottom-left
            y = sourceTexture.height - y - height;

            // Create a new Texture2D to hold the frame
            Texture2D frameTexture = new Texture2D(width, height, sourceTexture.format, false);
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.SetPixels(sourceTexture.GetPixels(x, y, width, height));
            frameTexture.Apply();

            return frameTexture;
        }

        private async void LoadTextures(bool firstPageOnly, CancellationToken ct)
        {
            int chunkSize = AI.Config.previewChunkSize;

            List<AssetInfo> files = _filteredFiles.Take(firstPageOnly ? 20 * 8 : _filteredFiles.Count()).ToList();

            for (int i = 0; i < files.Count; i += chunkSize)
            {
                try
                {
                    if (ct.IsCancellationRequested) return;

                    List<Task> tasks = new List<Task>();

                    int chunkEnd = Math.Min(i + chunkSize, files.Count);
                    for (int idx = i; idx < chunkEnd; idx++)
                    {
                        if (ct.IsCancellationRequested) return;

                        int localIdx = idx; // capture value
                        AssetInfo info = files.ElementAt(localIdx);

                        tasks.Add(ProcessAssetInfoAsync(info, localIdx, ct));
                    }

                    await Task.WhenAll(tasks).WithCancellation(ct);
                }
                catch (OperationCanceledException)
                {
                    // Task was cancelled, exit the loop
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing asset: {e.Message}");
                }
            }
        }

        private async Task ProcessAssetInfoAsync(AssetInfo info, int idx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string previewFile = null;
            if (info.HasPreview(true)) previewFile = AssetImporter.ValidatePreviewFile(info, AI.GetPreviewFolder());
            if (previewFile == null || !info.HasPreview(true))
            {
                if (!AI.Config.showIconsForMissingPreviews) return;

                // check if well-known extension
                if (_staticPreviews.TryGetValue(info.Type, out string preview))
                {
                    SGrid.contents[idx].image = EditorGUIUtility.IconContent(preview).image;
                }
                else
                {
                    SGrid.contents[idx].image = EditorGUIUtility.IconContent("d_DefaultAsset Icon").image;
                }
                return;
            }

            Texture2D texture = await AssetUtils.LoadLocalTexture(
                previewFile,
                false,
                // _inMemoryMode != InMemoryModeState.None,
                (AI.Config.upscalePreviews && !AI.Config.upscaleLossless) ? AI.Config.upscaleSize : 0
            );

            if (texture == null)
            {
                info.PreviewState = AssetFile.PreviewOptions.None;
                DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", info.PreviewState, info.Id);
            }
            else if (SGrid.contents.Length > idx)
            {
                SGrid.contents[idx].image = texture;
            }
        }

        private void CalculateSearchBulkSelection()
        {
            _assetFileBulkTags.Clear();
            SGrid.selectionItems.ForEach(info => info.AssetTags?.ForEach(t =>
            {
                if (!_assetFileBulkTags.ContainsKey(t.Name)) _assetFileBulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _assetFileBulkTags[t.Name] = new Tuple<int, Color>(_assetFileBulkTags[t.Name].Item1 + 1, _assetFileBulkTags[t.Name].Item2);
            }));
            _assetFileAMProjectCount = SGrid.selectionItems.Count(info => info.AssetSource == Asset.Source.AssetManager && string.IsNullOrEmpty(info.Location));
            _assetFileAMCollectionCount = SGrid.selectionItems.Count(info => info.AssetSource == Asset.Source.AssetManager && !string.IsNullOrEmpty(info.Location));
        }

        private void OpenInSearch(AssetInfo info, bool force = false, bool showFilterTab = true)
        {
            if (info.Id <= 0) return;
            if (!force && info.FileCount <= 0) return;
            AssetInfo oldEntry = _selectedEntry;

            if (info.Exclude)
            {
                if (!EditorUtility.DisplayDialog("Package is Excluded", "This package is currently excluded from the search. Should it be included again?", "Include Again", "Cancel"))
                {
                    return;
                }
                AI.SetAssetExclusion(info, false);
                ReloadLookups();
            }
            ResetSearch(false, true);
            if (force) _selectedEntry = oldEntry;

            AI.Config.tab = 0;

            // search for exact match first
            string displayName = info.GetDisplayName().Replace("/", " ");
            if (info.SafeName == Asset.NONE)
            {
                _selectedAsset = 1;
            }
            else
            {
                _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == displayName + $" [{info.AssetId}]"));
            }
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == displayName.Substring(0, 1) + "/" + displayName + $" [{info.AssetId}]"));
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a.EndsWith(displayName + $" [{info.AssetId}]")));

            if (info.AssetSource == Asset.Source.RegistryPackage && _selectedPackageTypes == 1) _selectedPackageTypes = 0;
            _curPage = 1;
            if (showFilterTab) _searchInspectorTab = 1;
            PerformSearch(); // search immediately as "search automatically" setting might be off 
        }

        private void ResetSearch(bool filterBarOnly, bool keepAssetType)
        {
            if (!filterBarOnly)
            {
                _searchPhrase = "";
                if (!keepAssetType) AI.Config.searchType = 0;
            }

            _selectedEntry = null;
            _selectedAsset = 0;
            _selectedPackageTypes = 1;
            _selectedPackageSRPs = 0;
            _selectedImageType = 0;
            _selectedColorOption = 0;
            _selectedColor = Color.clear;
            _selectedPackageTag = 0;
            _selectedFileTag = 0;
            _selectedPublisher = 0;
            _selectedCategory = 0;
            _searchHeight = "";
            _checkMaxHeight = false;
            _searchWidth = "";
            _checkMaxWidth = false;
            _searchLength = "";
            _checkMaxLength = false;
            _searchSize = "";
            _checkMaxSize = false;

            // Clear active saved search when resetting
            _activeSavedSearchId = -1;
        }

        private async Task PerformCopyTo(AssetInfo info, string path, bool fromDragDrop = false)
        {
            if (info.InProject) return;
            if (string.IsNullOrEmpty(path)) return;

            while (info.DependencyState == AssetInfo.DependencyStateOptions.Calculating) await Task.Yield();
            if (info.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await CalculateDependencies(info);
            if (info.DependencySize > 0 && DependencyAnalysis.NeedsScan(info.Type))
            {
                CopyTo(info, path, true, false, false, fromDragDrop);
            }
            else
            {
                CopyTo(info, path, false, false, true, fromDragDrop);
            }
        }

        private static bool DragDropAvailable()
        {
#if UNITY_2021_2_OR_NEWER
            return true;
#else
            return false;
#endif
        }

#if UNITY_6000_3_OR_NEWER
        private void InitDragAndDrop()
        {
            DragAndDrop.ProjectBrowserDropHandlerV2 dropHandler = OnProjectWindowDrop;
            if (!DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.AddDropHandlerV2(dropHandler);
            }
        }

        private void DeinitDragAndDrop()
        {
            DragAndDrop.ProjectBrowserDropHandlerV2 dropHandler = OnProjectWindowDrop;
            if (DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.RemoveDropHandlerV2(dropHandler);
            }
        }

        private DragAndDropVisualMode OnProjectWindowDrop(EntityId dragEntityId, string dropUponPath, bool perform)
        {
            return DoOnProjectWindowDrop(dropUponPath, perform);
        }

#elif UNITY_2021_2_OR_NEWER
        private void InitDragAndDrop()
        {
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (!DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.AddDropHandler(dropHandler);
            }
        }

        private void DeinitDragAndDrop()
        {
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.RemoveDropHandler(dropHandler);
            }
        }

        private DragAndDropVisualMode OnProjectWindowDrop(int dragInstanceId, string dropUponPath, bool perform)
        {
            return DoOnProjectWindowDrop(dropUponPath, perform);
        }
#endif

#if UNITY_6000_3_OR_NEWER
        private DragAndDropVisualMode OnHierarchyDrop(EntityId dropTargetEntityId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }
        
        private DragAndDropVisualMode OnProjectBrowserDrop(EntityId dragEntityId, string dropUponPath, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }
#endif
#if UNITY_2021_2_OR_NEWER
        private DragAndDropVisualMode DoOnProjectWindowDrop(string dropUponPath, bool perform)
        {
            if (perform && _dragging)
            {
                _dragging = false;
                DeinitDragAndDrop();

                List<AssetInfo> infos = (List<AssetInfo>)DragAndDrop.GetGenericData("AssetInfo");
                if (infos != null && infos.Count > 0) // can happen in some edge asynchronous scenarios
                {
                    if (File.Exists(dropUponPath)) dropUponPath = Path.GetDirectoryName(dropUponPath);
                    PerformCopyToBulk(infos, dropUponPath);
                }
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Copy;
        }

        private async void PerformCopyToBulk(List<AssetInfo> infos, string targetPath)
        {
            if (infos.Count == 0) return;

            foreach (AssetInfo info in infos)
            {
                await PerformCopyTo(info, targetPath, true);
            }
            if (AI.Config.pingImported) PingAsset(infos[0]);
        }

        private DragAndDropVisualMode OnSceneDrop(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }

        private DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }

        private DragAndDropVisualMode OnProjectBrowserDrop(int dragInstanceId, string dropUponPath, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }

        private DragAndDropVisualMode OnInspectorDrop(Object[] targets, bool perform)
        {
            if (perform) StopDragDrop();
            return DragAndDropVisualMode.None;
        }

        private void HandleDragDrop()
        {
            if (AI.Config.disableDragDrop) return;

            switch (Event.current.type)
            {
                case EventType.MouseDrag:
                    if (!SGrid.IsMouseOverGrid) return;
                    if (!_draggingPossible || _dragging || _selectedEntry == null) return;

                    // Check if we've moved far enough and waited long enough to start dragging
                    float dragDistance = Vector2.Distance(Event.current.mousePosition, _dragStartPosition);
                    float timeSinceStart = Time.realtimeSinceStartup - _dragStartTime;

                    if (dragDistance < DRAG_THRESHOLD && timeSinceStart < DRAG_DELAY) return;

                    _dragging = true;

                    InitDragAndDrop();
                    DragAndDrop.PrepareStartDrag();

                    if (SGrid.selectionCount > 0)
                    {
                        DragAndDrop.SetGenericData("AssetInfo", SGrid.selectionItems);
                        DragAndDrop.objectReferences = SGrid.selectionItems
                            .Where(item => !string.IsNullOrWhiteSpace(item.ProjectPath))
                            .Select(item => AssetDatabase.LoadMainAssetAtPath(item.ProjectPath))
                            .ToArray();
                    }
                    else
                    {
                        DragAndDrop.SetGenericData("AssetInfo", new List<AssetInfo> {_selectedEntry});
                        if (!string.IsNullOrWhiteSpace(_selectedEntry.ProjectPath))
                        {
                            DragAndDrop.objectReferences = new[] {AssetDatabase.LoadMainAssetAtPath(_selectedEntry.ProjectPath)};
                        }
                    }
                    DragAndDrop.StartDrag("Dragging " + _selectedEntry);
                    Event.current.Use();
                    break;

                case EventType.MouseDown:
                    if (SGrid.IsMouseOverGrid)
                    {
                        _draggingPossible = true;
                        _dragStartPosition = Event.current.mousePosition;
                        _dragStartTime = Time.realtimeSinceStartup;
                    }
                    break;

                case EventType.MouseUp:
                    _draggingPossible = false;
                    StopDragDrop();
                    break;
            }
        }

        private void StopDragDrop()
        {
            if (_dragging)
            {
                _dragging = false;
                GUIUtility.hotControl = 0; // otherwise scene gizmos are still blocked
                DeinitDragAndDrop();
            }
        }
#else
        private void HandleDragDrop() {}
#endif

        private void SearchUpdateLoop()
        {
            if (Time.realtimeSinceStartup > _nextAnimTime
                && _animTexture != null && _selectedEntry != null
                && SGrid.selectionTile >= 0 && SGrid.contents != null)
            {
                if (_curAnimFrame > _animFrames.Count) _curAnimFrame = 1;
                Rect frameRect = _animFrames[_curAnimFrame - 1];

                // Extract the current frame as a Texture2D
                Texture2D curTexture = ExtractFrame(_animTexture, frameRect);

                // Destroy the previous frame texture to prevent memory leaks
                if (SGrid.contents[SGrid.selectionTile].image != null)
                {
                    DestroyImmediate(SGrid.contents[SGrid.selectionTile].image);
                }
                SGrid.contents[SGrid.selectionTile].image = curTexture;

                _nextAnimTime = Time.realtimeSinceStartup + AI.Config.animationSpeed;
                _curAnimFrame++;
                if (_curAnimFrame > AI.Config.animationGrid * AI.Config.animationGrid) _curAnimFrame = 1;
            }
        }

        private void DisposeSearchResultTextures()
        {
            if (SGrid.contents == null) return;

            for (int i = 0; i < SGrid.contents.Length; i++)
            {
                GUIContent content = SGrid.contents[i];
                if (content != null && content.image != null)
                {
                    // Skip built-in Unity icons which shouldn't be destroyed
                    if (content.image.name != "d_DefaultAsset Icon" &&
                        !AssetDatabase.GetAssetPath(content.image).StartsWith("Library/"))
                    {
                        DestroyImmediate(content.image);
                        content.image = null;
                    }
                }
            }

            DisposeAnimTexture();
        }

        private void DisposeAnimTexture()
        {
            if (_animTexture != null)
            {
                DestroyImmediate(_animTexture);
                _animTexture = null;
            }
        }
    }
}