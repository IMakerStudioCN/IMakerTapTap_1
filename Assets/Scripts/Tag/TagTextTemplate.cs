using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>模版里的一个顺序片段：要么是一段固定文本，要么是一个空缺。</summary>
    public class TagTextSegment
    {
        /// <summary>固定文本节点（空缺时为 null）。</summary>
        public GameObject TextObject;

        /// <summary>空缺（固定文本时为 null）。</summary>
        public TagKindSlot Slot;

        /// <summary>固定文本的内容。</summary>
        public string Text;

        public bool IsSlot => Slot != null;
    }

    /// <summary>一个空缺的配置（生成器用）。</summary>
    public struct SlotSetup
    {
        public string SlotId;
        public TagOfWords WordType;

        public SlotSetup(string slotId, TagOfWords wordType)
        {
            SlotId = slotId;
            WordType = wordType;
        }
    }
    /// <summary>进度。</summary>
    [Serializable]
    public class TagFillProgress
    {
        public int filled;
        public int total;

        public TagFillProgress(int filled, int total)
        {
            this.filled = filled;
            this.total = total;
        }
    }

    /// <summary>一条预设字体：Inspector 里配好「名字 + 字体」，再用索引选中。</summary>
    [Serializable]
    public class TagFontPreset
    {
        [Tooltip("在下拉里显示的名字")]
        public string name;

        [Tooltip("这个预设用的字体")]
        public TMP_FontAsset font;
    }

    /// <summary>把 int 字段画成「预设字体」下拉（TagFontPresetIndexDrawer 用）。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TagFontPresetIndexAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 【带空缺的文本模版】。
    ///
    /// 把它挂在模版根节点上，子节点按视觉顺序排列：
    ///
    ///   Template (本组件)
    ///   ├─ "我真的很"        <- 固定文本节点（TMP / Text）
    ///   ├─ Slot_A            <- TagKindSlot（空缺）
    ///   ├─ "，因为"
    ///   ├─ Slot_B            <- TagKindSlot（空缺）
    ///   └─ "。"
    ///
    /// 按【层级顺序（深度优先）】自动收集成线性片段序列，就是文本顺序。
    /// 因此可以通过调整节点顺序来调整阅读顺序，不需要额外配置。
    /// </summary>
    [DisallowMultipleComponent]
    public class TagTextTemplate : MonoBehaviour
    {
        [Header("文档标识（存档用）")]
        [SerializeField] private string mDocumentId = "text_fill";

        [Tooltip("玩家没有填的空缺在纯文本里显示成什么")]
        [SerializeField] private string mEmptyPlaceholder = "____";

        [Header("生成的文字样式（Configure / CreateSlot 造出来的 TMP 用）")]
        [Tooltip("预设字体列表：先在这里配好「名字 + 字体」，再用下面的下拉选择")]
        [SerializeField] private List<TagFontPreset> mFontPresets = new List<TagFontPreset>();

        [Tooltip("选中的预设序号；-1 表示不使用预设，直接落到 mFont")]
        [TagFontPresetIndex]
        [SerializeField] private int mFontPresetIndex = -1;

        [Tooltip("兜底字体：没选预设、或选中预设没配字体时用它；留空则用 TMP 默认字体")]
        [SerializeField] private TMP_FontAsset mFont;

        [Tooltip("运行时生成的文字颜色")]
        [SerializeField] private Color mTextColor = Color.black;

        [Header("日志")]
        [SerializeField] private bool mLog;

        private readonly List<TagTextSegment> mSegments = new List<TagTextSegment>();

        /// <summary>是否已经收集过（哪怕收集到 0 段）。EnsureCollected 用它判断，不能再按 Count==0 判断。</summary>
        private bool mCollected;

        /// <summary>正在收集的重入锁。</summary>
        private bool mCollecting;

        /// <summary>片段内容发生变化时触发（空缺填入/清空，或重新收集）。自适应排版靠它重排。</summary>
        public event Action SegmentsChanged;
        /// <summary>文档 Id。</summary>
        public string DocumentId => mDocumentId;

        /// <summary>按文本顺序排好的片段（只读）。</summary>
        public IReadOnlyList<TagTextSegment> Segments => mSegments;

        /// <summary>是否已经收集过片段（收集到 0 段也算收集过）。</summary>
        public bool IsCollected => mCollected;

        /// <summary>当前生效的字体（只读：优先级 = 选中的预设 &gt; mFont &gt; TMP 默认字体）。</summary>
        public TMP_FontAsset Font => ResolveFont();

        /// <summary>配置好的预设字体列表（只读）。</summary>
        public IReadOnlyList<TagFontPreset> FontPresets => mFontPresets;

        /// <summary>当前选中的预设序号；-1 表示不使用预设。</summary>
        public int FontPresetIndex => mFontPresetIndex;

        /// <summary>所有空缺是否都已填满。</summary>
        public bool IsAllSlotsFilled
        {
            get
            {
                EnsureCollected();

                int total = 0;

                foreach (TagTextSegment segment in mSegments)
                {
                    if (!segment.IsSlot)
                    {
                        continue;
                    }

                    total++;

                    if (segment.Slot.IsEmpty)
                    {
                        return false;
                    }
                }

                return total > 0;
            }
        }

        // ---------------- 生命周期 ----------------

        private void Awake()
        {
            Collect();
        }

        private void OnEnable()
        {
            Collect();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    segment.Slot.Changed -= OnSlotChanged;
                    segment.Slot.Changed += OnSlotChanged;
                }
            }
        }

        private void OnDisable()
        {
            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    segment.Slot.Changed -= OnSlotChanged;
                }
            }
        }

        /// <summary>
        /// 重新收集片段。
        ///
        /// 注意：这里会发 SegmentsChanged，而订阅方（NewsController.RefreshUI）会反过来调
        /// GetProgress() / IsAllSlotsFilled()，那些又都会走 EnsureCollected()。
        /// 所以必须加防重入锁，否则一旦收集到 0 段就会无限递归（StackOverflowException）。
        /// </summary>
        public void Collect()
        {
            if (mCollecting)
            {
                return;
            }

            mCollecting = true;

            try
            {
                mSegments.Clear();
                CollectInto(transform, mSegments);

                // 先套字体再发事件：订阅方（自适应排版）要按新字体量尺寸
                ApplyFontToTemplate();

                mCollected = true;
                Log($"收集到 {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");

                // 事件也在锁里发：万一订阅方绕一圈又调 Collect()，直接忽略，不会继续递归
                SegmentsChanged?.Invoke();
            }
            finally
            {
                mCollecting = false;
            }
        }

        /// <summary>
        /// 确保已经收集过片段。
        ///
        /// 判定条件必须是「从没收集过」，不能是 mSegments.Count == 0：
        /// 内容本来就是空的时候（Configure(null, null) 清空后、或这条新闻没配片段），
        /// 按 Count==0 判断会让每次调用都重新 Collect 并发 SegmentsChanged，
        /// 而订阅方 RefreshUI 又会调回来 —— 直接栈溢出。
        /// </summary>
        public void EnsureCollected()
        {
            if (!mCollected)
            {
                Collect();
            }
        }

        private int CountSlots()
        {
            int count = 0;

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>深度优先遍历，把固定文本与空缺按顺序收集起来。</summary>
        private static void CollectInto(Transform root, List<TagTextSegment> output)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                TagKindSlot slot = child.GetComponent<TagKindSlot>();

                if (slot != null)
                {
                    output.Add(new TagTextSegment { Slot = slot });
                    continue;   // 空缺内部的东西不再拆
                }

                string text = ExtractText(child.gameObject);

                if (text != null)
                {
                    output.Add(new TagTextSegment { TextObject = child.gameObject, Text = text });
                }

                // 继续往下找（比如中间还有一层布局节点）
                CollectInto(child, output);
            }
        }

        /// <summary>取出节点上的文本；没有文本组件则返回 null。</summary>
        private static string ExtractText(GameObject go)
        {
            TMP_Text tmp = go.GetComponent<TMP_Text>();

            if (tmp != null)
            {
                return tmp.text;
            }

            Text uiText = go.GetComponent<Text>();

            if (uiText != null)
            {
                return uiText.text;
            }

            return null;
        }

        // ---------------- 拼接 / 进度 ----------------

        /// <summary>把当前画面上的内容按顺序拼成完整文本。</summary>
        public string GetPlainText()
        {
            EnsureCollected();

            StringBuilder builder = new StringBuilder();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    builder.Append(segment.Slot.IsFilled ? segment.Slot.Content : mEmptyPlaceholder);
                }
                else
                {
                    builder.Append(segment.Text);
                }
            }

            return builder.ToString();
        }

        /// <summary>已填 / 总数。</summary>
        public TagFillProgress GetProgress()
        {
            EnsureCollected();

            int filled = 0;
            int total = 0;

            foreach (TagTextSegment segment in mSegments)
            {
                if (!segment.IsSlot)
                {
                    continue;
                }

                total++;

                if (segment.Slot.IsFilled)
                {
                    filled++;
                }
            }

            return new TagFillProgress(filled, total);
        }

        /// <summary>清空所有空缺。</summary>
        public void ClearAll(bool destroyDisplay = true)
        {
            EnsureCollected();

            foreach (TagTextSegment segment in mSegments)
            {
                segment.Slot?.Clear(destroyDisplay);
            }
        }

        // ---------------- 配置（生成器 / 运行时搭建用） ----------------

        /// <summary>
        /// 直接指定片段顺序（生成器在创建节点后调用，比自动遍历更可靠）。
        /// </summary>
        public void SetSegments(IEnumerable<TagTextSegment> segments)
        {
            mSegments.Clear();

            if (segments != null)
            {
                foreach (TagTextSegment segment in segments)
                {
                    if (segment != null && (segment.IsSlot || segment.TextObject != null))
                    {
                        mSegments.Add(segment);
                    }
                }
            }

            ApplyFontToTemplate();

            mCollected = true;
            Log($"SetSegments -> {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");
            SegmentsChanged?.Invoke();
        }

        /// <summary>
        /// 在模版下创建一个空缺节点（生成器 / 运行时搭建用）。
        /// 会自动补一个可命中的透明 Image。
        /// </summary>
        public TagKindSlot CreateSlot(string slotId, TagOfWords wordType,
            GameObject tagDisplayPrefab = null, string placeholderText = "____")
        {
            GameObject go = new GameObject(slotId, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.sizeDelta = new Vector2(120f, 60f);

            // 可命中的热区
            UnityEngine.UI.Image hit = go.AddComponent<UnityEngine.UI.Image>();
            hit.color = new Color(1f, 1f, 1f, 0.12f);
            hit.raycastTarget = true;

            // 空缺提示
            GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
            RectTransform phRect = (RectTransform)placeholder.transform;
            phRect.SetParent(rect, false);
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI phText = placeholder.AddComponent<TMPro.TextMeshProUGUI>();
            phText.text = placeholderText;
            phText.alignment = TMPro.TextAlignmentOptions.Center;
            phText.raycastTarget = false;
            ApplyTextStyle(phText);

            TagKindSlot slot = go.AddComponent<TagKindSlot>();
            slot.ApplySetup(slotId, wordType, tagDisplayPrefab);
            slot.SetPlaceholder(placeholder);

            return slot;
        }

        /// <summary>
        /// 按 (文本, 空缺) 交替的配置一次性搭好整个模版。
        /// 生成器用：会先清空所有子节点再重建。
        /// </summary>
        public void Configure(IList<object> sequence, GameObject tagDisplayPrefab = null)
        {
            // 清空现有子节点
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediateSafe(transform.GetChild(i).gameObject);
            }

            mSegments.Clear();

            int textIndex = 0;
            int slotIndex = 0;

            if (sequence != null)
            {
                foreach (object item in sequence)
                {
                    if (item is string text)
                    {
                        GameObject go = new GameObject($"Text_{textIndex++}", typeof(RectTransform));
                        RectTransform rect = (RectTransform)go.transform;
                        rect.SetParent(transform, false);
                        rect.sizeDelta = new Vector2(160f, 60f);

                        TMPro.TextMeshProUGUI tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
                        tmp.text = text;
                        tmp.alignment = TMPro.TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                        ApplyTextStyle(tmp);

                        mSegments.Add(new TagTextSegment { TextObject = go, Text = text });
                    }
                    else if (item is SlotSetup setup)
                    {
                        TagKindSlot slot = CreateSlot(
                            string.IsNullOrEmpty(setup.SlotId) ? $"Slot_{slotIndex}" : setup.SlotId,
                            setup.WordType,
                            tagDisplayPrefab);

                        slotIndex++;

                        mSegments.Add(new TagTextSegment { Slot = slot });
                    }
                }
            }

            // Configure 把原来的空缺全销毁重建了，必须重新挂 Changed：
            // 否则新空缺填入 / 清空时不会触发 SegmentsChanged，
            // 进度、预览、自适应排版、TagSlotFilledEvent 全部不会响应。
            foreach (TagTextSegment segment in mSegments)
            {
                if (!segment.IsSlot)
                {
                    continue;
                }

                segment.Slot.Changed -= OnSlotChanged;
                segment.Slot.Changed += OnSlotChanged;
            }

            ApplyFontToTemplate();

            mCollected = true;
            Log($"Configure -> {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");
            SegmentsChanged?.Invoke();
        }

        private static void DestroyImmediateSafe(GameObject go)
        {
            if (go == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }
#endif
            UnityEngine.Object.Destroy(go);
        }

        // ---------------- 存档 ----------------

        /// <summary>
        /// 生成按文本顺序保存的文档（每个固定文本段一条、每个空缺一条，空位也保留）。
        /// </summary>
        public TagFillDocument BuildDocument()
        {
            EnsureCollected();

            TagFillDocument document = new TagFillDocument
            {
                documentId = mDocumentId,
                version = 1
            };

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    TagKindSlot slot = segment.Slot;

                    document.items.Add(new TagFillItem
                    {
                        kind = TagFillItem.KindSlot,
                        slotId = slot.SlotId,
                        wordType = slot.WordType,
                        content = slot.Content,
                        tagId = slot.TagId,
                        filled = slot.IsFilled
                    });

                    document.slots.Add(new TagFillSlotEntry(slot.SlotId, slot.TagId, slot.IsFilled));
                }
                else
                {
                    document.items.Add(new TagFillItem
                    {
                        kind = TagFillItem.KindText,
                        content = segment.Text
                    });
                }
            }

            Log($"已生成文档，{document.SlotCount} 个空缺 / 已填 {document.FilledCount}");
            return document;
        }

        /// <summary>直接拿到 Json 字符串（可以丢进任何存档结构）。</summary>
        public string SaveToJson(bool pretty = true)
        {
            return BuildDocument().ToJson(pretty);
        }

        /// <summary>按 Json 字符串回填空缺。</summary>
        public bool LoadFromJson(string json)
        {
            if (!TagFillDocument.TryParse(json, out TagFillDocument document))
            {
                return false;
            }

            return ApplyDocument(document);
        }

        /// <summary>按文档回填空缺（先清空所有空缺，再按 tagId 重新填）。</summary>
        public bool ApplyDocument(TagFillDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EnsureCollected();
            ClearAll();

            ITagSystem tagSystem = TapTap.Interface.GetSystem<ITagSystem>();

            int applied = 0;

            foreach (TagFillSlotEntry entry in document.slots)
            {
                if (entry == null || !entry.filled || entry.tagId == 0)
                {
                    continue;
                }

                TagKindSlot slot = FindSlot(entry.slotId);

                if (slot == null)
                {
                    Debug.LogWarning($"[TagTextTemplate] 存档里的空缺 {entry.slotId} 在当前模版中不存在，跳过");
                    continue;
                }

                Tag_SO tag = tagSystem != null ? tagSystem.GetTagById(entry.tagId) : null;

                if (tag == null)
                {
                    Debug.LogWarning($"[TagTextTemplate] 找不到 tagId={entry.tagId} 的配置，跳过");
                    continue;
                }

                slot.Place(tag);
                applied++;
            }

            Log($"回填完成：{applied}/{document.SlotCount}");
            return true;
        }

        /// <summary>按 docId 找空缺。</summary>
        public TagKindSlot FindSlot(string slotId)
        {
            EnsureCollected();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot && segment.Slot.SlotId == slotId)
                {
                    return segment.Slot;
                }
            }

            return null;
        }

        // ---------------- 事件 ----------------

        private void OnSlotChanged(TagKindSlot slot)
        {
            // 刚填进去的 TagDisplay 是运行时实例化的，字体得补一次
            ApplyFontToSlot(slot);

            TypeEventSystem.Global.Send(new TagSlotFilledEvent
            {
                Template = this,
                Slot = slot
            });

            SegmentsChanged?.Invoke();
        }

        /// <summary>
        /// 解析当前生效的字体。
        ///
        /// 优先级：选中的预设（Inspector 里显式挑的那个）&gt; mFont（兜底 / 老预制体里本来就有的）&gt; TMP 默认字体。
        /// 预设越界（比如列表被删短了）、预设没配字体、mFontPresets 为 null 都安全退回，不会抛异常。
        /// </summary>
        public TMP_FontAsset ResolveFont()
        {
            if (mFontPresets != null &&
                mFontPresetIndex >= 0 &&
                mFontPresetIndex < mFontPresets.Count)
            {
                TagFontPreset preset = mFontPresets[mFontPresetIndex];

                if (preset != null && preset.font != null)
                {
                    return preset.font;
                }
            }

            return mFont;
        }

        /// <summary>
        /// 把当前生效的字体套到整棵模版下的所有 TMP 文字：
        /// 预制体里已有的固定文本、空缺的占位提示、以及运行时填进去的 TagDisplay。
        ///
        /// 这里只换字体与材质，不动颜色 —— 颜色仍由各节点自己 / ApplyTextStyle 决定。
        /// </summary>
        public void ApplyFontToTemplate()
        {
            ApplyFontToSubtree(transform);
        }

        /// <summary>只把字体套到某个空缺的子树（占位提示 + 已填入的显示件）。</summary>
        public void ApplyFontToSlot(TagKindSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            ApplyFontToSubtree(slot.transform);
        }

        private void ApplyFontToSubtree(Transform root)
        {
            if (root == null)
            {
                return;
            }

            TMP_FontAsset font = ResolveFont();

            if (font == null)
            {
                return;
            }

            // true：占位提示这类隐藏节点也要换，否则填上内容再清空又会露出旧字体
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                ApplyFont(text, font);
            }
        }

        /// <summary>
        /// 把生效字体（预设优先）+ 纯黑套到运行时生成的文字上。
        /// Configure / CreateSlot 现场造的 TMP 只能靠这里。
        /// </summary>
        private void ApplyTextStyle(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            ApplyFont(text, ResolveFont());
            text.color = mTextColor;
        }

        /// <summary>换字体并同步材质（材质还指着旧图集的话会显示成豆腐块）。</summary>
        private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
        {
            if (text == null || font == null)
            {
                return;
            }

            if (text.font != font)
            {
                text.font = font;
            }

            Material material = font.material;

            if (material != null &&
                (text.fontSharedMaterial == null || text.fontSharedMaterial.mainTexture != font.atlasTexture))
            {
                text.fontSharedMaterial = material;
            }
        }

        /// <summary>编辑器里手动把预设字体刷进预制体（右键组件菜单），省得进 Play 才看得到。</summary>
        [ContextMenu("把预设字体套到模版内文字")]
        private void ApplyFontToTemplateInEditor()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
                UnityEditor.Undo.RecordObjects(texts, "应用预设字体");
                ApplyFontToTemplate();

                foreach (TMP_Text text in texts)
                {
                    UnityEditor.EditorUtility.SetDirty(text);
                }

                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
#endif
            ApplyFontToTemplate();
        }

        private void Log(string message)
        {
            if (mLog)
            {
                Debug.Log($"[TagTextTemplate/{name}] {message}");
            }
        }
    }
}