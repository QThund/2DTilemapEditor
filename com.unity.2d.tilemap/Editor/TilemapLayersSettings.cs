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

        /// <summary>
        /// Gets the names of all the types of layer.
        /// </summary>
        /// <returns>All the names. It does not allocate memory, the list is cached.</returns>
        public static string[] GetLayerNames()
        {
            if (m_layers == null || m_layers.Length == 0)
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
            string[] foundAssets = AssetDatabase.FindAssets("t:TilemapLayersSettings");

            if (foundAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                TilemapLayersSettings settings = AssetDatabase.LoadAssetAtPath<TilemapLayersSettings>(assetPath);
                m_layers = settings.Layers;
                m_layerNames = new string[settings.Layers.Length];

                for(int i = 0; i < m_layers.Length; ++i)
                {
                    m_layerNames[i] = m_layers[i].Name;
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
                        SerializedProperty property = serializedObject.FindProperty(nameof(settings.Layers));
                        if(EditorGUILayout.PropertyField(property))
                        {
                            serializedObject.ApplyModifiedProperties();
                        }

                        if(GUILayout.Button("Save"))
                        {
                            m_layers = new TilemapLayer[0];
                            Debug.Log("Layers saved!");
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(settings);
            }
        }
    }
}
