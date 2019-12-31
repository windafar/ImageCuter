using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageBasic;
using VisualAttentionDetection;

namespace CutImage
{
    /// <summary>
    /// 自定义像素点，包含了选定区域判定
    /// </summary>
    public struct us_PixlPoint
    {
        /// <summary>
        /// 位置
        /// </summary>
        public Point point;
        /// <summary>
        /// 是否被检查过（未用）
        /// </summary>
        public bool IsChecked;
        /// <summary>
        /// 区域标记号
        /// </summary>
        public int AreaNum;

        public byte value;
    }
    /// <summary>
    /// 用于区域统计的数组元素,使用区域号来索引计数属性
    /// </summary>
    public struct us_AreaCount
    {
        public int AreaNumber;
        public int Count;
    }

    public class CutImageClass:IDisposable
    {
        #region ... private var ...
        /// <summary>
        /// 剪切的尺寸
        /// </summary>
        Rectangle CutRect;
        /// <summary>
        /// 有效区域范围（0-255）
        /// </summary>
        int Tolerance;
        unsafe byte* srcP, dstP;
        /// <summary>
        /// 最大值区域的最大值 (public get)
        /// </summary>
        int MaxAreaArrValue=-1;
        /// <summary>
        /// 寻找连续区域时的需要的width
        /// </summary>
        int StrideInFindArea=-1;
        /// <summary>
        /// 寻找连续区域时的最大区域号
        /// </summary>
        int CurAreaNumberInFindArea=-1;
        #endregion

        #region ... public func...
        /// <summary>
        /// 初始化该切图类
        /// </summary>
        /// <param name="srcBitmap">原图</param>
        /// <param name="CutRect">切割大小</param>
        /// <param name="Tolerance">亮度容差，用于寻找有效区域，如果该值太小则会造成区域太多而溢出区域统计数组（默认3000个）</param>
        public CutImageClass(Bitmap srcBitmap, Rectangle CutRect, int Tolerance = 200)
        {

            this.srcBitmap = srcBitmap;
            if (Tolerance > 255 || Tolerance < 0) throw new ArgumentException();
            this.Tolerance = Tolerance;
            this.CutRect = CutRect;
            curDestImageWidth = srcBitmap.Width;
            curDestImageHeight = srcBitmap.Height;
            AreaArr = new us_AreaCount[24000];//默认定义区域数量
        }

        public void InitVisualAttentionBitmap()
        {
            if (this.CurDestBitmap != null) this.CurDestBitmap= VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(this.CurDestBitmap);
            else this.CurDestBitmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
        }

