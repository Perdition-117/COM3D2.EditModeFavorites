# COM3D2.EditModeFavorites
Allows setting favorite items in edit mode.

![favorites](https://user-images.githubusercontent.com/87424475/209483060-1b1d3a31-72e8-432a-bd02-671064e09d37.png)

## Usage

Control-click an item in edit mode to toggle its favorite state. The modifier key is configurable.

Favorites will be marked with a star and placed before other items in the grid. (the latter behavior can be disabled)

If item grouping is enabled and a sub item is favorited the star will be orange instead of yellow in the main grid.

## Configuration

Settings are found in `BepInEx\config\net.perdition.com3d2.editmodefavorites.cfg` and may be modified directly or by using a BepInEx configuration manager.

## Installation

Download the latest version from [the release page](../../releases/latest). Extract the archive contents into `BepInEx\plugins`.
