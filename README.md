#VisualAttentionDetectionApply
说明：

CutImage项目

This is a project that uses the view significance graph to find a cutting area and generate an image of that area. 

This often used in design, or reduces web traffic. The inspiration comes from OSU's website: https://osu.ppy.sh/beatmapsets?q=&m=-1


![Image text](https://github.com/windafar/ImageCuter/blob/master/img/smg.jpg)


It's simple for apply it in your app, maybe looks like as:

        public Bitmap GetSRDFromPath(string filepath,
            double prewidth,double preheight,string premode,string pretype,
            Rectangle R,
            int Tolerance=200
            )
        {
            if (R == null) R = new System.Drawing.Rectangle(0, 0, 256, 256);
            MemoryStream mss = new MemoryStream();
            BasicMethodClass.MakeThumbnail(filepath, mss,prewidth, preheight, premode, pretype);
            if (mss.Length == 0) return null;
            var srcBitmap = new Bitmap(mss);
            var vdcmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
            mss.Dispose();
            CutImageClass cuter = new CutImageClass(vdcmap, R, Tolerance);
            var GenerImage = cuter.GCSsimp_getLightPointFromSource(srcBitmap);
            srcBitmap.Dispose();
            vdcmap.Dispose();
            return GenerImage;
        }
(Disposable you can use "using" instead of "disposable")
 I used this algorithm in an mp3 player:
 
 
 ![Image text](https://github.com/windafar/ImageCuter/blob/master/img/player.jpg)