        /// <summary>
        /// 获取一个最具显著性的源区域
        /// </summary>
        /// <param name="FinSourceImage">源图</param>
        /// <returns></returns>
        public Bitmap GCSsimp_getLightRegionFromSource(Bitmap FinSourceImage)
        {
            int DistWidth = CutRect.Width;
            int DistHeight = CutRect.Height;
            BitmapData srcBmData = CurDestBitmap.LockBits(new Rectangle(0, 0, curDestImageWidth, curDestImageHeight),
                      ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            IntPtr srcScan = srcBmData.Scan0;
            unsafe
            {

                srcP = (byte*)srcScan;
                int index = 0;
                if (fx == null)
                    FindArea(srcBmData);
                // if (MaxAreaArrValue == 0) throw new ArgumentException("Tolerance过低");
                int x_start = curDestImageWidth, x_end = 0, y_start = curDestImageHeight, y_end = 0;
                for (int y = 1; y < curDestImageHeight - 1; y++)
                {
                    index = y * srcBmData.Stride;
                    for (int x = 1; x < curDestImageWidth - 1; x++)
                    {
                        int Sx = fx[index].AreaNum;
                        //  if (Sx != 0) throw new Exception();
                        if (Sx == MAXAreaArrNumbler)
                        {
                            if (x_start > fx[index].point.X) x_start = fx[index].point.X;
                            if (x_end < fx[index].point.X) x_end = fx[index].point.X;
                            if (y_start > fx[index].point.Y) y_start = fx[index].point.Y;
                            if (y_end < fx[index].point.Y) y_end = fx[index].point.Y;
                        }
                        index++;
                    }
                }
                double _xm = (x_end + x_start) / 2.0;
                double _ym = (y_end + y_start) / 2.0;
                double _xs, _ys, _xe, _ye;
                double FinWidth = FinSourceImage.Width, FinHeight = FinSourceImage.Height;
                //projection by ratio
                _xm = _xm * FinWidth / curDestImageWidth;
                _ym = _ym * FinHeight / curDestImageHeight;
                //get rect of objective
                _xs = _xm - DistWidth / 2.0;
                _xe = _xm + DistWidth / 2.0;
                _ys = _ym - DistHeight / 2.0;
                _ye = _ym + DistHeight / 2.0;
                //cliping by rect
                if (_xs < 0)
                {
                    _xe += -_xs;
                    _xs = 0;
                    if (_xe > FinWidth + 0.01) throw new ArgumentException("cutting size is larger than source map");
                }
                else if (_xe > FinWidth)
                {
                    _xs -= _xe - FinWidth;
                    _xe = FinWidth;
                    if (_xs < -0.01) throw new ArgumentException("cutting size is larger than source map");
                }

                if (_ys < 0)
                {
                    _ye += -_ys;
                    _ys = 0;
                    if (_ye > FinHeight + 0.01) throw new ArgumentException("cutting size is larger than source map");
                }
                else if (_ye > FinHeight)
                {
                    _ys -= _ye - FinHeight;
                    _ye = FinHeight;
                    if (_ys < -0.01) throw new ArgumentException("cutting size is larger than source map");
                }

                CurDestBitmap.UnlockBits(srcBmData);
                CurDestBitmap.Dispose();
                CurDestBitmap = BasicMethodClass.CutImage(FinSourceImage, (int)_xs, (int)_ys, (int)_xe - (int)_xs, (int)_ye - (int)_ys);
            }
            return CurDestBitmap;
        }
        /// <summary>
        /// 根据初始化的Tolerance参数寻找各个连续区域，并标号,返回新的fx数组
        /// </summary>
        /// <param name="srcBmData"></param>
        /// <returns></returns>
        public unsafe void FindArea(BitmapData srcBmData)
        {
            int index = 0;
            StrideInFindArea = srcBmData.Stride;
            fx = new us_PixlPoint[StrideInFindArea * curDestImageHeight];
            srcP = (byte*)srcBmData.Scan0;
            //记录最大区域大小
            MaxAreaArrValue = 0;
            //记录最大区域的区域号
            MAXAreaArrNumbler = 0;
            //记录当前区域编号的使用值
            CurAreaNumberInFindArea = 0;
            for (int y = 1; y < curDestImageHeight - 2; y++)
            {
                index = y * StrideInFindArea;
                for (int x = 1; x < curDestImageWidth - 2; x++)
                {
                    fx[index].value = srcP[index];
                    fx[index].point = new Point(x, y);
                    if (fx[index].value > Tolerance)//在此容差范围内进行计数统计
                    {
                        if (!fx[index].IsChecked)
                        {
                            CurAreaNumberInFindArea++;
                            _FindAreaSeries(index);
                        }
                        if (fx[index].IsChecked)
                        {
                            //try
                            //{
                            int tem = ++(AreaArr[fx[index].AreaNum].Count);//区域统计
                            AreaArr[fx[index].AreaNum].AreaNumber = fx[index].AreaNum;
                            if (fx[index].AreaNum == 0) throw new Exception("存在区域未被标记");
                            //尝试使用areaNumber替代MaxAreaArrValue（因为最后结果意外，，2016年9月7日17:18:32）
                            if (MaxAreaArrValue < tem)
                            {
                                MaxAreaArrValue = tem;
                                MAXAreaArrNumbler = fx[index].AreaNum;
                            }
                            //    }
                            //catch (System.IndexOutOfRangeException e)
                            //{
                            //}
                        }
                    }
                    index++;
                }
            }
        }
        /// <summary>
        /// 这是一个测试函数，可以根据标好号后的us_PixlPoint[]标识出连续的前十区域
        /// </summary>
        /// <param name="AreaArr">区域统计数组</param>
        /// <param name="fx">标好号后的自定义图片数据</param>
        /// <param name="dstBitmapData">目标图片BitmapData</param>
        /// <param name="srcBitmapData">源图片BitmapData</param>
        /// <returns></returns>
        public static BitmapData DrawingArea(us_AreaCount[] AreaArr, us_PixlPoint[] fx, BitmapData dstBitmapData, BitmapData srcBitmapData)
        {
            int dstindex = 0, srcindex = 0;
            int bitWidth = dstBitmapData.Stride;
            int width = srcBitmapData.Width;
            int height = srcBitmapData.Height;
            int srcStride = srcBitmapData.Stride;
            var AreaArrTemp = AreaArr.OrderByDescending(x => x.Count).Take(10).ToArray();
            unsafe
            {
                byte* dstP = (byte*)(void*)dstBitmapData.Scan0;
                for (int y = 1; y < height - 1; y++)
                {
                    dstindex = y * bitWidth;
                    srcindex = y * srcStride;
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (fx[srcindex].AreaNum == AreaArrTemp[0].AreaNumber)
                        {
                            dstP[dstindex] = 241;
                            dstP[dstindex + 1] = 153;
                            dstP[dstindex + 2] = 95;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[1].AreaNumber)
                        {
                            dstP[dstindex] = 254;
                            dstP[dstindex + 1] = 231;
                            dstP[dstindex + 2] = 0;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[2].AreaNumber)
                        {
                            dstP[dstindex] = 131;
                            dstP[dstindex + 1] = 196;
                            dstP[dstindex + 2] = 125;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[3].AreaNumber)
                        {
                            dstP[dstindex] = 0;
                            dstP[dstindex + 1] = 167;
                            dstP[dstindex + 2] = 174;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[4].AreaNumber)
                        {
                            dstP[dstindex] = 56;
                            dstP[dstindex + 1] = 133;
                            dstP[dstindex + 2] = 199;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[5].AreaNumber)
                        {
                            dstP[dstindex] = 254;
                            dstP[dstindex + 1] = 231;
                            dstP[dstindex + 2] = 0;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[6].AreaNumber)
                        {
                            dstP[dstindex] = 131;
                            dstP[dstindex + 1] = 105;
                            dstP[dstindex + 2] = 175;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[7].AreaNumber)
                        {
                            dstP[dstindex] = 254;
                            dstP[dstindex + 1] = 231;
                            dstP[dstindex + 2] = 0;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[8].AreaNumber)
                        {
                            dstP[dstindex] = 156;
                            dstP[dstindex + 1] = 1;
                            dstP[dstindex + 2] = 131;
                        }
                        else if (fx[srcindex].AreaNum == AreaArrTemp[9].AreaNumber)
                        {
                            dstP[dstindex] = 197;
                            dstP[dstindex + 1] = 13;
                            dstP[dstindex + 2] = 76;
                        }
                        dstindex += 3;
                        srcindex++;
                    }

                }
            }
            return dstBitmapData;
        }

