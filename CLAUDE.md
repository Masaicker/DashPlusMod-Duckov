# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述 / Project Overview

DashPlus 是一个针对游戏 "Escape from Duckov" 的 Unity Mod，主要功能是增强游戏的闪避/翻滚机制，允许玩家自定义闪避距离、体力消耗和冷却时间。

## 构建和开发命令 / Build and Development Commands

### 构建项目
```bash
# 使用 .NET CLI 构建
cd DashPlus
dotnet build -c Debug

# 构建发布版本
dotnet build -c Release

# 清理项目
dotnet clean
```

### 使用 Visual Studio
```bash
# 打开解决方案
start DashPlus.sln

# 或使用命令行构建
msbuild DashPlus.sln /p:Configuration=Debug
```

## 代码架构 / Code Architecture

### 核心组件
- **ModBehaviour.cs**: 项目的主要类，继承自 `Duckov.Modding.ModBehaviour`
  - 管理Mod的生命周期
  - 处理闪避参数的动态修改
  - 提供GUI控制面板
  - 负责设置的保存和加载

### 关键功能模块

1. **参数修改系统**:
   - `dashDistanceMultiplier`: 闪避距离倍数控制
   - `staminaCost`: 体力消耗设置
   - `coolTime`: 冷却时间调整
   - 通过修改 `CharacterMainControl.Main.dashAction` 的属性实现

2. **GUI控制系统**:
   - 使用Unity OnGUI系统
   - 快捷键: `Ctrl+G` 切换显示/隐藏
   - 提供实时参数调整滑块
   - 支持中英文双语界面

3. **设置持久化**:
   - 使用 `PlayerPrefs` 保存用户配置
   - 场景切换时自动重新应用设置
   - Mod卸载时恢复原始值

### 依赖关系
- Unity Engine modules (Core, IMGUI, InputLegacy)
- Duckov游戏核心库:
  - `TeamSoda.Duckov.Core`
  - `TeamSoda.Duckov.Utilities`
  - `ItemStatsSystem`
  - `TeamSoda.MiniLocalizor`

## 开发注意事项 / Development Notes

### 游戏路径配置
项目引用了游戏安装目录下的DLL文件，路径为：
```
E:\Steam\steamapps\common\Escape from Duckov\Duckov_Data\Managed\
```

如果游戏安装路径不同，需要更新 `DashPlus.csproj` 中的 `HintPath`。

### 调试和日志
- 设置 `enableLogging = true` 启用调试日志
- 日志前缀为 `[DashPlus]`
- 可通过GUI面板切换日志开关

### 测试建议
- 建议在游戏暂停时(ESC)测试GUI功能
- 测试场景切换时设置是否保持
- 验证Mod卸载时原始值是否正确恢复