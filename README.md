# IMakerTapTap_1
用于爱美客第三届TapTap聚光灯创作者挑战赛

# Git LFS 资源后缀规范与检查流程

> 面向：美术、策划、TA、程序等全体成员
> 目的：让大体积/二进制资源走 Git LFS，避免仓库体积膨胀、提交卡顿、拉取超时。
> 核心原则：**凡是「不是代码文本」的资源文件，提交前都要确认它的后缀在下方清单里。**

---

## 一、什么是 Git LFS（一句话）

Git 原本擅长管理**文本代码**。图片、模型、音频、视频这些**二进制大文件**如果直接进 Git，每次改动都会让仓库体积翻倍、拉取变慢。

**Git LFS（Large File Storage）** 就是把这些大文件存到专用服务器，Git 里只放一个「指针」，从而保持仓库轻快。

> 判断标准：**你的文件能被文本编辑器打开看到乱码，或体积较大，就基本属于 LFS 范畴。**

---

## 二、资源文件后缀分类清单

### 图片 / 纹理类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.png` | PNG | 无损位图，游戏贴图 / UI 最常用 |
| `.jpg` | JPEG | 有损位图，体积小，适合照片类贴图 |
| `.tga` | Targa | 位图，常带 Alpha 通道 |
| `.tif` | TIFF | 高保真图像，美术源文件常用 |
| `.exr` | OpenEXR | HDR 高动态范围图，存光照/渲染数据 |
| `.psd` | Photoshop | PS 源文件，分层可编辑 |
| `.atlas` | Texture Atlas | 图集（Spine 等），贴图 + 元数据 |

### 3D 模型 / 场景类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.fbx` | Autodesk FBX | 通用 3D 交换格式，Unity 导入主力 |
| `.obj` | Wavefront OBJ | 通用模型格式，兼容性好 |
| `.ma` | Maya ASCII | Maya 场景源文件 |
| `.max` | 3ds Max Scene | 3ds Max 工程源文件 |
| `.blend` | Blender | Blender 工程源文件 |
| `.skel` | Spine Skeleton | 骨骼 / 骨架二进制数据（Spine 动画） |

### 音频类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.wav` | WAVE | 无损音频，音效常用 |
| `.mp3` | MP3 | 有损压缩音频，音乐 / 语音 |
| `.ogg` | Ogg Vorbis | 有损压缩音频，开源格式 |
| `.aif` / `.aiff` | AIFF | Apple 无损音频格式 |

### 视频类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.mp4` | MPEG-4 | 主流视频容器格式 |
| `.mov` | QuickTime | Apple 视频格式 |

### Unity 资源类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.unity` | Unity Scene | 场景文件 |
| `.prefab` | Unity Prefab | 预制体 |
| `.asset` | Unity Asset | 资源文件（ScriptableObject 等） |
| `.unitypackage` | Unity Package | Unity 资源包导出文件 |

### 二进制 / 程序集类

| 后缀 | 格式 | 说明 |
|---|---|---|
| `.dll` | Dynamic Link Library | 动态链接库 / 插件 |

---

## 三、提交前自查流程（美术 / 策划必看）

### 第 1 步：识别文件类型

问自己一个问题：

> **这个文件，用「记事本 / 文本编辑器」打开，是不是一堆乱码？**

- 是乱码 → 大概率是二进制，走 LFS，继续第 2 步。
- 能正常阅读（是代码、配置、文字）→ 属于文本，**不要**加进 LFS。

### 第 2 步：核对后缀

在「二、后缀清单」里查找你的文件后缀：

| 结果 | 操作 |
|---|---|
| ✅ 后缀**在**清单里 | 直接提交即可，系统会自动走 LFS，无需额外操作 |
| ❌ 后缀**不在**清单里 | 进入第 3 步 |

### 第 3 步：不在清单里怎么办

按以下顺序处理：

1. **先判断**：这是不是一次性/临时文件（如导出预览图、草稿）？
   - 是临时文件 → 不要提交到仓库，放进本地临时目录或忽略。
2. **确认是正式资源** → 联系 **TA 或程序**，说明：
   - 文件后缀是什么
   - 文件用途（贴图 / 模型 / 音频 / 视频…）
   - 大概体积
3. 由 TA/程序在 `.gitattributes` 中**补充对应后缀**，你拿到更新后再提交。