        #endregion

        #region ... public var ...
        public readonly Bitmap srcBitmap;
        /// <summary>
        /// 当前curDestImage的宽度，高度
        /// </summary>
        public int curDestImageWidth, curDestImageHeight;
        /// <summary>
        /// 自定义的图片数据
        /// </summary>
        public us_PixlPoint[] fx;
        /// <summary>
        /// 最大区域的最大值的区域号(public get)
        /// </summary>
        public int MAXAreaArrNumbler = -1;
        /// <summary>
        /// 预定义的区域统计数组（目前该数组是固定的）
        /// </summary>
        public us_AreaCount[] AreaArr;
        /// <summary>
        /// 对当前处理结果的保存
        /// </summary>
        public Bitmap CurDestBitmap;
#endregion

        #region ...private fun...
        /// <summary>
        /// 递归寻找一个连续区域
        /// </summary>
        /// <param name="index">寻找开始位置</param>
        /// <remarks>这个方法并没使用，留着以后作改写成图遍历的参考</remarks>
        unsafe void FindAreaSeries(int index)
        {
            fx[index].IsChecked = true;
            fx[index].AreaNum = CurAreaNumberInFindArea;
            if (index - 1 > 0 && srcP[index - 1] > Tolerance && !fx[index - 1].IsChecked)
                FindAreaSeries(index - 1);//判断条件的可以事先优化，下同
            if (index + 1 < StrideInFindArea * curDestImageHeight && srcP[index + 1] > Tolerance && !fx[index + 1].IsChecked)
                FindAreaSeries(index + 1);
            if (index + StrideInFindArea < StrideInFindArea * curDestImageHeight && srcP[index + StrideInFindArea] > Tolerance && !fx[index + StrideInFindArea].IsChecked)
                FindAreaSeries(index + StrideInFindArea);
            if (index - StrideInFindArea > 0 && srcP[index - StrideInFindArea] > Tolerance && !fx[index - StrideInFindArea].IsChecked)
                FindAreaSeries(index - StrideInFindArea);
        }

