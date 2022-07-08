using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MyForum.Models;
using Newtonsoft.Json;

namespace MyForum
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //在线人数
            Application["OnLineUserCount"] = 0;
            //访问次数
            Application["AccessTimes"] = 0;

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //没有root用户时，增加超级管理员root
            using (Data context = new Data())
            {
                //id为root的用户数
                var count1 = (from s in context.Users
                              where s.UserId == "root"
                              select s).Count();

                //name为root的用户数
                var count2 = (from s in context.Users
                              where s.UserName == "root"
                              select s).Count();

                //没root用户
                if (count1 == 0)
                {
                    //有人已经叫root了，帮他改名
                    if (count2 != 0)
                    {
                        var query = (from s in context.Users
                                     where s.UserName == "root"
                                     select s).Single();

                        query.UserName = Guid.NewGuid().ToString();
                        context.SaveChanges();
                    }
                    //没人叫root，增加root
                    context.Users.Add(new User
                    {
                        UserId = "root",
                        UserName = "root",
                        NickName="超级管理员",
                        Password = "root",
                        Head = "root.jpg",
                        Disc = "超级管理员",
                        Lv = 999,
                        Exp = 0,
                        Identity = 0,
                        Access = 0,
                        Posts = 0,
                        Replys = 0,
                        RegisterTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                        MailBox= "mymvcforum@126.com",
                        MVCGold=999999,
                        Rank=10,
                        GetRankTime= DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss")
                    });
                    //增加空的收藏夹信息
                    context.StarLists.Add(new StarList{
                        UserId="root",
                        Star=JsonConvert.SerializeObject(new List<string>())
                    });
                    context.SaveChanges();
                }
            }
            //没有帖子时增加一个初始帖子
            using (Data context = new Data())
            {
                var count = context.Posts.Count();
                if (count == 0)
                {
                    context.Posts.Add(new Post
                    {
                        PostId = Guid.NewGuid().ToString(),
                        UserId = "root",
                        Time = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                        Title = "欢迎来到我的MVC论坛！",
                        Replys = 0,
                        LastReplyTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                        LastReplyUserId = "root",
                        IsShown = true,
                        IsTop = 1,
                        AccessTime = 0,
                        Likes = 0,
                        Content = System.IO.File.ReadAllText(Server.MapPath("/Attached/default/FirstPost.txt")),//从文件中读取
                        Locked=false,
                        Elite=true,
                        Type = 1//版务
                    });
                    context.SaveChanges();
                }
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            Application.Lock();
            Application["OnLineUserCount"] = Convert.ToInt32(Application["OnLineUserCount"]) + 1;
            Application["AccessTimes"]=Convert.ToInt32(Application["AccessTimes"]) + 1;
            Application.UnLock();
        }

        protected void Session_End(object sender, EventArgs e)
        {
            Application.Lock();
            Application["OnLineUserCount"] = Convert.ToInt32(Application["OnLineUserCount"]) - 1;
            Application.UnLock();
        }
    }
}
