# DashPlus 闪避增强

一个针对 "Escape from Duckov" 的Unity Mod，提供游戏参数的实时调节功能。未来可能会添加更多优化手感或数据调整的内容。

---

## 中文介绍

### 功能特性

#### 闪避参数
- 闪避距离倍数调节 (0.1x-5.0x)
- 体力消耗设置 (0-50)
- 冷却时间调整 (0-5秒)
- 闪避换弹开关 (开启/禁用闪避时自动换弹)

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

#### 视野设置参数
- 自定义视野开关 (启用/禁用自定义FOV)
- 视野倍数调节 (0.2x-3.0x)
- 快捷调整：当自定义视野开启时，可使用 Ctrl+鼠标滚轮 进行平滑视野缩放

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
- ✅ 视野控制系统（通过修改FOV参数实现）
- ✅ 闪避换弹系统（底层绕过动作系统，固定时间快速换弹）

---

## English Introduction

### Features

#### Dash Parameters
- Dash Distance Control (0.1x-5.0x)
- Stamina Cost Settings (0-50)
- Cooldown Time Adjustment (0-5 seconds)
- Dash Reload Toggle (Enable/Disable auto-reload during dash)

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

#### FOV Settings Parameters
- Custom FOV Toggle (Enable/Disable custom field of view)
- FOV Multiplier Adjustment (0.2x-3.0x)
- Quick Adjustment: When custom FOV is enabled, use Ctrl+Mouse Wheel for smooth FOV scaling

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
- ✅ FOV control system (modifies FOV parameters)
- ✅ Dash reload system (bypasses action system for instant fixed-time reload)

---

### 许可证 / License
本项目仅供学习和交流使用。/ This project is for learning and communication purposes only.

## 支持一下｜Support
如果你觉得这个项目对你有帮助，欢迎在 Ko-fi 给我买杯咖啡 ☕  
If you found this project useful, you’re welcome to buy me a coffee on Ko-fi ☕

[![Buy me a coffee](https://cdn.prod.website-files.com/5c14e387dab576fe667689cf/670f5a0171bfb928b21a7e00_support_me_on_kofi_beige.png)](https://ko-fi.com/masaicker)
