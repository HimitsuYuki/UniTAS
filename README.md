# UniTAS

[![Discord](https://img.shields.io/discord/1093033615161573490)](https://discord.gg/ddMqdqgPeB)

![Static Badge](https://img.shields.io/badge/Discord%20DM-%40yuu0141-5865F2?style=flat)

A tool that lets you TAS unity games

- The tool doesn't bypass anti cheat or anything, USE AT YOUR OWN RISK!
- :warning: The tool is early in development and only basic games work
    - This also means TASes made in earlier versions might not work in later versions
- This is a [BepInEx 5] mod

# TASing in UniTAS

Currently, you write a script in lua to control the game rather than recording inputs in game

This is planned to change to a more traditional emulator-like workflow, with frame advancing support later on

To get the hang of it, check the tutorial [here](https://github.com/Eddio0141/UniTAS/wiki/TAS-Movie-Script-Tutorial) and
if stuck on anything, the [wiki](https://github.com/Eddio0141/UniTAS/wiki) should help you out, otherwise you can ask on
discord or GitHub discussions

# What games work

- Currently, anything that [BepInEx 5] supports, ranging from unity 3 to latest, and games that use Mono and not IL2CPP
- Check [compatibility-list](docs/compatibility-list.md) for tested games
- Any games using [rewired input system](https://guavaman.com/projects/rewired/) has limited support as of now, games
  using this may not work correctly

# How to install

- Install [BepInEx 5] to your game
- Download the latest release from [here](https://github.com/Eddio0141/UniTAS/releases/latest), or nightly versions
  from [here](https://github.com/Eddio0141/UniTAS/actions)
- Unzip UniTAS and place it inside the `patchers` folder your game's `BepInEx` folder

# How to use

- Press `F1` to open the GUI, from there you can load a movie and play it
- Check out `BepInEx/Config/UniTAS.cfg` to change most settings

# How to build

## Requirements

- Make sure you have [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download) installed on your system
- Add BepInEx nuget feed with `dotnet nuget add source "https://nuget.bepinex.dev/v3/index.json"`

## Build

- Clone the repo with `git clone`
- Initialize submodules with `git submodule update --init --recursive`
- Run `dotnet build UniTAS` at the base directory
    - Remove the `UniTAS` if you are in the inner `UniTAS` directory
    - If you need to choose `Release` or `Debug` config, do so with the `--configuration` flag
- Output folder would be in `UniTAS/Patcher/bin/{Debug|Release}`
    - The output content can be copied directly inside a `BepInEx` folder to be used

[BepInEx 5]: https://docs.bepinex.dev/articles/user_guide/installation/index.html
