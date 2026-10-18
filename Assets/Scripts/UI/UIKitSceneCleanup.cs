using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapTapFirst
{
    /// <summary>
    /// 场景切换后清理 UIKit.Table 里已经销毁的面板条目。
    ///
    /// 背景：UIRoot 是放在场景里的对象（Start / GamePlay / Test 各一份），切场景时它和挂在下面的面板会被销毁，
    /// 但 UIKit.Table 是静态表、只在 OnApplicationQuit 清，于是表里留下已销毁的 IPanel。
    /// 下次 OpenPanel 命中这个死条目时，UIPanel.Show() 里的 gameObject 访问会抛 MissingReferenceException。
    ///
    /// 这里只删“已销毁”的条目，活着的面板（包括 SetActive(false) 的）一律不动；
    /// 也不能用 UIKit.Table.Remove(panel)，它取 key 时会访问 panel.Transform，对已销毁对象会再抛一次异常。
    /// </summary>
    public static class UIKitSceneCleanup
    {
        static bool s_Subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (s_Subscribed) return;

            s_Subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            Cleanup();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Cleanup();
        }

        static void OnSceneUnloaded(Scene scene)
        {
            Cleanup();
        }

        public static void Cleanup()
        {
            var dead = new HashSet<IPanel>();

            CollectDead(UIKit.Table.GameObjectNameIndex.Dictionary, dead);
            CollectDead(UIKit.Table.TypeIndex.Dictionary, dead);

            if (dead.Count == 0) return;

            foreach (var panel in dead)
            {
                if (panel.Info != null && !panel.Info.IsRecycled)
                {
                    panel.Info.Recycle2Cache();
                }

                panel.Info = null;
            }

            RemoveDead(UIKit.Table.GameObjectNameIndex.Dictionary, dead);
            RemoveDead(UIKit.Table.TypeIndex.Dictionary, dead);

            Debug.Log("[UIKitSceneCleanup] 已清理 " + dead.Count + " 个已销毁的面板条目");
        }

        static void CollectDead<TKey>(IDictionary<TKey, List<IPanel>> index, HashSet<IPanel> dead)
        {
            foreach (var pair in index)
            {
                var panels = pair.Value;

                for (var i = 0; i < panels.Count; i++)
                {
                    var panel = panels[i];

                    if (panel is UnityEngine.Object unityObject && unityObject == null)
                    {
                        dead.Add(panel);
                    }
                }
            }
        }

        static void RemoveDead<TKey>(IDictionary<TKey, List<IPanel>> index, HashSet<IPanel> dead)
        {
            foreach (var pair in index)
            {
                var panels = pair.Value;

                for (var i = panels.Count - 1; i >= 0; i--)
                {
                    if (dead.Contains(panels[i]))
                    {
                        panels.RemoveAt(i);
                    }
                }
            }
        }
    }
}
