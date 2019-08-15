using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageBasic;

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

    public class CutImageClass
    {

        /// <summary>
        /// 传入用于寻找区域的(灰度)图片
        /// </summary>
        Bitmap srcBitmap;
        /// <summary>
        /// 剪切的尺寸
        /// </summary>
        Rectangle CutRect;
        /// <summary>
        /// 有效区域范围（0-255）
        /// </summary>
        int Tolerance;
        int srcWidth, srcHeight;
        /// <summary>
        /// 由源高度和源宽度生成
        /// </summary>
        Rectangle srcRect;

        unsafe byte* srcP, dstP;
        /// <summary>
        /// 自定义的图片数据
        /// </summary>
        public us_PixlPoint[] fx;
        /// <summary>
        /// 最大值区域的最大值 (public get)
        /// </summary>
        public int MaxAreaArrValue;
        /// <summary>
        /// 最大区域的最大值的区域号(public get)
        /// </summary>
        public int MAXAreaArrNumbler;
        /// <summary>
        /// 预定义的区域统计数组（目前该数组是固定的）
        /// </summary>
        public us_AreaCount[] AreaArr;
        /// <summary>
        /// 寻找连续区域时的需要的width
        /// </summary>
        int StrideInFindArea;
        /// <summary>
        /// 寻找连续区域时的最大区域号
        /// </summary>
        int CurAreaNumberInFindArea;

        /// <summary>
        /// 初始化该切图类
        /// </summary>
        /// <param name="srcBitmap">8位灰度图</param>
        /// <param name="CutRect">切割大小</param>
        /// <param name="Tolerance">亮度容差，用于寻找有效区域，如果该值太小则会造成区域太多而溢出区域统计数组（默认3000个）</param>
        public CutImageClass(Bitmap srcBitmap, Rectangle CutRect, int Tolerance = 200)
        {
            this.srcBitmap = srcBitmap;
            if (Tolerance > 255 || Tolerance < 0) throw new ArgumentException();
            this.Tolerance = Tolerance;
            this.CutRect = CutRect;
            srcWidth = srcBitmap.Width;
            srcHeight = srcBitmap.Height;
            srcRect = new Rectangle(0, 0, srcWidth, srcHeight);
            AreaArr = new us_AreaCount[24000];//默认定义区域数量
        }
        /// <summary>
        /// 获取最具显著性的一个白色区域
        /// </summary>
        /// <returns></returns>
        public Bitmap GCSsimp_getLightPoint()
        {

            BitmapData srcBmData = srcBitmap.LockBits(srcRect,
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(srcBitmap.Width, srcBitmap.Height);
            BitmapData dstBmData = dstBitmap.LockBits(new Rectangle(0, 0, srcBitmap.Width, srcHeight),
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {

                srcP = (byte*)srcScan;
                dstP = (byte*)dstScan;
                int index = 0;
                if (fx == null)
                    fx = FindArea(srcBmData);
                index = 0;
                if (MaxAreaArrValue == 0) throw new ArgumentException("Tolerance过低");
                int x_start = srcWidth, x_end = 0, y_start = srcHeight, y_end = 0;
                for (int y = 1; y < srcHeight - 1; y++)
                {
                    index = y * srcBmData.Stride;
                    for (int x = 1; x < srcWidth - 1; x++)
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
                index = 0;
                for (int c_i = y_start; c_i < y_end; c_i++)
                {
                    index = dstBmData.Stride * c_i + x_start;
                    for (int c_j = x_start; c_j < x_end; c_j++)
                    {
                        dstP[index] = (byte)255;
                        index++;

                    }
                }
            }
            dstBitmap.UnlockBits(dstBmData);
            srcBitmap.UnlockBits(srcBmData);
            return dstBitmap;
        }
        /// <summary>
        /// 获取一个最具显著性的源区域
        /// </summary>
        /// <param name="FinSourceImage">源图</param>
        /// <returns></returns>
        public Bitmap GCSsimp_getLightPointFromSource(Bitmap FinSourceImage)
        {
            int DistWidth = CutRect.Width;
            int DistHeight = CutRect.Height;
            BitmapData srcBmData = srcBitmap.LockBits(srcRect,
                      ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            Bitmap bmpOut=null;
            IntPtr srcScan = srcBmData.Scan0;
            unsafe
            {

                srcP = (byte*)srcScan;
                int index = 0;
                if (fx == null)
                    fx = FindArea(srcBmData);
                index = 0;
               // if (MaxAreaArrValue == 0) throw new ArgumentException("Tolerance过低");
                int x_start = srcWidth, x_end = 0, y_start = srcHeight, y_end = 0;
                for (int y = 1; y < srcHeight - 1; y++)
                {
                    index = y * srcBmData.Stride;
                    for (int x = 1; x < srcWidth - 1; x++)
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
                int _xm = (x_end + x_start) / 2;
                int _ym = (y_end + y_start) / 2;
                int _xs, _ys, _xe, _ye;
                int FinWidth = FinSourceImage.Width, FinHeight = FinSourceImage.Height;
                _xm = _xm * FinWidth / srcWidth;
                _ym = _ym * FinHeight / srcHeight;
                _xs = _xm - DistWidth / 2 > 0 ? _xm - DistWidth / 2 : 0;
                _xe = _xm + DistWidth / 2 < FinWidth ? _xm + DistWidth / 2 : FinWidth;
                if (_xs == 0)
                {
                    _xe = _xe + DistWidth / 2 < FinWidth ?
                        _xe += DistWidth / 2 : FinWidth;
                }
                if (_xe == FinWidth)
                {
                    _xs = _xs - DistWidth / 2 >= 0 ?
                        _xs -= DistWidth / 2 : 0;
                }
                _ys = _ym - DistHeight / 2 > 0 ? _ym - DistHeight / 2 : 0;
                _ye = _ym + DistHeight / 2 < FinHeight ? _ym + DistHeight / 2 : FinHeight;
                if (_ys == 0)
                {
                    _ye = _ye + DistHeight / 2 < FinHeight ?
                        _ye += DistHeight / 2 : FinHeight;
                }
                if (_ye == FinHeight)
                {
                    _ys = _ys - DistHeight / 2 > 0 ?
                        _ys -= DistHeight / 2 : 0;
                }

                //BitmapData FinSourceImageData = FinSourceImage.LockBits(new Rectangle(0, 0, FinSourceImage.Width, FinSourceImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                //byte* finP = (byte*)FinSourceImageData.Scan0;
                //index = 0;
                //int destIndex = 0;
                 bmpOut = new Bitmap(_xe - _xs, _ye - _ys, PixelFormat.Format24bppRgb);
             // bmpOut=  BasicMethodClass.CutImage(FinSourceImage, _xs, _ys, _xe - _xs, _ye - _ys);
          //      BitmapData bmpOutData = bmpOut.LockBits(new Rectangle(0, 0, bmpOut.Width, bmpOut.Height),
          //ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
          //      dstP = (byte*)bmpOutData.Scan0;
                //for (int c_i = _ys, b_i=0; c_i < _ye; c_i++,b_i++)
                //{
                //    index = FinSourceImageData.Stride * c_i + _xs;
                ////    destIndex = bmpOutData.Stride * b_i;
                //    for (int c_j = _xs; c_j < _xe; c_j++)
                //    {
                //        dstP[destIndex] = finP[index];
                //        dstP[destIndex + 1] = finP[index + 1];
                //        dstP[destIndex + 2] = finP[index + 2];
                //  //      destIndex += 3;
                //        index += 3;
                //    }
                //}
            //    bmpOut.UnlockBits(bmpOutData);
                srcBitmap.UnlockBits(srcBmData);
                bmpOut = BasicMethodClass.CutImage(FinSourceImage, _xs, _ys, _xe - _xs, _ye - _ys);

                // FinSourceImage.UnlockBits(FinSourceImageData);
                //dstBitmap = BasicMethodClass.CutImage(dstBitmap, _xs, _ys, _xe-_xs, _ye-_ys);
            }
            return bmpOut;
        }

        /// <summary>
        /// 根据初始化的Tolerance参数寻找各个连续区域，并标号,返回新的fx数组
        /// </summary>
        /// <param name="srcBmData"></param>
        /// <returns></returns>
        public unsafe us_PixlPoint[] FindArea(BitmapData srcBmData)
        {
            int index = 0;
            StrideInFindArea = srcBmData.Stride;
            fx = new us_PixlPoint[StrideInFindArea * srcHeight];
            srcP = (byte*)srcBmData.Scan0;
            //记录最大区域大小
            MaxAreaArrValue = 0;
            //记录最大区域的区域号
            MAXAreaArrNumbler = 0;
            //记录当前区域编号的使用值
            CurAreaNumberInFindArea = 0;
            for (int y = 1; y < srcHeight - 2; y++)
            {
                index = y * StrideInFindArea;
                for (int x = 1; x < srcWidth - 2; x++)
                {
                    fx[index].value = srcP[index];
                    fx[index].point = new Point(x, y);
                    if (fx[index].value > Tolerance)//在此容差范围内进行计数统计
                    {
                        if (!fx[index].IsChecked)
                        {
                            CurAreaNumberInFindArea++;
                            find_lare_count = 0;
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

            return fx;
        }
        int find_lare_count = 0;
        /// <summary>
        /// 递归寻找一个连续区域
        /// </summary>
        /// <param name="index">寻找开始位置</param>
        unsafe void FindAreaSeries(int index)
        {
           // if (find_lare_count++ > 512) { find_lare_count = 0; return; }
            fx[index].IsChecked = true;
            fx[index].AreaNum = CurAreaNumberInFindArea;
            //fx[index].value = srcP[index];
            if (index - 1 > 0 && srcP[index - 1] > Tolerance && !fx[index - 1].IsChecked)
                FindAreaSeries(index - 1);//判断条件的可以事先优化，下同
            if (index + 1<StrideInFindArea*srcHeight&&srcP[index + 1] > Tolerance && !fx[index + 1].IsChecked)
                FindAreaSeries(index + 1);
            if (index + StrideInFindArea< StrideInFindArea * srcHeight && srcP[index + StrideInFindArea] > Tolerance && !fx[index + StrideInFindArea].IsChecked)
                FindAreaSeries(index + StrideInFindArea);
            if (index - StrideInFindArea > 0 && srcP[index - StrideInFindArea] > Tolerance && !fx[index - StrideInFindArea].IsChecked)
                FindAreaSeries(index - StrideInFindArea);
        }

        unsafe void _FindAreaSeries(int idnex)
        {
            bool temp=false;
            QuadtreeRecurrenceHelper<StackElemData> helper = new QuadtreeRecurrenceHelper<StackElemData>(
                (p) => {
                    p.OtherParameter.fx[p.OtherParameter.index].IsChecked = true;
                    p.OtherParameter.fx[p.OtherParameter.index].AreaNum = CurAreaNumberInFindArea;
                    return p;
                },
                (p) => { temp = p.OtherParameter.index - 1 > 0 && srcP[p.OtherParameter.index - 1] > Tolerance && !fx[p.OtherParameter.index - 1].IsChecked;
                    if(temp)
                        p.OtherParameter.index--;
                    return temp; },
                (p) => { temp = p.OtherParameter.index + 1 < StrideInFindArea * srcHeight && srcP[p.OtherParameter.index + 1] > Tolerance && !fx[p.OtherParameter.index + 1].IsChecked;
                    if (temp)
                        p.OtherParameter.index++;
                    return temp; },
                (p) => { temp = p.OtherParameter.index + StrideInFindArea < StrideInFindArea * srcHeight && srcP[p.OtherParameter.index + StrideInFindArea] > Tolerance && !fx[p.OtherParameter.index + StrideInFindArea].IsChecked;
                    if (temp)
                        p.OtherParameter.index += StrideInFindArea;
                    return temp; },
                (p) => { temp = p.OtherParameter.index - StrideInFindArea > 0 && srcP[p.OtherParameter.index - StrideInFindArea] > Tolerance && !fx[p.OtherParameter.index - StrideInFindArea].IsChecked;
                    if (temp)
                        p.OtherParameter.index -= StrideInFindArea;
                    return temp; },
                new StackElem<StackElemData>(){  fragmentIndex=0,
                    OtherParameter =new StackElemData { fx=fx,index=idnex} }
                );
           fx= helper.Recurrence().OtherParameter.fx;
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
    }
    /// <summary>
    /// 一个用于切图的策略
    /// </summary>
    public class CutImageStrategy
    {
        int Distance { set; get; }
        int MaxAreaNum { set; get; }
        int[] StrategyResultNumbles;
        us_AreaCount[] AreaArr;
        us_PixlPoint[] fx;
        public CutImageStrategy(us_AreaCount[] AreaArr, us_PixlPoint[] fx)
        {
            this.AreaArr = AreaArr;
            this.fx = fx;
        }
        /// <summary>
        /// 设置临近的距离
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public CutImageStrategy SetDistance(int distance)
        {
            Distance = distance;
            return this;
        }
        /// <summary>
        /// 设置临近距离内的最大区域数
        /// </summary>
        /// <param name="max_area_num"></param>
        /// <returns></returns>
        public CutImageStrategy SetMaxAreaNum(int max_area_num)
        {
            MaxAreaNum = max_area_num;
            return this;
        }
        /// <summary>
        /// 会产生满足条件的区域号
        /// </summary>
        /// <returns></returns>
        public CutImageStrategy ApplyStrategy_Close()
        {
            us_AreaCount[] AreaArrTemp = AreaArr.OrderByDescending(x => x.Count).Take(MaxAreaNum).ToArray();
            //寻找临近区域的代价太高，暂停，看有没有其他办法
            //目前的思路是求出前MaxAreaNum标记的各个点集合的最短距离
            //能够想到的方法是通过求出各个点集合的形体然后求距离，这需要一个高效的算法（分治？）。
            //另外还有种办法就是在递归寻找区域的时候对区域进行整合，直接作为一个区域，但是这会加重栈溢出的危险
            return this;
        }
        int temp_i = 0;
        /// <summary>
        /// 根据应用的切图策略判断一个点是否应该被纳入目标图的矩形中
        /// </summary>
        /// <returns></returns>
        public bool Is_ExitInStrategyResult(int ArreaNumble)
        {
            for (temp_i = 0; temp_i < StrategyResultNumbles.Length; temp_i++)
            {
                if (StrategyResultNumbles[temp_i] == ArreaNumble) return true;
            }
            return false;

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
