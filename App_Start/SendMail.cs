using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace MyForum.App_Start
{
    public static class SendMail
    {
        public static void DoSend(string password,string mailbox,string username)
        {
            string smtpclient = ConfigurationManager.ConnectionStrings["smtpclient"].ConnectionString;
            string mymailbox = ConfigurationManager.ConnectionStrings["mymailbox"].ConnectionString;
            string mypassword = ConfigurationManager.ConnectionStrings["mypassword"].ConnectionString;

            //设置SMTP服务器信息
            SmtpClient client = new SmtpClient(smtpclient);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(mymailbox, mypassword);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            //设置邮件信息
            MailMessage mm = new MailMessage("mymvcforum@126.com", mailbox);
            mm.Subject = "找回密码";//邮件主题           
            mm.Body = $"亲爱的用户[{username}]，你在MVC论坛的密码为[{password}]";//邮件内容

            //发送邮件
            client.Send(mm);
        }
    }
}