> ⚠️ **绝对不要**：因为「后缀不在清单」就直接把大文件硬塞进 Git（绕过 LFS）。这会让仓库瞬间膨胀，且后续清理成本极高。

---

## 四、如何确认文件真的走了 LFS

提交后，用下面任一方法自查：

### 方法 1：命令行（程序适用）

```bash
# 查看已追踪的 LFS 文件
git lfs ls-files

# 查看某个具体文件是否走 LFS
git lfs track "Assets/Art/xxx.png"
# 如果输出已配置的后缀规则，说明已覆盖
```

### 方法 2：查看 Git 提交记录里的文件标记

LFS 文件在 Git 里显示为一个「指针」小文件，体积通常只有几百字节；如果某张图在提交记录里显示为几十 MB，说明它**没走 LFS**，需要立即上报。

### 方法 3：图形工具（美术适用）

- **SourceTree / Fork / GitHub Desktop**：文件列表里 LFS 文件会有特殊标记（如 LFS 图标或标签）。
- 不确定时，直接问程序确认。

---

## 五、常见遗漏与注意事项

| ❌ 错误做法 | ✅ 正确做法 |
|---|---|
| 后缀不在清单，直接硬提交大文件 | 先联系 TA/程序补后缀 |
| 把 `.cs`、`.json` 等代码文本也配进 LFS | 文本不进 LFS，保持 Git 原生管理 |
| 只配后缀、忘了执行 `git lfs install` | 首次用 LFS 先执行安装（程序一次性配置） |
| 提交 `.psd`、`.blend`、`.max` 等源文件 | 源文件也走 LFS，与导出件分开目录 |
| 在 `.gitignore` 里忽略所有美术源文件 | 需要协作的源文件走 LFS，不需要的才忽略 |
| 大文件提交后再想转 LFS | 转换成本高，务必**提交前**确认 |

---

## 六、完整 .gitattributes 配置（交给程序/TA 维护）

> 放在**仓库根目录**（与 `Assets/` 同级），全体成员的 LFS 规则都以此为准。

```gitattributes
# ===== 图片 / 纹理 =====
*.png  filter=lfs diff=lfs merge=lfs -text
*.tif  filter=lfs diff=lfs merge=lfs -text
*.exr  filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text
*.tga  filter=lfs diff=lfs merge=lfs -text
*.jpg  filter=lfs diff=lfs merge=lfs -text
*.atlas filter=lfs diff=lfs merge=lfs -text

# ===== 3D 模型 / 场景 =====
*.ma    filter=lfs diff=lfs merge=lfs -text
*.fbx   filter=lfs diff=lfs merge=lfs -text
*.obj   filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.max   filter=lfs diff=lfs merge=lfs -text
*.skel  filter=lfs diff=lfs merge=lfs -text

# ===== 音频 =====
*.wav  filter=lfs diff=lfs merge=lfs -text
*.mp3  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.aiff filter=lfs diff=lfs merge=lfs -text
*.aif  filter=lfs diff=lfs merge=lfs -text

# ===== 视频 =====
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text

# ===== Unity 资源 =====
*.unity       filter=lfs diff=lfs merge=lfs -text
*.prefab      filter=lfs diff=lfs merge=lfs -text
*.asset       filter=lfs diff=lfs merge=lfs -text
*.unitypackage filter=lfs diff=lfs merge=lfs -text

# ===== 二进制 / 程序集 =====
*.dll filter=lfs diff=lfs merge=lfs -text
```

> 说明：`filter=lfs` 表示走 LFS；`diff=lfs`、`merge=lfs` 表示用 LFS 方式做差异与合并；`-text` 表示按二进制处理、不做换行符转换。

---

## 七、团队提交流程总览

```text
新增/修改资源文件
        ↓
判断：文本 or 二进制？
   ├─ 文本（代码/配置）──→ 正常提交，不进 LFS
   └─ 二进制/大文件
           ↓
      后缀在清单里？
        ├─ 是 ──→ 直接提交（自动走 LFS）
        └─ 否 ──→ 联系 TA/程序补后缀 → 拿到更新 → 提交
                        ↓
              提交后自查（git lfs ls-files / 图形工具标记）
```

---

## 八、一句话速记

> **打开是乱码的、或超过几 MB 的资源文件 = 走 LFS；后缀不在清单 = 先找 TA/程序补，别硬塞。**
