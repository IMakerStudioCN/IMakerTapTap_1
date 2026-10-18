# 资源完整性检查（Tools/CheckAssetIntegrity.ps1）

防止「美术资源在 Inspector 里变成 Missing，但文件还在 art 目录」这类事故再次发生。

## 背景：这类事故是怎么发生的

1. 两条并行分支各自用 Unity 导入同一个 PNG → 同一张图出现 **两套 GUID**；合并时 `.meta` 的 guid 行冲突，
   解决时选错一边，或者干脆把 `<<<<<<<` / `=======` / `>>>>>>>` **连同冲突标记一起提交**。
2. `.meta` 带冲突标记后 Unity 的 YAML 解析会失败，退化成「字符串匹配取第一个 guid」，
   于是资源被挂到错误 GUID 上：`Start.unity` / `StartPanel.prefab` 等依然显示 Missing，而 git 不会有任何提示。
3. 场景 / 预制件是 LFS 文件，冲突标记还可能被写进 `.unity`（历史上 `Start.unity` 就被写过一次，
   现场保留在 `_SceneBackupRescue/`）。

## 三种模式

| 模式 | 检查内容 | 用在哪 |
|---|---|---|
| `Staged` | 暂存区文件的冲突标记 | 本地 pre-commit 钩子（快） |
| `Diff` | 本次改动的冲突标记 + 已有 `.meta` 的 GUID 变更 + 新增资源缺 `.meta` + 重复 GUID | CI（PR） |
| `Full` | 全量：冲突标记 + 重复 GUID + 缺 `.meta` + 悬空 GUID 引用 | 本地排查 / 合并前自查 |

```powershell
# 本地全量自查（在 Unity 工程目录下跑，有 Library/PackageCache 时最准）
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\CheckAssetIntegrity.ps1

# 与基线对比（CI 用的就是这个）
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\CheckAssetIntegrity.ps1 -Mode Diff -Base origin/main
```

> 装了 PowerShell 7 的话把 `powershell` 换成 `pwsh` 即可；没装就用上面的写法
> （`-ExecutionPolicy Bypass` 是为了绕开默认禁止运行脚本的策略）。

退出码：`0` 通过，`1` 发现问题。

## 三个清单文件

- `Tools/asset-integrity-ignore.txt`：**已知可接受**的悬空 GUID（历史遗留、QFramework 模板、指向已删除资源等）。
  登记在这里的不会让检查失败，**新增**的悬空引用仍然会被拦下。
- `Tools/asset-guid-change-allowlist.txt`：允许「已有 `.meta` 的 guid 被主动改动」的例外登记。
  不登记就报错，避免再次出现「换掉 GUID 却没改引用」。
- `Tools/asset-missing-meta-ignore.txt`：「资源文件缺配套 `.meta`」的例外（生成物 / 缓存文件）。

## 为什么要检查「缺 .meta」

`Assets/Art/ui/desktop/popup.png` 曾经只有 png 没有 `popup.png.meta`：每个人打开工程时 Unity 都会各自生成一个**不同的**
GUID，提交后就变成同路径两套 GUID，合并必然出现冲突（`feature/NewAudio` 与 `main` 的 popup 冲突就是这么来的）。
所以 `.meta` 必须和资源文件一起提交。

## 启用本地 pre-commit 钩子（可选，一次性）

```powershell
git config core.hooksPath .githooks
```

之后每次提交前会自动检查暂存文件里有没有残留冲突标记。

## CI

`.github/workflows/asset-integrity.yml` 会在 PR 到 `main` 时跑 `Diff` 模式。
注意：`main` 有分支保护、只能走 PR，所以这道闸门覆盖了所有进入 main 的改动。

## 修冲突标记的正确姿势

`.meta` 冲突**不要**两边都留：

1. 先查清「合并后的场景 / 预制件引用的是哪个 GUID」（跑一次 `Full` 模式的悬空引用扫描最快）；
2. 只保留那一个 `guid:` 行，删掉 `<<<<<<<` / `=======` / `>>>>>>>`；
3. `git diff --check` 确认没有 leftover conflict marker 再提交。
