using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace MyForum.App_Start
{
    public static class ZipImage
    {
        public static Image zipImage(byte[] bs)//递归
        {
            if (bs.Length > 1024 * 100)//最大100k
            {
                Image img = Image.FromStream(new MemoryStream(bs));
                Bitmap b = new Bitmap((int)(img.Width * 0.5), (int)(img.Height * 0.5));
                b.SetResolution(96, 96);//设置分辨率
                Graphics g = Graphics.FromImage(b);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Rectangle recTo = new Rectangle(0, 0, b.Width, b.Height);//表示目标大小
                Rectangle recFrom = new Rectangle(0, 0, img.Width, img.Height);//表示源文件大小
                g.DrawImage(img, recTo, recFrom, GraphicsUnit.Pixel);
                g.Dispose();
                MemoryStream ms = new MemoryStream();
                b.Save(ms, ImageFormat.Jpeg);
                return zipImage(ms.GetBuffer());
            }
            else
            {
                return Image.FromStream(new MemoryStream(bs));
            }
        }

        public static byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }
    }
}