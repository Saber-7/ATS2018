using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using 线路绘图工具;

namespace ATS
{
    public class Section : 线路绘图工具.Section, IFlash, IRailway
    {
        public static int StartByte { get; set; }

        static Pen DefaultPen_ = new Pen(Brushes.Cyan, 3);
        static Pen OccupyPen_ = new Pen(Brushes.Red, 3);
        static Pen RouteLockPen_ = new Pen(Brushes.White, 3);
        static Pen ProtectPen_ = new Pen(Brushes.Yellow, 3);
        static Pen BlockPen_ = new Pen(Brushes.Red, 7);

        public static Pen DefaultPen { get { return DefaultPen_; } }
        public static Pen OccupyPen { get { return OccupyPen_; } }
        public static Pen RouteLockPen { get { return RouteLockPen_; } }
        public static Pen BlockPen { get { return BlockPen_; } }
        public static Pen ProtectPen { get { return ProtectPen_; } }

        public List<Graphic> Graphics
        {
            get { return graphics_; }
            set { value = graphics_; }
        }
        #region 彭亚枫添加部分

       // /// <summary>
       // /// 配置带箭头的线
       // /// </summary>
        public Section()
        {
            //RouteLockPen_.EndLineCap = PenLineCap.Triangle;
            //RouteLockPen_.StartLineCap = PenLineCap.Triangle;
            //RouteLockPen_.Thickness = 10;
            DirVectorList = new List<Vector>();

            CreateTriangle();
            
        }

        void CreateDirVectors()
        {
            foreach (var item in graphics_)
            {
                Line l = item as Line;
                Vector vector = new Vector(l.Pt1.X - l.Pt0.X, l.Pt1.Y - l.Pt0.Y);
                vector.Normalize();
                DirVectorList.Add(vector);
            }
            Direction = DefaultDirection.UpWard;
        }

        /// <summary>
        /// 定义三角形及相关
        /// </summary>
        PathGeometry Triangle;
        double[,] points = { { 12, 0 }, { 0, 6 }, { 0, -6 } };
        void CreateTriangle()
        {
            Triangle = new PathGeometry();
            List<Point> pointList=new List<Point>();
            for(int i=0;i<points.Length/2;i++) pointList.Add(new Point(points[i,0],points[i,1]));
            PathFigure pf = new PathFigure();
            pf.IsClosed = true;
            pf.StartPoint = pointList[0];
            for (int i = 1; i < pointList.Count; i++) pf.Segments.Add(new LineSegment(pointList[i],true));
            Triangle.Figures.Add(pf);
        }

        //public void SetDir(DefaultDirection dir)
        //{
        //    if (dir == Direction && dir != null)
        //    {

        //    }
        //    else if (dir != null)
        //    {
        //        for (int i = 0; i < DirVectorList.Count; i++)
        //        {
        //            DirVectorList[i].Negate();
        //        }
        //        DirVectorList.Reverse();
        //    }
        //    Direction = dir;
        //}

        //0直道长度 1斜线长度
        double[] lens = new double[2];
        public double[] Lens
        {
            get { return lens; }
            set { lens = value; }
        }

        //1表示上行，0表示下行
        //public int Direction { get; set; } 
        //Direc

        public List<Vector> DirVectorList { get; set; }

        #endregion
        int startByte_;
        //RightButtonMenuHandler handler_;

        public bool IsStatusChanged { get; set; }

