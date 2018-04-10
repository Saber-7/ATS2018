using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATS.View;
using ATS.Model;
using ATS.Helpers;
using System.Windows.Controls;
using System.Threading;
using System.Windows;


namespace ATS.ViewModel
{
    class PlanViewModel : ViewModelBase
    {
        public PlanViewModel()
        {
            InitCommand();
            InitPlans();
            //App.Current.Dispatcher.BeginInvoke(new Action(InitPlans));

        }

        private PlanModel pm;


        //存储计划内容
        private List<planitem> _planItemList = new List<planitem>();
        public List<planitem> PlanItemList
        {
            get { return _planItemList; }
            set 
            {
                if (value != _planItemList)
                {
                    _planItemList = value;
                    RaisePropertyChanged("PlanList");
                }
            }
        }





        /// <summary>
        /// 初始化计划
        /// </summary>
        void InitPlans()
        {
            if(pm==null) pm = new PlanModel();
            PlanItemList = (from c in pm.planitem
                                       select c).ToList();

        }

        void InitCommand()
        {
            SelectPlanChange = new RelayCommand(ItemSelectionChanged);
            SelectJiaoChange = new RelayCommand(JiaoSelectionChanged);
            OKCommand = new RelayCommand(OKmethod);
            ExitCommand = new RelayCommand(Exitmethod);
        }

        #region 选择表

        public RelayCommand SelectPlanChange { get; set; }
        
        //EditTable绑定对象
        List<selectjiao> _SJList;

        public List<selectjiao> SJList
        {
            get { return _SJList; }
            set {
                if (value != _SJList)
                {
                    _SJList = value; 
                    RaisePropertyChanged("SJList");
                } 
            }
        }

        private void ItemSelectionChanged(object obj)
        {
            int n = (int)obj; 
            if (n > -1)
            {
                SJList= PlanItemList[n].selectjiao.ToList();
                foreach (var item in SJList)
                {
                    //
                }
                if (SJList.First()!=null)
                PlanList = SJList.First().plan.ToList();
            }


        }



        #endregion

        #region 编辑表

        public RelayCommand SelectJiaoChange { get; set; }

        List<plan> _planList;

        public List<plan> PlanList
        {
            get { return _planList; }
            set {
                if (value != _planList)
                {
                    _planList = value;
                    RaisePropertyChanged("PlanList");
                } 

            }
        }
        private void JiaoSelectionChanged(object obj)
        {
            int n = (int)obj;
            if (n > -1&&n<SJList.Count)
            {
                PlanList= SJList[n].plan.ToList();
            }


        }

        #endregion

        #region 详细表

        #endregion

        #region 确定命令
        public RelayCommand OKCommand { get; set; }
        public RelayCommand ExitCommand { get; set; }

        public Action<List<selectjiao>> UpdateRunChartAction { get; set; }
        public void OKmethod(object obj)
        {
            if (SJList == null)
            {
                MessageBox.Show("请选择计划后确定");
            }
            else
            {
                PlanWindow pw = (PlanWindow)obj;
                //先隐藏完事再退出防止卡
                pw.Visibility = Visibility.Hidden;
                UpdateRunChartAction(SJList);
                pm.SaveChanges();
                pm.Dispose();
                pw.Close();
            }

        }

        public void Exitmethod(object obj)
        {
            pm.Dispose();
            PlanWindow pw = (PlanWindow)obj;
            pw.Close();
        }

        #endregion


    }
}
