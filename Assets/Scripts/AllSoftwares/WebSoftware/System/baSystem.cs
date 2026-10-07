using QFramework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IbaSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IbaSystem>(new baSystem());
    public interface IbaSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        List<baLineSingleSO> GetBaLineSingleList();  //所有baLine的列表
        List<baLineSingleSO> GetBaYouHaveList();//玩家拥有的baLine的列表
        List<int> GetBaLineSingleIDList();//所有baLine的ID列表
        //吧贴列表的增删查改：针对玩家拥有的BaYouHaveList
        baLineSingleSO GetBaLineSingleSOByID(int baID);//通过ID获取baLineso
        void AddBaYouHave(int baID);//添加baLineSO
        void DelBaYouHave(int baID);//删除baLineSO
        bool ChickBaYouHave(int baID);//查看是否有baLineSO

        baLineSingleSO GetYouHaveListSOByBaIDIn(int baID);


    }

    public class baSystem : AbstractSystem, IbaSystem
    {
        private ResLoader mResLoader = ResLoader.Allocate();
        public List<baLineSingleSO> baLineSingleList = new List<baLineSingleSO>();  //所有baLine的列表
        public List<baLineSingleSO> baYouHaveList = new List<baLineSingleSO>();//玩家拥有的baLine的列表
        public List<int> baLineSingleIDList = new List<int>();//所有baLine的ID列表

        private void UpdataSave()
        {
            baYouHaveList = this.GetUtility<IJsonSaveUtility>().Get<baSystemData>("baSystemData").baYouHaveList;
        }

        public void AddBaYouHave(int baID)
        {
            if(!ChickBaYouHave(baID))
            {
                baYouHaveList.Add(GetBaLineSingleSOByID(baID));
                //this.GetUtility<IJsonSaveUtility>().Get<baSystemData>("baSystemData").baYouHaveList.Add(GetBaLineSingleSOByID(baID));
                UpdataSave();
                return;
            }
            Debug.LogWarning("失败");
        }

        public bool ChickBaYouHave(int baID)
        {
            return baYouHaveList.Contains(GetBaLineSingleSOByID(baID));
        }

        public void DelBaYouHave(int baID)
        {
            if(!ChickBaYouHave(baID))
            {
                Debug.LogWarning("不要重复删除");
                return;
            }
            baYouHaveList.Remove(GetBaLineSingleSOByID(baID));
            //this.GetUtility<IJsonSaveUtility>().Get<baSystemData>("baSystemData").baYouHaveList.Remove(GetBaLineSingleSOByID(baID));
            UpdataSave();
        }

        public List<int> GetBaLineSingleIDList()
        {
            return baLineSingleIDList;
        }

        public List<baLineSingleSO> GetBaLineSingleList()
        {
            return baLineSingleList;
        }
        //you BUG
        public baLineSingleSO GetBaLineSingleSOByID(int baID)
        {
            if(!baLineSingleIDList.Contains(baID))
            {
                Debug.LogWarning("baID列表中没有这个");
                return null;
            }

            return baLineSingleList[baLineSingleIDList.IndexOf(baID)];
        }

        public List<baLineSingleSO> GetBaYouHaveList()
        {
            return baYouHaveList;
        }
        public baLineSingleSO GetYouHaveListSOByBaIDIn(int baID)
        {
            if(ChickBaYouHave(baID))
            {
                return GetBaLineSingleSOByID(baID);
            }
            else
            {
                Debug.LogWarning("你没有这个帖");
                return null;
            }
        }
        protected override void OnInit()
        {
            baLineSingleList = mResLoader.LoadSync<baLineList_SO>("baLineList").baLineList;
            baYouHaveList = this.GetUtility<IJsonSaveUtility>().Get<baSystemData>("baSystemData").baYouHaveList;
            baLineSingleIDList = this.GetUtility<IJsonSaveUtility>().Get<baSystemData>("baSystemData").baLineSingleIDList;
            if (baLineSingleIDList.Count == 0)
            {
                foreach(var i in baLineSingleList)
                {
                    baLineSingleIDList.Add(i.baID);
                }
            }
            this.RegisterEvent<SendBaEvent>(e =>
            {
                AddBaYouHave(e.baID);
            });
        }
    }
    public class baSystemData
    {
        public List<baLineSingleSO> baYouHaveList = new List<baLineSingleSO>();
        public List<int> baLineSingleIDList = new List<int>();
    }
}
