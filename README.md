# FreeGhost

[English](#english) | [中文](#中文)

## English

FreeGhost is a client-side BepInEx mod for PEAK. After your scout dies and PEAK creates the Ghost, the camera and local Ghost enter a first-person noclip flight mode while PEAK's original spectate target selection, Ghost RPCs, revive flow, and scene lifecycle remain in control.

Baseline: PEAK `2.0.a` (Steam build `24676019`), BepInEx 5, .NET Standard 2.1.

### Controls

| Action | Input |
|---|---|
| Move | PEAK's rebound movement input |
| Look | PEAK's rebound look input |
| Ascend | PEAK's jump input |
| Descend | PEAK's crouch input |
| Move faster | PEAK's sprint input |
| Toggle free/vanilla spectate mode | `R` by default |

Movement follows the camera's horizontal heading. Looking up or down does not change the WASD flight plane; jump and crouch control altitude. Keyboard and controller movement inputs, analog magnitude, sensitivity, inversion, and PEAK rebinding continue to work. The mode shortcut is a BepInEx shortcut and can be changed in the config file.

Each Ghost session starts in free mode. Pressing the mode shortcut while free returns control to PEAK's original spectate camera without changing its current target. Pressing it again enters free mode at the current vanilla camera position; an earlier free-flight position is not restored.

Free movement is limited to a three-dimensional sphere centered on the position where free mode was entered. The default radius is 1 km and can be configured. Changing spectate targets does not move this center. Movement and mode switching are disabled while PEAK blocks player input, a menu is open, or the game is paused.

### Installation

Install `BepInEx-BepInExPack_PEAK-5.4.2403`, then install through a Thunderstore-compatible manager. For manual installation, copy `plugins/FreeGhost.dll` from the package into the game's `BepInEx/plugins` directory.

Only the player operating FreeGhost needs the mod. Host authority is not required.

### Configuration

The config file is generated at `BepInEx/config/com.github.lllei.FreeGhost.cfg`.

| Key | Default | Allowed range |
|---|---:|---:|
| `General.Enabled` | `true` | boolean |
| `Movement.BaseSpeed` | `8.0` | `0.1` to `1000` m/s |
| `Movement.SprintMultiplier` | `2.5` | `1` to `20` |
| `Movement.MaxDistance` | `1000` | `10` to `10000` m |
| `Movement.ModeToggleShortcut` | `R` | BepInEx keyboard shortcut |
| `Networking.SyncToVanillaClients` | `true` | boolean |

Invalid numeric values are clamped or replaced with safe defaults at runtime.

### Vanilla-client synchronization

FreeGhost does not add RPCs or network events. While the local player is dead and free ghost mode is active, it changes only the `CharacterSyncData` copy returned for serialization:

```text
ghost = target.Center - direction * spectateZoom + Vector3.up * 0.5
```

The encoder subtracts PEAK 2.0's fixed world-up offset, analytically solves the direction for the desired world position, converts it through PEAK's own direction-to-look helper, and quantizes the look angles to their nearest half-float values. It then projects and quantizes the distance for that transmitted direction. The local player's real look values are never overwritten.

Vanilla clients keep PEAK's original `Vector3.Lerp(..., 3 * deltaTime)` smoothing. Because both look angles and distance are half floats, error grows with distance, but remains small at the mod's ordinary 1 km radius. Unlike PEAK 1.x's rotated offset, PEAK 2.0's fixed world-up offset can be removed before solving, so close positions no longer need a special approximation.

There is no long-distance network clamp. Values outside the finite half-float range, missing targets, and other unsafe states retain valid vanilla values instead of sending NaN or infinity. Disable `Networking.SyncToVanillaClients` to keep only local free movement.

### Known compatibility limitation

PEAK derives a dead remote character's `lookDirection` from the synchronized `lookValues`. Most consumers are visual or unavailable while dead, but PEAK `2.0.a` Scoutmaster visibility checks still iterate all characters without excluding dead ones. Consequently, vanilla-client synchronization can change whether a Scoutmaster considers the dead scout's body to be looking at it. This cannot be eliminated while encoding arbitrary 3D Ghost positions solely through vanilla fields. Disable vanilla-client synchronization if this affects a session or conflicts with another mod that reads dead-player look values.

### Building

Copy `PeakGameDir.props.example` to the gitignored `PeakGameDir.props` and set the PEAK directory, or set `PEAK_GAME_DIR`.

```powershell
dotnet test tests\FreeGhost.Core.Tests\FreeGhost.Core.Tests.csproj -c Release
dotnet build FreeGhost.sln -c Release
dotnet msbuild src\FreeGhost\FreeGhost.csproj -t:Deploy -p:Configuration=Debug
dotnet msbuild src\FreeGhost\FreeGhost.csproj -t:PackageThunderstore -p:Configuration=Release
```

Normal builds never write to the game directory. `Deploy` is explicit. Packaging writes `artifacts/lllei-FreeGhost-<version>.zip`.
For a release, update `Version` in `FreeGhost.csproj` and `version_number` in `manifest.json`. MSBuild generates the BepInEx plugin version from `Version`, and packaging stops if the manifest and compiled DLL versions do not agree.

## 中文

FreeGhost 是 PEAK 的客户端 BepInEx Mod。童子军死亡并由原版生成 Ghost 后，相机和本地 Ghost 会进入第一人称无碰撞自由飞行模式，同时保留原版的观战目标选择、Ghost RPC、复活和场景生命周期。

开发基线：PEAK `2.0.a`（Steam build `24676019`）、BepInEx 5、.NET Standard 2.1。

### 控制

| 操作 | 输入 |
|---|---|
| 移动 | PEAK 中重新绑定后的移动输入 |
| 观察 | PEAK 中重新绑定后的视角输入 |
| 上升 | 跳跃 |
| 下降 | 蹲下 |
| 加速 | 冲刺 |
| 切换自由移动 / 原版观战模式 | 默认 `R` |

前后左右沿相机朝向的水平面移动；俯视和仰视不会改变 WASD 的升降方向，跳跃和蹲下专门控制高度。键鼠、手柄、摇杆幅度、灵敏度、反转设置及游戏按键重绑定均沿用原版。模式切换键是 BepInEx 快捷键，可在配置文件中修改。

每次生成 Ghost 后默认进入自由模式。在自由模式按下切换键会停止 Mod 对相机、Ghost 和网络位置的覆盖，立即恢复 PEAK 原版观战，但不会改变当前观战目标；再次按下时，会从此刻的原版观战相机位置开始自由移动，不会恢复上一次的自由坐标。

自由移动范围是以每次进入自由模式时的相机位置为圆心的三维球体，默认半径为 1 km，可以通过配置修改。切换观战目标不会移动该圆心。暂停、菜单打开或游戏阻塞玩家输入时不会移动，也不会切换模式。

### 安装

先安装 `BepInEx-BepInExPack_PEAK-5.4.2403`，再通过兼容 Thunderstore 的 Mod 管理器安装。手动安装时，把发布包中的 `plugins/FreeGhost.dll` 复制到游戏的 `BepInEx/plugins` 目录。

只需要自由 Ghost 的操作者安装本 Mod，不要求房主权限。

### 配置

配置文件生成于 `BepInEx/config/com.github.lllei.FreeGhost.cfg`，配置键、默认值及范围与英文表格一致。最大移动半径为 `Movement.MaxDistance`，默认 `1000` 米，允许 `10..10000` 米；模式切换键为 `Movement.ModeToggleShortcut`，默认 `R`。非法数值会在运行时回退或钳制到安全范围。

### 原版客户端同步

FreeGhost 不新增 RPC 或网络事件。Mod 只在本地角色死亡且自由模式活动时修改待序列化的 `CharacterSyncData` 副本，不覆盖玩家真实视角数据。编码器先减去 PEAK 2.0 固定的世界坐标向上偏移，再用解析公式求出所需方向，调用游戏自己的方向转换方法，把视角量化到最近的 half-float 值，然后按量化后的实际发送方向重新投影并量化距离。

未安装 Mod 的客户端继续使用原版 `Vector3.Lerp(..., 3 * deltaTime)` 平滑。视角和距离都是 half float，因此距离越远量化误差越大，但在默认 1 km 移动半径内通常很小。PEAK 2.0 将原来随视角旋转的偏移改成了固定的世界坐标 `Vector3.up * 0.5f`，因此编码前可以直接消除该偏移，近距离位置不再需要特殊近似。

编码器不再做远距离截断。目标不存在、超出 half-float 有限范围、数值异常或无法安全编码时会保留有效原版同步值，不发送 NaN 或无穷值。发生兼容问题时可关闭 `Networking.SyncToVanillaClients`，只保留本地自由移动。

### 已知兼容性限制

PEAK 会从同步的 `lookValues` 计算远端死者的 `lookDirection`。`2.0.a` 的 Scoutmaster 视线检查仍会遍历所有角色且不排除死者，因此原版客户端同步可能改变 Scoutmaster 是否认为死者尸体正看着它。在“仅操作者安装 Mod、且不新增网络协议”的约束下，无法同时消除这一副作用并同步任意三维 Ghost 位置。受影响时请关闭原版客户端同步；其他读取死者视角的 Mod 也可能存在类似兼容问题。

### 构建

把 `PeakGameDir.props.example` 复制为已被忽略的 `PeakGameDir.props` 并填写 PEAK 目录，或设置 `PEAK_GAME_DIR`。普通构建不会覆盖游戏文件；只有显式 `Deploy` 目标会部署。发布时同时更新 `FreeGhost.csproj` 的 `Version` 和 `manifest.json` 的 `version_number`；MSBuild 会据此生成 BepInEx 插件版本，打包会校验 manifest 与 DLL 版本是否一致。命令见英文构建章节。