        unsafe void _FindAreaSeries(int idnex)
        {
            bool temp = false;
            QuadtreeRecurrenceHelper<StackElemData> helper = new QuadtreeRecurrenceHelper<StackElemData>(
                (p) => {
                    p.OtherParameter.fx[p.OtherParameter.index].IsChecked = true;
                    p.OtherParameter.fx[p.OtherParameter.index].AreaNum = CurAreaNumberInFindArea;
                    return p;
                },
                (p) => {
                    temp = p.OtherParameter.index - 1 > 0 && srcP[p.OtherParameter.index - 1] > Tolerance && !fx[p.OtherParameter.index - 1].IsChecked;
                    if (temp)
                        p.OtherParameter.index--;
                    return temp;
                },
                (p) => {
                    temp = p.OtherParameter.index + 1 < StrideInFindArea * curDestImageHeight && srcP[p.OtherParameter.index + 1] > Tolerance && !fx[p.OtherParameter.index + 1].IsChecked;
                    if (temp)
                        p.OtherParameter.index++;
                    return temp;
                },
                (p) => {
                    temp = p.OtherParameter.index + StrideInFindArea < StrideInFindArea * curDestImageHeight && srcP[p.OtherParameter.index + StrideInFindArea] > Tolerance && !fx[p.OtherParameter.index + StrideInFindArea].IsChecked;
                    if (temp)
                        p.OtherParameter.index += StrideInFindArea;
                    return temp;
                },
                (p) => {
                    temp = p.OtherParameter.index - StrideInFindArea > 0 && srcP[p.OtherParameter.index - StrideInFindArea] > Tolerance && !fx[p.OtherParameter.index - StrideInFindArea].IsChecked;
                    if (temp)
                        p.OtherParameter.index -= StrideInFindArea;
                    return temp;
                },
                new StackElem<StackElemData>()
                {
                    fragmentIndex = 0,
                    OtherParameter = new StackElemData { fx = fx, index = idnex }
                }
                );
            fx = helper.Recurrence().OtherParameter.fx;
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                srcBitmap.Dispose();
                CurDestBitmap.Dispose();

                fx = null;
                AreaArr = null;

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~CutImageClass()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
             GC.SuppressFinalize(this);
        }
        #endregion

    }

