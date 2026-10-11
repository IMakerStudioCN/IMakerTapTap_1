using QFramework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IMybaSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IMybaSystem>(new MybaSystem());
    public interface IMybaSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public List<string> GetMyBaLineContentList();
        public List<int> GetdataList();
        public List<int> GetClickList();
        public List<int> GetRepliseList();
        //增删查改
        public void AddMyBaLineContent(string content);
        public void RemoveMyBaLineContent(string content);
        public string GetMyBaLineContent(int i);

        public void AddClick(int i);
        public void RemoveClick(int i);
        public void AddReplise(int i);
        public void RemoveReplise(int i);
        public bool checkMyba(int i);

        //myba回复信息列表
        public List<baLineSingleSO> GetmybaReplyesByID(int mybaID);
        public void AddmybaReply(int newsType, int mybaID, int nums);
        //获取不同种类的回复
        public baLineSingleSO GetBaLineByType(int newsType);//0为胡扯 1为邪恶 2为公正 3正常 4其他
        public baLineSingleSO GetbalaReply();
        public baLineSingleSO GetevilReply();
        public baLineSingleSO GetfairReply();
        public baLineSingleSO GetnormalReply();
        public baLineSingleSO GetotherReply();
    }

    public class MybaSystem : AbstractSystem, IMybaSystem
    {
        public class MybaReplseList
        {
            public List<baLineSingleSO> mybaReplse = new List<baLineSingleSO>();
        }
        #region 数据
        private ImyRepliesSystem myrepliesSystem;
        public List<string> myBaLineContentList = new List<string>();
        public List<int> dataList = new List<int>();
        public List<int> clickList = new List<int>();
        public List <int> repliseList = new List<int>();
        public List<MybaReplseList> mybaReplseLists = new List<MybaReplseList>();//分了5类来增加单条SO
        #endregion
        protected override void OnInit()
        {
            //防空的随机数
            System.Random rnd = new System.Random();
            //注册+初始化
            myrepliesSystem = this.GetSystem<ImyRepliesSystem>();
            myBaLineContentList = this.GetUtility<IJsonSaveUtility>().Get<mybaSystemData>("mybaSystemData").myBaLineContent;
            dataList = this.GetUtility<IJsonSaveUtility>().Get<mybaSystemData>("mybaSystemData").dataList;
            repliseList = this.GetUtility<IJsonSaveUtility>().Get<mybaSystemData>("mybaSystemData").repliesList;
            clickList = this.GetUtility<IJsonSaveUtility>().Get<mybaSystemData>("mybaSystemData").clickList;
            //获取mybaSendEvent
            this.RegisterEvent<mybaSendEvent>(e => 
            {
                myBaLineContentList.Add(e.content);
                if(e.data >0)
                    dataList.Add(e.data);
                else
                    dataList.Add(rnd.Next(1,32));

                if (e.click >= 0)
                    clickList.Add(e.click);
                else
                    clickList.Add(rnd.Next(1,101));
                if (e.reply >= 0)
                    repliseList.Add(e.reply);
                else
                    repliseList.Add(rnd.Next(1, 101));
            });
            //mybaReplySendEvent增加帖子回复
            this.RegisterEvent<mybaReplySendEvent>(e =>
            {
                AddmybaReply(e.NewsType, e.mybaID, e.nums);
            });
        }
        private void EnsureMybaReplySlot(int mybaID)
        {
            if (mybaID < 0) return;
            while (mybaReplseLists.Count <= mybaID)
                mybaReplseLists.Add(new MybaReplseList());
        }
        //获取各类的信息
        #region 获取各类的回复信息
        public baLineSingleSO GetbalaReply()
        {
            return myrepliesSystem.GetbalaReply();
        }
        public baLineSingleSO GetevilReply()
        {
            return myrepliesSystem.GetevilReply();
        }
        public baLineSingleSO GetfairReply()
        {
            return myrepliesSystem.GetfairReply();
        }
        public baLineSingleSO GetnormalReply()
        {
            return myrepliesSystem.GetnormalReply();
        }
        public baLineSingleSO GetotherReply()
        {
            return myrepliesSystem.GetotherReply();
        }
        #endregion

        //控制获取回复
        //在class中List中添加SO
        public void AddmybaReply(int newsType, int mybaID,int nums)
        {
            if (nums <= 0) return;
            EnsureMybaReplySlot(mybaID);

            int mainCount = nums / 2;

            for(int i = 0;i<mainCount;i++)
            {
                var so = GetBaLineByType(newsType);
                if (so != null) 
                    mybaReplseLists[mybaID].mybaReplse.Add(so);
            }
            System.Random rand = new System.Random();
            for(int i = 0;i<(nums - mainCount);i++)
            {
                mybaReplseLists[mybaID].mybaReplse.Add(GetBaLineByType(rand.Next(0,5)));
                mybaReplseLists[mybaID].mybaReplse.Add(GetBaLineByType(rand.Next(0,5)));
            }
            
            
        }
        public List<baLineSingleSO> GetmybaReplyesByID(int mybaID)
        {
            EnsureMybaReplySlot(mybaID);
            return mybaReplseLists[mybaID].mybaReplse;
        }

        //0为胡扯 1为邪恶 2为公正 3正常 4其他
        public baLineSingleSO GetBaLineByType(int newsType)
        {
            switch(newsType)
            {
                case 0:
                    return GetbalaReply();
                case 1:
                    return GetevilReply();
                case 2:
                    return GetfairReply();
                case 3:
                    return GetnormalReply();
                case 4:
                    return GetotherReply();
                default:
                    Debug.LogWarning("没有myba的回复没有添加成功");
                    return null;
            }
        }


        public List<int> GetdataList()
        {
            return dataList;
        }
        public List<int> GetClickList()
        {
            return clickList;
        }
        public List<int> GetRepliseList()
        {
            return repliseList;
        }
        public List<string> GetMyBaLineContentList()
        {
            return myBaLineContentList;
        }
        public string GetMyBaLineContent(int i)
        {
            return myBaLineContentList[i];
        }


        //增删查改
        public void AddMyBaLineContent(string content)
        {
            myBaLineContentList.Add(content);
        }

        public void RemoveMyBaLineContent(string content)
        {
            myBaLineContentList.Remove(content);
        }

        public void AddClick(int i)
        {
            clickList.Add(i);
        }
        public void RemoveClick(int i)
        {
            clickList.Remove(i);
        }
        public void AddReplise(int i)
        {
            repliseList.Add(i);
        }
        public void RemoveReplise(int i)
        { 
            repliseList.Remove(i);
        }
        public bool checkMyba(int i)
        {
            i = i + 1;
            return (clickList.Count >= i && repliseList.Count >= i &&myBaLineContentList.Count>=i);
        }


    }
    public class mybaSystemData
    {
        public List<string> myBaLineContent = new List<string>();
        public List<int> dataList = new List<int>();
        public List<int> clickList = new List<int> ();
        public List<int> repliesList = new List<int> ();

    }
}
