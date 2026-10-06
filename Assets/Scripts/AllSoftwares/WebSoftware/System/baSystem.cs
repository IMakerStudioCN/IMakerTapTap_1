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



    }

    public class baSystem : AbstractSystem, IbaSystem
    {
        public List<baLineSingleSO> baLineSingleList;  //所有baLine的列表
        public List<baLineSingleSO> baYouHaveList;//玩家拥有的baLine的列表
        public List<int> baLineSingleIDList;//所有baLine的ID列表

        public void AddBaYouHave(int baID)
        {
            if(!ChickBaYouHave(baID))
            {
                baYouHaveList.Add(GetBaLineSingleSOByID(baID));
            }

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
        }

        public List<int> GetBaLineSingleIDList()
        {
            return baLineSingleIDList;
        }

        public List<baLineSingleSO> GetBaLineSingleList()
        {
            return baLineSingleList;
        }

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

        protected override void OnInit()
        {
        }
    }
    public class baSystemData
    {
        public List<baLineSingleSO> baYouHaveList;
        public List<int> baLineSingleIDList;
    }
}
