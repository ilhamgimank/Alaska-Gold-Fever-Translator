# ⛏️ Alaska Gold Fever Translator Mod

![Mod Version](https://img.shields.io/badge/Version-0.1.0-brightgreen?style=for-the-badge)
![BepInEx](https://img.shields.io/badge/Requires-BepInEx_5.4.23.5-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?style=for-the-badge)

**Alaska Gold Fever Translator** is an advanced, automated, and comprehensive localization tool built using the BepInEx framework and HarmonyLib for *Alaska Gold Fever*.

This mod goes far beyond simple text replacement. It features a smart string dumper, an asynchronous Auto-Translation Engine, an advanced UI Path Scanner, and sets the foundation for future texture replacement and developer tools!

---

## ✨ Key Features

Designed with ultimate flexibility, this mod automates the tedious parts of game translation and gives you full control over the game's UI and assets.

### 1. 📖 Universal Text Engine Support
* Real-time text translation using clean JSON files.
* Supports all Unity text engines: **UGUI Text**, **TextMeshPro (TMP)**, **Legacy TextMesh 3D**, and **IMGUI (GUIContent)**.
* **Smart Auto-Dumper:** Automatically captures untranslated strings into memory and saves them safely to JSON files asynchronously without causing game stutters.
* **Robust Spam Filter:** Internal Unity engine strings (HDRP/URP parameters) are automatically blocked and ignored.

### 2. 🤖 Auto-Translation Engine (Google API)
* Built-in asynchronous background queue system that automatically fetches translations without freezing the game.
* Uses Google Translate API with built-in delays to prevent IP bans.
* **Auto Deduplication:** Cleans and merges case-sensitive duplicate entries in your translation files automatically upon loading.

### 3. 🎯 Advanced UI Path Scanner
* **Absolute UI Scanner (Ctrl + Right Click / Alt + Right Click):** Hover your mouse over any text or image in the game and scan it. The mod will print its exact hierarchical "Path", "Sprite Name", and "Text Content" directly to the BepInEx console.
* **Smart Blacklist:** The scanner automatically ignores giant invisible raycast-blocking layers (like `UIMask` or `BlockSprite`) to accurately detect the actual button/icon you are pointing at!

### 4. 🗂️ Automated Workspace Generation
* On the first launch, the mod automatically creates a highly organized workspace: `[Default Textures]`, `[Custom Fonts]`, `Dumps`, and `Localization`.

### 5. 🛠️ Upcoming Features (Phase 3 & 4)
* **Dynamic Image & Texture Translation:** Automatic dumping and replacing of UI textures and sprites.
* **In-Game Developer Toolkit (F11 Menu):** A sleek GUI panel for live hot-reloading (JSON & PNGs), toggling translation engines, and switching languages on the fly.
* **Smart Regex Handling:** For dynamic texts containing numbers and variables.

---

## 📥 Installation Guide

To use this mod, you need the base game and **BepInEx 5** (the standard Unity mod loader).

### Step 1: Installing BepInEx
1. Download BepInEx version 5 (x64) from the [BepInEx GitHub Releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract the downloaded `.zip` file.
3. Move the extracted contents (`BepInEx` folder, `doorstop_config.ini`, and `winhttp.dll`) into your game's root directory (the same folder as the Alaska Gold Fever `.exe`).
4. **Run the game once.** Wait until you reach the Main Menu, then close the game to generate the config folders.

### Step 2: Installing the Translator Mod
1. Download the latest `.zip` release file from the **[Releases]** section.
2. Extract the contents and move the `Alaska Gold Fever Translator` folder into the `BepInEx/plugins/` directory.
3. Run the game!

---

## ⌨️ Quick Start Guide

* **Translating Text:** Look inside `BepInEx/plugins/Alaska Gold Fever Translator/Localization/Indonesian/Strings/`. Open `translation_strings.json`, write your translated text on the right side of the colon, save, and restart the game.
* **Scanning UI Paths:** Hover over any text/image, hold **Left Ctrl** (or **Left Alt** for textures), and **Right-Click**. Check the black BepInEx console for the exact details.

---

## 👨‍💻 Credits

* **Author/Developer:** Ilham Gimank / Ilham Nurjaman
* Developed with high dedication for the modding localization community.