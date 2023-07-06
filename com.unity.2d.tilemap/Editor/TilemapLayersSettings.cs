using System;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Stores the definition of the types of layer that form every grid in the project. There may be zero to many layers of each type in a grid.
    /// Normally, there will be one type of layer per Sorting layer. The order of the types depend on which one is above the others. The first layer is over everything.
    /// Every type of layer has a prefab which will be instantiated when a new layer is added.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTilemapLayersSettings", menuName = "2D/Tilemap layers settings")]
    [Serializable]
    public class TilemapLayersSettings : ScriptableObject
    {
        [Serializable]
        public struct TilemapLayer
        {
            public string Name;
            public GameObject LayerPrefab;
        }

        public TilemapLayer[] Layers = new TilemapLayer[0];

        private static TilemapLayer[] m_layers = new TilemapLayer[0];
        private static string[] m_layerNames = new string[0];
        private static string[] m_layerNamesWithNone = new string[0];

        /// <summary>
        /// Gets the names of all the types of layer, including one option called "None" at the beginning.
        /// </summary>
        /// <returns>All the names. It does not allocate memory, the list is cached.</returns>
        public static string[] GetLayerNamesWithNone()
        {
            if (m_layerNamesWithNone == null || m_layerNamesWithNone.Length == 0)
            {
                CacheLayers();
            }

            return m_layerNamesWithNone;
        }

        /// <summary>
        /// Gets the names of all the types of layer.
        /// </summary>
        /// <returns>All the names. It does not allocate memory, the list is cached.</returns>
        public static string[] GetLayerNames()
        {
            if (m_layerNames == null || m_layerNames.Length == 0)
            {
                CacheLayers();
            }

            return m_layerNames;
        }

        /// <summary>
        /// Gets all the information about all the types of layer.
        /// </summary>
        /// <returns>The types of layer. It does not allocate memory, the list is cached.</returns>
        public static TilemapLayer[] GetLayers()
        {
            if(m_layers == null || m_layers.Length == 0)
            {
                CacheLayers();
            }

            return m_layers;
        }

        private static void CacheLayers()
        {
            string[] foundAssets = AssetDatabase.FindAssets("t:" + nameof(TilemapLayersSettings));

            if (foundAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                TilemapLayersSettings settings = AssetDatabase.LoadAssetAtPath<TilemapLayersSettings>(assetPath);
                m_layers = settings.Layers;
                m_layerNames = new string[settings.Layers.Length];
                m_layerNamesWithNone = new string[settings.Layers.Length + 1];
                m_layerNamesWithNone[0] = "None";

                for (int i = 0; i < m_layers.Length; ++i)
                {
                    m_layerNames[i] = m_layers[i].Name;
                }

                for (int i = 1; i < m_layers.Length + 1; ++i)
                {
                    m_layerNamesWithNone[i] = m_layers[i - 1].Name;
                }
            }
        }

        [CustomEditor(typeof(TilemapLayersSettings))]
        private class TilemapLayersSettingsEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                TilemapLayersSettings settings = target as TilemapLayersSettings;

                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.HelpBox("Add the tilemap layer types of your project. Normally, layer types match the Sorting layers you use for your sprites, so some layers are drawn atop others. This layer types will appear as groups in the layer list of the Tile Palette. A different prefab can be instantiated for every layer type when creatng new layers using the buttons of the layer list in the Tile Palette.", MessageType.Info, true);

                        SerializedProperty property = serializedObject.FindProperty(nameof(settings.Layers));
                        EditorGUILayout.PropertyField(property);

                        if(GUILayout.Button("Save"))
                        {
                            m_layers = new TilemapLayer[0];
                            Debug.Log("Layers saved!");
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(settings);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
