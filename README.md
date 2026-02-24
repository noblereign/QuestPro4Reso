# QuestPro4Reso

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that brings the Quest Pro's [eye tracking](https://developer.oculus.com/documentation/unity/move-eye-tracking/) and [natural expressions](https://developer.oculus.com/documentation/unity/move-face-tracking/) to [Resonite](https://resonite.com/) avatars.

Related issues on the Resonite GitHub:
1. https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/4

## Usage
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Download the [latest release](https://github.com/noblereign/QuestPro4Reso/releases/latest) of this mod and place it in your Resonite install folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\` for a default install. Extract the archive, ensuring that `OSCCore` is present and `QuestProModule` is present in `rml_mods`. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install, or you can create this folder if it is missing.
1. Install [ALVR](https://github.com/alvr-org/ALVR-nightly/releases) and run it.
1. Start the game.

If you want to verify that the mod is working you can check your Resonite logs, or create an EmptyObject with an AvatarRawEyeData/AvatarRawMouthData Component (Found under Users -> Common Avatar System -> Face -> AvatarRawEyeData/AvatarRawMouthData).

A big thanks to [dfgHiatus](https://github.com/dfgHiatus) for creating the original Quest Pro mod. Check it out [here](https://github.com/dfgHiatus/QuestPro4Neos).

Also, thanks to Pear for testing this mod, not owning the headset myself this would not have been possible without them.
