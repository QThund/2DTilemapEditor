# Tile Palette editor window redesign
This package uses the *com.unity.2d.tilemap* package (v1.0.0) of Unity 2020.3.4 as base.

(You can read this article with another format at https://www.jailbrokengame.com/2023/07/06/tile-palette-editor-window-redesign/)

# Introduction

I am going right to the point. I have “forked” the code of the com.unity.2d.tilemap package (v1.0.0) because, in my opinion and experience, drawing tilemap-based scenarios in Unity with the current design feels clunky, slow, even annoying sometimes. I thought that I could try to improve that UI and transform it into something more to my liking and I spent a week diving into the code and changing it, adding new features without breaking the existing ones.

This article describes everything you need to know to integrate and use this package in your new projects or in a project that already contains tilemaps and tile palettes. There is a demo project in the repository which uses the package and provides some sample assets for you to play with them.

Try it! Give me your opinion, there is plenty room for improvement, this is a first step. You can fork it too and adapt it to your needs.Read the "Installation notes" section below.

Github repository of the forked package: https://github.com/QThund/2DTilemapEditor

# Main visual design changes
  
  - The Active Tilemap / Target dropdown menu has been replaced with a vertical column that shows all the existing tilemaps of the grid.
  - The Palette dropdown menu has been replaced with a toolbar that displays all existing palettes using icons.
  - Buttons related to the tile grid have been moved atop the grid.

# More layers!

In Unity the concept of layer is used by several sub-systems. There are Physic / Culling layers, Sorting layers, Render layer masks… but I think there is one more place where this concept makes complete sense: Tilemaps.

I know most people already use tilemaps (with different Sorting layers) as layers of scenarios, it is there de facto, but Unity is too generic in the way it treats tilemaps in a Grid. The UI does not care about that, they are just game objects in a list.

The new design changes this way of thinking. Every tilemap is a layer and every layer is of one type. There may exist many layers of the same type. A type is determined by a Sorting layer (the property of TilemapRenderer) and it indicates whether the layers of that type are rendered above or below all the layers of another type. Tilemap-layers are grouped by type and have different numbers for their Order in layer (another property of TilemapRenderer) which indicates the order in which the layers are rendered inside a group (the higher, the nearer to the camera). For example:

  Overlay (a type, Sorting layer: Overlay)
    Overlay_0 (a tilemap-layer, Order in layer: 0)
    Overlay_-1 (a tilemap-layer, Order in layer: -1)
  Foreground (a type, Sorting layer: Foreground)
    Foreground_2 (a tilemap-layer, Order in layer: 2)
    Foreground_0 (a tilemap-layer, Order in layer: 0)
  Background (a type, Sorting layer: Background)
    Background_0 (a tilemap-layer, Order in layer: 0)
    Background_-99 (a tilemap-layer, Order in layer: -99)

The list of layers above is sorted by their distance to the camera, from the nearest to the furthest or, put in another way, from the last drawn layer to the first.

My intention with the new design is that a game designer almost forgets about tilemaps, grids, sorting layers or whatever that does not have to do with just drawing the scenario.

# Layer types configuration

You are in charge of defining the layer types your project needs. Typically they will match 1:1 the Sorting layers you already defined for your sprites, at least those that are used by elements of the scenario.

Layer types are stored in an editor asset called TilemapLayersSettings. If it does not exist yet, there is a button in the Tile Palette window that generates it. Every row in the list has a name for the type (it does not necessarily have to match the name of the Sorting layer) and a reference to a prefab. The prefab must contain a TilemapRenderer and is the one that will be instantiated when creating a new layer from the layer list for that layer type (read following section). As you add new layer types, new header rows will appear in the layer list of the Tile Palette window.

# Tilemap-layer list

It occupies the left side of the Tile Palette window (hence the tile grid has been stretched) and shows all the existing layers in a Grid object of the scene so it is easy for a designer to pick the layer to paint on. Tilemap-layers are grouped by type and both types and layers are sorted by their distance to the camera as seen in the previous example: the nearest at the top, the furthest at the bottom of the column.

## Selecting layers

Just click on the name of a layer in the list and the corresponding tilemap will be also selected. You can see how the name of the selected layer appears in the “Tilemap” box in the scene view too, so if you hide the Tile Palette window you still know which layer you are painting on.

Consistency between scene hierarchy and layer list

The layer list and the tilemaps under the grid of the scene must be consistent all the time (putting aside drawing delays).

  - Every time a tilemap is manually added to the grid (if it fulfills the naming convention), it appears in the list.
  - When a tilemap is manually removed, it disappears from the list.
  - If a tilemap is selected in the scene hierarchy, it is selected in the layer list.
  - Changing the Order in layer property in the tilemap, it moves that layer up or down in the list.
  - When a layer is added to the list, a new tilemap is created in the scene (always as the last child of the grid).
  - Changing its order in the list modifies the Order in layer property of the tilemap (and the name of the instance).
  - Selecting a layer in the list also selects the tilemap instance in the scene.

Anyway, I do not recommend adding or modifying tilemaps manually, not only to avoid undesired behaviors but also to adapt your mind to a new way of thinking.

## Creating new layers

There are 2 buttons next to the name of every type of layer. One of them instantiates a new layer on top of the layers in the group, whereas the other does it at the bottom.

The name of the new tilemap-layer instances is formed by the type of the layer and the order of the layer in its group, separated with an underscore, for example: “Foreground_1”.

The prefab to instantiate when creating a layer depends on the configuration (see TilemapLayersSettings). Take into account that new instances will still be linked to their prefabs (so you can change the prefab later affecting all your tilemaps).

Instances will be added always as the last child of the grid, no matter what type or order number it has, so in case the grid is already a prefab with layers this operation will still work (you know, adding objects in the middle of the hierarchy of a prefab would be problematic).

New layers are automatically selected as they are created.

## Reordering layers

There are 2 buttons next to the name of a selected layer. One of them moves the layer upwards and the other does the opposite. This operation changes the Order in layer of the tilemap and the number in its name. A layer does not “jump” to the next group in the list, no matter how big or small the number is.

Changing the position of a layer in the list does not affect the position of the instance in the scene hierarchy.

## Removing layers

Currently, there are no buttons to remove a layer in the list. Although this was more a matter of lack of time (confirmation prompt, undo mechanism, etc.) than a design decision, the thing is that it avoids misclicks and it is not a frequent operation. So, in this case, you must remove the tilemap instance directly from the scene hierarchy. The layer will disappear from the list.

Tile Palette selection toolbar

It is placed atop the tile grid and displays all the existing tile palettes (prefabs) in the project. There is one button per palette which contains an icon that corresponds to a tile of the palette so it is very easy to identify and occupies less space in the window. The tooltip text of the button shows the name of the palette. Clicking a button changes the content of the tile grid. The button that corresponds to the current palette is highlighted.

Optionally, if you use the name convention consisting in a “P_” prefix (for prefab) and a “Palette” suffix, both parts will be removed from the tooltip. For example, “P_TemplePalette” becomes just “Temple”.

## Setting the icon of the palette

If you select a valid tile of a palette, a new button (“Set palette icon”) will appear over the tile grid. When you click on it the icon of the palette is replaced with the sprite corresponding to the selected tile.

Palette icons are stored in an editor asset called GridPaletteIconsCache. If it does not exist yet, it will be automatically created (and shown in the project browser window).

# Automatic layer selection by palette tile

This is one of the features that were harder to implement and that I am more proud of. Imagine you have a tile in your palette that will always be used for drawing walls and, of course, you have another tile that is used for the background only. Wouldn't it be awesome that every time you select the wall-tile, the layer that is intended for drawing walls was automatically selected? And then, when you chose the background-tile, the layer that is intended for drawing the background was automatically selected too? Well that is what this feature does.

It is a kind-of contextual layer selection that saves you the time of selecting the proper layer. For every tile, you can set which layer should be selected, and that layer is defined by a type and its order number, for example [“Foreground”, 1] which would select the layer “Foreground_1”, if it exists in the list.

If the layer that should be selected is not there, the current selection will not change and a red button will appear over the tile grid, which allows you to create that layer. The new layer will be selected automatically.

This mechanism is optional, in two senses: You can completely disable it by unchecking a toggle that appears over the tile grid (“Tile’s layer selection”), and it will only affect tiles created using the custom tile class (see following section).

## Setting up tiles

The layer to select automatically is stored in the asset file of every tile.

By default, Unity uses a ScriptableObject of type Tile (derived from TileBase). You can see this if you open the Edit→Preferences menu, and click on the “Tile Palette” section. There you will find the “Create Tile Method” field, a dropdown where there is normally only one item called “DefaultTile”. The selected method will be used when dragging sprite sheets onto the tile grid of the Tile Palette window. It is possible to add new methods using the [CreateTileFromPalette] attribute.

I created a class called “CustomDefaultTile” derived from “Tile”, which adds 2 new fields: The layer type and the sorting order in layer. If you change the “Create Tile Method” field explained above and use the “CustomDefaultTile” option, the new class will be used when creating new tiles. The layer types that appear in the dropdown of the asset depend on what you have defined in the TilemapLayersSettings asset previously.

## Upgrading existing tile assets

If you are adding this package to an ongoing project and want to use this feature, just select all the tile assets you want to convert, enable the Debug mode of the Inspector and replace the “Script” field content with the CustomDefaultTile script.

Beware this cannot be easily undone! I suggest that you do not have any pending change to commit in your working copy so you can discard everything in case you do not like it.

# Tile grid and its buttons

## Tile selection (behavior changed)

One of the things that motivated the modification of this package the most was the behavior of the tile selection tool. When I added new tiles to the palette I had to enable the “edition mode” and move them to other coordinates of the grid according to any personal criteria. The problem was that, every time I selected a tile, the Inspector of the asset of that tile was focused. This means that I had to go back to the tab of the Tile Palette and select the Move tool, move the tile wherever and then select another tile which focused the Inspector again…

Now when a tile is selected, it does nothing. If you want to select the asset that corresponds to the tile, there is a button (“Select tile asset”) over the grid that does exactly that.

## Edit mode

The “Edit” toggle has been moved to the grid area, which is highlighted with a colored frame when palette edition is enabled.

# Installation notes

Download a copy of the repository from Github (https://github.com/QThund/2DTilemapEditor) wherever you want (I recommend you to put it at the same level the folder of your project is). It has 2 subfolders: a demo project and the package.

Replacing the official package with my modded version will not affect any of your assets. To perform the replacement just go to the manifest.json file in the Packages folder of your project and look for the line:

"com.unity.2d.tilemap": "1.0.0”,

Change it by writing the relative path to the modded package folder in your working copy, like this:

"com.unity.2d.tilemap": "file:../../com.unity.2d.tilemap",

You should see that the Tile Palette window is now different (it may close by itself, open it again by clicking on the top menu Window→2D→Tile Palette). I strongly recommend you to read the entire article in order to understand it properly and for you to be able to upgrade your project and make all features work.

## Compatibility

The package has been tested in versions 2020.3.4 and 2021.3.12. If it does not work for the version of Unity you are using, tell me and I may take a look to adapt the code.

# Known issues

Since the code of the package corresponds to the version available for Unity 2020.3.4, and although it works perfectly in v2021, some features that were developed in newer versions of the package (like the “Grid” button) will not be available anymore. The changes applied to the old package should be ported to the new version of the package for everything to be back again, and I am not going to spend such time unless many people asked for it.

It is not possible to undo operations performed by the buttons of the layer list (layer creation, order modification). I tried but it did not work properly.

# Future ideas

  - Separated panels: The layer list area could be a window separated from the rest.
  - More than one tile grid: Having several palettes selected and visible at the same time.
  - Moving the TilemapLayersSettings contents to the Preferences window.
