# IronWolf VR Custom Avatars Mod

A BepInEx mod for [IronWolf VR](https://store.steampowered.com/app/552080/IronWolf_VR/) that replaces the default head with a custom one that must be exported from Unity.

## Install

1. Download [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3) and extract into your IronWolf game folder like `steamapps\common\IronWolf`

2. Extract this mod into `steamapps\common\IronWolf\BepInEx\plugins`

3. Each of your friend's heads lives in `steamapps\common\IronWolf\Avatars\your_steam_username` with a folder per Steam username (with only letters)

   Note: When the mod launches it will save each player's Steam ID as a simple text file inside each folder which is used to search for the head. Otherwise it looks for their Steam name (with only letters so like "Cool_Joe_123" becomes "CoolJoe").

4. Launch IronWolf normally

## Tips

Inside each folder is `offset.txt` which lets you tweak how the head looks.

## Making a head

1. In Blender create the head mesh and place it in the scene origin (0,0,0)

2. Recommended width of 0.2m

3. Rotate it facing "upwards" and apply all transforms

4. Export as FBX with Unit Scale

5. In Unity import the FBX and ensure its scale is 1,1,1 and position and rotation is 0,0,0. Add materials as you like, then create a new `.prefab` file

6. Create an [AssetBundle](https://docs.unity3d.com/6000.1/Documentation/Manual/AssetBundlesIntro.html) called `head` and add your prefab and all of its dependencies (like materials and textures)

7. Drop into the `Avatars` folder in your game eg. `steamapps\common\IronWolf\Avatars\my_steam_username`

## Development

1. Update `.csproj` with correct DLL paths

2. Update `build.ps1` with correct game paths

3. Run `build.ps1`

4. Launch IronWolf normally

Tip: Change some settings in `steamapps\common\IronWolf\BepInEx\config\com.imagitama.customavatars.cfg`
