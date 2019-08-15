using CutImage;
using ImageBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualAttentionDetection;

namespace OtherAppy
{
    /// <summary>
    /// 基于视觉显著性区域寻找主色调
    /// </summary>
    public class DominantHue
    {
        struct myrgbcolor {
           public double R;public double G;public double B;
        }

        Bitmap desBitmap;

        ///1，提出显著性区域
        ///2，求取区域色调
        public DominantHue(Bitmap source) => this.desBitmap = source;

        public Color GetDominantHue(double S=0.66,double L=0.66,int AttentionValue=200)
        {
            var destinatBitmap = new Bitmap(desBitmap);
            var VisualAttentionBitmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(destinatBitmap);
            int width = VisualAttentionBitmap.Width, height = VisualAttentionBitmap.Height;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
            BitmapData VisualAttentionBmData = VisualAttentionBitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            CutImageClass cuter = new CutImageClass(VisualAttentionBitmap, new System.Drawing.Rectangle(0, 0, 700, 200), AttentionValue);
            us_PixlPoint[] AreaArr = cuter.FindArea(VisualAttentionBmData);
            var fx = cuter.fx;
            var MAXAreaArrNumbler = cuter.MAXAreaArrNumbler;

            List<myrgbcolor> list = new List<myrgbcolor>();
            int desWidth = desBitmap.Width;
            int desHeight = desBitmap.Height;
            //Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(desWidth, desHeight);
            BitmapData dstBmData = destinatBitmap.LockBits(new Rectangle(0, 0, desWidth, desHeight),
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                //byte* srcP = (byte*)srcScan;
                byte* dstP = (byte*)dstScan;
                int SourceIndex = 0;
                int DstIndex = 0;
                int colorCount = 0;
                //if (MaxAreaArrValue == 0) throw new ArgumentException("Tolerance过低");
                //int x_start = desWidth, x_end = 0, y_start = desHeight, y_end = 0;
                for (int y = 1; y < desHeight - 1; y++)
                {
                    DstIndex = y * dstBmData.Stride;
                    SourceIndex = y * VisualAttentionBmData.Stride;
                    for (int x = 1; x < desWidth - 1; x++)
                    {
                        int Sx = fx[SourceIndex].AreaNum;
                        //  if (Sx != 0) throw new Exception();
                        if (Sx == MAXAreaArrNumbler)
                        {
                            list.Add(new myrgbcolor
                            {
                                R = dstP[DstIndex],
                                G = dstP[DstIndex + 1],
                                B = dstP[DstIndex + 2]
                            });
                        }
                        DstIndex += 3;
                        SourceIndex++;
                    }
                }
                double R=0, G=0, B=0;
                foreach (var c in list) {
                    R += c.R / list.Count();
                    G += c.G / list.Count();
                    B += c.B / list.Count();
                }
                var HSL= BasicMethodClass.ColorHelper.RgbToHsl(new BasicMethodClass.ColorHelper.ColorRGB((int)R, (int)G, (int)B));
                HSL.L = (int)(255*L);HSL.S = (int)(255 * S);
                var RGB = BasicMethodClass.ColorHelper.HslToRgb(HSL);

                return Color.FromArgb(RGB.R, RGB.G, RGB.B);
            }
        }

        private us_AreaCount GetMaxValue(us_AreaCount[] LIST) {
            us_AreaCount result = new us_AreaCount();
            int count=0;
            for (int i = 0; i < LIST.Count();i++) {
                if (LIST[i].Count > count) { count = LIST[i].Count; result = LIST[i]; }
                else continue;
            }

            return result;
        }
    }
}


///
/// 注意：使用模糊（如果不行直接固定固定色调）方式解决单色问题
///
