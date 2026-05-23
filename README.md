# Casualties Unknown Mod (未知伤亡 多功能辅助)

基于 BepInEx 和 Harmony 开发的《未知伤亡》多功能模块化辅助 Mod。采用了纯代码手绘的 ClickGUI，并支持不是很丰富的自动化与透视功能。

## ✨ 功能列表 (Features)
* **🙎‍♂️ 玩家模块 (Player)**
  * **灵魂出窍**：让你可以自由移动视角
  * **长手模式**：让你拥有更长的手臂
  * **隔墙取物**：让你可以隔墙拿取物品
  * **绷带高手**：自动高速完成包扎动作，并在完成后精准停止

* **👁️ 视觉模块 (Render)**
  * **物品透视**：在掉落的物品上显示物品名称
  * **生物透视**：在生物身上标注名称
  * **陷阱警告**：在陷阱上标注陷阱名称
  * **局部光照扩大**：允许你扩大/缩小光照范围大小
  * **视距拉远**：扩大/缩小你的视野 (FOV)

* **🏃 移动模块 (Movement)**
  * **超级飞侠**：允许你拥有飞行能力
  * **跳跃增强**：允许你自定义跳跃高度
  * **实体穿墙**：无视物理碰撞，允许你穿墙而过

* **🌍 世界模块 (World)**
  * **秒开锁**：自动开锁（支持密码锁与撬锁）

* **🔧 杂项模块 (Misc)**
  * **聊天机翻**：自动将聊天栏中的外语文本翻译为中文
  * **万事通模式**：无视智力属性限制，强制看懂所有物品介绍
  * **语言设置 (Language)**：一键切换 ClickGUI 的显示语言 (中/英)
  * **菜单设置**：允许你自定义呼出菜单的快捷键

## 📥 安装指南
1. 确保游戏已安装 BepInEx。
2. 将编译好的 `.dll` 放入游戏的 `BepInEx/plugins` 目录中。
3. 进游戏后，按下 `F6` 键呼出控制菜单。

## 📥 编译指令
* **dotnet build -c Release**








# Casualties Unknown Mod

A multi-functional, modular utility mod for *Casualties Unknown*, built on BepInEx and Harmony. It features a custom pure-code drawn ClickGUI, supports a rich variety of automation and ESP features, and includes a **seamless English/Chinese bilingual switching** system.

## ✨ Features

* **🙎‍♂️ Player Module**
  * **Freecam**: Allows you to detach and move the camera freely.
  * **Long Arms**: Extends your interaction reach.
  * **Grab Through Walls**: Allows you to pick up items through solid objects.
  * **Auto Bandage**: Rapidly automates the bandaging mini-game and stops precisely when the bleeding is healed.

* **👁️ Render Module**
  * **Item ESP**: Displays the names of dropped items through walls.
  * **Creature ESP**: Highlights and labels creatures/entities.
  * **Trap ESP**: Highlights and warns you about traps.
  * **Light Modifier**: Allows you to expand or shrink the local illumination radius.
  * **FOV Changer**: Customizes your Field of View.

* **🏃 Movement Module**
  * **Fly Hack**: Grants you the ability to fly.
  * **Jump Boost**: Allows you to customize your jump height.
  * **NoClip**: Ignores physical collisions, allowing you to walk through walls.

* **🌍 World Module**
  * **Instant Unlock**: Automatically opens locks (supports both combination locks and lockpicking).

* **🔧 Misc Module**
  * **Chat Auto-Translate**: Automatically translates foreign text in the chat box.
  * **Omniscient Mode**: Bypasses intelligence stat limits, allowing you to understand all item descriptions.
  * **Language**: One-click switch for ClickGUI display language (EN/ZH).
  * **Menu Keybind**: Allows you to customize the hotkey to toggle the mod menu.

## 📥 Installation

1. Ensure you have [BepInEx](https://github.com/BepInEx/BepInEx) installed in your game.
2. Drop the compiled `.dll` file into your game's `BepInEx/plugins` directory.
3. Launch the game and press the `F6` key (default) to open the mod menu.

## 🛠️ Build Instructions

To build this project from source, run the following command in the project root directory:
```bash
dotnet build -c Release
