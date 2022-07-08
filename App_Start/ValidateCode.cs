using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace MyForum.App_Start
{
    //获取方式           
    //byte[] result = GetVerifyCode();
    //return File(result, "image/jpeg jpeg jpg jpe");
    public class ValidateCode
    {
        public int length;//验证码长度
        public string VerCode;//存储验证码
        //初始化函数
        public ValidateCode(int length)
        {
            this.length = length;
            VerCode = "";
        }
        public ValidateCode()
        {
            this.length = 4;
            VerCode = "";
        }
        //生成验证码
        public byte[] GetVerifyCode()
        {
            int codeW = 20*length;//画布大小
            int codeH = 30;
            int fontSize = 16;//字体大小
            //常量信息
            Color[] color = { Color.Black, Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Brown, Color.DarkBlue }; //字体颜色、、
            string[] font = { "Times New Roman" };
            char[] character = { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'd', 'e', 'f', 'h', 'k', 'm', 'n', 'r', 'x', 'y', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'W', 'X', 'Y' };
            //生成验证码字符串
            Random rnd = new Random();
            string code = "";
            for (int i = 0; i < length; i++)
                code += character[rnd.Next(character.Length)];
            //在类中存储验证码
            VerCode = code;
            //创建画布
            Bitmap bmp = new Bitmap(codeW, codeH - 3);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            //画噪线
            for (int i = 0; i < 3; i++)
            {
                int x1 = rnd.Next(codeW);
                int y1 = rnd.Next(codeH);
                int x2 = rnd.Next(codeW);
                int y2 = rnd.Next(codeH);
                Color clr = color[rnd.Next(color.Length)];
                g.DrawLine(new Pen(clr), x1, y1, x2, y2);
            }
            //画验证码
            for (int i = 0; i < length; i++)
            {
                string fnt = font[rnd.Next(font.Length)];
                Font ft = new Font(fnt, fontSize);
                Color clr = color[rnd.Next(color.Length)];
                g.DrawString(code[i].ToString(), ft, new SolidBrush(clr), (float)i * 18, (float)0);
            }
            //将验证码写入图片内存流中，以image/png格式输出
            MemoryStream ms = new MemoryStream();
            try
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            {
                g.Dispose();
                bmp.Dispose();
                ms.Dispose();
            }
        }
    }
}