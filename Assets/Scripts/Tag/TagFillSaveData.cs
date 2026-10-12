using System;

namespace TapTapFirst
{
    /// <summary>
    /// 文本填空的存档载体：本体就是一段 Json 字符串（按文本顺序的 items 列表）。
    /// 在 TapTap.Init() 里 save.Add&lt;TagFillSaveData&gt;() 注册后即可用。
    /// </summary>
    [Serializable]
    public class TagFillSaveData
    {
        /// <summary>多个模版时用不同 key：DocumentId -> Json。</summary>
        public System.Collections.Generic.List<TagFillEntryData> documents =
            new System.Collections.Generic.List<TagFillEntryData>();

        public void Set(string documentId, string json)
        {
            TagFillEntryData entry = Find(documentId);

            if (entry == null)
            {
                documents.Add(new TagFillEntryData(documentId, json));
                return;
            }

            entry.json = json;
        }

        public string Get(string documentId)
        {
            TagFillEntryData entry = Find(documentId);
            return entry != null ? entry.json : null;
        }

        public bool TryGet(string documentId, out string json)
        {
            json = Get(documentId);
            return !string.IsNullOrEmpty(json);
        }

        public bool Remove(string documentId)
        {
            TagFillEntryData entry = Find(documentId);
            return entry != null && documents.Remove(entry);
        }

        private TagFillEntryData Find(string documentId)
        {
            for (int i = 0; i < documents.Count; i++)
            {
                if (documents[i] != null && documents[i].documentId == documentId)
                {
                    return documents[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public class TagFillEntryData
    {
        public string documentId;
        public string json;

        public TagFillEntryData() { }

        public TagFillEntryData(string documentId, string json)
        {
            this.documentId = documentId;
            this.json = json;
        }
    }

    /// <summary>某个空缺被填上/清空时发出，TagSystem 用它判断"整张模版是否填满"。</summary>
    public struct TagSlotFilledEvent
    {
        public TagTextTemplate Template;
        public TagKindSlot Slot;
    }
}