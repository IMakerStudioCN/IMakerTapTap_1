using QFramework;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TapTapFirst
{
    // 已生成模块接口 IbaListSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IbaListSystem>(new baListSystem());
    public interface IbaListSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        //获取列表
        List<baLineList_SO> GetAllbaList();
        List<baLineList_SO> GetbaLineListYouHave();
        public List<baLineList_SO> GetIsCheckbaList();
        baLineList_SO GetbaLineListByID(int baListID);
        //拥有列表的增删查改
        baLineList_SO GetbaLineListByIDInYouHave(int baListID);
        void AddbaLineList(int baListID);
        void DelbaLineList(int baListID);
        bool CheckbaLineListYouHave(int baListID);
        bool CheckbaLineList(int baListID);
        //在已有的增加点击的
        void AddIsCheckbaList(int baListID);
    }

    public class baListSystem : AbstractSystem, IbaListSystem
    {
        #region 元素
        private IJsonSaveUtility mJsonSaveUtility => this.GetUtility<IJsonSaveUtility>();
        private ResLoader mResLoader = ResLoader.Allocate();
        public List<baLineList_SO> AllbaList = new List<baLineList_SO>();
        public List<baLineList_SO> baLineListYouHave = new List<baLineList_SO>();
        public List<baLineList_SO> isCheckbaList;
        #endregion
        protected override void OnInit()
        {
            //注册+初始化
            AllbaList = mResLoader.LoadSync<baListList_SO>("baListList").baLineList;
            baLineListYouHave = mJsonSaveUtility.Get<baListSystemData>("baListSystemData").baLineListYouHave;
            if (baLineListYouHave == null)
                Debug.LogWarning("kong");
            isCheckbaList = mJsonSaveUtility.Get<baListSystemData>("baListSystemData").isCheckbaList;
            //订阅
            this.RegisterEvent<SendBaEvent>(e =>
            {
                AddbaLineList(e.baID);
            });
        }
        public List<baLineList_SO> GetIsCheckbaList()
        {
            return isCheckbaList;
        }
        public List<baLineList_SO> GetAllbaList()
        {
            return AllbaList;
        }
        public List<baLineList_SO> GetbaLineListYouHave()
        {
            return baLineListYouHave;
        }
        //得到存SO的列表SO文件
        public baLineList_SO GetbaLineListByIDInYouHave(int baListID)
        {
            if(CheckbaLineListYouHave(baListID))
            {
                return baLineListYouHave.FirstOrDefault(x => x.listID == baListID);
            }
            Debug.LogWarning("拥有的表中没有这个ba表");
            return null;
        }
        public baLineList_SO GetbaLineListByID(int baListID)
        {
            if (CheckbaLineList(baListID))
            {
                return AllbaList.FirstOrDefault(x => x.listID == baListID);
            }
            Debug.LogWarning("所有表中没有这个ba表");
            return null;
        }

        public void AddbaLineList(int baListID)
        {
            if(!CheckbaLineList(baListID))
                return;
            if(!CheckbaLineListYouHave(baListID))
                baLineListYouHave.Add(GetbaLineListByID(baListID));
            
        }

        public void DelbaLineList(int baListID)
        {
            if (CheckbaLineListYouHave(baListID))
            {
                baLineListYouHave.Remove(GetbaLineListByID(baListID));
            }
            else
            {
                Debug.LogWarning("不要重复删除");
            }
        }

        public bool CheckbaLineListYouHave(int baListID)
        {
            return baLineListYouHave.FirstOrDefault(x => x.listID == baListID) != null;
        }
        public bool CheckbaLineList(int baListID)
        {
            return AllbaList.FirstOrDefault(x => x.listID == baListID) != null;
        }
        public void AddIsCheckbaList(int baListID)
        {
            if (isCheckbaList.FirstOrDefault(x => x.listID == baListID) == null)
            {
                isCheckbaList.Add(GetbaLineListByIDInYouHave(baListID));
            }
            else
            {
                Debug.LogWarning("传入的点击列表重复了");
            }
        }
    }
    public class baListSystemData
    {
        public List<baLineList_SO> baLineListYouHave = new List<baLineList_SO>();
        public List<baLineList_SO> isCheckbaList = new List<baLineList_SO>();
    }
}
