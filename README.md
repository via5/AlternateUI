## Introduction
AlternateUI mainly replaces the male and female morphs UI, but also has several tweaks that can be enabled.

## Usage
Add `AlternateUI.cslist` from `via5.AlternateUI.x` as a session plugin. To load the plugin every time VaM starts, save it as the default session plugin preset. The plugin UI contains toggles for every feature.

<a href="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/session-plugins.png"><img src="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/session-plugins.png" width="300"></a>
<a href="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/session-plugin-presets.png"><img src="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/session-plugin-presets.png" width="300"></a>

## Features

### Morphs UI
<a href="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/morphs-ui.png"><img src="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/morphs-ui.png" width="400" align="right"></a>

This alternate new Morphs UI is much faster than the default one. It has a slider for pages, fast search that supports regular expressions and a tree for the categories. Some features are missing from the default UI. If you need them, disable the AlternateUI plugin to revert to the default UI.

- All sliders supports the mouse wheel.
- The search box supports regexes if the text begins and ends with a forward slash, such as `/cheek .* size/`.
- Right-click the categories button to revert to `All`.
- There are three filter checkboxes: Favorites, Latest and Active.
- There's a tooltip with a lot of information when hovering the morph name with the mouse.
- Buttons:
    - `R range`: resets the range to the default, clamping the value if necessary.
    - `Def`: resets the value to the default.
    - `R`: resets both the range and value to the default.
    - `+Range`: doubles the range.
    - `F`: set as favorite.

<a href="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/morphs-ui-categories.png"><img src="https://raw.githubusercontent.com/via5/AlternateUI/master/doc/morphs-ui-categories.png" width="300" align="right"></a>

The` Categories` button will open a tree where the category can be selected. All morphs belonging to this category or any of its children will be displayed. The first two items will always be `Morph` and `Pose`. Other top-level categories will be displayed below these two.

<br><br><br><br><br><br><br><br><br><br>

## Tweaks
### Middle-click remove
Middle-click atoms in the selection list to remove them.

### Right-click skin texture reload
In the `Skin Textures` tab, right-click the `Select` button to reload this texture. Useful when editing textures.

### Monospace log
Use the `Consolas` font for the error log.

### Edit mode on load
Switch to Edit mode every time a scene is loaded.

### Focus head on load
When a scene is loaded, center the camera to the head of the first atom in the scene. Works best with `Disable load position` enabled.

### Disable load position
Some scenes will move the camera to a specific location when they load. This disables the load position for all scenes and keeps the camera where it was.

### Move new lights
The `InvisibleLight`'s initial position is a bit too close to the center for characters that are in the default position. This will move new `InvisibleLight` atoms slightly forwards for better illumination.

## Licence
AlternateUI is released under [Creative Commons Zero](https://creativecommons.org/share-your-work/public-domain/cc0). This project is in the public domain.