        bool isOccupied_ = true;
        bool IsOccupied
        {
            get { return isOccupied_; }
            set
            {
                if (isOccupied_ != value)
                {
                    isOccupied_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        bool isRouteLock_ = true;
        bool IsRouteLock
        {
            get { return isRouteLock_; }
            set
            {
                if (isRouteLock_ != value)
                {
                    isRouteLock_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        bool isBlocked_ = true;
        bool IsBlocked
        {
            get { return isBlocked_; }
            set
            {
                if (isBlocked_ != value)
                {
                    isBlocked_ = value;
                    IsStatusChanged = true;
                }
            }
        }


        bool isProtected_ = true;
        bool IsProtected
        {
            get { return isProtected_; }
            set
            {
                if (isProtected_ != value)
                {
                    isProtected_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        public bool IsFlashing { get; set; }
        public bool FlashFlag { get; set; }
        public bool FlashNameFlag { get; set; }
        public bool IsFailed { get { return false; } }

        public Graphic InsuLine { get; set; }


        /// <summary>
        /// 产生变换矩阵
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        Matrix CreateMatrix(Line line)
        {
            double offsetX = line.Pt0.X / 2 + line.Pt1.X / 2;
            double offsetY = line.Pt0.Y / 2 + line.Pt1.Y / 2;
            Matrix matrix = Matrix.Identity;
            int n = graphics_.IndexOf(line);
            Vector tv=Direction==DefaultDirection.UpWard?new Vector(-1,0):new Vector(1,0);
            double cos=tv*DirVectorList[n];
            //可以这样算是个意外
            double angle = (System.Math.Atan2(DirVectorList[n].Y, DirVectorList[n].X) - System.Math.Atan2(tv.Y, tv.X));
            angle *= 180 / System.Math.PI;
            matrix.Rotate(angle);
            matrix.Translate(offsetX, offsetY);
            return matrix;
        }
        

        protected override void OnRender(DrawingContext dc)
        {
            if (DirVectorList.Count == 0) CreateDirVectors();
            foreach (Line line in graphics_)
            {
                if (IsBlocked)
                {
                    line.OnRender(dc, BlockPen_);
                }
                int n = graphics_.IndexOf(line);
                line.OnRender(dc, IsOccupied ? OccupyPen_ :
                    (IsRouteLock ? RouteLockPen_ :
                    (IsProtected ? ProtectPen_ : DefaultPen_)));
                //画箭头
                if (IsRouteLock&&!IsProtected&&!IsOccupied&&graphics_.Count>0)
                {   
                    Triangle.Transform = new MatrixTransform(CreateMatrix(line));
                    dc.DrawGeometry(Brushes.White,RouteLockPen_,Triangle);
                }
            }

            if (!FlashNameFlag)
            {
                dc.DrawText(formattedName_, namePoint_);
            }

            //分割区段
            if (InsuLine is Line)
            {
                (InsuLine as Line).OnRender(dc, Section.DefaultPen);
            }
        }

        public void UpdateStatus(byte[] recvBuf, int nRecv)
        {
            const byte TRUE_VALUE = 0X05;
            IsRouteLock = (recvBuf[startByte_] & 0x0f) == TRUE_VALUE;
            IsBlocked = (recvBuf[startByte_] >> 4) == TRUE_VALUE;
            IsProtected = (recvBuf[startByte_ + 1] & 0x0f) == TRUE_VALUE;
            IsOccupied = (recvBuf[startByte_ + 1] >> 4) == TRUE_VALUE;
            Direction=(recvBuf[startByte_+2]& 0x0f)==(byte)(TRUE_VALUE)?DefaultDirection.UpWard:DefaultDirection.DownWard;
        }

        public void SetStartByte(int ID)
        {
            startByte_ = StartByte + ID * 3;
            AddInsulation();
        }

        public void ResetDefaultStatus()
        {
            isOccupied_ = true;
            isRouteLock_ = true;
            isBlocked_ = true;
        }
    
        public void AddInsulation()
        {
            List<Point> leftPts = new List<Point>();
            //GetLeftPoints(leftPts);

            //处理区段分割的问题
            Line l = this.Graphics[0] as Line;
            leftPts.Add(l.Pt0);


            InsuLine = new Line()
            {
                Pt0 = new Point(leftPts[0].X, leftPts[0].Y - Line.LineThickness),
                Pt1 = new Point(leftPts[0].X, leftPts[0].Y + Line.LineThickness)
            };
        }

        public override string ToString()
        {
            return string.Format("{0, -16} [ 占用: {1}, 进路锁: {2}, 封锁: {3}, 保护: {4} ]", name_,
                isOccupied_ ? "是" : "否", 
                isRouteLock_ ? "是" : "否", 
                isBlocked_ ? "是" : "否",
                isProtected_ ? "是" : "否");
        }
    }
}
