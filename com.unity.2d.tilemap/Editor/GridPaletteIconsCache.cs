using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// An editor asset that stores the icons that correspond to each tileset palette of the project. Its contents are generated by the Tile Palette.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGridPaletteIconsCache", menuName = "2D/Grid palette icons cache")]
    [Serializable]
    public class GridPaletteIconsCache : ScriptableObject
    {
        [Serializable]
        protected struct GridPaletteIcon
        {
            public GameObject Palette;
            public Sprite Icon;
        }

        [SerializeField]
        protected List<GridPaletteIcon> m_Icons = new List<GridPaletteIcon>();

        private static List<GridPaletteIcon> m_cachedIcons = new List<GridPaletteIcon>();

        /// <summary>
        /// Retrieves the icon that corresponds to a given palette asset.
        /// </summary>
        /// <param name="palette">The palette asset.</param>
        /// <returns>A sprite that determines the texture and the coordinates of the icon in that texture.</returns>
        public static Sprite GetIconByPalette(GameObject palette)
        {
            Sprite foundSprite = null;

            for(int i = 0; i < m_cachedIcons.Count; ++i)
            {
                if(m_cachedIcons[i].Palette == palette)
                {
                    foundSprite = m_cachedIcons[i].Icon;
                    break;
                }
            }

            return foundSprite;
        }

        /// <summary>
        /// Stores an icon and associates it to a given palette asset. If the palette already has an icon, it will be replaced.
        /// </summary>
        /// <param name="icon">The icon sprite.</param>
        /// <param name="palette">The palette asset.</param>
        public static void SetIconForPalette(Sprite icon, GameObject palette)
        {
            string[] foundAssets = AssetDatabase.FindAssets("t:" + nameof(GridPaletteIconsCache));

            if (foundAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                GridPaletteIconsCache iconsCache = AssetDatabase.LoadAssetAtPath<GridPaletteIconsCache>(assetPath);
                bool iconReplaced = false;

                for (int i = 0; i < iconsCache.m_Icons.Count; ++i)
                {
                    if (iconsCache.m_Icons[i].Palette == palette)
                    {
                        iconsCache.m_Icons[i] = new GridPaletteIcon(){ Palette = palette, Icon = icon };
                        iconReplaced = true;
                        break;
                    }
                }

                if(!iconReplaced)
                {
                    iconsCache.m_Icons.Add(new GridPaletteIcon() { Palette = palette, Icon = icon });
                }

                EditorUtility.SetDirty(iconsCache);
                m_cachedIcons = iconsCache.m_Icons;
            }
            else
            {
                GridPaletteIconsCache iconsCache = ScriptableObject.CreateInstance<GridPaletteIconsCache>();
                iconsCache.m_Icons.Add(new GridPaletteIcon() { Palette = palette, Icon = icon });
                AssetDatabase.CreateAsset(iconsCache, "Assets/" + nameof(GridPaletteIconsCache) + ".asset");
                EditorUtility.SetDirty(iconsCache);

                EditorGUIUtility.PingObject(iconsCache);
                Debug.Log("Created asset at Assets/" + nameof(GridPaletteIconsCache) + ".");
            }

            CachePaletteIcons();
        }

        public static void CachePaletteIcons()
        {
            string[] foundAssets = AssetDatabase.FindAssets("t:GridPaletteIconsCache");

            if (foundAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                GridPaletteIconsCache icons = AssetDatabase.LoadAssetAtPath<GridPaletteIconsCache>(assetPath);
                m_cachedIcons = icons.m_Icons;
            }
        }

        private void OnEnable()
        {
            CachePaletteIcons();
        }
    }
}