    /// <summary>
    /// 对CutImageClass的函数式包装
    /// </summary>
    static public class FunctionEncapsulation
    {
        public static CutImageClass MakeThumbnail(this CutImageClass cutImageClass,int width , int height,string mode="W")
        {
            if (cutImageClass.CurDestBitmap == null)
                cutImageClass.CurDestBitmap = BasicMethodClass
                        .MakeThumbnail(cutImageClass.srcBitmap, width, height, mode, "jpg");
            else
                cutImageClass.CurDestBitmap = BasicMethodClass
                        .MakeThumbnail(cutImageClass.CurDestBitmap,width,height, mode, "jpg");

            cutImageClass.curDestImageWidth = cutImageClass.CurDestBitmap.Width;
            cutImageClass.curDestImageHeight = cutImageClass.CurDestBitmap.Height;

            //mss.Dispose();
            return cutImageClass;
        }
       public static CutImageClass MakeVisualAttentionBitmap(this CutImageClass cutImageClass)
        {
            cutImageClass.InitVisualAttentionBitmap();
            return cutImageClass;
        }
        public static CutImageClass MakeCutBitmap(this CutImageClass cutImageClass)
        {
            if (cutImageClass.CurDestBitmap == null)//如果CurDestBitmap没数据将直接使用亮度数据
                cutImageClass.CurDestBitmap = BasicMethodClass.RGB2Gray(cutImageClass.srcBitmap);
            int width = cutImageClass.CurDestBitmap.Width, height = cutImageClass.CurDestBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData VisualAttentionBmData = cutImageClass.CurDestBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            cutImageClass.FindArea(VisualAttentionBmData);
            cutImageClass.CurDestBitmap.UnlockBits(VisualAttentionBmData);
            cutImageClass.GCSsimp_getLightRegionFromSource(cutImageClass.srcBitmap);
            return cutImageClass;
        }

        public static Bitmap OutputAreaBitmap(this CutImageClass cutImageClass)
        {
            int width = cutImageClass.CurDestBitmap.Width, height = cutImageClass.CurDestBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData VisualAttentionBmData = cutImageClass.CurDestBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            Bitmap areaImage = new Bitmap(width, height);
            BitmapData areaBmData = areaImage.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            cutImageClass.FindArea(VisualAttentionBmData);
            CutImageClass.DrawingArea(cutImageClass.AreaArr, cutImageClass.fx, areaBmData, VisualAttentionBmData);
            cutImageClass.CurDestBitmap.UnlockBits(VisualAttentionBmData);

            areaImage.UnlockBits(areaBmData);

            return areaImage;
        }
        public static Bitmap OutputCurrentDestImage(this CutImageClass cutImageClass)
        {
            return cutImageClass.CurDestBitmap?? cutImageClass.srcBitmap;
        }

    }

    public class StackElemData
    {
       public us_PixlPoint[] fx;
       public int index;
    }

    /// <summary>
    /// 四叉树递归栈模拟
    /// </summary>
    /// <typeparam name="StackElem<DataType>">递归作用对象的类型</typeparam>
    public class QuadtreeRecurrenceHelper<DataType> where DataType : StackElemData
    {

