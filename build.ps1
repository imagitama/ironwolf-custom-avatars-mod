dotnet build -c Release

echo "Copying"

mkdir E:\SteamLibrary\steamapps\common\IronWolf\BepInEx\plugins\CustomAvatars -Force

cp .\bin\Release\net472\CustomAvatars.dll E:\SteamLibrary\steamapps\common\IronWolf\BepInEx\plugins\CustomAvatars

cp README.md E:\SteamLibrary\steamapps\common\IronWolf\BepInEx\plugins\CustomAvatars

echo "Done"