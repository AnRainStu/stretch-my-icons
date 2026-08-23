# stretch-my-icons

Language / 语言 / 言語:

- [English](#english)
- [简体中文](#简体中文)
- [日本語](#日本語)

## English

Windows desktop icon stretching prototype.

### Approach

This project will not patch Explorer itself. The MVP will use a companion app that:

1. Finds the desktop `WorkerW` / `Progman` host window.
2. Renders a transparent always-on-top layer behind the desktop icons.
3. Detects icon positions and stretches the visual background behind them.
4. Fills the extra space with sampled or solid background color.
5. Keeps the effect live when the desktop layout changes.

### MVP scope

- Windows 10 and 11 only.
- Desktop icons only.
- No taskbar changes.
- No shell injection.

### Likely stack

- C#
- WPF or WinUI 3 for the settings UI
- Win32 interop for window discovery and layering

### Phases

1. Probe the desktop window hierarchy.
2. Draw a test overlay aligned to the icon grid.
3. Replace the test overlay with stretched icon panels.
4. Add live refresh on resize, refresh, and icon changes.
5. Add a small settings panel.

### Risks

- Explorer updates may change the desktop window tree.
- Per-monitor DPI handling can shift icon math.
- Painting behind icons must avoid flicker and input bugs.

## 简体中文

Windows 桌面图标拉伸原型。

### 实现思路

这个项目不会直接修改 Explorer 本体。MVP 会使用一个伴随程序来实现：

1. 找到桌面的 `WorkerW` / `Progman` 宿主窗口。
2. 在桌面图标后方绘制透明、始终置顶的图层。
3. 检测图标位置，并把其背后的视觉背景拉伸。
4. 用采样色或纯色填充多出来的区域。
5. 在桌面布局变化时保持效果持续生效。

### MVP 范围

- 仅支持 Windows 10 和 11。
- 仅处理桌面图标。
- 不修改任务栏。
- 不做 shell 注入。

### 可能技术栈

- C#
- 设置界面使用 WPF 或 WinUI 3
- 使用 Win32 interop 做窗口发现和图层处理

### 阶段

1. 探测桌面窗口层级。
2. 绘制一个与图标网格对齐的测试覆盖层。
3. 把测试覆盖层替换成拉伸图标面板。
4. 加入对缩放、刷新和图标变化的实时更新。
5. 加一个小型设置面板。

### 风险

- Explorer 更新可能会改变桌面窗口树。
- 每个显示器的 DPI 处理可能让图标坐标偏移。
- 在图标后方绘制必须避免闪烁和输入问题。

## 日本語

Windows デスクトップのアイコンを伸ばす試作。

### 方針

このプロジェクトは Explorer 自体を改造しません。MVP は補助アプリとして次を行います。

1. デスクトップの `WorkerW` / `Progman` ホストウィンドウを見つける。
2. アイコンの背後に透明な常時最前面レイヤーを描画する。
3. アイコン位置を検出し、その背後の見た目を横に伸ばす。
4. 余白をサンプリング色または単色で埋める。
5. デスクトップ配置が変わっても効果を維持する。

### MVP 範囲

- Windows 10 / 11 のみ。
- デスクトップアイコンのみ。
- タスクバーは変更しない。
- シェル注入はしない。

### 想定スタック

- C#
- 設定 UI は WPF か WinUI 3
- ウィンドウ検出とレイヤー処理は Win32 interop

### 手順

1. デスクトップのウィンドウ階層を調べる。
2. アイコングリッドに合わせたテスト用オーバーレイを描く。
3. それを伸縮するアイコン背景パネルに置き換える。
4. リサイズ、更新、アイコン変更に追従する。
5. 小さな設定パネルを追加する。

### リスク

- Explorer の更新でデスクトップのウィンドウ構造が変わる可能性がある。
- マルチモニター DPI でアイコン座標がずれる可能性がある。
- アイコン背後の描画はちらつきや入力不具合を避ける必要がある。
