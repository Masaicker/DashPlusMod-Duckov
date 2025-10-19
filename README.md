# DashPlus 闪避增强

一个针对 "Escape from Duckov" 的Unity Mod，提供游戏参数的实时调节功能。未来可能会添加更多优化手感或数据调整的内容。

---

## 中文介绍

### 功能特性

#### 闪避参数
- 闪避距离倍数调节 (0.1x-5.0x)
- 体力消耗设置 (0-50)
- 冷却时间调整 (0-5秒)

#### 奔跑参数
- 步行速度倍数调节 (1.0x-5.0x)
- 奔跑速度倍数调节 (1.0x-5.0x)
- 体力消耗率倍数调节 (0x-5.0x)
- 体力恢复率倍数调节 (1.0x-5.0x)
- 体力恢复延迟倍数调节 (0x-5.0x)

#### 移动手感参数
- 惯性控制开关 (禁用/启用移动惯性效果)

#### 负重设置参数
- 无限负重开关 (无视重量限制，可携带任意重量的物品)

#### 使用方法
1. **安装Mod**:
   - 手动安装：将DashPlus.dll放入游戏Mods文件夹
   - Steam创意工坊：直接订阅该MOD
2. **启动游戏**: 运行游戏并加载存档
3. **打开控制面板**: 按 `Ctrl+G` 打开GUI控制面板
4. **调节参数**: 使用滑块实时调整各项参数
5. **设置保存**: 所有设置自动保存，下次启动时生效

> **建议**: 在ESC暂停时使用控制面板，以便调整参数

#### 技术特性
- ✅ 实时参数修改，无需重启游戏
- ✅ 设置持久化保存到PlayerPrefs
- ✅ Mod卸载时自动恢复原始值
- ✅ 完整的中英文双语界面
- ✅ 详细的调试日志输出
- ✅ 安全的参数边界检查
- ✅ 惯性控制系统（基于加速度调节实现）
- ✅ 无限负重系统（通过修改MaxWeight属性实现）

---

## English Introduction

### Features

#### Dash Parameters
- Dash Distance Control (0.1x-5.0x)
- Stamina Cost Settings (0-50)
- Cooldown Time Adjustment (0-5 seconds)

#### Run Parameters
- Walk Speed Control (1.0x-5.0x)
- Run Speed Control (1.0x-5.0x)
- Stamina Drain Rate Control (0x-5.0x)
- Stamina Recover Rate Control (1.0x-5.0x)
- Stamina Recover Delay Control (0x-5.0x)

#### Movement Feel Parameters
- Inertia Control Toggle (Disable/Enable movement inertia effects)

#### Weight Settings Parameters
- Infinite Weight Toggle (Ignore weight restrictions, carry items of any weight)

#### Usage
1. **Install Mod**:
   - Manual Installation: Place DashPlus.dll in the game Mods folder
   - Steam Workshop: Subscribe to the MOD directly
2. **Launch Game**: Run the game and load your save
3. **Open Control Panel**: Press `Ctrl+G` to open the GUI control panel
4. **Adjust Parameters**: Use sliders to adjust parameters in real-time
5. **Settings Save**: All settings are automatically saved and persist between sessions

> **Recommended**: Use the control panel while paused (ESC) for parameter adjustment

#### Technical Features
- ✅ Real-time parameter modification without game restart
- ✅ Persistent settings saved to PlayerPrefs
- ✅ Automatic restore of original values when mod is unloaded
- ✅ Complete bilingual (Chinese/English) interface
- ✅ Detailed debug logging output
- ✅ Safe parameter boundary checking
- ✅ Inertia control system (implemented through acceleration adjustment)
- ✅ Infinite weight system (implemented by modifying MaxWeight property)

---

### 许可证 / License
本项目仅供学习和交流使用。/ This project is for learning and communication purposes only.
https://github.com/Masaicker/DashPlusMod-Duckov