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
        baLineList_SO GetbaLineListByID(int baListID);
        //列表的增删查改
        baLineList_SO GetbaLineListByIDInYouHave(int baListID);
        void AddbaLineList(int baListID);
        void DelbaLineList(int baListID);
        bool CheckbaLineListYouHave(int baListID);
        bool CheckbaLineList(int baListID);

    }

    public class baListSystem : AbstractSystem, IbaListSystem
    {
        #region 元素
        public List<baLineList_SO> AllbaList;
        public List<baLineList_SO> baLineListYouHave;

        #endregion
        protected override void OnInit()
        {

        }
        public List<baLineList_SO> GetAllbaList()
        {
            return AllbaList;
        }
        public List<baLineList_SO> GetbaLineListYouHave()
        {
            return baLineListYouHave;
        }

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
                return baLineListYouHave.FirstOrDefault(x => x.listID == baListID);
            }
            Debug.LogWarning("所有表中没有这个ba表");
            return null;
        }

        public void AddbaLineList(int baListID)
        {
            if(CheckbaLineList(baListID))
            {
                baLineListYouHave.Add(GetbaLineListByID(baListID));
            }
           
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
    }
}
