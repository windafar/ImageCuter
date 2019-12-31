#VisualAttentionDetectionApply
说明：

CutImage项目

This is a project that uses the view significance graph to find a cutting area and generate an image of that area. 

This often used in design, or reduces web traffic. The inspiration comes from OSU's website: https://osu.ppy.sh/beatmapsets?q=&m=-1


![Image text](https://github.com/windafar/ImageCuter/blob/master/img/smg.jpg)


It's simple for apply it in your app, maybe looks like as:

            CutImageClass cuter = new CutImageClass(new System.Drawing.Bitmap("pic1.jpg"), new System.Drawing.Rectangle(0, 0, 364, 240), 215);  
            var GenerImage =cuter  
                        .MakeThumbnail(364, 240)//可选，压缩当前结果  
                        .MakeVisualAttentionBitmap()//建议，制作显著图  
                        .MakeCutBitmap()//制作切图  
                        .OutputCurrentDestImage();//输出  

 I used this algorithm in an mp3 player:
 
 
 ![Image text](https://github.com/windafar/ImageCuter/blob/master/img/player.jpg)



