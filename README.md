#VisualAttentionDetectionApply
说明：

CutImage项目

This is a project that uses the view significance graph to find a cutting area and generate an image of that area.  
It can applies in design, or reduces web traffic. The inspiration is from OSU's website: https://osu.ppy.sh/beatmapsets?q=&m=-1  
Reference paper: http://ivrgwww.epfl.ch/supplementary_material/RK_CVPR09/  
Its essence is to solve Euclidean distance of color in LAB color space  
这个项目使用视觉显著性图来找到一个切割区域并生成该区域的图像。  
它可以应用于设计，或者减少网络流量。灵感来自于OSU的网站。  
 
![example](https://s2.ax1x.com/2020/01/03/laOxkF.jpg)


It's simple for apply it in your app, maybe looks like as:

            CutImageClass cuter = new CutImageClass(new System.Drawing.Bitmap("pic1.jpg"), new System.Drawing.Rectangle(0, 0, 364, 240), 215);  
            var GenerImage =cuter  
                        .MakeThumbnail(364, 240)//Optionally, compress the current result image，可选，压缩当前图片
                        .MakeVisualAttentionBitmap()//Recommend, make a significant image，推荐，生成显著性图片
                        .MakeCutBitmap()//Make cutting image，切割
                        .OutputCurrentDestImage();//Output current result image，输出

 I used this algorithm in an mp3 player:
 
 
 ![apply](https://s2.ax1x.com/2020/01/03/laLF3R.jpg)



