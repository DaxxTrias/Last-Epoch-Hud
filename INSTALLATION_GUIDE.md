# Last Epoch HUD Mod - Installation Guide

## Prerequisites
- [x] Last Epoch installed 
- [x] MelonLoader installed
- [x] Project paths updated

## Installation Steps

### Step 1: Generate Il2Cpp Assemblies
**You MUST do this first!**

1. Launch Last Epoch through Steam
2. Let the game load completely (you'll see MelonLoader messages in console)
3. Exit the game
4. This creates the `Il2CppAssemblies` folder needed for compilation

### Step 2: Build the Mod
After running the game once:

```bash
# In VS Code, run the build task (Ctrl+Shift+P -> "Tasks: Run Task" -> "build")
# Or use command line (replace <project-path> with your actual path):
dotnet build "<project-path>\Mod\Mod.csproj"
```

### Step 3: Install the Mod
1. Copy the built `Mod.dll` from:
   `Mod\bin\Debug\net6.0\win-x64\Mod.dll`
   
2. To the Last Epoch Mods folder:
   `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\`
   
3. Create the `Mods` folder if it doesn't exist

### Step 4: Run the Game
1. Launch Last Epoch
2. Press **Insert** key in-game to open the mod menu

## ⚠️ IMPORTANT WARNINGS
- This mod violates Last Epoch's Terms of Service
- You may get banned for using it
- Use at your own risk!

## Troubleshooting
- If build fails: Make sure you ran the game once first
- If mod doesn't load: Check MelonLoader console for errors
- If paths are wrong: Update the .csproj file paths manually

## 🎮 Mod Features

### Core Features
- **ESP** for items, enemies, gold
- **Auto health potions** (configurable threshold)
- **Map hack** (full minimap reveal)
- **Camera/minimap zoom unlock**
- **Fog removal**
- **Player lantern**
- **Use any waypoint** (risky option)

### 🎯 **NEW: Advanced Minimap Enemy Circles**
**Enhanced enemy tracking system with full customization!**

#### Enemy Color Coding:
- 🔴 **Red circles** = Boss enemies (always shown)
- 🟡 **Yellow circles** = Rare enemies  
- 🔵 **Blue circles** = Magic enemies
- ⚪ **White circles** = Normal/White enemies

#### Smart Features:
- **Performance Optimized**: Up to **100 enemies** displayed simultaneously (5x increase!)
- **Tab-Only Visibility**: Circles only appear when minimap is open (Tab key)
- **Dead Enemy Filtering**: No circles for dead enemies
- **Distance-Based Display**: Only shows nearby enemies within draw distance

#### Full Customization Controls:
- ✅ **"Show Magic Monsters (Blue)"** - Toggle blue circles on/off
- ✅ **"Show Rare Monsters (Yellow)"** - Toggle yellow circles on/off  
- ✅ **"Show White Monsters (White)"** - Toggle normal enemy circles on/off
- 🎛️ **Map Scale Slider**: Adjustable from 0.5x to 15.0x (default: 8.3x)
- 🎛️ **Circle Size**: Adjustable circle diameter
- 🎛️ **Draw Distance**: Control how far enemies are detected
- 🎛️ **Position Adjustment**: Fine-tune X/Y offset for perfect alignment
- 🎛️ **Minimap Radius**: Adjust detection area bounds

#### Performance Features:
- **Smart Filtering**: Only creates circles for enabled monster types
- **Optimized Updates**: 30-frame throttling prevents lag
- **Memory Management**: Automatic cleanup of old circles
- **Bounds Checking**: Prevents off-screen circle creation

### 🎮 Usage Instructions:
1. **Open mod menu** with Insert key
2. **Navigate to minimap settings** section
3. **Configure enemy types** using the checkboxes
4. **Adjust positioning** with X/Y offset sliders
5. **Fine-tune scale** for perfect minimap alignment
6. **Press Tab in-game** to see enemy circles on minimap

### ⚙️ Advanced Settings:
All settings are **automatically saved** and **remembered between sessions**:
- Monster type visibility preferences
- Scale and positioning adjustments
- Circle size and draw distance
- All customizations persist across game restarts

## 📋 Recent Updates:
- ✅ **Increased enemy limit** from 20 to 100 enemies
- ✅ **Added monster type filtering** with individual toggles
- ✅ **Improved performance** with smart filtering system
- ✅ **Enhanced UI** with organized settings sections
- ✅ **Fixed visibility control** - circles only show when Tab is pressed
- ✅ **Resolved debug spam** that caused game lag
- ✅ **Added dead enemy filtering** for cleaner display
- ✅ **Expanded map scale range** up to 15.0x
- ✅ **Complete code cleanup** with optimized debug system

Press **Insert** in-game to access all settings and customize your experience!

---