        Func<StackElem<StackElemData>, bool> f1; Func<StackElem<StackElemData>, bool> f2; Func<StackElem<StackElemData>, bool> f3; Func<StackElem<StackElemData>, bool> f4;
        Func<StackElem<StackElemData>, StackElem<StackElemData>> f;
        StackElem<StackElemData> stackElem;
        /// <summary>
        /// 初始化四叉树递归模拟
        /// </summary>
        /// <param name="f">递归作用函数</param>
        /// <param name="f1">递归条件函数1</param>
        /// <param name="f2">递归条件函数2</param>
        /// <param name="f3">递归条件函数3</param>
        /// <param name="f4">递归条件函数4</param>
        /// <param name="stackElem">递归作用对象</param>
        public QuadtreeRecurrenceHelper(Func<StackElem<StackElemData>, StackElem<StackElemData>> f,
            Func<StackElem<StackElemData>, bool> f1, Func<StackElem<StackElemData>, bool> f2, Func<StackElem<StackElemData>, bool> f3, Func<StackElem<StackElemData>, bool> f4,
            StackElem<StackElemData> stackElem)
        {
            this.f1 = f1;
            this.f2 = f2;
            this.f3 = f3;
            this.f4 = f4;
            this.f = f;
            this.stackElem = stackElem;
        }
        public StackElem<StackElemData> Recurrence()
        {
            int CaseNum = 0;


            do
            {
                if (stackElem.fragmentIndex == 5)//如果将要执行第5个片段，则表示一次递归完了
                CaseNum = 5;

                if (CaseNum == 0)//代表进入了函数
                {
                    f(stackElem);//实施操作
                    if (f1(stackElem))//当满足f1234函数时判断为true后，代表需要进行递归，其参数stackElem里面的OtherParameter就会更新为下次需要的参数
                        CaseNum = 1;//记录此次位置，便于后面更改为下次递归的完成后的开始位置
                    else if (f2(stackElem))
                        CaseNum = 2;
                    else if (f3(stackElem))
                        CaseNum = 3;
                    else if (f4(stackElem))
                        CaseNum = 4;

                }
                else if(CaseNum!=5)
                {//pop后，接着记录的caseNum执行
                    while (true)
                    {
                        if (CaseNum == 1)
                        {
                            if (f1(stackElem)) { CaseNum = 1; break; }
                            if (f2(stackElem)) { CaseNum = 2; break; }
                            if (f3(stackElem)) { CaseNum = 3; break; }
                            if (f4(stackElem)) { CaseNum = 4; break; }
                        }
                        else if (CaseNum == 2)
                        {
                            if (f2(stackElem)) { CaseNum = 2; break; }
                            if (f3(stackElem)) { CaseNum = 3; break; }
                            if (f4(stackElem)) { CaseNum = 4; break; }

                        }
                        else if (CaseNum == 3)
                        {
                            if (f3(stackElem)) { CaseNum = 3; break; }
                            if (f4(stackElem)) { CaseNum = 4; break; }
                        }
                        else if (CaseNum == 4)
                        {
                            if (f4(stackElem)) { CaseNum = 4; break; }
                        }
                        CaseNum = 0; break;
                    }
                }

                if (CaseNum == 0|| CaseNum == 5)
                { //条件未匹配到或者已经递归完成，pop出函数               
                    if (RecurrenceStack.Count == 0) break;
                    StackElem<StackElemData> elem = RecurrenceStack.Pop();
                    CaseNum = elem.fragmentIndex;//此处不对
                    stackElem = elem;
                    continue;
                }
                //    else RecurrenceStack.Push(new StackElem<DataType> { fragmentIndex = CaseNum, OtherParameter = stackElem.OtherParameter });
                else
                {
                    switch (CaseNum)
                    {
                        case 1://记录出函数时的片段位置，然后进入函数（push）
                            CaseNum++;
                            RecurrenceStack.Push(new StackElem<StackElemData> { fragmentIndex = CaseNum,OtherParameter=new StackElemData { fx = stackElem.OtherParameter.fx, index = stackElem.OtherParameter.index } });
                            break;
                        case 2:
                            CaseNum++;
                            RecurrenceStack.Push(new StackElem<StackElemData> { fragmentIndex = CaseNum, OtherParameter = new StackElemData { fx = stackElem.OtherParameter.fx, index = stackElem.OtherParameter.index } });
                            break;
                        case 3:
                            CaseNum++;
                            RecurrenceStack.Push(new StackElem<StackElemData> { fragmentIndex = CaseNum, OtherParameter = new StackElemData { fx = stackElem.OtherParameter.fx, index = stackElem.OtherParameter.index } });
                            break;
                        case 4:
                            CaseNum++;
                            RecurrenceStack.Push(new StackElem<StackElemData> { fragmentIndex = CaseNum, OtherParameter = new StackElemData { fx = stackElem.OtherParameter.fx, index = stackElem.OtherParameter.index } });
                            break;
                    }
                    CaseNum = 0;//进入函数信号
                }
            } while (RecurrenceStack.Count != 0);
            return stackElem;
        }

        Stack<StackElem<StackElemData>> RecurrenceStack = new Stack<StackElem<StackElemData>>();
    }

    public class StackElem<DataType> where DataType:StackElemData
    {
        public int fragmentIndex;
        public DataType OtherParameter;
    }
}
