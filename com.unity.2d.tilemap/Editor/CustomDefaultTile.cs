using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Tilemaps;
#endif

namespace UnityEngine.Tilemaps
{
    /// <summary>
    /// A tile that stores extra settings, some related to the tilemap layer it is intended to be used on.
    /// </summary>
    [CanEditMultipleObjects]
    [Serializable]
    public class CustomDefaultTile : Tile
    {
        [Tooltip("The type of layer on which the tile is most probably going to be used. None by default.")]
        [SerializeField]
        protected int m_TilemapLayer = -1;

        [Tooltip("The sorting order of the tilemap layer with respect to other layers of the same type. The highest index will be drawn atop the rest.")]
        [SerializeField]
        protected int m_SortingOrderInLayer;

        /// <summary>
        /// Gets or sets the index of the type of layer on which the tile is most probably going to be used. The index corresponds to the TilemapLayersSettings asset.
        /// </summary>
        public int TilemapLayerIndex
        {
            get
            {
                return m_TilemapLayer;
            }

            set
            {
                m_TilemapLayer = value;
            }
        }

        /// <summary>
        /// Gets or sets the sorting order of the tilemap layer with respect to other layers of the same type. The highest index will be drawn atop the rest.
        /// </summary>
        public int SortingOrderInLayer
        {
            get
            {
                return m_SortingOrderInLayer;
            }

            set
            {
                m_SortingOrderInLayer = value;
            }
        }

        [CreateTileFromPalette]
        public static TileBase CreateCustomDefaultTile(Sprite sprite)
        {
            CustomDefaultTile customTile = ScriptableObject.CreateInstance<CustomDefaultTile>();
            customTile.sprite = sprite;
            customTile.name = sprite.name;
            return customTile;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CustomDefaultTile)), CanEditMultipleObjects]
    public class CustomDefaultTileEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent ColliderType = EditorGUIUtility.TrTextContent("Collider type");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
            public static readonly GUIContent Sprite = EditorGUIUtility.TrTextContent("Sprite");
            public static readonly GUIContent TilemapLayer = EditorGUIUtility.TrTextContent("Tilemap layer");
            public static readonly GUIContent SortingOrderInLayer = EditorGUIUtility.TrTextContent("Sorting order in layer");
            public static readonly string TilemapLayerHelbox = "The tilemap layer settings are used in editor to determine which layer to select automatically (if the feature is enabled) when the tile is chosen in the Tile Palette.";
            public static readonly GUIContent TilemapLayerTitle = new GUIContent("Tilemap layer settings");
        }

        public override void OnInspectorGUI()
        {
            CustomDefaultTile tile = target as CustomDefaultTile;

            bool spriteChanged = false;
            bool colorChanged = false;
            bool colliderTypeChanged = false;
            bool tilemapLayerIndexChanged = false;
            bool sortingOrderInLayerChanged = false;
            
            EditorGUILayout.BeginVertical();
            {
                EditorGUI.BeginChangeCheck();
                {
                    tile.sprite = EditorGUILayout.ObjectField(Styles.Sprite, tile.sprite, typeof(Sprite), true) as Sprite;
                }
                spriteChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                {
                    tile.color = EditorGUILayout.ColorField(Styles.Color, tile.color);
                }
                colorChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                {
                    tile.colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup(Styles.ColliderType, tile.colliderType);
                }
                colliderTypeChanged = EditorGUI.EndChangeCheck();

                EditorGUILayout.Space(20.0f);
                EditorGUILayout.LabelField(Styles.TilemapLayerTitle, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(Styles.TilemapLayerHelbox, MessageType.Info, true);

                EditorGUI.BeginChangeCheck();
                {
                    tile.TilemapLayerIndex = EditorGUILayout.Popup(Styles.TilemapLayer, tile.TilemapLayerIndex + 1, TilemapLayersSettings.GetLayerNamesWithNone());
                    tile.TilemapLayerIndex -= 1; // None option
                }
                tilemapLayerIndexChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                {
                    tile.SortingOrderInLayer = EditorGUILayout.IntField(Styles.SortingOrderInLayer, tile.SortingOrderInLayer);
                }
                sortingOrderInLayerChanged = EditorGUI.EndChangeCheck();
            }
            EditorGUILayout.EndVertical();

            for (int i = 1; i < targets.Length; ++i)
            {
                CustomDefaultTile currentTile = targets[i] as CustomDefaultTile;
                
                if(spriteChanged)
                {
                    currentTile.sprite = tile.sprite;
                }

                if (colorChanged)
                {
                    currentTile.color = tile.color;
                }

                if (colliderTypeChanged)
                {
                    currentTile.colliderType = tile.colliderType;
                }

                if (tilemapLayerIndexChanged)
                {
                    currentTile.TilemapLayerIndex = tile.TilemapLayerIndex;
                }

                if (sortingOrderInLayerChanged)
                {
                    currentTile.SortingOrderInLayer = tile.SortingOrderInLayer;
                }
            }

            if(spriteChanged || colorChanged || colliderTypeChanged || tilemapLayerIndexChanged || sortingOrderInLayerChanged)
            {
                for(int i = 0; i < targets.Length; ++i)
                {
                    EditorUtility.SetDirty(targets[i]);
                }
            }
        }
    }
#endif
}
