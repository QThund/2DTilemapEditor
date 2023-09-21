using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Event = UnityEngine.Event;
using Object = UnityEngine.Object;

using UnityEditor.SceneManagement;
#if UNITY_2019 || UNITY_2019_1_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace UnityEditor.Tilemaps
{
    internal class GridPaintPaletteWindow : EditorWindow
    {
        internal enum TilemapFocusMode
        {
            None = 0,
            Tilemap = 1,
            Grid = 2
        }
        private static readonly string k_TilemapFocusModeEditorPref = "TilemapFocusMode";
        private TilemapFocusMode focusMode
        {
            get
            {
                return (TilemapFocusMode)EditorPrefs.GetInt(k_TilemapFocusModeEditorPref, (int)TilemapFocusMode.None);
            }
            set
            {
                EditorPrefs.SetInt(k_TilemapFocusModeEditorPref, (int)value);
            }
        }

        private static readonly string k_TilemapLastPaletteEditorPref = "TilemapLastPalette";
        private string lastTilemapPalette
        {
            get
            {
                return EditorPrefs.GetString(k_TilemapLastPaletteEditorPref, "");
            }
            set
            {
                EditorPrefs.SetString(k_TilemapLastPaletteEditorPref, value);
            }
        }

        private static class MouseStyles
        {
            // The following paths match the enums in OperatingSystemFamily
            public static readonly string[] mouseCursorOSPath =
            {
                "", // Other OS
                "Cursors/macOS",
                "Cursors/Windows",
                "Cursors/Linux",
            };
            // The following paths match the enums in OperatingSystemFamily
            public static readonly Vector2[] mouseCursorOSHotspot =
            {
                Vector2.zero, // Other OS
                new Vector2(6f, 4f),
                new Vector2(6f, 4f),
                new Vector2(6f, 4f),
            };
            // The following paths match the enums in sceneViewEditModes above
            public static readonly string[] mouseCursorTexturePaths =
            {
                "",
                "Grid.MoveTool.png",
                "Grid.PaintTool.png",
                "Grid.BoxTool.png",
                "Grid.PickingTool.png",
                "Grid.EraserTool.png",
                "Grid.FillTool.png",
            };
            public static readonly Texture2D[] mouseCursorTextures;
            static MouseStyles()
            {
                mouseCursorTextures = new Texture2D[mouseCursorTexturePaths.Length];
                int osIndex = (int)SystemInfo.operatingSystemFamily;
                for (int i = 0; i < mouseCursorTexturePaths.Length; ++i)
                {
                    if ((mouseCursorOSPath[osIndex] != null && mouseCursorOSPath[osIndex].Length > 0)
                        && (mouseCursorTexturePaths[i] != null && mouseCursorTexturePaths[i].Length > 0))
                    {
                        string cursorPath = Utils.Paths.Combine(mouseCursorOSPath[osIndex], mouseCursorTexturePaths[i]);
                        mouseCursorTextures[i] = EditorGUIUtility.LoadRequired(cursorPath) as Texture2D;
                    }
                    else
                        mouseCursorTextures[i] = null;
                }
            }
        }

        private static class Styles
        {
            public static readonly GUIContent emptyProjectInfo = EditorGUIUtility.TrTextContent("Create a new palette in the dropdown above.");
            public static readonly GUIContent emptyPaletteInfo = EditorGUIUtility.TrTextContent("Drag Tile, Sprite or Sprite Texture assets here.");
            public static readonly GUIContent invalidPaletteInfo = EditorGUIUtility.TrTextContent("This is an invalid palette. Did you delete the palette asset?");
            public static readonly GUIContent invalidGridInfo = EditorGUIUtility.TrTextContent("The palette has an invalid Grid. Did you add a Grid to the palette asset?");
            public static readonly GUIContent selectPaintTarget = EditorGUIUtility.TrTextContent("Select Paint Target");
            public static readonly GUIContent selectPalettePrefab = EditorGUIUtility.TrTextContent("Select Palette Prefab");
            public static readonly GUIContent selectTileAsset = EditorGUIUtility.TrTextContent("Select Tile Asset");
            public static readonly GUIContent unlockPaletteEditing = EditorGUIUtility.TrTextContent("Unlock Palette Editing");
            public static readonly GUIContent lockPaletteEditing = EditorGUIUtility.TrTextContent("Lock Palette Editing");
            public static readonly GUIContent openTilePalettePreferences = EditorGUIUtility.TrTextContent("Open Tile Palette Preferences");
            public static readonly GUIContent createNewPalette = EditorGUIUtility.TrTextContent("Create New Palette");
            public static readonly GUIContent focusLabel = EditorGUIUtility.TrTextContent("Focus On");
            public static readonly GUIContent rendererOverlayTitleLabel = EditorGUIUtility.TrTextContent("Tilemap");
            public static readonly GUIContent activeTargetLabel = EditorGUIUtility.TrTextContent("Active Tilemap", "Specifies the currently active Tilemap used for painting in the Scene View.");
            public static readonly GUIContent prefabWarningIcon = EditorGUIUtility.TrIconContent("console.warnicon.sml", "Editing Tilemaps in Prefabs will have better performance if edited in Prefab Mode.");

            public static readonly GUIContent tilePalette = EditorGUIUtility.TrTextContent("Tile Palette");
            public static readonly GUIContent edit = EditorGUIUtility.TrTextContent("Edit", "Toggle to edit current Tile Palette");
            public static readonly GUIContent editModified = EditorGUIUtility.TrTextContent("Edit*", "Toggle to save edits for current Tile Palette");
            public static readonly GUIContent gizmos = EditorGUIUtility.TrTextContent("Gizmos", "Toggle visibility of Gizmos in the Tile Palette");
            public static readonly GUIContent lockZPosition = EditorGUIUtility.TrTextContent("Lock Z Position", "Toggle editing of Z position");
            public static readonly GUIContent zPosition = EditorGUIUtility.TrTextContent("Z Position", "Set a Z position for the active Brush for painting");
            public static readonly GUIContent resetZPosition = EditorGUIUtility.TrTextContent("Reset", "Reset Z position for the active Brush");
            public static readonly GUIStyle ToolbarTitleStyle = "Toolbar";
            public static readonly GUIStyle dragHandle = "RL DragHandle";
            public static readonly float dragPadding = 3f;

            public static readonly GUILayoutOption[] dropdownOptions = { GUILayout.Width(k_DropdownWidth) };

            public static readonly GUIContent newLayerAtTopButtonText = EditorGUIUtility.TrTextContent("T", "Inserts a new tilemap layer instance at the top of the layer group of this type.");
            public static readonly GUIContent newLayerAtBottomButtonText = EditorGUIUtility.TrTextContent("B", "Inserts a new tilemap layer instance at the bottom of the layer group of this type.");
            public static readonly GUIContent moveLayerUpButtonText = EditorGUIUtility.TrTextContent("\u25B2", "Increases the sorting order in layer index of the tilemap, moving it upwards in the list.");
            public static readonly GUIContent moveLayerDownButtonText = EditorGUIUtility.TrTextContent("\u25BC", "Reduces the sorting order in layer index of the tilemap, moving it downwards in the list.");
            public static readonly GUIContent createLayerButtonText = EditorGUIUtility.TrTextContent("Create layer", "Creates the instance of a new tilemap layer of the type and with the sorting index determined by the selected tile.");
            public static readonly GUIContent selectTileButtonText = EditorGUIUtility.TrTextContent("Select tile asset", "Selects the asset of the selected tile and shows it in the inspector.");
            public static readonly GUIContent tileLayerSelectionToggleText = EditorGUIUtility.TrTextContent("Tile's layer selection", "When enabled, selecting a tile will automatically focus the layer described in the tile, if it exists.");
            public static readonly GUIContent setPaletteIcon = EditorGUIUtility.TrTextContent("Set palette icon", "Stores the sprite of the selected tile as the icon of the current palette.");
            public static readonly GUIContent noPalettesAvailableText = EditorGUIUtility.TrTextContent("No palettes available.");
            public static readonly GUIContent noGridInSceneText = EditorGUIUtility.TrTextContent("There is no grid in the scene.");
            public static readonly GUIContent noLayersInGrid = EditorGUIUtility.TrTextContent("No layers in grid.");
            public static readonly GUIContent createTilemapLayerSettings = EditorGUIUtility.TrTextContent("Create layer settings.", "It will create the asset were you can setup the tilemap layer types of your project.");
            public static readonly GUIContent defineLayerTypes = EditorGUIUtility.TrTextContent("Define layer types.", "Selects the tilemap layer settings asset.");
        }

        private class TilePaletteSaveScope : IDisposable
        {
            private GameObject m_GameObject;

            public TilePaletteSaveScope(GameObject paletteInstance)
            {
                m_GameObject = paletteInstance;
                if (m_GameObject != null)
                {
                    GridPaintingState.savingPalette = true;
                    SetHideFlagsRecursively(paletteInstance, HideFlags.HideInHierarchy);
                    foreach (var renderer in paletteInstance.GetComponentsInChildren<Renderer>())
                        renderer.gameObject.layer = 0;
                }
            }

            public void Dispose()
            {
                if (m_GameObject != null)
                {
                    SetHideFlagsRecursively(m_GameObject, HideFlags.HideAndDontSave);
                    GridPaintingState.savingPalette = false;
                }
            }

            private void SetHideFlagsRecursively(GameObject root, HideFlags flags)
            {
                root.hideFlags = flags;
                for (int i = 0; i < root.transform.childCount; i++)
                    SetHideFlagsRecursively(root.transform.GetChild(i).gameObject, flags);
            }
        }

        internal class TilePaletteProperties
        {
            public enum PrefabEditModeSettings
            {
                EnableDialog = 0,
                EditInPrefabMode = 1,
                EditInScene = 2
            }

            public static readonly string targetEditModeDialogTitle = L10n.Tr("Open in Prefab Mode");
            public static readonly string targetEditModeDialogMessage = L10n.Tr("Editing Tilemaps in Prefabs will have better performance if edited in Prefab Mode. Do you want to open it in Prefab Mode or edit it in the Scene?");
            public static readonly string targetEditModeDialogYes = L10n.Tr("Prefab Mode");
            public static readonly string targetEditModeDialogChange = L10n.Tr("Preferences");
            public static readonly string targetEditModeDialogNo = L10n.Tr("Scene");

            public static readonly string targetEditModeEditorPref = "TilePalette.TargetEditMode";
            public static readonly string targetEditModeLookup = "Target Edit Mode";
            public static readonly string tilePalettePreferencesLookup = "Tile Palette";

            public static readonly GUIContent targetEditModeDialogLabel = EditorGUIUtility.TrTextContent(targetEditModeLookup, "Controls the behaviour of editing a Prefab Instance when one is selected as the Active Target in the Tile Palette");
        }

        private static readonly GridBrushBase.Tool[] k_SceneViewEditModes =
        {
            GridBrushBase.Tool.Select,
            GridBrushBase.Tool.Move,
            GridBrushBase.Tool.Paint,
            GridBrushBase.Tool.Box,
            GridBrushBase.Tool.Pick,
            GridBrushBase.Tool.Erase,
            GridBrushBase.Tool.FloodFill
        };

        private const float k_DropdownWidth = 200f;
        private const float k_ActiveTargetLabelWidth = 90f;
        private const float k_ActiveTargetDropdownWidth = 180f;
        private const float k_ActiveTargetWarningSize = 20f;
        private const float k_TopAreaHeight = 104f;
        private const float k_MinBrushInspectorHeight = 50f;
        private const float k_MinClipboardHeight = 200f;
        private const float k_ToolbarHeight = 17f;
        private const float k_ResizerDragRectPadding = 10f;
        private const float k_LayersPanelWidth = 150.0f + 20.0f * 2.0f;
        private const float k_TilemapLayerHeaderButtonWidth = 20.0f;
        private static readonly Vector2 k_MinWindowSize = new Vector2(k_ActiveTargetLabelWidth + k_ActiveTargetDropdownWidth + k_ActiveTargetWarningSize, 200f);
        private const int k_MinimumRowsInTilemapLayerList = 5;

        private PaintableSceneViewGrid m_PaintableSceneViewGrid;

        class ShortcutContext : IShortcutToolContext
        {
            public bool active { get; set; }
        }

        readonly ShortcutContext m_ShortcutContext = new ShortcutContext { active = true };

        [FormerlyPrefKeyAs("Grid Painting/Select", "s")]
        [Shortcut("Grid Painting/Select", typeof(ShortcutContext), KeyCode.S)]
        static void GridSelectKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(SelectTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Move", "m")]
        [Shortcut("Grid Painting/Move", typeof(ShortcutContext), KeyCode.M)]
        static void GridMoveKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(MoveTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Brush", "b")]
        [Shortcut("Grid Painting/Brush", typeof(ShortcutContext), KeyCode.B)]
        static void GridBrushKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(PaintTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Rectangle", "u")]
        [Shortcut("Grid Painting/Rectangle", typeof(ShortcutContext), KeyCode.U)]
        static void GridRectangleKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(BoxTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Picker", "i")]
        [Shortcut("Grid Painting/Picker", typeof(ShortcutContext), KeyCode.I)]
        static void GridPickerKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(PickingTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Erase", "d")]
        [Shortcut("Grid Painting/Erase", typeof(ShortcutContext), KeyCode.D)]
        static void GridEraseKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(EraseTool));
        }

        [FormerlyPrefKeyAs("Grid Painting/Fill", "g")]
        [Shortcut("Grid Painting/Fill", typeof(ShortcutContext), KeyCode.G)]
        static void GridFillKey()
        {
            TilemapEditorTool.ToggleActiveEditorTool(typeof(FillTool));
        }

        static void RotateBrush(GridBrushBase.RotationDirection direction)
        {
            GridPaintingState.gridBrush.Rotate(direction, GridPaintingState.activeGrid.cellLayout);
            GridPaintingState.activeGrid.Repaint();
        }

        [FormerlyPrefKeyAs("Grid Painting/Rotate Clockwise", "[")]
        [Shortcut("Grid Painting/Rotate Clockwise", typeof(ShortcutContext), KeyCode.LeftBracket)]
        static void RotateBrushClockwise()
        {
            if (GridPaintingState.gridBrush != null && GridPaintingState.activeGrid != null)
                RotateBrush(GridBrushBase.RotationDirection.Clockwise);
        }

        [FormerlyPrefKeyAs("Grid Painting/Rotate Anti-Clockwise", "]")]
        [Shortcut("Grid Painting/Rotate Anti-Clockwise", typeof(ShortcutContext), KeyCode.RightBracket)]
        static void RotateBrushAntiClockwise()
        {
            if (GridPaintingState.gridBrush != null && GridPaintingState.activeGrid != null)
                RotateBrush(GridBrushBase.RotationDirection.CounterClockwise);
        }

        static void FlipBrush(GridBrushBase.FlipAxis axis)
        {
            GridPaintingState.gridBrush.Flip(axis, GridPaintingState.activeGrid.cellLayout);
            GridPaintingState.activeGrid.Repaint();
        }

        [FormerlyPrefKeyAs("Grid Painting/Flip X", "#[")]
        [Shortcut("Grid Painting/Flip X", typeof(ShortcutContext), KeyCode.LeftBracket, ShortcutModifiers.Shift)]
        static void FlipBrushX()
        {
            if (GridPaintingState.gridBrush != null && GridPaintingState.activeGrid != null)
                FlipBrush(GridBrushBase.FlipAxis.X);
        }

        [FormerlyPrefKeyAs("Grid Painting/Flip Y", "#]")]
        [Shortcut("Grid Painting/Flip Y", typeof(ShortcutContext), KeyCode.RightBracket, ShortcutModifiers.Shift)]
        static void FlipBrushY()
        {
            if (GridPaintingState.gridBrush != null && GridPaintingState.activeGrid != null)
                FlipBrush(GridBrushBase.FlipAxis.Y);
        }

        static void ChangeBrushZ(int change)
        {
            GridPaintingState.gridBrush.ChangeZPosition(change);
            GridPaintingState.activeGrid.ChangeZPosition(change);
            GridPaintingState.activeGrid.Repaint();
            foreach (var window in GridPaintPaletteWindow.instances)
            {
                window.Repaint();
            }
        }

        [Shortcut("Grid Painting/Increase Z", typeof(ShortcutContext), KeyCode.Minus)]
        static void IncreaseBrushZ()
        {
            if (GridPaintingState.gridBrush != null
                && GridPaintingState.activeGrid != null
                && GridPaintingState.activeBrushEditor != null
                && GridPaintingState.activeBrushEditor.canChangeZPosition)
                ChangeBrushZ(1);
        }

        [Shortcut("Grid Painting/Decrease Z", typeof(ShortcutContext), KeyCode.Equals)]
        static void DecreaseBrushZ()
        {
            if (GridPaintingState.gridBrush != null
                && GridPaintingState.activeGrid != null
                && GridPaintingState.activeBrushEditor != null
                && GridPaintingState.activeBrushEditor.canChangeZPosition)
                ChangeBrushZ(-1);
        }

        internal static void PreferencesGUI()
        {
            using (new SettingsWindow.GUIScope())
            {
                EditorGUI.BeginChangeCheck();
                var val = (TilePaletteProperties.PrefabEditModeSettings)EditorGUILayout.EnumPopup(TilePaletteProperties.targetEditModeDialogLabel, (TilePaletteProperties.PrefabEditModeSettings)EditorPrefs.GetInt(TilePaletteProperties.targetEditModeEditorPref, 0));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(TilePaletteProperties.targetEditModeEditorPref, (int)val);
                }
            }
        }

        private static List<GridPaintPaletteWindow> s_Instances;
        public static List<GridPaintPaletteWindow> instances
        {
            get
            {
                if (s_Instances == null)
                    s_Instances = new List<GridPaintPaletteWindow>();
                return s_Instances;
            }
        }

        public static bool isActive
        {
            get
            {
                return s_Instances != null && s_Instances.Count > 0;
            }
        }

        [SerializeField]
        private PreviewResizer m_PreviewResizer;

        private GridPalettesDropdown m_PaletteDropdown;

        [SerializeField]
        private GameObject m_Palette;

        [SerializeField]
        private bool m_DrawGizmos;

        internal bool drawGizmos
        {
            get { return m_DrawGizmos; }
        }

        public GameObject palette
        {
            get
            {
                return m_Palette;
            }
            set
            {
                if (m_Palette != value)
                {
                    clipboardView.OnBeforePaletteSelectionChanged();
                    m_Palette = value;
                    clipboardView.OnAfterPaletteSelectionChanged();
                    lastTilemapPalette = AssetDatabase.GetAssetPath(m_Palette);
                    GridPaintingState.OnPaletteChanged(m_Palette);
                    Repaint();
                }
            }
        }

        private GameObject m_PaletteInstance;
        public GameObject paletteInstance
        {
            get
            {
                return m_PaletteInstance;
            }
        }

        private bool m_DelayedResetPaletteInstance;
        private bool m_Enabled;

        public GridPaintPaletteClipboard clipboardView { get; private set; }

        private Vector2 m_BrushScroll;
        private GridBrushEditorBase m_PreviousToolActivatedEditor;
        private GridBrushBase.Tool m_PreviousToolActivated;

        private PreviewRenderUtility m_PreviewUtility;
        public PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_Enabled && m_PreviewUtility == null)
                    InitPreviewUtility();

                return m_PreviewUtility;
            }
        }

        // Tile selection data
        private TileBase m_previousActiveTile = null;
        private bool m_enableTileLayerSelection = true;

        // Tilemap layers data
        private Vector2 m_tilemapLayersScrollViewPos = Vector2.zero;
        private int m_cachedActiveTargetsHashCode = 0;

        private class TilemapLayer
        {
            public bool IsSelected;
            public TilemapRenderer TilemapInstance;
            public int SortIndex;
            public string LayerType;
        }

        private List<TilemapLayer> m_tilemapLayers = new List<TilemapLayer>();

        // Palette selection data
        Vector2 m_paletteScrollView = Vector2.zero;

        private void OnSelectionChange()
        {
            // Update active palette if user has selected a palette prefab
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                bool isPrefab = EditorUtility.IsPersistent(selectedObject) || (selectedObject.hideFlags & HideFlags.NotEditable) != 0;
                if (isPrefab)
                {
                    var assetPath = AssetDatabase.GetAssetPath(selectedObject);
                    var allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                    foreach (var asset in allAssets)
                    {
                        if (asset != null && asset.GetType() == typeof(GridPalette))
                        {
                            var targetPalette = (GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath);
                            if (targetPalette != palette)
                            {
                                palette = targetPalette;
                                Repaint();
                            }
                            break;
                        }
                    }
                }
            }
        }
        
        private void OnGUI()
        {

            EditorGUILayout.BeginVertical();
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            float leftMargin = (Screen.width / EditorGUIUtility.pixelsPerPoint - TilemapEditorTool.tilemapEditorToolsToolbarSize) * 0.5f;
            GUILayout.Space(leftMargin);
            float toolbarHeight = 40.0f;
            DoTilemapToolbar();
            GUILayout.Space(leftMargin);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(leftMargin);
            //DoActiveTargetsGUI();
            GUILayout.Space(leftMargin);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6f);
            //Rect clipboardToolbarRect = = EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.ToolbarTitleStyle);

            //EditorGUILayout.EndHorizontal();
            //DoClipboardHeader(position.width);

            EditorGUILayout.EndVertical();

            // The context menu only works in the top of the window
            if(GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                HandleContextMenu();
            }

            EditorGUILayout.BeginVertical();
            const float k_PaletteSelectionAreaHeight = 115.0f;
            const float k_ClipboardLeftMarging = 5.0f;
            float heightOfTilemapLayers = 20.0f * Mathf.Max(GridPaintingState.validTargets.Length + TilemapLayersSettings.GetLayers().Length + 1, k_MinimumRowsInTilemapLayerList); // This must be subtracted to the height of the clipboard because it is drawn in a space defined by a Space call, so adding new elements, like toggles, makes the height be greater and that disarranges the elements below
            
            ConvertGridPrefabToPalette(new Rect());
            // The area of the drag handler of the brush inspector
            Rect dragRect = new Rect(k_DropdownWidth + k_ResizerDragRectPadding, 
                                     0, 
                                     position.width - k_DropdownWidth - k_ResizerDragRectPadding, 
                                     k_ToolbarHeight);

            // The area of the brush inspector
            float brushInspectorSize = m_PreviewResizer.ResizeHandle(position, k_MinBrushInspectorHeight, k_MinClipboardHeight, k_ToolbarHeight, dragRect);
            // The area of the tile selector / clipboard
            float clipboardHeight = position.height - brushInspectorSize - k_PaletteSelectionAreaHeight - toolbarHeight + k_ToolbarHeight;
            Rect clipboardRect = new Rect(k_LayersPanelWidth + k_ClipboardLeftMarging, 
                                          k_PaletteSelectionAreaHeight, 
                                          position.width - k_LayersPanelWidth - k_ClipboardLeftMarging, 
                                          clipboardHeight);
            EditorGUILayout.BeginHorizontal();
            DoTilemapLayersGUI(new Rect(0.0f, toolbarHeight, k_LayersPanelWidth, clipboardHeight + k_PaletteSelectionAreaHeight - toolbarHeight));
            DoPalettesSelectionList(position.width - k_LayersPanelWidth);
            EditorGUILayout.EndHorizontal();

            // Note: Logic and visual representation is split due to the mouse inputs are handled by the first drawn element. The toggle is drawn, then the clipboard is drawn over it, then the toggle is drawn again (without logic) just to make it visible
            // Enable Edit mode toggle
            Rect enableEditModeToggleRect = DoEnableEditModeToggleLogic(clipboardRect);
            // Draw gizmos toggle
            Rect drawGizmosToggleRect = DoDrawGizmosToggleLogic(enableEditModeToggleRect);
            // Tile layer selection toggle
            Rect tileLayerSelectionToggleRect = DoTileLayerSelectionToggleLogic(drawGizmosToggleRect);
            // Set palette icon button
            Rect setPaletteIconButtonRect = DoSetPaletteIconButtonLogic(tileLayerSelectionToggleRect);

            // Select tile asset button
            Rect selectTileButtonRect = DoSelectTileAssetButtonLogic(clipboardRect);

            CustomDefaultTile activeTile = clipboardView.activeTile as CustomDefaultTile;
            int tileLayerIndex = -1;
            bool tileLayerExists = GetTileLayerIndex(activeTile, out tileLayerIndex);

            // Create new layer button
            bool showCreateLayerButton = activeTile != null && 
                                         !tileLayerExists && 
                                         (tileLayerIndex >= 0 || m_tilemapLayers.Count == 0) &&
                                         activeTile.TilemapLayerIndex >= 0;
            Rect createLayerButtonRect = selectTileButtonRect;

            if (showCreateLayerButton)
            {
                createLayerButtonRect = DoCreateTileLayerButtonLogic(selectTileButtonRect, tileLayerIndex);
            }

            // If enabled, it selects the layer of the tile when selected
            if (m_enableTileLayerSelection && 
                CheckNewTileSelected() && 
                tileLayerExists && 
                tileLayerIndex >= 0)
            {
                SelectTarget(-1, m_tilemapLayers[tileLayerIndex].TilemapInstance.gameObject);
            }

            // Draws the grid
            OnClipboardGUI(clipboardRect);

            DoTileLayerSelectionToggleVisualRepresentation(tileLayerSelectionToggleRect);
            DoSelectTileAssetButtonVisualRepresentation(selectTileButtonRect);

            DoSetPaletteIconButtonVisualRepresentation(setPaletteIconButtonRect);

            if (showCreateLayerButton)
            {
                DoCreateTileLayerButtonVisualRepresentation(createLayerButtonRect);
            }

            DoEnableEditModeToggleVisualRepresentation(enableEditModeToggleRect);
            DoDrawGizmosToggleVisualRepresentation(drawGizmosToggleRect);

            EditorGUILayout.EndVertical();

            // Area from the layer list to the brush selector
            GUILayout.Space(clipboardHeight + k_PaletteSelectionAreaHeight - heightOfTilemapLayers - toolbarHeight);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.ToolbarTitleStyle);
            DoBrushesDropdownToolbar();
            EditorGUILayout.EndHorizontal();
            m_BrushScroll = GUILayout.BeginScrollView(m_BrushScroll, false, false);
            GUILayout.Space(4f);
            OnBrushInspectorGUI();
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            Color oldColor = Handles.color;
            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(0, clipboardRect.yMax + 0.5f, 0), new Vector3(Screen.width, clipboardRect.yMax + 0.5f, 0));
            Handles.color = Color.black.AlphaMultiplied(0.33f);
            Handles.DrawLine(new Vector3(0, GUILayoutUtility.GetLastRect().yMax + 0.5f, 0), new Vector3(Screen.width, GUILayoutUtility.GetLastRect().yMax + 0.5f, 0));
            Handles.color = oldColor;
            
            EditorGUILayout.BeginVertical();

            GUILayout.Space(2f);

            EditorGUILayout.EndVertical();

            // Keep repainting until all previews are loaded
            if (AssetPreview.IsLoadingAssetPreviews(GetInstanceID()))
                Repaint();

            // Release keyboard focus on click to empty space
            if (Event.current.type == EventType.MouseDown)
                GUIUtility.keyboardControl = 0;
        }

        private bool CheckNewTileSelected()
        {
            bool wasTileSelected = false;

            if (m_previousActiveTile != clipboardView.activeTile as CustomDefaultTile)
            {
                // A new tile was selected
                m_previousActiveTile = clipboardView.activeTile;

                wasTileSelected = true;
            }

            return wasTileSelected;
        }

        // Returns true if the layer of the tile exists
        private bool GetTileLayerIndex(CustomDefaultTile activeTile, out int layerIndex)
        {
            if (activeTile != null)
            {
                // Checks whether the layer of the tile exists and, otherwise, in which position should it be inserted if created
                int activeTileSortingIndex = CalculateLayerSortingIndex(activeTile.TilemapLayerIndex, activeTile.SortingOrderInLayer);

                for (int i = 0; i < m_tilemapLayers.Count; ++i)
                {
                    if (m_tilemapLayers[i].SortIndex == activeTileSortingIndex)
                    {
                        layerIndex = i;
                        return true;
                    }
                    else if (m_tilemapLayers[i].SortIndex > activeTileSortingIndex)
                    {
                        layerIndex = i;
                        return false;
                    }
                }

                layerIndex = m_tilemapLayers.Count;
                return false;
            }

            layerIndex = -1;
            return false;
        }

        private int CalculateLayerSortingIndex(int layerTypeIndex, int sortingOrderInLayer)
        {
            return layerTypeIndex * 100 - sortingOrderInLayer;
        }
        
        private Rect DoDrawGizmosToggleLogic(Rect buttonPosition)
        {
            Rect drawGizmosToggleRect = buttonPosition;
            drawGizmosToggleRect.x = position.width - 70.0f;
            drawGizmosToggleRect.y += 20.0f;
            drawGizmosToggleRect.width = 70.0f;
            drawGizmosToggleRect.height = 20.0f;

            using (new EditorGUI.DisabledScope(palette == null))
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_DrawGizmos = EditorGUI.ToggleLeft(drawGizmosToggleRect, Styles.gizmos, m_DrawGizmos, EditorStyles.toolbarButton);
                } 
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_DrawGizmos)
                    {
                        clipboardView.SavePaletteIfNecessary();
                        ResetPreviewInstance();
                    }
                    Repaint();
                }
            }

            return drawGizmosToggleRect;
        }

        private void DoDrawGizmosToggleVisualRepresentation(Rect togglePosition)
        {
            EditorGUI.ToggleLeft(togglePosition, Styles.gizmos, m_DrawGizmos, EditorStyles.toolbarButton);
        }

        private Rect DoEnableEditModeToggleLogic(Rect buttonPosition)
        {
            Rect enableEditModeToggleRect = buttonPosition;
            enableEditModeToggleRect.x = position.width - 70.0f;
            enableEditModeToggleRect.y += 0.0f;
            enableEditModeToggleRect.width = 70.0f;
            enableEditModeToggleRect.height = 20.0f;

            using (new EditorGUI.DisabledScope(palette == null))
            {
                clipboardView.unlocked = EditorGUI.ToggleLeft(enableEditModeToggleRect, clipboardView.isModified ? Styles.editModified : Styles.edit, clipboardView.unlocked, EditorStyles.toolbarButton);
            }

            return enableEditModeToggleRect;
        }

        private void DoEnableEditModeToggleVisualRepresentation(Rect togglePosition)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = clipboardView.unlocked ? Color.magenta : previousColor;
            GUI.color = GUI.backgroundColor;
            EditorGUI.ToggleLeft(togglePosition, clipboardView.isModified ? Styles.editModified : Styles.edit, clipboardView.unlocked, EditorStyles.toolbarButton);
            GUI.color = previousColor;
            GUI.backgroundColor = previousColor;
        }

        private Rect DoSetPaletteIconButtonLogic(Rect buttonPosition)
        {
            Rect setPaletteIconButtonRect = buttonPosition;
            setPaletteIconButtonRect.x = position.width - 100.0f;
            setPaletteIconButtonRect.y += 20.0f;
            setPaletteIconButtonRect.width = 100.0f;
            setPaletteIconButtonRect.height = 20.0f;

            if (clipboardView.activeTile != null &&
                EditorGUI.Button(setPaletteIconButtonRect, Styles.setPaletteIcon))
            {
                //GridPalette gridPalette = GridPaletteUtility.GetGridPaletteFromPaletteAsset(palette);
                GridPaletteIconsCache.SetIconForPalette((clipboardView.activeTile as Tile).sprite, palette);
            }

            return setPaletteIconButtonRect;
        }

        private void DoSetPaletteIconButtonVisualRepresentation(Rect buttonPosition)
        {
            if (clipboardView.activeTile != null &&
                EditorGUI.Button(buttonPosition, Styles.setPaletteIcon))
            {
            }
        }

        private Rect DoCreateTileLayerButtonLogic(Rect buttonPosition, int newLayerInsertionIndex)
        {
            Grid grid = GetTilemapsGrid();
            CustomDefaultTile activeTile = clipboardView.activeTile as CustomDefaultTile;

            if (grid == null || activeTile == null)
            {
                return buttonPosition;
            }

            Rect createLayerButtonRect = buttonPosition;
            createLayerButtonRect.y += 20.0f;
            createLayerButtonRect.width = 120.0f;
            createLayerButtonRect.height = 40.0f;

            if (EditorGUI.Button(createLayerButtonRect, Styles.createLayerButtonText))
            {
                string layerTypeName = TilemapLayersSettings.GetLayers()[activeTile.TilemapLayerIndex].Name;

                TilemapRenderer tilemapRenderer = (PrefabUtility.InstantiatePrefab(TilemapLayersSettings.GetLayers()[activeTile.TilemapLayerIndex].LayerPrefab, grid.transform) as GameObject).GetComponent<TilemapRenderer>();
                tilemapRenderer.name = layerTypeName + "_" + activeTile.SortingOrderInLayer;
                tilemapRenderer.sortingOrder = activeTile.SortingOrderInLayer;

                TilemapLayer newLayer = new TilemapLayer()
                                            {
                                                IsSelected = true,
                                                TilemapInstance = tilemapRenderer,
                                                LayerType = layerTypeName,
                                                SortIndex = CalculateLayerSortingIndex(activeTile.TilemapLayerIndex, activeTile.SortingOrderInLayer)
                                            };
                m_tilemapLayers.Insert(newLayerInsertionIndex, newLayer);
                SelectTarget(-1, tilemapRenderer.gameObject);
                Debug.Log("Layer created.");
            }

            return createLayerButtonRect;
        }

        private void DoCreateTileLayerButtonVisualRepresentation(Rect buttonPosition)
        {
            CustomDefaultTile activeTile = clipboardView.activeTile as CustomDefaultTile;

            if(activeTile == null)
            {
                return;
            }

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (EditorGUI.Button(buttonPosition, Styles.createLayerButtonText))
            {
            }

            // The name of the layer to be created
            Rect labelRect = buttonPosition;
            labelRect.y += 17.0f;
            labelRect.height = 23.0f;

            string layerTypeName = TilemapLayersSettings.GetLayers()[activeTile.TilemapLayerIndex].Name;

            EditorGUI.LabelField(labelRect, layerTypeName + "_" + activeTile.SortingOrderInLayer, EditorStyles.contentToolbar);

            GUI.backgroundColor = previousColor;

            EditorGUI.DrawOutline(buttonPosition, 1, Color.red);
        }

        private Rect DoTileLayerSelectionToggleLogic(Rect buttonPosition)
        {
            Rect tileLayerSelectionToggleRect = buttonPosition;
            tileLayerSelectionToggleRect.x = position.width - 140.0f;
            tileLayerSelectionToggleRect.y += 20.0f;
            tileLayerSelectionToggleRect.width = 140.0f;
            tileLayerSelectionToggleRect.height = 20.0f;

            m_enableTileLayerSelection = EditorGUI.ToggleLeft(tileLayerSelectionToggleRect, Styles.tileLayerSelectionToggleText, m_enableTileLayerSelection);

            return tileLayerSelectionToggleRect;
        }

        private void DoTileLayerSelectionToggleVisualRepresentation(Rect buttonPosition)
        {
            EditorGUI.ToggleLeft(buttonPosition, Styles.tileLayerSelectionToggleText, m_enableTileLayerSelection, EditorStyles.toolbarButton);
        }

        private Rect DoSelectTileAssetButtonLogic(Rect buttonPosition)
        {
            Rect selectTileButtonRect = buttonPosition;
            selectTileButtonRect.y += 0.0f;
            selectTileButtonRect.width = 120.0f;
            selectTileButtonRect.height = 20.0f;

            if (clipboardView.activeTile != null &&
                EditorGUI.Button(selectTileButtonRect, Styles.selectTileButtonText))
            {
                Selection.activeObject = clipboardView.activeTile;
                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            }

            return selectTileButtonRect;
        }

        private void DoSelectTileAssetButtonVisualRepresentation(Rect buttonPosition)
        {
            if (clipboardView.activeTile != null &&
                EditorGUI.Button(buttonPosition, Styles.selectTileButtonText))
            {
            }
        }

        static void DoTilemapToolbar()
        {
            EditorTool active = EditorToolManager.activeTool;
            EditorTool selected;


#if UNITY_2021 || UNITY_2021_1_OR_NEWER
            if (EditorGUILayout.EditorToolbar(GUIContent.none, active, TilemapEditorTool.tilemapEditorTools, out selected))
#elif UNITY_2019 || UNITY_2019_1_OR_NEWER
            if (EditorGUILayout.EditorToolbar(active, TilemapEditorTool.tilemapEditorTools, out selected))
#endif
            {
                if (active == selected)
#if UNITY_2021 || UNITY_2021_1_OR_NEWER
                    ToolManager.SetActiveTool(EditorToolManager.activeTool);
#elif UNITY_2019 || UNITY_2019_1_OR_NEWER
                    ToolManager.SetActiveTool(EditorToolManager.GetLastTool(x => !TilemapEditorTool.tilemapEditorTools.Contains(x)));
#endif
                else
                    ToolManager.SetActiveTool(selected);
            }
        }

        public void DelayedResetPreviewInstance()
        {
            m_DelayedResetPaletteInstance = true;
        }

        public void ResetPreviewInstance()
        {
            if (m_PreviewUtility == null)
                InitPreviewUtility();

            m_DelayedResetPaletteInstance = false;
            DestroyPreviewInstance();
            if (palette != null)
            {
                m_PaletteInstance = previewUtility.InstantiatePrefabInScene(palette);

                // Disconnecting prefabs is no longer possible.
                // If performance of overrides on palette palette instance turns out to be a problem.
                // unpack the prefab instance here, and overwrite the prefab later instead of reconnecting.
                PrefabUtility.UnpackPrefabInstance(m_PaletteInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

                EditorUtility.InitInstantiatedPreviewRecursive(m_PaletteInstance);
                m_PaletteInstance.transform.position = new Vector3(0, 0, 0);
                m_PaletteInstance.transform.rotation = Quaternion.identity;
                m_PaletteInstance.transform.localScale = Vector3.one;

                GridPalette paletteAsset = GridPaletteUtility.GetGridPaletteFromPaletteAsset(palette);
                if (paletteAsset != null)
                {
                    if (paletteAsset.cellSizing == GridPalette.CellSizing.Automatic)
                    {
                        Grid grid = m_PaletteInstance.GetComponent<Grid>();
                        if (grid != null)
                        {
                            grid.cellSize = GridPaletteUtility.CalculateAutoCellSize(grid, grid.cellSize);
                        }
                        else
                        {
                            Debug.LogWarning("Grid component not found from: " + palette.name);
                        }
                    }

                    previewUtility.camera.transparencySortMode = paletteAsset.transparencySortMode;
                    previewUtility.camera.transparencySortAxis = paletteAsset.transparencySortAxis;
                }
                else
                {
                    Debug.LogWarning("GridPalette subasset not found from: " + palette.name);
                    previewUtility.camera.transparencySortMode = TransparencySortMode.Default;
                    previewUtility.camera.transparencySortAxis = new Vector3(0f, 0f, 1f);
                }

                foreach (var transform in m_PaletteInstance.GetComponentsInChildren<Transform>())
                    transform.gameObject.hideFlags = HideFlags.HideAndDontSave;

                // Show all renderers from Palettes from previous versions
                PreviewRenderUtility.SetEnabledRecursive(m_PaletteInstance, true);

                clipboardView.ResetPreviewMesh();
            }
        }

        public void DestroyPreviewInstance()
        {
            if (m_PaletteInstance != null)
            {
                Undo.ClearUndo(m_PaletteInstance);
                DestroyImmediate(m_PaletteInstance);
            }
        }

        public void InitPreviewUtility()
        {
            int previewCullingLayer = Camera.PreviewCullingLayer;

            m_PreviewUtility = new PreviewRenderUtility(true, true);
            m_PreviewUtility.camera.cullingMask = 1 << previewCullingLayer;
            m_PreviewUtility.camera.gameObject.layer = previewCullingLayer;
            m_PreviewUtility.lights[0].gameObject.layer = previewCullingLayer;
            m_PreviewUtility.camera.orthographic = true;
            m_PreviewUtility.camera.orthographicSize = 5f;
            m_PreviewUtility.camera.transform.position = new Vector3(0f, 0f, -10f);
            m_PreviewUtility.ambientColor = new Color(1f, 1f, 1f, 0);

            ResetPreviewInstance();
            clipboardView.SetupPreviewCameraOnInit();
        }

        private void HandleContextMenu()
        {
            if (Event.current.type == EventType.ContextClick)
            {
                DoContextMenu();
                Event.current.Use();
            }
        }

        public void SavePalette()
        {
            if (paletteInstance != null && palette != null)
            {
                using (new TilePaletteSaveScope(paletteInstance))
                {
                    string path = AssetDatabase.GetAssetPath(palette);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(paletteInstance, path, InteractionMode.AutomatedAction);
                }

                ResetPreviewInstance();
                Repaint();
            }
        }

        private void DoContextMenu()
        {
            GenericMenu pm = new GenericMenu();
            if (GridPaintingState.scenePaintTarget != null)
                pm.AddItem(Styles.selectPaintTarget, false, SelectPaintTarget);
            else
                pm.AddDisabledItem(Styles.selectPaintTarget);

            if (palette != null)
                pm.AddItem(Styles.selectPalettePrefab, false, SelectPaletteAsset);
            else
                pm.AddDisabledItem(Styles.selectPalettePrefab);

            if (clipboardView.activeTile != null)
                pm.AddItem(Styles.selectTileAsset, false, SelectTileAsset);
            else
                pm.AddDisabledItem(Styles.selectTileAsset);

            pm.AddSeparator("");

            if (clipboardView.unlocked)
                pm.AddItem(Styles.lockPaletteEditing, false, FlipLocked);
            else
                pm.AddItem(Styles.unlockPaletteEditing, false, FlipLocked);

            pm.AddItem(Styles.openTilePalettePreferences, false, OpenTilePalettePreferences);

            pm.ShowAsContext();
        }

        private void OpenTilePalettePreferences()
        {
            var settingsWindow = SettingsWindow.Show(SettingsScope.User);
            settingsWindow.FilterProviders(TilePaletteProperties.tilePalettePreferencesLookup);
        }

        private void FlipLocked()
        {
            clipboardView.unlocked = !clipboardView.unlocked;
        }

        private void SelectPaintTarget()
        {
            Selection.activeObject = GridPaintingState.scenePaintTarget;
        }

        private void SelectPaletteAsset()
        {
            Selection.activeObject = palette;
        }

        private void SelectTileAsset()
        {
            Selection.activeObject = clipboardView.activeTile;
        }

        private bool NotOverridingColor(GridBrush defaultGridBrush)
        {
            foreach (var cell in defaultGridBrush.cells)
            {
                TileBase tile = cell.tile;
                if (tile is Tile && ((tile as Tile).flags & TileFlags.LockColor) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void DoBrushesDropdownToolbar()
        {
            GUIContent content = GUIContent.Temp(GridPaintingState.gridBrush.name);
            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, EditorStyles.toolbarPopup, Styles.dropdownOptions))
            {
                var menuData = new GridBrushesDropdown.MenuItemProvider();
                var flexibleMenu = new GridBrushesDropdown(menuData, GridPaletteBrushes.brushes.IndexOf(GridPaintingState.gridBrush), null, SelectBrush, k_DropdownWidth);
                PopupWindow.Show(GUILayoutUtility.topLevel.GetLast(), flexibleMenu);
            }
            if (Event.current.type == EventType.Repaint)
            {
                var dragRect = GUILayoutUtility.GetLastRect();
                var dragIconRect = new Rect();
                dragIconRect.x = dragRect.x + dragRect.width + Styles.dragPadding;
                dragIconRect.y = dragRect.y + (dragRect.height - Styles.dragHandle.fixedHeight) / 2 + 1;
                dragIconRect.width = position.width - (dragIconRect.x) - Styles.dragPadding;
                dragIconRect.height = Styles.dragHandle.fixedHeight;
                Styles.dragHandle.Draw(dragIconRect, GUIContent.none, false, false, false, false);
            }
            GUILayout.FlexibleSpace();
        }

        private void SelectBrush(int i, object o)
        {
            GridPaintingState.gridBrush = GridPaletteBrushes.brushes[i];
        }

        public void OnEnable()
        {
            m_Enabled = true;
            instances.Add(this);
            if (clipboardView == null)
            {
                clipboardView = CreateInstance<GridPaintPaletteClipboard>();
                clipboardView.owner = this;
                clipboardView.hideFlags = HideFlags.HideAndDontSave;
                clipboardView.unlocked = false;
            }

            if (m_PaintableSceneViewGrid == null)
            {
                m_PaintableSceneViewGrid = CreateInstance<PaintableSceneViewGrid>();
                m_PaintableSceneViewGrid.hideFlags = HideFlags.HideAndDontSave;
            }

            GridPaletteBrushes.FlushCache();
            ShortcutIntegration.instance.profileManager.shortcutBindingChanged += UpdateTooltips;
            GridSelection.gridSelectionChanged += OnGridSelectionChanged;
            GridPaintingState.RegisterPainterInterest(this);
            GridPaintingState.scenePaintTargetChanged += OnScenePaintTargetChanged;
            GridPaintingState.brushChanged += OnBrushChanged;
            SceneView.duringSceneGui += OnSceneViewGUI;
            PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;
            EditorApplication.projectWasLoaded += OnProjectLoaded;

            AssetPreview.SetPreviewTextureCacheSize(256, GetInstanceID());
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;

            if (m_PreviewResizer == null)
            {
                m_PreviewResizer = new PreviewResizer();
                m_PreviewResizer.Init("TilemapBrushInspector");
            }

            minSize = k_MinWindowSize;

            if (palette == null && !String.IsNullOrEmpty(lastTilemapPalette))
            {
                palette = GridPalettes.palettes
                    .Where((palette, index) => (AssetDatabase.GetAssetPath(palette) == lastTilemapPalette))
                    .FirstOrDefault();
            }
            if (palette == null && GridPalettes.palettes.Count > 0)
            {
                palette = GridPalettes.palettes[0];
            }

            ToolManager.activeToolChanged += ActiveToolChanged;
            ToolManager.activeToolChanging += ActiveToolChanging;

            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_ShortcutContext);

            TilemapLayersSettings.CacheLayers();
            GridPaletteIconsCache.CachePaletteIcons();
        }

        private static void UpdateTooltips(IShortcutProfileManager obj, Identifier identifier, ShortcutBinding oldBinding, ShortcutBinding newBinding)
        {
            TilemapEditorTool.UpdateTooltips();
        }

        private void PrefabInstanceUpdated(GameObject updatedPrefab)
        {
            // case 947462: Reset the palette instance after its prefab has been updated as it could have been changed
            if (m_PaletteInstance != null && PrefabUtility.GetCorrespondingObjectFromSource(updatedPrefab) == m_Palette && !GridPaintingState.savingPalette)
            {
                ResetPreviewInstance();
                Repaint();
            }
        }

        private void OnProjectLoaded()
        {
            // ShortcutIntegration instance is recreated after LoadLayout which wipes the OnEnable registration
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_ShortcutContext);
        }

        private void OnBrushChanged(GridBrushBase brush)
        {
            DisableFocus();
            if (brush is GridBrush)
                EnableFocus();
            SceneView.RepaintAll();
        }

        private void OnGridSelectionChanged()
        {
            Repaint();
        }

        public void OnDisable()
        {
            m_Enabled = false;
            DisableFocus();
            focusMode = TilemapFocusMode.None;

            CallOnToolDeactivated();
            instances.Remove(this);
            if (instances.Count <= 1)
                GridPaintingState.gridBrush = null;
            DestroyPreviewInstance();
            DestroyImmediate(clipboardView);
            DestroyImmediate(m_PaintableSceneViewGrid);

            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
            m_PreviewUtility = null;

            if (PaintableGrid.InGridEditMode())
            {
                // Set Editor Tool to an always available Tool, as Tile Palette Tools are not available any more
                ToolManager.SetActiveTool<UnityEditor.RectTool>();
            }

            ShortcutIntegration.instance.profileManager.shortcutBindingChanged -= UpdateTooltips;
            ToolManager.activeToolChanged -= ActiveToolChanged;
            ToolManager.activeToolChanging -= ActiveToolChanging;
            GridSelection.gridSelectionChanged -= OnGridSelectionChanged;
            SceneView.duringSceneGui -= OnSceneViewGUI;
            GridPaintingState.scenePaintTargetChanged -= OnScenePaintTargetChanged;
            GridPaintingState.brushChanged -= OnBrushChanged;
            GridPaintingState.UnregisterPainterInterest(this);
            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;
            EditorApplication.projectWasLoaded -= OnProjectLoaded;

            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_ShortcutContext);
        }

        private void OnScenePaintTargetChanged(GameObject scenePaintTarget)
        {
            DisableFocus();
            EnableFocus();
            Repaint();
        }

        private void ActiveToolChanged()
        {
            if (GridPaintingState.gridBrush != null && PaintableGrid.InGridEditMode() && GridPaintingState.activeBrushEditor != null)
            {
                GridBrushBase.Tool tool = PaintableGrid.EditTypeToBrushTool(ToolManager.activeToolType);
                GridPaintingState.activeBrushEditor.OnToolActivated(tool);
                m_PreviousToolActivatedEditor = GridPaintingState.activeBrushEditor;
                m_PreviousToolActivated = tool;

                for (int i = 0; i < k_SceneViewEditModes.Length; ++i)
                {
                    if (k_SceneViewEditModes[i] == tool)
                    {
                        Cursor.SetCursor(MouseStyles.mouseCursorTextures[i],
                            MouseStyles.mouseCursorTextures[i] != null ? MouseStyles.mouseCursorOSHotspot[(int)SystemInfo.operatingSystemFamily] : Vector2.zero,
                            CursorMode.Auto);
                        break;
                    }
                }
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            Repaint();
        }

        private void ActiveToolChanging()
        {
            if (!TilemapEditorTool.IsActive(typeof(MoveTool)) && !TilemapEditorTool.IsActive(typeof(SelectTool)))
            {
                GridSelection.Clear();
            }
            CallOnToolDeactivated();
        }

        private void CallOnToolDeactivated()
        {
            if (GridPaintingState.gridBrush != null && m_PreviousToolActivatedEditor != null)
            {
                m_PreviousToolActivatedEditor.OnToolDeactivated(m_PreviousToolActivated);
                m_PreviousToolActivatedEditor = null;

                if (!PaintableGrid.InGridEditMode())
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        internal void ResetZPosition()
        {
            GridPaintingState.gridBrush.ResetZPosition();
            GridPaintingState.lastActiveGrid.ResetZPosition();
        }

        private void OnBrushInspectorGUI()
        {
            if (GridPaintingState.gridBrush == null)
                return;

            // Brush Inspector GUI
            EditorGUI.BeginChangeCheck();
            if (GridPaintingState.activeBrushEditor != null)
                GridPaintingState.activeBrushEditor.OnPaintInspectorGUI();
            else if (GridPaintingState.fallbackEditor != null)
                GridPaintingState.fallbackEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                GridPaletteBrushes.ActiveGridBrushAssetChanged();
            }

            // Z Position Inspector
            var hasLastActiveGrid = GridPaintingState.lastActiveGrid != null;
            using (new EditorGUI.DisabledScope(!hasLastActiveGrid))
            {
                var lockZPosition = false;
                if (GridPaintingState.activeBrushEditor != null)
                {
                    EditorGUI.BeginChangeCheck();
                    lockZPosition = EditorGUILayout.Toggle(Styles.lockZPosition, !GridPaintingState.activeBrushEditor.canChangeZPosition);
                    if (EditorGUI.EndChangeCheck())
                        GridPaintingState.activeBrushEditor.canChangeZPosition = !lockZPosition;
                }
                using (new EditorGUI.DisabledScope(lockZPosition))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    var zPosition = EditorGUILayout.DelayedIntField(Styles.zPosition, hasLastActiveGrid ? GridPaintingState.lastActiveGrid.zPosition : 0);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GridPaintingState.gridBrush.ChangeZPosition(zPosition - GridPaintingState.lastActiveGrid.zPosition);
                        GridPaintingState.lastActiveGrid.zPosition = zPosition;
                    }
                    if (GUILayout.Button(Styles.resetZPosition))
                    {
                        ResetZPosition();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private bool IsObjectPrefabInstance(Object target)
        {
            return target != null && PrefabUtility.IsPartOfRegularPrefab(target);
        }

        private GameObject FindPrefabInstanceEquivalent(GameObject prefabInstance, GameObject prefabTarget)
        {
            var prefabRoot = prefabTarget.transform.root.gameObject;
            var currentTransform = prefabTarget.transform;
            var reverseTransformOrder = new Stack<int>();
            while (currentTransform != prefabRoot.transform && currentTransform.parent != null)
            {
                var parentTransform = currentTransform.parent;
                for (int i = 0; i < parentTransform.childCount; ++i)
                {
                    if (currentTransform == parentTransform.GetChild(i))
                    {
                        reverseTransformOrder.Push(i);
                        break;
                    }
                }
                currentTransform = currentTransform.parent;
            }

            currentTransform = prefabInstance.transform;
            while (reverseTransformOrder.Count > 0)
            {
                var childIndex = reverseTransformOrder.Pop();
                if (childIndex >= currentTransform.childCount)
                    return null;
                currentTransform = currentTransform.GetChild(childIndex);
            }
            return currentTransform.gameObject;
        }

        private void GoToPrefabMode(GameObject target)
        {
            var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(target);
            var assetPath = AssetDatabase.GetAssetPath(prefabObject);
            var stage = PrefabStageUtility.OpenPrefab(assetPath);
            var prefabInstance = stage.prefabContentsRoot;
            var prefabTarget = FindPrefabInstanceEquivalent(prefabInstance, prefabObject);
            if (prefabTarget != null)
            {
                GridPaintingState.scenePaintTarget = prefabTarget;
            }
        }
        /*
        private void DoActiveTargetsGUI()
        {
            using (new EditorGUI.DisabledScope(GridPaintingState.validTargets == null || GridPaintingState.scenePaintTarget == null))
            {
                bool hasPaintTarget = GridPaintingState.scenePaintTarget != null;
                bool needWarning = IsObjectPrefabInstance(GridPaintingState.scenePaintTarget);

                GUILayout.Label(Styles.activeTargetLabel, GUILayout.Width(k_ActiveTargetLabelWidth), GUILayout.Height(k_ActiveTargetWarningSize));
                GUIContent content = GUIContent.Temp(hasPaintTarget ? GridPaintingState.scenePaintTarget.name : "Nothing");
                if (EditorGUILayout.DropdownButton(content, FocusType.Passive, EditorStyles.popup, GUILayout.Width(k_ActiveTargetDropdownWidth - (needWarning ? k_ActiveTargetWarningSize : 0f)), GUILayout.Height(k_ActiveTargetWarningSize)))
                {
                    int index = hasPaintTarget ? Array.IndexOf(GridPaintingState.validTargets, GridPaintingState.scenePaintTarget) : 0;
                    var menuData = new GridPaintTargetsDropdown.MenuItemProvider();
                    var flexibleMenu = new GridPaintTargetsDropdown(menuData, index, null, SelectTarget, k_ActiveTargetDropdownWidth);
                    PopupWindow.Show(GUILayoutUtility.topLevel.GetLast(), flexibleMenu);
                }
                if (needWarning)
                    GUILayout.Label(Styles.prefabWarningIcon, GUILayout.Width(k_ActiveTargetWarningSize), GUILayout.Height(k_ActiveTargetWarningSize));
            }
        }
        */

        private Grid GetTilemapsGrid()
        {
            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();
            GameObject[] results = currentStageHandle.FindComponentsOfType<Grid>().Where(x => x.gameObject.scene.isLoaded &&
                                                                                                    x.gameObject.activeInHierarchy).Select(x => x.gameObject).ToArray();
            Grid grid = results.Length > 0 ? results[0].GetComponent<Grid>() 
                                           : null;
            return grid;
        }

        private void DoTilemapLayersGUI(Rect panelRect)
        {
            TilemapLayersSettings.TilemapLayer[] layerTypes = TilemapLayersSettings.GetLayers();

            Grid grid = GetTilemapsGrid();

            if(grid != null)
            {
                // If the tilemaps hierarchy changed, rebuild the layers
                if (GridPaintingState.validTargets.GetHashCode() != m_cachedActiveTargetsHashCode)
                {
                    m_cachedActiveTargetsHashCode = GridPaintingState.validTargets.GetHashCode();

                    for (int i = 0; i < m_tilemapLayers.Count; ++i)
                    {
                        // Some tilemaps were removed from the grid
                        if (m_tilemapLayers[i].TilemapInstance == null)
                        {
                            if (m_tilemapLayers[i].IsSelected && GridPaintingState.validTargets.Length > 0)
                            {
                                // Selects the first target by default
                                SelectTarget(0, GridPaintingState.validTargets[0].gameObject);
                            }

                            m_tilemapLayers.RemoveAt(i);
                            --i;
                        }
                    }

                    for (int i = 0; i < GridPaintingState.validTargets.Length; ++i)
                    {
                        bool targetWasInList = false;

                        for(int j = 0; j < m_tilemapLayers.Count; ++j)
                        {
                            if (m_tilemapLayers[j].TilemapInstance == null || 
                                GridPaintingState.validTargets[i] == m_tilemapLayers[j].TilemapInstance.gameObject)
                            {
                                targetWasInList = true;
                                break;
                            }
                        }

                        // New tilemaps were added to the grid
                        if(!targetWasInList)
                        {
                            int layerTypePosition = 0;

                            for(; layerTypePosition < layerTypes.Length; ++layerTypePosition)
                            {
                                if(GridPaintingState.validTargets[i].name.StartsWith(layerTypes[layerTypePosition].Name))
                                {
                                    break;
                                }
                            }

                            m_tilemapLayers.Add(new TilemapLayer(){ IsSelected = false,
                                                                    TilemapInstance = GridPaintingState.validTargets[i].GetComponent<TilemapRenderer>(),
                                                                    SortIndex = CalculateLayerSortingIndex(layerTypePosition, GridPaintingState.validTargets[i].GetComponent<TilemapRenderer>().sortingOrder),
                                                                    LayerType = layerTypePosition < layerTypes.Length ? layerTypes[layerTypePosition].Name 
                                                                                                                      : string.Empty });
                        }
                    }

                    // Sorts all tilemap layers according to settings and their sort index
                    m_tilemapLayers.Sort((a, b) => { return a.SortIndex - b.SortIndex; });

                    Debug.Log("Layers changed. List rebuilt."); 
                }
            }

            // Draws the layer list
            Rect viewRect = panelRect;
            viewRect.height = 20.0f * (GridPaintingState.validTargets.Length + TilemapLayersSettings.GetLayers().Length + 2);

            m_tilemapLayersScrollViewPos = GUI.BeginScrollView(panelRect, m_tilemapLayersScrollViewPos, viewRect);
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(panelRect.width));
                {
                    if (grid != null)
                    {
                        // Selection according to scene hierarchy
                        for (int i = 0; i < m_tilemapLayers.Count; ++i)
                        {
                            m_tilemapLayers[i].IsSelected = GridPaintingState.scenePaintTarget == m_tilemapLayers[i].TilemapInstance.gameObject;
                        }

                        // Tilemap type headers
                        Color previousColor = GUI.backgroundColor;
                        GUIStyleState previousState = EditorStyles.foldoutHeader.focused;
                        EditorStyles.foldoutHeader.focused = EditorStyles.foldoutHeader.normal;
                        int layerIndex = -1;

                        for(int i = 0; i < layerTypes.Length; ++i)
                        {
                            EditorGUILayout.BeginHorizontal(GUILayout.Width(k_LayersPanelWidth));
                            {
                                GUI.backgroundColor = Color.black;
                                GUILayout.Label(layerTypes[i].Name, EditorStyles.foldoutHeader);
                                GUI.backgroundColor = previousColor;
                            }
                            EditorGUILayout.EndHorizontal();

                            Rect labelRect = GUILayoutUtility.GetLastRect();
                            Rect buttonRect = new Rect(labelRect.x + labelRect.width - k_TilemapLayerHeaderButtonWidth * 2.0f,
                                                       labelRect.y,
                                                       k_TilemapLayerHeaderButtonWidth,
                                                       labelRect.height);

                            // Add new layer at the top
                            if (EditorGUI.Button(buttonRect, Styles.newLayerAtTopButtonText))
                            {
                                //Undo.RecordObject(grid.gameObject, "Tilemap layer added");
                                EditorUtility.SetDirty(grid.gameObject);

                                GameObject newTilemap = PrefabUtility.InstantiatePrefab(layerTypes[i].LayerPrefab, grid.transform) as GameObject;
                                TilemapLayer newLayer = new TilemapLayer()
                                                            {
                                                                IsSelected = true,
                                                                TilemapInstance = newTilemap.GetComponent<TilemapRenderer>(),
                                                                LayerType = layerTypes[i].Name
                                                            };

                                int nextTypeSortIndex = CalculateLayerSortingIndex(i, -50); // 50 = 100 (separation among types) / 2
                                bool hasAddedLayer = false;

                                for (int k = 0; k < m_tilemapLayers.Count; ++k)
                                {
                                    if(m_tilemapLayers[k].SortIndex > nextTypeSortIndex)
                                    {
                                        // There are not layers of this type, add the first one
                                        newLayer.TilemapInstance.sortingOrder = 0;
                                        newLayer.SortIndex = CalculateLayerSortingIndex(i, 0);
                                        newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                        m_tilemapLayers.Insert(k, newLayer);
                                        hasAddedLayer = true;
                                        break;
                                    }
                                    else if(m_tilemapLayers[k].LayerType == layerTypes[i].Name)
                                    {
                                        // There are layers of ths type, add a new one atop of them
                                        newLayer.TilemapInstance.sortingOrder = m_tilemapLayers[k].TilemapInstance.sortingOrder + 1;
                                        newLayer.SortIndex = m_tilemapLayers[k].SortIndex - 1;
                                        newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                        m_tilemapLayers.Insert(k, newLayer);
                                        hasAddedLayer = true;
                                        break;
                                    }
                                }

                                if(!hasAddedLayer)
                                {
                                    // There are not layers of the last type, add the first one
                                    newLayer.TilemapInstance.sortingOrder = 0;
                                    newLayer.SortIndex = CalculateLayerSortingIndex(i, 0);
                                    newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                    m_tilemapLayers.Add(newLayer);
                                }

                                SelectTarget(-1, newLayer.TilemapInstance.gameObject);
                            }

                            buttonRect.x = labelRect.x + labelRect.width - k_TilemapLayerHeaderButtonWidth;

                            // Add new layer at the bottom
                            if (EditorGUI.Button(buttonRect, Styles.newLayerAtBottomButtonText))
                            {
                                //Undo.RecordObject(grid.gameObject, "Tilemap layer added");
                                EditorUtility.SetDirty(grid.gameObject);

                                GameObject newTilemap = PrefabUtility.InstantiatePrefab(layerTypes[i].LayerPrefab, grid.transform) as GameObject;
                                TilemapLayer newLayer = new TilemapLayer()
                                                            {
                                                                IsSelected = true,
                                                                TilemapInstance = newTilemap.GetComponent<TilemapRenderer>(),
                                                                LayerType = layerTypes[i].Name
                                                            };

                                int nextTypeSortIndex = CalculateLayerSortingIndex(i, 50); // 50 = 100 (separation among types) / 2
                                bool hasAddedLayer = false;

                                for (int k = m_tilemapLayers.Count - 1; k >= 0; --k)
                                {
                                    if (m_tilemapLayers[k].SortIndex < nextTypeSortIndex)
                                    {
                                        // There are not layers of this type, add the first one
                                        newLayer.TilemapInstance.sortingOrder = 0;
                                        newLayer.SortIndex = CalculateLayerSortingIndex(i, 0);
                                        newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                        m_tilemapLayers.Insert(k + 1, newLayer);
                                        hasAddedLayer = true;
                                        break;
                                    }
                                    else if (m_tilemapLayers[k].LayerType == layerTypes[i].Name)
                                    {
                                        // There are layers of ths type, add a new one atop of them
                                        newLayer.TilemapInstance.sortingOrder = m_tilemapLayers[k].TilemapInstance.sortingOrder - 1;
                                        newLayer.SortIndex = m_tilemapLayers[k].SortIndex + 1;
                                        newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                        m_tilemapLayers.Insert(k + 1, newLayer);
                                        hasAddedLayer = true;
                                        break;
                                    }
                                }

                                if(!hasAddedLayer)
                                {
                                    // There are not layers of the first type, add the first one
                                    newLayer.TilemapInstance.sortingOrder = 0;
                                    newLayer.SortIndex = CalculateLayerSortingIndex(i, 0);
                                    newLayer.TilemapInstance.name = layerTypes[i].Name + "_" + newLayer.TilemapInstance.sortingOrder;
                                    m_tilemapLayers.Insert(0, newLayer);
                                }

                                SelectTarget(-1, newLayer.TilemapInstance.gameObject);
                            }

                            if (layerIndex < m_tilemapLayers.Count - 1 && m_tilemapLayers[layerIndex + 1].LayerType == layerTypes[i].Name)
                            {
                                for (layerIndex = layerIndex + 1; layerIndex < m_tilemapLayers.Count; ++layerIndex)
                                {
                                    bool previousStatus = m_tilemapLayers[layerIndex].IsSelected;

                                    EditorGUILayout.BeginHorizontal(GUILayout.Width(k_LayersPanelWidth));
                                    {
                                        labelRect = GUILayoutUtility.GetRect(new GUIContent(m_tilemapLayers[layerIndex].TilemapInstance.name), EditorStyles.toolbarButtonLeft, GUILayout.Width(k_LayersPanelWidth - k_TilemapLayerHeaderButtonWidth * 2.0f));

                                        bool isEnabled = m_tilemapLayers[layerIndex].TilemapInstance.gameObject.activeInHierarchy;

                                        // Checks right click, which enables / disables the layer
                                        if (Event.current.isMouse && 
                                            Event.current.type == EventType.MouseDown && 
                                            labelRect.Contains(Event.current.mousePosition) &&
                                            Event.current.button == 1)
                                        {
                                            m_tilemapLayers[layerIndex].TilemapInstance.gameObject.SetActive(!isEnabled);
                                            Repaint();
                                        }

                                        // Layer selection button
                                        GUI.backgroundColor = isEnabled ? m_tilemapLayers[layerIndex].IsSelected ? Color.green 
                                                                                                                 : previousColor
                                                                                                     : Color.red;
                                        if(EditorGUI.Button(labelRect, new GUIContent(m_tilemapLayers[layerIndex].TilemapInstance.name), EditorStyles.toolbarButton))
                                        {
                                            m_tilemapLayers[layerIndex].IsSelected = !m_tilemapLayers[layerIndex].IsSelected;
                                        }

                                        GUI.backgroundColor = previousColor;

                                        // Layer buttons
                                        Rect listItemRect = labelRect;
                                        listItemRect.width += k_TilemapLayerHeaderButtonWidth * 2.0f;

                                        if (m_tilemapLayers[layerIndex].IsSelected)
                                        {
                                            listItemRect.x += labelRect.width;
                                            listItemRect.width -= labelRect.width;

                                            GUI.backgroundColor = m_tilemapLayers[layerIndex].IsSelected ? Color.green : previousColor;
                                            EditorGUI.LabelField(listItemRect, string.Empty, EditorStyles.toolbarButtonLeft);
                                            GUI.backgroundColor = previousColor;

                                            buttonRect = new Rect(labelRect.x + labelRect.width,
                                                                   labelRect.y,
                                                                   k_TilemapLayerHeaderButtonWidth,
                                                                   labelRect.height);

                                            GUI.backgroundColor = m_tilemapLayers[layerIndex].IsSelected ? Color.green : previousColor;

                                            // Move layer Up button
                                            if (EditorGUI.Button(buttonRect, Styles.moveLayerUpButtonText, EditorStyles.toolbarButton))
                                            {
                                                //Undo.RecordObjects(new Object[]{ m_tilemapLayers[i].TilemapInstance.gameObject, m_tilemapLayers[i].TilemapInstance }, "Tilemap layer order change");
                                                EditorUtility.SetDirty(m_tilemapLayers[layerIndex].TilemapInstance.gameObject);
                                                m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder = m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder + 1;
                                                m_tilemapLayers[layerIndex].SortIndex--;
                                                m_tilemapLayers[layerIndex].TilemapInstance.name = m_tilemapLayers[layerIndex].LayerType + "_" + m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder;
                                                m_cachedActiveTargetsHashCode = 0;
                                            }

                                            buttonRect.x = labelRect.x + labelRect.width + k_TilemapLayerHeaderButtonWidth;

                                            // Move layer Down button
                                            if (EditorGUI.Button(buttonRect, Styles.moveLayerDownButtonText, EditorStyles.toolbarButton))
                                            {
                                                //Undo.RecordObjects(new Object[]{ m_tilemapLayers[i].TilemapInstance.gameObject, m_tilemapLayers[i].TilemapInstance }, "Tilemap layer order change");
                                                EditorUtility.SetDirty(m_tilemapLayers[layerIndex].TilemapInstance.gameObject);
                                                m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder = m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder - 1;
                                                m_tilemapLayers[layerIndex].SortIndex++;
                                                m_tilemapLayers[layerIndex].TilemapInstance.name = m_tilemapLayers[layerIndex].LayerType + "_" + m_tilemapLayers[layerIndex].TilemapInstance.sortingOrder;
                                                m_cachedActiveTargetsHashCode = 0;
                                            }

                                            GUI.backgroundColor = previousColor;
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();

                                    // New selection
                                    if (!previousStatus && m_tilemapLayers[layerIndex].IsSelected)
                                    {
                                        SelectTarget(-1, m_tilemapLayers[layerIndex].TilemapInstance.gameObject);
                                    }

                                    // It was the last later of the current type
                                    if(layerIndex + 1 < m_tilemapLayers.Count && m_tilemapLayers[layerIndex + 1].LayerType != layerTypes[i].Name)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        EditorStyles.foldoutHeader.focused = previousState;
                    }
                    else // grid == null
                    {
                        Color previousColor = GUI.color;
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField(Styles.noGridInSceneText, EditorStyles.toolbarTextField);
                        GUI.color = previousColor;
                    }

                    // Makes sure the list has a minimum height
                    int drawnRows = m_tilemapLayers.Count + (grid == null ? 1 
                                                                          : layerTypes.Length);

                    // Select tilemap layers settings button
                    if (GUILayout.Button(Styles.defineLayerTypes))
                    {
                        string[] assetPaths = AssetDatabase.FindAssets("t:" + nameof(TilemapLayersSettings));

                        if (assetPaths.Length == 0)
                        {
                            TilemapLayersSettings settings = ScriptableObject.CreateInstance<TilemapLayersSettings>();
                            AssetDatabase.CreateAsset(settings, "Assets/" + nameof(TilemapLayersSettings) + ".asset");
                            EditorUtility.SetDirty(settings);

                            EditorGUIUtility.PingObject(settings);
                            Selection.activeObject = settings;
                            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                            Debug.Log("Created asset at Assets/" + nameof(TilemapLayersSettings) + ".");
                        }
                        else
                        {
                            TilemapLayersSettings asset = AssetDatabase.LoadAssetAtPath<TilemapLayersSettings>(AssetDatabase.GUIDToAssetPath(assetPaths[0]));
                            Selection.activeObject = asset;
                            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                        }
                    }

                    drawnRows++;

                    if (drawnRows < k_MinimumRowsInTilemapLayerList)
                    {
                        // Fills the space until it has the desited height
                        for (int k = 0; k < k_MinimumRowsInTilemapLayerList - drawnRows; ++k)
                        {
                            EditorGUILayout.LabelField(string.Empty);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            GUI.EndScrollView();
        }

        private void SelectTarget(int i, object o)
        {
            var obj = o as GameObject;

            // Selects the target in the scene
            Selection.activeObject = obj;

            var isPrefabInstance = IsObjectPrefabInstance(obj);
            if (isPrefabInstance)
            {
                var editMode = (TilePaletteProperties.PrefabEditModeSettings)EditorPrefs.GetInt(TilePaletteProperties.targetEditModeEditorPref, 0);
                switch (editMode)
                {
                    case TilePaletteProperties.PrefabEditModeSettings.EnableDialog:
                    {
                        var option = EditorUtility.DisplayDialogComplex(TilePaletteProperties.targetEditModeDialogTitle
                            , TilePaletteProperties.targetEditModeDialogMessage
                            , TilePaletteProperties.targetEditModeDialogYes
                            , TilePaletteProperties.targetEditModeDialogNo
                            , TilePaletteProperties.targetEditModeDialogChange);
                        switch (option)
                        {
                            case 0:
                                GoToPrefabMode(obj);
                                return;
                            case 1:
                                // Do nothing here for "No"
                                break;
                            case 2:
                                var settingsWindow = SettingsWindow.Show(SettingsScope.User);
                                settingsWindow.FilterProviders(TilePaletteProperties.targetEditModeLookup);
                                break;
                        }
                    }
                    break;
                    case TilePaletteProperties.PrefabEditModeSettings.EditInPrefabMode:
                        GoToPrefabMode(obj);
                        return;
                    case TilePaletteProperties.PrefabEditModeSettings.EditInScene:
                    default:
                        break;
                }
            }

            GridPaintingState.scenePaintTarget = obj;
            if (GridPaintingState.scenePaintTarget != null)
                EditorGUIUtility.PingObject(GridPaintingState.scenePaintTarget);
        }
        /*
        private void DoClipboardHeader()
        {
            if (!GridPalettes.palettes.Contains(palette) || palette == null) // Palette not in list means it was deleted
            {
                GridPalettes.CleanCache();
                if (GridPalettes.palettes.Count > 0)
                {
                    palette = GridPalettes.palettes.LastOrDefault();
                }
            }

            EditorGUILayout.BeginHorizontal();
            DoPalettesDropdown();
            using (new EditorGUI.DisabledScope(palette == null))
            {
                clipboardView.unlocked = GUILayout.Toggle(clipboardView.unlocked,
                    clipboardView.isModified ? Styles.editModified : Styles.edit,
                    EditorStyles.toolbarButton);
            }
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(palette == null))
            {
                EditorGUI.BeginChangeCheck();
                m_DrawGizmos = GUILayout.Toggle(m_DrawGizmos, Styles.gizmos, EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_DrawGizmos)
                    {
                        clipboardView.SavePaletteIfNecessary();
                        ResetPreviewInstance();
                    }
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DoPalettesDropdown()
        {
            string name = palette != null ? palette.name : Styles.createNewPalette.text;
            Rect rect = GUILayoutUtility.GetRect(GUIContent.Temp(name), EditorStyles.toolbarDropDown, Styles.dropdownOptions);
            rect.x = k_LayersPanelWidth;
            if (GridPalettes.palettes.Count == 0)
            {
                if (EditorGUI.DropdownButton(rect, GUIContent.Temp(name), FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    OpenAddPalettePopup(rect);
                }
            }
            else
            {
                GUIContent content = GUIContent.Temp(GridPalettes.palettes.Count > 0 && palette != null ? GridPalettesDropdown.AdaptPaletteAssetName(palette.name) : Styles.createNewPalette.text);
                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.toolbarPopup))
                {
                    var menuData = new GridPalettesDropdown.MenuItemProvider();
                    m_PaletteDropdown = new GridPalettesDropdown(menuData, GridPalettes.palettes.IndexOf(palette), null, SelectPalette, k_DropdownWidth);
                    PopupWindow.Show(GUILayoutUtility.topLevel.GetLast(), m_PaletteDropdown);
                }
            }
        }
        */
        private void SelectPalette(int i, object o)
        {
            if (i < GridPalettes.palettes.Count)
            {
                palette = GridPalettes.palettes[i];
            }
            else
            {
                m_PaletteDropdown.editorWindow.Close();
                OpenAddPalettePopup(new Rect(0, 0, 0, 0));
            }
        }

        public static string AdaptPaletteAssetName(string assetName)
        {
            int nameStartIndex = 0;
            int nameLength = assetName.Length;

            if (assetName.StartsWith("P_"))
            {
                nameStartIndex = 2; // "P_".length
            }

            if (assetName.EndsWith("Palette"))
            {
                nameLength = assetName.Length - nameStartIndex - 7; // "Palette".length
            }

            return assetName.Substring(nameStartIndex, nameLength);
        }

        private void DoPalettesSelectionList(float areaWidth)
        {
            const float BUTTON_WIDTH = 30.0f;
            areaWidth -= 10.0f; // Borders
            int columns = Mathf.Clamp(Mathf.FloorToInt(areaWidth / (BUTTON_WIDTH + 5.0f)), 1, GridPalettes.palettes.Count);

            if (GridPalettes.palettes.Count == 0)
            {
                GridPalettes.instance.RefreshPalettesCache();

                Color previousColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField(Styles.noPalettesAvailableText, EditorStyles.toolbarTextField);
                GUI.color = previousColor;
                return;
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(areaWidth));
            {
                m_paletteScrollView = EditorGUILayout.BeginScrollView(m_paletteScrollView, GUILayout.Height((BUTTON_WIDTH + 5.0f) * 2), GUILayout.Width(areaWidth));
                {
                    EditorGUILayout.BeginVertical();
                    {
                        for (int i = 0; i < GridPalettes.palettes.Count; ++i)
                        {
                            if (i % columns == 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                            }

                            Sprite icon = GridPaletteIconsCache.GetIconByPalette(GridPalettes.palettes[i]);
                            string buttonText = icon == null ? GridPalettes.palettes[i].name : string.Empty;

                            if (GUILayout.Button(new GUIContent(buttonText, AdaptPaletteAssetName(GridPalettes.palettes[i].name)), GUILayout.Height(BUTTON_WIDTH), GUILayout.Width(BUTTON_WIDTH)))
                            {
                                palette = GridPalettes.palettes[i];
                                GridPaletteIconsCache.CachePaletteIcons();

                                // Right click selects the palette asset
                                if(Event.current.button == 1)
                                {
                                    Selection.activeObject = palette;
                                    EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                                }
                            }

                            if(palette == GridPalettes.palettes[i])
                            {
                                EditorGUI.DrawOutline(GUILayoutUtility.GetLastRect(), 1, Color.green);
                            }

                            if (icon != null)
                            {
                                Rect buttonRect = GUILayoutUtility.GetLastRect();
                                buttonRect.x += 3.0f;
                                buttonRect.y += 3.0f;
                                buttonRect.width -= 6.0f;
                                buttonRect.height -= 6.0f;

                                Rect textureRect = icon.textureRect;
                                textureRect.x /= icon.texture.width;
                                textureRect.y /= icon.texture.height;
                                textureRect.width /= icon.texture.width;
                                textureRect.height /= icon.texture.height;

                                GUI.DrawTextureWithTexCoords(buttonRect, icon.texture, textureRect);
                            }

                            if (i % columns == columns - 1)
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        /*
                        for (int i = GridPalettes.palettes.Count; i < GridPalettes.palettes.Count + 14; ++i)
                        {
                            if (i % columns == 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                            }

                            if (GUILayout.Button(new GUIContent(""), GUILayout.Height(BUTTON_WIDTH), GUILayout.Width(BUTTON_WIDTH)))
                            {

                            }

                            if (i % columns == columns - 1)
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        */
                        if (GridPalettes.palettes.Count % columns != 0)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void OpenAddPalettePopup(Rect rect)
        {
            bool popupOpened = GridPaletteAddPopup.ShowAtPosition(rect, this);
            if (popupOpened)
                GUIUtility.ExitGUI();
        }

        private void DisplayClipboardText(GUIContent clipboardText, Rect position)
        {
            Color old = GUI.color;
            GUI.color = Color.gray;
            var infoSize = GUI.skin.label.CalcSize(clipboardText);
            Rect rect = new Rect(position.center.x - infoSize.x * .5f, position.center.y - infoSize.y, 500, 100);
            GUI.Label(rect, clipboardText);
            GUI.color = old;
        }

        private void OnClipboardGUI(Rect position)
        {
            if (Event.current.type != EventType.Layout && position.Contains(Event.current.mousePosition) && GridPaintingState.activeGrid != clipboardView && clipboardView.unlocked)
            {
                GridPaintingState.activeGrid = clipboardView;
                SceneView.RepaintAll();
            }

            // Validate palette (case 1017965)
            GUIContent paletteError = null;
            if (palette == null)
            {
                if (GridPalettes.palettes.Count == 0)
                    paletteError = Styles.emptyProjectInfo;
                else
                    paletteError = Styles.invalidPaletteInfo;
            }
            else if (palette.GetComponent<Grid>() == null)
            {
                paletteError = Styles.invalidGridInfo;
            }

            if (paletteError != null)
            {
                DisplayClipboardText(paletteError, position);
                return;
            }

            bool oldEnabled = GUI.enabled;
            GUI.enabled = !clipboardView.showNewEmptyClipboardInfo || DragAndDrop.objectReferences.Length > 0;

            if (Event.current.type == EventType.Repaint)
                clipboardView.guiRect = position;

            if (m_DelayedResetPaletteInstance)
                ResetPreviewInstance();

            EditorGUI.BeginChangeCheck();
            clipboardView.OnGUI();
            if (EditorGUI.EndChangeCheck())
                Repaint();

            GUI.enabled = oldEnabled;

            if (clipboardView.showNewEmptyClipboardInfo)
            {
                DisplayClipboardText(Styles.emptyPaletteInfo, position);
            }

            // If in Edit mode, a colored frame is drawn
            if (clipboardView.unlocked)
            {
                EditorGUI.DrawOutline(position, 2, Color.magenta);
            }
        }

        private void ConvertGridPrefabToPalette(Rect targetPosition)
        {
            if (!targetPosition.Contains(Event.current.mousePosition)
                || (Event.current.type != EventType.DragPerform
                    && Event.current.type != EventType.DragUpdated)
                || DragAndDrop.objectReferences.Length != 1)
                return;

            var draggedObject = DragAndDrop.objectReferences[0];
            if (!PrefabUtility.IsPartOfRegularPrefab(draggedObject))
                return;

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    Event.current.Use();
                    GUI.changed = true;
                }
                break;
                case EventType.DragPerform:
                {
                    var path = AssetDatabase.GetAssetPath(draggedObject);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    bool hasNewPaletteAsset = false;
                    Grid gridPrefab = null;
                    foreach (var asset in assets)
                    {
                        var gridPalette = asset as GridPalette;
                        hasNewPaletteAsset |= gridPalette != null;
                        GameObject go = asset as GameObject;
                        if (go != null)
                        {
                            var grid = go.GetComponent<Grid>();
                            if (grid != null)
                                gridPrefab = grid;
                        }
                    }
                    if (!hasNewPaletteAsset && gridPrefab != null)
                    {
                        var cellLayout = gridPrefab.cellLayout;
                        var cellSizing = (cellLayout == GridLayout.CellLayout.Rectangle
                            || cellLayout == GridLayout.CellLayout.Hexagon)
                            ? GridPalette.CellSizing.Automatic
                            : GridPalette.CellSizing.Manual;
                        var newPalette = GridPaletteUtility.CreateGridPalette(cellSizing);
                        AssetDatabase.AddObjectToAsset(newPalette, path);
                        AssetDatabase.ForceReserializeAssets(new string[] {path});
                        AssetDatabase.SaveAssets();
                        Event.current.Use();
                        GUIUtility.ExitGUI();
                    }
                }
                break;
            }
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            if (GridPaintingState.defaultBrush != null && GridPaintingState.scenePaintTarget != null)
                SceneViewOverlay.Window(Styles.rendererOverlayTitleLabel, DisplayFocusMode, (int)SceneViewOverlay.Ordering.TilemapRenderer, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
            else if (focusMode != TilemapFocusMode.None)
            {
                // case 946284: Disable Focus if focus mode is set but there is nothing to focus on
                DisableFocus();
                focusMode = TilemapFocusMode.None;
            }
        }

        internal void SetFocusMode(TilemapFocusMode tilemapFocusMode)
        {
            if (tilemapFocusMode != focusMode)
            {
                DisableFocus();
                focusMode = tilemapFocusMode;
                EnableFocus();
            }
        }

        private void DisplayFocusMode(Object displayTarget, SceneView sceneView)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            var fieldWidth = EditorGUIUtility.fieldWidth;

            Color previousColor = GUI.color;

            if (m_tilemapLayers.Count > 0)
            {
                int previousFontSize = EditorStyles.toolbarButton.fontSize;
                FontStyle previousFontStyle = EditorStyles.toolbarButton.fontStyle;
                EditorStyles.toolbarButton.fontSize += 4;
                EditorStyles.toolbarButton.fontStyle = FontStyle.Bold;

                GUI.color = Color.green;
                EditorGUILayout.LabelField(GridPaintingState.scenePaintTarget.name, EditorStyles.toolbarButton);

                EditorStyles.toolbarButton.fontSize = previousFontSize;
                EditorStyles.toolbarButton.fontStyle = previousFontStyle;
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField(Styles.noLayersInGrid, EditorStyles.toolbarButton);
            }

            GUI.color = previousColor;

            EditorGUIUtility.labelWidth = EditorGUIUtility.fieldWidth =
                0.5f * (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth);
            var newFocus = (TilemapFocusMode)EditorGUILayout.EnumPopup(Styles.focusLabel, focusMode);
            SetFocusMode(newFocus);
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
        }

        private void FilterSingleSceneObjectInScene(int instanceID)
        {
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.SetSceneViewFiltering(true);

            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();
            if (currentStageHandle.IsValid() && !currentStageHandle.isMainStage)
            {
                HierarchyProperty.FilterSingleSceneObjectInScene(instanceID
                    , false
                    , new UnityEngine.SceneManagement.Scene[] { currentStageHandle.customScene });
            }
            else
            {
                HierarchyProperty.FilterSingleSceneObject(instanceID, false);
            }

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Repaint();
        }

        private void EnableFocus()
        {
            if (GridPaintingState.scenePaintTarget == null)
                return;

            switch (focusMode)
            {
                case TilemapFocusMode.Tilemap:
                {
                    FilterSingleSceneObjectInScene(GridPaintingState.scenePaintTarget.GetInstanceID());
                    break;
                }
                case TilemapFocusMode.Grid:
                {
                    Tilemap tilemap = GridPaintingState.scenePaintTarget.GetComponent<Tilemap>();
                    if (tilemap != null && tilemap.layoutGrid != null)
                    {
                        FilterSingleSceneObjectInScene(tilemap.layoutGrid.gameObject.GetInstanceID());
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        private void DisableFocus()
        {
            if (focusMode == TilemapFocusMode.None)
                return;

            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();
            if (currentStageHandle.IsValid() && !currentStageHandle.isMainStage)
            {
                HierarchyProperty.ClearSceneObjectsFilterInScene(new UnityEngine.SceneManagement.Scene[] { currentStageHandle.customScene });
            }
            else
            {
                HierarchyProperty.ClearSceneObjectsFilter();
            }

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.SetSceneViewFiltering(false);
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        [MenuItem("Window/2D/Tile Palette", false, 2)]
        public static void OpenTilemapPalette()
        {
            GridPaintPaletteWindow w = GetWindow<GridPaintPaletteWindow>();
            w.titleContent = Styles.tilePalette;
        }

        // TODO: Better way of clearing caches than AssetPostprocessor
        public class AssetProcessor : AssetPostprocessor
        {
            public override int GetPostprocessOrder()
            {
                return int.MaxValue;
            }

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {
                if (GridPaintingState.savingPalette)
                    return;

                foreach (var window in instances)
                {
                    window.DelayedResetPreviewInstance();
                }
            }
        }

        public class PaletteAssetModificationProcessor : AssetModificationProcessor
        {
            static void OnWillCreateAsset(string assetName)
            {
                SavePalettesIfRequired(null);
            }

            static string[] OnWillSaveAssets(string[] paths)
            {
                SavePalettesIfRequired(paths);
                return paths;
            }

            static void SavePalettesIfRequired(string[] paths)
            {
                if (GridPaintingState.savingPalette)
                    return;

                foreach (var window in instances)
                {
                    if (window.clipboardView.isModified)
                    {
                        window.clipboardView.CheckRevertIfChanged(paths);
                        window.clipboardView.SavePaletteIfNecessary();
                        window.Repaint();
                    }
                }
            }
        }
    }
}
