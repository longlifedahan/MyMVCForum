using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyForum.Models;
using MyForum.App_Start;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Text;
using System.Data;
using System.Net;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Data.Entity;
using System.Text.RegularExpressions;

namespace MyForum.Controllers
{
    //找回密码
    public class HomeController : Controller
    {
        #region 页面
        //主页
        public ActionResult Index()
        {
            return View();
        }

        //登陆界面
        public ActionResult Login()
        {
            if (!RequestExtensions.IsMobileBrowser(System.Web.HttpContext.Current.Request))//不是手机
                return View();
            else//是手机
                return View("Login_Mobile");
        }

        //注册界面
        public ActionResult Register()
        {
            if (!RequestExtensions.IsMobileBrowser(System.Web.HttpContext.Current.Request))//不是手机
                return View();
            else
                return View("Register_Mobile");
        }

        //帖子详情界面
        public ActionResult PostDetail(string Id)
        {
            if (Id == "" || Id == null)
                return Content("没有输入帖子id！");
            //赋值该帖子url中的Id
            ViewData["postid"] = Id;
            using (Data context = new Data())
            {
                //帖子数
                var count = (from s in context.Posts
                             where s.PostId == Id
                             select s).Count();
                //找不到帖子
                if (count == 0)
                    return Content("找不到对应id的帖子！");
                //找到帖子
                var query = (from s in context.Posts
                             where s.PostId == Id
                             select s).Single();
                //得到用户id
                string userid = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                //得到用户身份
                int identity = 2;//默认普通用户
                //若登陆得到权限
                if (userid != "")
                {
                    identity = (from s in context.Users
                                where s.UserId == userid
                                select s.Identity).Single();
                }
                //权限控制（不显示的帖子仅自己、管理员、超管可访问）
                if (query.IsShown == false && query.UserId != userid && identity != 0 && identity != 1)
                    return Content("该帖子已被设置为隐藏，无法打开（隐藏的帖子仅自己、管理员、超级管理员可以浏览）！");

                query.AccessTime++;
                context.SaveChanges();
                ViewData["istop"] = query.IsTop;
                ViewData["accesstime"] = query.AccessTime;
                ViewData["replys"] = query.Replys;
                ViewData["likes"] = query.Likes;
                ViewData["isshown"] = query.IsShown;
                ViewData["locked"] = query.Locked;
                ViewData["elite"] = query.Elite;
                ViewData["type"] = query.Type;
            }
            return View();
        }

        //修改个人信息界面
        public ActionResult EditInfo()
        {
            if (Session["userid"] == null)
                return Content("未登录！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                var query = (from s in context.Users
                             where s.UserId == userid
                             select s).Single();
                ViewData["userinfo"] = query;
            }
            return View();
        }

        //我的帖子列表界面
        public ActionResult MyPosts()
        {
            return View();
        }

        //编辑帖子
        public ActionResult EditPost(string Id)
        {
            if (Id == "" || Id == null)
                return Content("没有输入帖子id");
            using (Data context = new Data())
            {
                var count = (from s in context.Posts
                             where s.PostId == Id
                             select s).Count();
                if (count == 0)
                    return Content("找不到对应id的帖子！");

                var query = (from s in context.Posts
                             where s.PostId == Id
                             select s).Single();
                ViewData["postid"] = query.PostId;
                ViewData["title"] = query.Title;
                ViewData["content"] = query.Content;
                ViewData["type"] = query.Type;
                return View();
            }
        }

        //个人信息界面
        public ActionResult PersonalInfo()
        {
            if (Session["userid"] == null)
            {
                ViewData["username"] = "未登录用户";
                ViewData["nickname"] = "未登录用户";
                ViewData["head"] = "default.jpg";
                ViewData["disc"] = "还没有登录，这里什么也没有";
                ViewData["lv"] = 0;
                ViewData["exp"] = 0;
                ViewData["gold"] = 0;
                ViewData["identity"] = "未登录用户";
                ViewData["access"] = "无权限";
                ViewData["posts"] = 0;
                ViewData["replys"] = 0;
                ViewData["registertime"] = "未注册";
                ViewData["mail"] = "无邮箱";
                ViewData["rank"] = 0;
            }
            else
            {
                using (Data context = new Data())
                {
                    string userid = Session["userid"].ToString();
                    var query = (from s in context.Users
                                 where s.UserId == userid
                                 select s).Single();
                    ViewData["username"] = query.UserName;
                    ViewData["nickname"] = query.NickName;
                    ViewData["head"] = query.Head;
                    ViewData["disc"] = query.Disc;
                    ViewData["lv"] = query.Lv;
                    ViewData["exp"] = query.Exp;
                    ViewData["gold"] = query.MVCGold;
                    ViewData["identity"] = (query.Identity == 0) ? "超级管理员" : ((query.Identity == 1) ? "管理员" : "普通用户");
                    ViewData["access"] = (query.Access == 0) ? "正常(允许发帖回帖)" : ((query.Access == 1) ? "禁止发帖(允许回帖)" : (query.Access == 2) ? "禁止发帖回帖" : "禁止登录");
                    ViewData["posts"] = query.Posts;
                    ViewData["replys"] = query.Replys;
                    ViewData["registertime"] = query.RegisterTime;
                    ViewData["mail"] = query.MailBox;
                    ViewData["rank"] = query.Rank;
                }
            }
            return View();
        }

        //供他人查询的PersonalInfo（不显示邮件）
        public ActionResult PersonalInfoWithoutMail(string Username)
        {
            using (Data context = new Data())
            {
                //查询用户个数
                var count = (from s in context.Users
                             where s.UserName == Username
                             select s).Count();
                //没有用户
                if (count == 0)
                    return Content("找不到对应用户名的用户！");
                //得到用户信息
                var query = (from s in context.Users
                             where s.UserName == Username
                             select s).Single();
                int identity = 2;//默认用户权限
                //得到当前用户权限
                if (Session["userid"] != null)
                    identity = int.Parse(Session["identity"].ToString());
                ViewData["username"] = identity == 0 ? query.UserName : (identity == 1 ? query.UserName : "非管理/超管不可查询用户名");
                ViewData["nickname"] = query.NickName;
                ViewData["head"] = query.Head;
                ViewData["disc"] = query.Disc;
                ViewData["lv"] = query.Lv;
                ViewData["exp"] = query.Exp;
                ViewData["gold"] = query.MVCGold;
                ViewData["identity"] = (query.Identity == 0) ? "超级管理员" : ((query.Identity == 1) ? "管理员" : "普通用户");
                ViewData["access"] = (query.Access == 0) ? "正常(允许发帖回帖)" : ((query.Access == 1) ? "禁止发帖(允许回帖)" : (query.Access == 2) ? "禁止发帖回帖" : "禁止登录");
                ViewData["posts"] = query.Posts;
                ViewData["replys"] = query.Replys;
                ViewData["registertime"] = query.RegisterTime;
                ViewData["mail"] = identity == 0 ? query.MailBox : (identity == 1 ? query.MailBox : "非管理/超管不可查询邮箱");
                ViewData["rank"] = query.Rank;
            }
            return View("PersonalInfo");
        }

        //直连数据库
        public ActionResult LinkDataBaseDirectly()
        {
            return View();
        }

        //收藏界面
        public ActionResult Star()
        {
            return View("");
        }
        #endregion

        #region 登录注册与个人信息
        //进行注册
        public ActionResult DoReg(string username, string nickname, HttpPostedFileBase head, string disc, string password, string mailbox, string inputcode)
        {
            if (Session["vercode"] == null)
                return Content("验证码已过期，请刷新以重新获得验证码！");
            if (inputcode.ToLower() != Session["vercode"].ToString().ToLower())
                return Content("验证码错误！");
            if (username.Trim() == "" || username == null || username.ToLower().Contains("null"))
                return Content("不合法的用户名(不得全为空且不得含null)！");
            if (nickname.ToLower().Contains("null"))
                return Content("不合法的昵称(不得含null)！");
            Session["vercode"] = null;//释放
            using (Data context = new Data())
            {
                var query1 = (from s in context.Users
                              where s.UserName == username
                              select s).Count();
                if (query1 == 0)//没注册过
                {
                    User newuser = new User();
                    newuser.UserId = Guid.NewGuid().ToString();
                    newuser.UserName = username;
                    newuser.NickName = (nickname.Trim() == "" || nickname == null) ? username : WordFilter.DoFilter(nickname.Trim());
                    newuser.Password = password;
                    newuser.Disc = (disc == "" || disc == null || disc == "请输入自我描述") ? "这个人太懒了，这里什么也没有" : WordFilter.DoFilter(disc);
                    newuser.Identity = 2;//普通用户
                    newuser.Lv = 1;
                    newuser.Exp = 0;
                    newuser.Access = 0;
                    newuser.Posts = 0;
                    newuser.Replys = 0;
                    newuser.RegisterTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                    newuser.MailBox = mailbox;
                    newuser.MVCGold = 5;
                    newuser.Rank = 1;
                    newuser.GetRankTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                    if (head == null)//没头像
                    {
                        newuser.Head = "default.jpg";
                    }
                    else//有头像
                    {
                        if (head.ContentType == "image/jpeg" || head.ContentType == "image/png")
                        {
                            var realfilename = Guid.NewGuid().ToString() + "_" + head.FileName;
                            var path = $"/Heads/{realfilename}";
                            //head.SaveAs(Server.MapPath(path));//保存文件
                            ZipImage.zipImage(ZipImage.StreamToBytes(head.InputStream)).Save(Server.MapPath(path));//压缩后保存
                            newuser.Head = realfilename;
                        }
                        else
                        {
                            return Content("上传的头像非jpg/png格式！");
                        }
                    }
                    context.Users.Add(newuser);
                    //增加收藏夹信息
                    context.StarLists.Add(new StarList
                    {
                        UserId = newuser.UserId,
                        Star = JsonConvert.SerializeObject(new List<string>())
                    });
                    context.SaveChanges();
                    return Content("注册成功！");

                }
                else//注册过
                {
                    return Content("用户名已注册过，请更换用户名！");
                }
            }
        }

        //验证码
        public ActionResult GetPic()
        {
            ValidateCode vc = new ValidateCode(4);
            byte[] result = vc.GetVerifyCode();
            Session["vercode"] = vc.VerCode;
            return File(result, "image/jpeg jpeg jpg jpe");
        }

        //验证登录
        public ActionResult DoLogin(string username, string password)
        {
            using (Data context = new Data())
            {
                if (username == "" || password == "")
                {
                    return Content("账号或密码为空！");
                }
                var query = (from s in context.Users
                             where s.UserName == username
                             select s).ToList();
                if (query.Count == 0)
                    return Content("找不到对应的账号！");
                if (query[0].Password != password)
                    return Content("密码错误！");
                if (query[0].Access == 3)
                    return Content("该账号已被禁止登录，请联系管理员解锁！");
                //升级
                bool levelup = false;
                while (query[0].Exp >= query[0].Lv * 10)
                {
                    query[0].Exp -= query[0].Lv * 10;
                    query[0].Lv++;
                    query[0].MVCGold += query[0].Lv;//获得等于等级的金币
                    levelup = true;
                }
                if (levelup)
                    context.SaveChanges();
                Session["userid"] = query[0].UserId;
                Session["identity"] = query[0].Identity;//权限（0超级管理员，1管理员，普通用户）
                Session["access"] = query[0].Access;//状态：0正常，1禁止发帖，2禁止发帖回帖，3禁止登录
                Session["rank"] = query[0].Rank;
                return Content("登录成功！");
            }
        }

        //退出登录
        public ActionResult Logout()
        {
            Session["userid"] = null;
            Session["identity"] = null;
            Session["access"] = null;
            Session["rank"] = null;
            return View("Index");
        }

        //修改个人信息
        public ActionResult UpdateInfo(string nickname, HttpPostedFileBase head, string disc, string password)
        {
            if (Session["userid"] == null)
                return Content("无权限！");
            if (nickname.ToLower().Contains("null") || nickname.Trim() == "")
                return Content("不合法的昵称(昵称不得为空格且不得含null)！");
            using (Data context = new Data())
            {
                string userid = Session["userid"].ToString();
                //找到用户（自己）
                var user = (from s in context.Users
                            where s.UserId == userid
                            select s).Single();
                //修改信息
                user.NickName = WordFilter.DoFilter(nickname.Trim());
                user.Password = password;
                user.Disc = WordFilter.DoFilter(disc);
                if (head == null)//没上传新头像
                {
                    user.Head = user.Head;//不变
                }
                else//上传了新头像
                {
                    if (head.ContentType == "image/jpeg" || head.ContentType == "image/png")
                    {
                        var realfilename = Guid.NewGuid().ToString() + "_" + head.FileName;
                        var path = $"/Heads/{realfilename}";
                        //head.SaveAs(Server.MapPath(path));//保存文件
                        ZipImage.zipImage(ZipImage.StreamToBytes(head.InputStream)).Save(Server.MapPath(path));//压缩后保存
                        user.Head = realfilename;
                    }
                    else
                    {
                        return Content("上传的头像非jpg/png格式！");
                    }
                }
                context.SaveChanges();//保存修改
                return Content("修改成功！");
            }
        }
        #endregion

        #region 发帖、回帖与帖子列表
        //(1)获得帖子列表（非管理员）
        public ActionResult GetPosts(int page, int rows)
        {
            using (Data context = new Data())
            {
                string name = Request["name"];
                string value = Request["value"];
                string sort = Request["sort"];
                string order = Request["order"];
                string type = Request["type"];
                //没有搜索内容
                if (value == "" || value == null)
                {
                    //无分类
                    if (type == "-1" || type == "" || type == null)
                    {
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            var count = (from s in context.Posts
                                         where s.IsShown == true
                                         select s).Count();//获得记录条数

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         where s1.IsShown == true
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";
                            var count = (from s in context.Posts
                                         where s.IsShown == true
                                         select s).Count();//获得记录条数

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.IsShown == true
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).OrderBy(sort, order).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //有分类
                    else
                    {
                        int posttype = int.Parse(type);
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            var count = (from s in context.Posts
                                         where s.IsShown == true
                                         select s).Where(u => u.Type == posttype).Count();//获得记录条数

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         where s1.IsShown == true
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Where(u => u.Type == posttype).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";

                            var count = (from s in context.Posts
                                         where s.IsShown == true
                                         select s).Where(u => u.Type == posttype).Count();//获得记录条数

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.IsShown == true
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Where(u => u.Type == posttype).OrderBy(sort, order).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                //按帖子名搜索
                if (name == "1")
                {
                    //无分类
                    if (type == "-1" || type == "" || type == null)
                    {
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         select s1).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         select s1).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).OrderBy(sort, order).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //有分类
                    else
                    {
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            int posttype = int.Parse(type);
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         select s1).Where(u => u.Type == posttype).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Where(u => u.Type == posttype).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";
                            int posttype = int.Parse(type);
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         select s1).Where(u => u.Type == posttype).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s1.Title.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Where(u => u.Type == posttype).OrderBy(sort, order).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                //按发帖人搜索（name=="2"）
                else
                {
                    //无分类
                    if (type == "-1" || type == "" || type == null)
                    {
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         select s1).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         select s1).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).OrderBy(sort, order).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //有分类
                    else
                    {
                        //不排序
                        if (sort == "" || sort == null)
                        {
                            int posttype = int.Parse(type);
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         select s1).Where(u => u.Type == posttype).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).Where(u => u.Type == posttype).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                        //排序
                        else
                        {
                            //翻转（datagrid的按键设计不合理）
                            if (order == "asc")
                                order = "desc";
                            else
                                order = "asc";
                            int posttype = int.Parse(type);
                            var count = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         select s1).Where(u => u.Type == posttype).Count();

                            var query = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         join s3 in context.Users
                                         on s1.LastReplyUserId equals s3.UserId
                                         where s2.NickName.Contains(value) && s1.IsShown == true
                                         orderby s1.IsTop descending, s1.LastReplyTime descending
                                         select new
                                         {
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Time = s1.Time,
                                             Title = s1.Title,
                                             LastReplyTime = s1.LastReplyTime,
                                             Replys = s1.Replys,
                                             LastReplyNickName = s3.NickName,
                                             AccessTime = s1.AccessTime,
                                             IsTop = s1.IsTop,
                                             Likes = s1.Likes,
                                             Locked = s1.Locked,
                                             Elite = s1.Elite,
                                             Type = s1.Type,
                                             Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                                         }).OrderBy(sort, order).Where(u => u.Type == posttype).Skip((page - 1) * rows).Take(rows).ToList();
                            Dictionary<string, Object> map = new Dictionary<string, object>();
                            map.Add("rows", query);
                            map.Add("total", count);
                            return Json(map, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
        }

        //(2)得到某个人的所有帖子（某个用户自己）
        public ActionResult GetMyPosts(int page, int rows)
        {
            using (Data context = new Data())
            {
                if (Session["userid"] == null)
                    return Content("未登录用户不允许使用此功能！");
                string userid = Session["userid"].ToString();
                var count = (from s in context.Posts
                             where s.UserId == userid
                             select s).Count();//获得记录条数

                var query = (from s1 in context.Posts
                             join s2 in context.Users
                             on s1.UserId equals s2.UserId
                             join s3 in context.Users
                             on s1.LastReplyUserId equals s3.UserId
                             where s1.UserId == userid
                             orderby s1.IsTop descending, s1.LastReplyTime descending
                             select new
                             {
                                 PostId = s1.PostId,
                                 UserName = s2.UserName,
                                 Time = s1.Time,
                                 Title = s1.Title,
                                 LastReplyTime = s1.LastReplyTime,
                                 Replys = s1.Replys,
                                 LastReplyNickName = s3.NickName,
                                 Status = s1.IsShown,
                                 AccessTime = s1.AccessTime,
                                 IsTop = s1.IsTop,
                                 Likes = s1.Likes,
                                 Locked = s1.Locked,
                                 Elite = s1.Elite,
                                 Type = s1.Type,
                                 Rec = s1.IsTop * 1000 + (s1.Elite ? 100 : 0) + s1.Replys + s1.Likes * 0.3 + s1.AccessTime * 0.1
                             }).Skip((page - 1) * rows).Take(rows).ToList();
                Dictionary<string, Object> map = new Dictionary<string, object>();
                map.Add("rows", query);
                map.Add("total", count);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //【帖子浏览】得到某个帖子和回复信息（上下翻页）
        public ActionResult GetPostDetail(string postid, int page, int rows)
        {
            if (postid != null)
            {
                using (Data context = new Data())
                {
                    var count = (from s in context.Replys
                                 where s.PostId == postid
                                 select s).Count() + 1;

                    if (page == 1)//第一页
                    {
                        var query1 = (from s1 in context.Posts
                                      join s2 in context.Users
                                      on s1.UserId equals s2.UserId
                                      where s1.PostId == postid
                                      select new
                                      {
                                          UserName = s2.UserName,
                                          Lv = s2.Lv,
                                          Head = s2.Head,
                                          Time = s1.Time,
                                          Title = s1.Title,
                                          Content = s1.Content,
                                          Identity = s2.Identity,
                                          NickName = s2.NickName,
                                          Rank = s2.Rank
                                      }).Single();

                        var query = (from s1 in context.Replys
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     where s1.PostId == postid
                                     orderby s1.Time
                                     select new
                                     {
                                         UserName = s2.UserName,
                                         Lv = s2.Lv,
                                         Head = s2.Head,
                                         Time = s1.Time,
                                         Title = "",
                                         Content = s1.Content,
                                         Identity = s2.Identity,
                                         NickName = s2.NickName,
                                         Rank = s2.Rank
                                     }).Skip(0).Take(rows - 1).ToList();

                        query.Insert(0, query1);
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                    else//后面的页
                    {
                        var query = (from s1 in context.Replys
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     where s1.PostId == postid
                                     orderby s1.Time ascending
                                     select new
                                     {
                                         UserName = s2.UserName,
                                         Lv = s2.Lv,
                                         Head = s2.Head,
                                         Time = s1.Time,
                                         Title = "",
                                         Content = s1.Content,
                                         Identity = s2.Identity,
                                         NickName = s2.NickName,
                                         Rank = s2.Rank
                                     }).Skip((page - 1) * rows - 1).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                return Content("No Post!");
            }
        }

        //【帖子预览】帖子预览信息（预览帖子）
        public ActionResult PostPreview(string postid)
        {
            using (Data context = new Data())
            {
                //能否普找到帖子帖子
                var count = (from s in context.Posts
                             where s.PostId == postid
                             select s).Count();
                if (count == 0)
                    return Content("找不到对应帖子！");
                //找到了帖子
                var thispost = (from s in context.Posts
                                where s.PostId == postid
                                select s).Single();
                #region 权限控制
                //得到用户id（可检测为空情况）
                string userid = (Session["userid"] == null) ? "" : Session["userid"].ToString();
                //得到用户身份
                int identity = 2;//默认普通用户
                //若登陆得到权限
                if (userid != "")
                {
                    identity = (from s in context.Users
                                where s.UserId == userid
                                select s.Identity).Single();
                }
                //权限控制（不显示的帖子仅自己、管理员、超管可访问）
                if (thispost.IsShown == false && thispost.UserId != userid && identity != 0 && identity != 1)
                    return Json(new
                    {
                        Title = "隐藏帖子",
                        Content = "该帖子已被设置为隐藏，无法预览（隐藏的帖子仅自己、管理员、超级管理员可以浏览）！",
                        UserName = "隐藏",
                        Lv = "隐藏",
                        Time = thispost.Time,
                        Head = "default.jpg"
                    }, JsonRequestBehavior.AllowGet);
                #endregion
                //返回帖子
                thispost.AccessTime++;
                context.SaveChanges();
                var query1 = (from s1 in context.Posts
                              join s2 in context.Users
                              on s1.UserId equals s2.UserId
                              where s1.PostId == postid
                              select new
                              {
                                  UserName = s2.UserName,
                                  Lv = s2.Lv,
                                  Head = s2.Head,
                                  Time = s1.Time,
                                  Title = s1.Title,
                                  Content = s1.Content,
                                  IsTop = s1.IsTop,
                                  AccessTime = s1.AccessTime,
                                  Replys = s1.Replys,
                                  Likes = s1.Likes,
                                  Identity = s2.Identity,
                                  NickName = s2.NickName,
                                  Type = s1.Type,
                                  Rank = s2.Rank
                              }).Single();
                return Json(query1, JsonRequestBehavior.AllowGet);
            }
        }

        //得到帖子名称
        public ActionResult GetPostName(string id)
        {
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == id
                             select s.Title).Single();
                return Content(query);
            }
        }

        //发帖
        [ValidateInput(false)]
        public ActionResult AddPost(string title, string content, int type)
        {
            if (Session["userid"] == null)
                return Content("未登录用户不得发帖！");
            if (Session["access"].ToString() != "0")
                return Content("该用户已被禁止发帖，请联系超级管理员解锁权限！");
            if (title.Trim() == "" || title == null)
                return Content("发帖失败！帖子标题不得全为空！");
            if (type == 1 && Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
                return Content("错误，仅管理员/超级管理员可以发表版务帖子！");
            using (Data context = new Data())
            {
                //分类不合法则调整（0-6）
                if (type < 0 || type > 6)
                    type = 0;
                //增加帖子
                context.Posts.Add(new Post
                {
                    PostId = Guid.NewGuid().ToString(),
                    UserId = Session["userid"].ToString(),
                    Time = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                    Title = WordFilter.DoFilter(title.Trim()),
                    Content = WordFilter.DoFilter(content),
                    Replys = 0,
                    LastReplyTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                    LastReplyUserId = Session["userid"].ToString(),
                    IsShown = true,
                    AccessTime = 0,
                    IsTop = 0,
                    Likes = 0,
                    Locked = false,
                    Elite = false,
                    Type = type
                });

                //发帖增加5经验
                string userid = Session["userid"].ToString();
                var query = (from s in context.Users
                             where s.UserId == userid
                             select s).Single();
                query.Exp += 5;
                if (query.Exp >= query.Lv * 10)
                {
                    query.Exp -= query.Lv * 10;
                    query.Lv++;
                    query.MVCGold += query.Lv;//获得等于等级的金币
                }
                query.Posts++;
                query.MVCGold += 3;

                context.SaveChanges();
            }
            return Content("发帖成功(增加5经验、3金币)！");
        }

        //编辑帖子
        [ValidateInput(false)]
        public ActionResult DoEditPost(string postid, string title, string content, int type)
        {
            if (Session["userid"] == null)
                return Content("未登录用户不得编辑帖子！");
            if (title.Trim() == "" || title == null)
                return Content("修改失败！帖子标题不得全为空！");
            if (type == 1 && Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
                return Content("错误，仅管理员/超级管理员可以发表版务帖子！");
            //当前用户信息
            string userid = Session["userid"].ToString();
            string identity = Session["identity"].ToString();
            using (Data context = new Data())
            {
                //分类不合法则调整（0-6）
                if (type < 0 || type > 6)
                    type = 0;
                //帖子
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                //发帖人
                var user = (from s in context.Users
                            where s.UserId == query.UserId
                            select s).Single();

                if (user.UserId == userid || identity == "0")
                {
                    query.Content = WordFilter.DoFilter(content);
                    query.Title = WordFilter.DoFilter(title.Trim());
                    query.Type = type;
                    context.SaveChanges();
                    return Content("修改成功！");
                }
                else
                {
                    return Content("只有帖主和超级管理员有权限修改帖子！");
                }
            }
        }

        //回帖
        [ValidateInput(false)]
        public ActionResult AddReply(string postid, string content)
        {
            if (Session["userid"] == null)
                return Content("未登录用户不得回帖！");
            if (Session["access"].ToString() != "0" && Session["access"].ToString() != "1")
                return Content("该用户已被禁止回帖，请联系超级管理员解锁权限！");
            using (Data context = new Data())
            {
                //查询帖子信息
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                //锁定的帖子仅管理员可回复
                if (query.Locked == true)
                {
                    string uid = Session["userid"].ToString();
                    var identity = (from s in context.Users
                                    where s.UserId == uid
                                    select s.Identity).Single();
                    if (identity != 0 && identity != 1)
                        return Content("该帖子已锁定，仅管理员/超级管理员可回复！");
                }
                //加回复
                context.Replys.Add(new Reply
                {
                    ReplyId = Guid.NewGuid().ToString(),
                    PostId = postid,
                    UserId = Session["userid"].ToString(),
                    Time = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss"),
                    Content = WordFilter.DoFilter(content)
                });
                //修改帖子相关信息
                query.Replys++;
                query.LastReplyTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                query.LastReplyUserId = Session["userid"].ToString();
                //回帖人增加经验、回复数
                string userid = Session["userid"].ToString();
                var query2 = (from s in context.Users
                              where s.UserId == userid
                              select s).Single();
                query2.Exp += 2;
                if (query2.Exp >= query2.Lv * 10)
                {
                    query2.Exp -= query2.Lv * 10;
                    query2.Lv++;
                    query2.MVCGold += query2.Lv;//获得等于等级的金币
                }
                query2.Replys++;
                query2.MVCGold++;
                //发帖人增加经验（不包括自己）
                var query3 = (from s in context.Users
                              where s.UserId == query.UserId && s.UserId != userid
                              select s).ToList();
                if (query3.Count != 0)
                {
                    query3[0].Exp += 1;
                    if (query3[0].Exp >= query3[0].Lv * 10)
                    {
                        query3[0].Exp -= query3[0].Lv * 10;
                        query3[0].Lv++;
                        query3[0].MVCGold += query3[0].Lv;//获得等于等级的金币
                    }
                }
                context.SaveChanges();
            }
            return Content("回帖成功(增加2经验、1金币，发帖人增加1经验)！");
        }
        #endregion

        #region 管理相关
        //(3)管理员：展示所有帖子
        public ActionResult GetAllPosts(int page, int rows)
        {
            if (Session["userid"] == null)
                return Content("无权限！");
            if (Session["identity"].ToString() == "2")
                return Content("无权限！");
            using (Data context = new Data())
            {
                string name = Request["name"];
                string value = Request["value"];
                if (name == "3" || name == "4" || name == "5")
                {
                    //3为所有锁定的帖子
                    if (name == "3")
                    {
                        var count = (from s1 in context.Posts
                                     where s1.IsShown == true
                                     select s1).Count();

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     where s1.Locked == true
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                    //4为所有隐藏的帖子
                    else if (name == "4")
                    {
                        var count = (from s1 in context.Posts
                                     where s1.IsShown == false
                                     select s1).Count();

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     where s1.IsShown == false
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                    //5为所有精华的帖子
                    else
                    {
                        var count = (from s1 in context.Posts
                                     where s1.IsShown == false
                                     select s1).Count();

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     where s1.Elite == true
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    //所有帖子
                    if (value == "" || value == null)//无搜索
                    {
                        var count = context.Posts.Count();//获得记录条数

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                    //按帖子名搜索
                    if (name == "1")
                    {
                        var count = (from s1 in context.Posts
                                     where s1.Title.Contains(value)
                                     select s1).Count();

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     where s1.Title.Contains(value)
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                    //按发帖人搜索（name=="2"）
                    else
                    {
                        var count = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     where s2.NickName.Contains(value)
                                     select s1).Count();

                        var query = (from s1 in context.Posts
                                     join s2 in context.Users
                                     on s1.UserId equals s2.UserId
                                     join s3 in context.Users
                                     on s1.LastReplyUserId equals s3.UserId
                                     where s2.NickName.Contains(value)
                                     orderby s1.IsTop descending, s1.LastReplyTime descending
                                     select new
                                     {
                                         PostId = s1.PostId,
                                         UserName = s2.UserName,
                                         NickName = s2.NickName,
                                         Time = s1.Time,
                                         Title = s1.Title,
                                         LastReplyTime = s1.LastReplyTime,
                                         Replys = s1.Replys,
                                         LastReplyUserName = s3.UserName,
                                         LastReplyNickName = s3.NickName,
                                         Status = s1.IsShown,
                                         AccessTime = s1.AccessTime,
                                         IsTop = s1.IsTop,
                                         Likes = s1.Likes,
                                         Locked = s1.Locked,
                                         Elite = s1.Elite,
                                         Type = s1.Type
                                     }).Skip((page - 1) * rows).Take(rows).ToList();
                        Dictionary<string, Object> map = new Dictionary<string, object>();
                        map.Add("rows", query);
                        map.Add("total", count);
                        return Json(map, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }

        //管理员：隐藏/恢复帖子
        public ActionResult ChangeStatus(string postid)
        {
            if (Session["userid"] == null)
                return Content("无权限！");
            if (Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
            {
                return Content("错误！非超级管理员/管理员不得隐藏/恢复帖子！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                query.IsShown = !query.IsShown;
                context.SaveChanges();
            }
            return Content("修改成功！");
        }

        //普通用户：隐藏/恢复帖子
        public ActionResult ChangeMyStatus(string postid)
        {
            if (Session["userid"] == null)
                return Content("无权限！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();

                //发帖人
                var user = (from s in context.Users
                            where s.UserId == query.UserId
                            select s).Single();

                if (user.UserId == userid)
                {
                    query.IsShown = !query.IsShown;
                    context.SaveChanges();
                    return Content("修改成功！");
                }
                else
                {
                    return Content("只有权限修改自己帖子的状态！");
                }
            }
        }

        //管理员：置顶/取消置顶帖子
        public ActionResult ChangeTop(string postid)
        {
            //权限验证
            if (Session["userid"] == null)
            {
                return Content("无权限！");
            }
            if (Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
            {
                return Content("错误！非超级管理员/管理员不得置顶/取消置顶帖子！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                if (query.IsTop == 0)
                    query.IsTop = 1;
                else if (query.IsTop == 1)
                    query.IsTop = 0;
                context.SaveChanges();
            }
            return Content("修改成功！");
        }

        //管理员：加精/取消加精帖子
        public ActionResult ChangeElite(string postid)
        {
            //权限验证
            if (Session["userid"] == null)
            {
                return Content("无权限！");
            }
            if (Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
            {
                return Content("错误！非超级管理员/管理员不得加精/取消加精帖子！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                query.Elite = !query.Elite;
                context.SaveChanges();
            }
            return Content("修改成功！");
        }

        //管理员：锁定/取消锁定帖子
        public ActionResult ChangeLock(string postid)
        {
            //权限验证
            if (Session["userid"] == null)
            {
                return Content("无权限！");
            }
            if (Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1")
            {
                return Content("错误！非超级管理员/管理员不得加精/取消加精帖子！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                query.Locked = !query.Locked;
                context.SaveChanges();
            }
            return Content("修改成功！");
        }

        //（超级）管理员：查询所有用户
        public ActionResult GetUsers(int page, int rows)
        {
            if (Session["userid"] == null)
                return Content("无权限！");
            if (Session["identity"].ToString() != "0")
                return Content("无权限！");
            using (Data context = new Data())
            {
                string username = Request["username"];
                if (username == null || username == "")
                {
                    var count = context.Users.Count();//获得记录条数

                    var query = (from s in context.Users
                                 orderby s.RegisterTime
                                 select s).Skip((page - 1) * rows).Take(rows).ToList();
                    Dictionary<string, Object> map = new Dictionary<string, object>();
                    map.Add("rows", query);
                    map.Add("total", count);
                    return Json(map, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var count = (from s in context.Users
                                 where s.NickName.Contains(username)
                                 select s).Count();

                    var query = (from s in context.Users
                                 where s.NickName.Contains(username)
                                 orderby s.RegisterTime
                                 select s).Skip((page - 1) * rows).Take(rows).ToList();
                    Dictionary<string, Object> map = new Dictionary<string, object>();
                    map.Add("rows", query);
                    map.Add("total", count);
                    return Json(map, JsonRequestBehavior.AllowGet);
                }
            }
        }

        //得到userid
        public ActionResult GetUserId()
        {
            if (Session["userid"] != null)
                return Content(Session["userid"].ToString());
            else
                return Content("未登录！");
        }

        //设置身份
        public ActionResult ChangeIdentity(string userid, string newidentity)
        {
            //权限验证
            if (Session["userid"] == null)
                return Content("无权限！");
            if (Session["identity"].ToString() != "0")
            {
                return Content("错误！非超级管理员不得设置身份！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Users
                             where s.UserId == userid
                             select s).Single();
                if (userid == "root")
                {
                    return Content("错误！不允许修改root用户的超级管理员身份！");
                }
                if ((query.Identity == 0 || newidentity == "0") && Session["userid"].ToString() != "root")
                {
                    return Content("错误！非root用户不能修改超级管理员身份，也不能授予超级管理员身份！");
                }
                else
                {
                    query.Identity = int.Parse(newidentity);
                    context.SaveChanges();
                    return Content("修改成功！");
                }
            }
        }

        //设置权限
        public ActionResult ChangeAccess(string userid, string newaccess)
        {
            //权限验证
            if (Session["userid"] == null)
                return Content("无权限！");
            if (Session["identity"].ToString() != "0")
            {
                return Content("错误！非超级管理员不得设置权限！");
            }
            using (Data context = new Data())
            {
                var query = (from s in context.Users
                             where s.UserId == userid
                             select s).Single();
                if (userid == "root")
                {
                    return Content("错误！不允许修改root用户的权限！");
                }
                if (query.Identity == 0 && Session["userid"].ToString() != "root" && userid != Session["userid"].ToString())
                {
                    return Content("错误！非root用户不能修改除自己外的超级管理员的权限！");
                }
                else
                {
                    query.Access = int.Parse(newaccess);
                    context.SaveChanges();
                    return Content("修改成功！");
                }
            }
        }
        #endregion

        #region 直连数据库
        public ActionResult Excuate(string sql, string type)
        {
            //1:ExecuteNonQuery()
            //2:ExecuteScalar()
            //3:SQLiteDataAdapter()
            if (Session["userid"] == null || Session["userid"].ToString() != "root")
                return Content("致命错误！没有root用户权限，请刷新！");
            if (sql.ToUpper().Contains("CREATE TABLE") || sql.ToUpper().Contains("DROP TABLE") || sql.ToUpper().Contains("ALTER TABLE"))
                return Content("致命错误！不允许创建/修改/删除表！");
            else
            {
                string connstr = "data source=" + Server.MapPath("/App_Data/Data.db");
                using (SQLiteConnection conn = new SQLiteConnection(connstr))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();
                        cmd.CommandText = sql;
                        StringBuilder sb = new StringBuilder();
                        try
                        {
                            if (type == "1")
                            {
                                var result = cmd.ExecuteNonQuery();
                                sb.Append("成功！影响行数：" + result.ToString());
                            }
                            else if (type == "2")
                            {
                                var result = cmd.ExecuteScalar();
                                sb.Append("成功！运行结果：" + result.ToString());
                            }
                            else if (type == "3")
                            {
                                DataTable dt = new DataTable();
                                SQLiteDataAdapter sda = new SQLiteDataAdapter(cmd);
                                sda.Fill(dt);
                                conn.Close();
                                sb.Append("<table cellSpacing='0' cellPadding='0' width ='100%' border='1' style='text-align:center'>");
                                sb.Append("<tr>");
                                sb.Append("<th>RowId</th>");
                                foreach (DataColumn column in dt.Columns)
                                {
                                    sb.Append("<th>" + column.ColumnName + "</th>");
                                }
                                sb.Append("</tr>");
                                int iColsCount = dt.Columns.Count;
                                for (int j = 0; j < dt.Rows.Count; j++)
                                {
                                    sb.Append("<tr>");
                                    sb.Append("<td>" + ((int)(j + 1)).ToString() + "</td>");
                                    for (int k = 0; k < dt.Columns.Count; k++)
                                    {
                                        sb.Append("<td>");
                                        object obj = dt.Rows[j][k];
                                        if (obj == DBNull.Value || obj.ToString() == "")
                                        {
                                            obj = " ";//如果是NULL或空，则在HTML里面使用一个空格替换之
                                        }
                                        //不encode就会直接显示图片，不好！
                                        string strCellContent = WebUtility.HtmlEncode(obj.ToString().Trim());
                                        sb.Append("<span>" + strCellContent + "</span>");
                                        sb.Append("</td>");
                                    }
                                    sb.Append("</tr>");
                                }
                                sb.Append("</table>");
                            }
                        }
                        catch (Exception ex)
                        {
                            conn.Close();
                            return Content("错误！原因：" + ex.ToString());
                        }
                        conn.Close();
                        return Content(sb.ToString());
                    }
                }
            }

        }
        #endregion

        #region 其他功能
        //找回密码
        public ActionResult FindPass(string username, string mailbox)
        {
            using (Data context = new Data())
            {
                var query = (from s in context.Users
                             where s.UserName == username && s.MailBox == mailbox
                             select s).ToList();
                if (query.Count == 0)
                {
                    return Content("用户名和邮箱不匹配，无法找回密码");
                }
                else
                {
                    SendMail.DoSend(query[0].Password, mailbox, username);
                    return Content("密码已发送至绑定的邮箱");
                }
            }
        }

        //获得金币数
        public ActionResult HasCoins()
        {
            //没登录是0
            if (Session["userid"] == null)
                return Json(0, JsonRequestBehavior.AllowGet);
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                var query = (from s in context.Users
                             where s.UserId == userid
                             select s).Single();
                return Json(query.MVCGold, JsonRequestBehavior.AllowGet);
            }
        }

        //打赏金币
        public ActionResult GiveCoins(string postid, string givecoins)
        {
            if (Session["userid"] == null)
                return Content("未登录，无法打赏！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                //给出金币的用户
                var query1 = (from s in context.Users
                              where s.UserId == userid
                              select s).Single();
                //发帖用户id
                var userid2 = (from s in context.Posts
                               where s.PostId == postid
                               select s.UserId).Single();

                //获得金币用户
                var query2 = (from s in context.Users
                              where s.UserId == userid2
                              select s).Single();

                if (query1.MVCGold < int.Parse(givecoins))
                    return Content("打赏失败！您可能在别的帖子进行了打赏，金币不足！");
                else
                {
                    query1.MVCGold -= int.Parse(givecoins);
                    query2.MVCGold += int.Parse(givecoins);
                    context.SaveChanges();
                    return Content($"打赏成功！消耗金币{int.Parse(givecoins)}，用户[{query2.UserName}]获得金币{int.Parse(givecoins)}！");
                }

            }
        }

        //点赞
        public ActionResult DoLike(string postid)
        {
            if (postid == null || postid == "")
                return Content("找不到对应帖子！");
            using (Data context = new Data())
            {
                var query = (from s in context.Posts
                             where s.PostId == postid
                             select s).Single();
                query.Likes++;
                context.SaveChanges();
                return Content("点赞成功！");
            }
        }

        //收藏
        public ActionResult DoStar(string postid)
        {
            if (postid == null || postid == "")
                return Content("找不到对应帖子！");
            if (Session["userid"] == null)
                return Content("无权限！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                //得到表序列化后的字符串
                var query = (from s in context.StarLists
                             where s.UserId == userid
                             select s).Single();
                //反序列化
                List<string> stars = JsonConvert.DeserializeObject<List<string>>(query.Star);
                if (stars.Contains(postid))
                {
                    return Content("已收藏，不需要重复收藏！");
                }
                else
                {
                    stars.Add(postid);
                    query.Star = JsonConvert.SerializeObject(stars);
                    context.SaveChanges();
                    return Content("收藏成功！");

                }
            }
        }

        //取消收藏
        public ActionResult DoUnStar(string postid)
        {
            if (postid == null || postid == "")
                return Content("找不到对应帖子！");
            if (Session["userid"] == null)
                return Content("无权限！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                //得到表序列化后的字符串
                var query = (from s in context.StarLists
                             where s.UserId == userid
                             select s).Single();
                //反序列化
                List<string> stars = JsonConvert.DeserializeObject<List<string>>(query.Star);
                if (!stars.Contains(postid))
                {
                    return Content("未收藏，无法取消收藏！");
                }
                else
                {
                    stars.Remove(postid);
                    query.Star = JsonConvert.SerializeObject(stars);
                    context.SaveChanges();
                    return Content("取消收藏成功！");

                }
            }
        }

        //得到是否收藏
        public ActionResult IsStar(string postid)
        {
            if (postid == null || postid == "")
                return Content("找不到对应帖子！");
            if (Session["userid"] == null)
                return Content("无权限！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                //得到表序列化后的字符串
                var query = (from s in context.StarLists
                             where s.UserId == userid
                             select s).Single();
                //反序列化
                List<string> stars = JsonConvert.DeserializeObject<List<string>>(query.Star);
                if (stars.Contains(postid))
                {
                    return Content("true");
                }
                else
                {
                    return Content("false");

                }
            }
        }

        //【4】得到收藏的帖子
        public ActionResult GetStars(int page, int rows)
        {
            using (Data context = new Data())
            {
                if (Session["userid"] == null)
                    return Content("未登录用户不允许使用此功能！");
                string userid = Session["userid"].ToString();

                //得到表序列化后的字符串
                var starlist = (from s in context.StarLists
                                where s.UserId == userid
                                select s).Single();

                //反序列化（所有收藏）
                List<string> allstars = JsonConvert.DeserializeObject<List<string>>(starlist.Star);

                //收藏条数
                var count = allstars.Count();

                //所选的收藏（无需排序）
                var stars = (from s in allstars
                             select s).Skip((page - 1) * rows).Take(rows).ToList();

                //具体的收藏信息（用List<object>可以获得匿名类）
                var query = new List<object>();

                for (int i = 0; i < stars.Count; i++)
                {
                    string postid = stars[i];//获得postid
                    //只选取部分信息
                    var ifexist = (from s in context.Posts
                                   where s.PostId == postid
                                   select s).Count();

                    if (ifexist != 0)
                    {
                        var thisquery = (from s1 in context.Posts
                                         join s2 in context.Users
                                         on s1.UserId equals s2.UserId
                                         where s1.PostId == postid
                                         select new
                                         {
                                             Time = s1.Time,
                                             PostId = s1.PostId,
                                             NickName = s2.NickName,
                                             Title = s1.Title + (s1.IsShown == true ? "" : "[隐藏]"),
                                             Type = s1.Type
                                         }).Single();
                        //加入列表
                        query.Add(thisquery);
                    }
                    else
                    {
                        //已失效帖子
                        var badpost = new
                        {
                            PostId = postid,
                            UserName = "已失效",
                            Title = "已失效帖子，请及时清除！"
                        };
                        query.Add(badpost);
                    }
                }

                Dictionary<string, Object> map = new Dictionary<string, object>();
                map.Add("rows", query);
                map.Add("total", count);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //是否签到
        public bool HasSigned(string userid)
        {
            DateTime chinatime = DateTime.UtcNow.AddHours(8);
            using (Data context = new Data())
            {
                //查询签到记录条数
                var query = (from s in context.CheckIns
                             where s.UserId == userid
                             select s).Count();
                //从来没有签到记录
                if (query == 0)
                    return false;//允许签到
                //有签到记录，取最大日期的                
                var query2 = (from s in context.CheckIns
                              where s.UserId == userid
                              orderby s.CheckInTime descending
                              select s.CheckInTime).First();
                //根据string得到datetime，不可直接用datetime，会报错！
                DateTimeFormatInfo dtFormat = new DateTimeFormatInfo();
                dtFormat.ShortDatePattern = "yyyy/MM/dd";
                DateTime dt = Convert.ToDateTime(query2, dtFormat);
                //在同一天，说明签到过
                if (dt.Day == chinatime.Day && dt.Month == chinatime.Month && dt.Year == chinatime.Year)
                    return true;//不允许签到
                else//不在同一天
                    return false;//允许签到
            }
        }

        //是否签到
        public ActionResult CheckHasSigned()
        {
            if (Session["userid"] == null)
                return Content("false");
            return Content(HasSigned(Session["userid"].ToString()) ? "true" : "false");

        }

        //签到
        public ActionResult SignUp()
        {
            if (Session["userid"].ToString() == null)
                return Content("无权限！");
            string userid = Session["userid"].ToString();
            using (Data context = new Data())
            {
                //签到过
                if (HasSigned(userid))
                    return Content("今日已签到过，不可重复签到！");
                else
                {
                    context.CheckIns.Add(new CheckIn
                    {
                        UserId = userid,
                        CheckInTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd")
                    });
                    var user = (from s in context.Users
                                where s.UserId == userid
                                select s).Single();
                    user.Exp += 10;
                    user.MVCGold += (int)(Math.Sqrt(user.Lv)) + 5;
                    if (user.Exp >= user.Lv * 10)
                    {
                        user.Exp -= user.Lv * 10;
                        user.Lv++;
                        user.MVCGold += user.Lv;//获得等于等级的金币
                    }
                    context.SaveChanges();
                    return Content($"签到成功！获得经验{10}，金币{(int)(Math.Sqrt(user.Lv)) + 5}");
                }
            }
        }
        #endregion

        #region 附加界面（二次开发）
        //游戏列表（界面）
        public ActionResult GamePage()
        {
            string dir = Server.MapPath("/") + "Games";
            string[] dirs = Directory.GetDirectories(dir);
            StringBuilder sb = new StringBuilder();
            sb.Append("<table border=\"1\" style=\"text-align:center\">");
            sb.Append("<tr><th width=\"300px\">游戏名称</th><th width=\"150px\">访问链接</th></tr>");
            for (int i = 0; i < dirs.Length; i++)
            {
                string[] spilts = dirs[i].Split('\\');
                sb.Append("<tr>");
                sb.Append("<td>");
                sb.Append(spilts[spilts.Length - 1]);
                sb.Append("</td>");
                sb.Append("<td>");
                sb.Append($"<a href=\"/Games/{spilts[spilts.Length - 1]}/index.html\" target=\"_blank\">游戏链接</a>");
                sb.Append("</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            ViewData["gametable"] = sb.ToString();
            return View();
        }

        //排行榜（界面）
        public ActionResult RankList()
        {
            return View();
        }

        //获得头衔排行数据
        public ActionResult GetRankList()
        {
            using (Data context = new Data())
            {
                Dictionary<string, Object> map = new Dictionary<string, object>();
                var count = context.Users.Count();
                if (count > 10)
                    count = 10;
                map.Add("total", count);
                var ranktop = (from s in context.Users
                               orderby s.Rank descending, s.GetRankTime ascending//获得头衔越早越好（降序）
                               select new
                               {
                                   s.NickName,
                                   s.Rank,
                                   s.GetRankTime
                               }).Take(count).ToList();
                map.Add("rows", ranktop);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //获得金币排行数据
        public ActionResult GetGoldList()
        {
            using (Data context = new Data())
            {
                Dictionary<string, Object> map = new Dictionary<string, object>();
                var count = context.Users.Count();
                if (count > 10)
                    count = 10;
                map.Add("total", count);
                var goldtop = (from s in context.Users
                               orderby s.MVCGold descending, s.Rank descending//金币相同，头衔高的优先
                               select new
                               {
                                   s.NickName,
                                   s.Rank,
                                   s.MVCGold
                               }).Take(count).ToList();
                map.Add("rows", goldtop);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //获得等级排行数据
        public ActionResult GetLvList()
        {
            using (Data context = new Data())
            {
                Dictionary<string, Object> map = new Dictionary<string, object>();
                var count = context.Users.Count();
                if (count > 10)
                    count = 10;
                map.Add("total", count);
                var lvtop = (from s in context.Users
                             orderby s.Lv descending, s.Exp descending, s.Rank descending//等级经验相同，头衔高的优先
                             select new
                             {
                                 s.NickName,
                                 s.Rank,
                                 s.Lv,
                                 s.Exp
                             }).Take(count).ToList();
                map.Add("rows", lvtop);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //商店界面
        public ActionResult Shop()
        {
            return View("");
        }

        //得到下一个可以购买的头衔
        public ActionResult GetNextBuy()
        {
            if (Session["userid"] == null)
                return Content("0");
            else
            {
                if (Session["rank"].ToString() == "10")
                    return Content("0");
                else
                    return Content((int.Parse(Session["rank"].ToString()) + 1).ToString());
            }
        }

        //购买头衔
        public ActionResult BuyRank()
        {
            if (Session["rank"] == null)
                return Content("未登录，无权限购买！");
            else
            {
                int rank = int.Parse(Session["rank"].ToString());
                if (rank == 10)
                    return Content("你已经满级啦，不需要继续购买头衔！多余的金币可以用来打赏喜欢的帖子~");
                else
                {
                    int[] updategold = new int[] { 0, 10, 30, 60, 100, 200, 400, 700, 1000, 2233 };
                    string[] ranks = new string[] {"", "坚韧黑铁", "英勇黄铜", "不屈白银", "荣耀黄金", "华贵铂金",
                        "璀璨钻石", "超凡大师", "傲世宗师", "最强王者", "无上至尊" };
                    int costgold = updategold[rank];//rank=1:3;rank=9:2233
                    using (Data context = new Data())
                    {
                        string userid = Session["userid"].ToString();
                        var user = (from s in context.Users
                                    where s.UserId == userid
                                    select s).Single();
                        int hasgold = user.MVCGold;
                        if (hasgold >= costgold)
                        {
                            user.MVCGold -= costgold;
                            user.Rank++;
                            Session["rank"] = user.Rank;
                            context.SaveChanges();
                            return Content($"恭喜你升级成功！花费金币{costgold}，将头衔从{ranks[rank]}提升到了{user.Rank}！请再接再厉哦！");
                        }
                        else
                        {
                            return Content("啊哦，金币不足！请发帖/回帖/签到积累足够的金币再来升级头衔吧~");
                        }
                    }
                }
            }
        }

        //数据
        public ActionResult Datas()
        {
            if (Session["identity"] == null || (Session["identity"].ToString() != "0" && Session["identity"].ToString() != "1"))
                return Content("无权限访问！");
            //总访客量、当前访客量、总注册用户数、总帖子、总回复；ok
            //总体帖子分类饼状图（ajax方法）——1  ok 
            //月帖子、月回复；——2   ok
            //当月每天帖子、回复折线图（ajax方法）——2  ok
            //日帖子、日回复；——3   ok
            //当日各分类帖子、回复柱状图（ajax方法）——3  ok（非最佳方案）
            using (Data context = new Data())
            {
                //总访客量
                ViewData["AccessTimes"] = Convert.ToInt32(System.Web.HttpContext.Current.Application["AccessTimes"]);
                //当前访客量
                ViewData["OnLineUserCount"] = Convert.ToInt32(System.Web.HttpContext.Current.Application["OnLineUserCount"]);
                //总注册用户
                ViewData["Users"] = context.Users.Count();
                //总帖子
                ViewData["Posts"] = context.Posts.Count();
                //总回复
                ViewData["Replys"] = context.Replys.Count();
                return View();
            }
        }

        //数据
        public ActionResult GetData(string date, int type)
        {
            using (Data context = new Data())
            {
                //总体帖子分类饼状图
                if (type == 1)
                {
                    string[] types = new string[] { "无", "版务", "二次元", "游戏", "小说", "技术", "杂谈" };
                    var query = (from s in context.Posts
                                 group s by new { s.Type } into g
                                 select new
                                 {
                                     value = g.Count(),
                                     type = g.Key.Type
                                 }).ToList();
                    List<object> result = new List<object>();
                    foreach (var item in query)
                    {
                        result.Add(new
                        {
                            value = item.value,
                            name = types[item.type]
                        });
                    }
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                //月数据
                else if (type == 2)
                {
                    //xxxx-xx-xx
                    date = date.Substring(0, 7);
                    Dictionary<string, Object> map = new Dictionary<string, object>();
                    //帖子数
                    var post = (from s in context.Posts
                                where s.Time.Contains(date)
                                select s).Count();
                    //回复数
                    var reply = (from s in context.Replys
                                 where s.Time.Contains(date)
                                 select s).Count();
                    //日期
                    List<string> days = new List<string>();
                    List<int> posts = new List<int>();
                    List<int> replys = new List<int>();
                    for (int i = 1; i <= DateTime.DaysInMonth(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(5, 2))); i++)
                    {
                        days.Add(i.ToString());
                        string nowdate = date + "/" + i.ToString().PadLeft(2, '0');
                        var query1 = (from s in context.Posts
                                      where s.Time.Contains(nowdate)
                                      select s).Count();
                        posts.Add(query1);
                        var query2 = (from s1 in context.Replys
                                      join s2 in context.Posts
                                      on s1.PostId equals s2.PostId
                                      where s1.Time.Contains(nowdate)
                                      select s1).Count();
                        replys.Add(query2);
                    }
                    //每日帖子数
                    map.Add("post", post);
                    map.Add("reply", reply);
                    map.Add("days", days);
                    map.Add("posts", posts);
                    map.Add("replys", replys);
                    return Json(map, JsonRequestBehavior.AllowGet);
                }
                //日数据
                else if (type == 3)
                {
                    Dictionary<string, Object> map = new Dictionary<string, object>();
                    //帖子数
                    var post = (from s in context.Posts
                                where s.Time.Contains(date)
                                select s).Count();
                    //回复数
                    var reply = (from s in context.Replys
                                 where s.Time.Contains(date)
                                 select s).Count();
                    //帖子分类
                    string[] types = new string[] { "无", "版务", "二次元", "游戏", "小说", "技术", "杂谈" };
                    //帖子分类数
                    var query1 = (from s in context.Posts
                                  where s.Time.Contains(date)
                                  group s by new { s.Type } into g
                                  select new
                                  {
                                      type = g.Key.Type,
                                      value = g.Count()
                                  }).ToList();
                    int[] postnumber = new int[] { 0, 0, 0, 0, 0, 0, 0 };
                    foreach (var item in query1)
                    {
                        postnumber[item.type] = item.value;
                    }
                    //分类回复数
                    var query2 = (from s1 in context.Replys
                                  join s2 in context.Posts
                                  on s1.PostId equals s2.PostId
                                  where s1.Time.Contains(date)
                                  group s1 by new { s2.Type } into g
                                  select new
                                  {
                                      type = g.Key.Type,
                                      value = g.Count()
                                  }).ToList();
                    int[] replynumber = new int[] { 0, 0, 0, 0, 0, 0, 0 };
                    foreach (var item in query2)
                    {
                        replynumber[item.type] = item.value;
                    }
                    map.Add("post", post);
                    map.Add("reply", reply);
                    map.Add("type", types);
                    map.Add("posttype", postnumber);
                    map.Add("replynumber", replynumber);
                    return Json(map, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Content("空！");
                }
            }
        }

        //我的回复
        public ActionResult MyReplys()
        {
            return View();
        }

        //得到我的回复
        public ActionResult GetMyReplys(int page, int rows)
        {
            using (Data context = new Data())
            {
                //权限检测
                if (Session["userid"] == null)
                    return Content("未登录用户不允许使用此功能！");
                string userid = Session["userid"].ToString();

                var count = context.Replys.Count();

                var query = (from s1 in context.Replys
                             join s2 in context.Posts
                             on s1.PostId equals s2.PostId
                             where s1.UserId == userid
                             orderby s1.Time descending
                             select new
                             {
                                 ReplyId=s1.ReplyId,
                                 PostId=s1.PostId,
                                 Content=s1.Content,
                                 Time=s1.Time,
                                 Title=s2.Title + (s2.IsShown == true ? "" : "[隐藏]"),
                                 Type=s2.Type
                             }).Skip((page - 1) * rows).Take(rows).ToList();
                //去除html标签的正则表达式
                Regex regex = new Regex(@"<[^>]+>|</[^>]+>");
                var query2 = (from s in query
                              select new
                              {
                                  ReplyId = s.ReplyId,
                                  PostId = s.PostId,
                                  Content_Short = (regex.Replace(s.Content,"").Length>=100)?regex.Replace(s.Content, "").Substring(0,100)+"......": regex.Replace(s.Content, ""),
                                  Time = s.Time,
                                  Title = s.Title,
                                  Type = s.Type
                              }).ToList();
                Dictionary<string, Object> map = new Dictionary<string, object>();
                map.Add("rows", query2);
                map.Add("total", count);
                return Json(map, JsonRequestBehavior.AllowGet);
            }
        }

        //得到一条回复
        public ActionResult GetAReply(string replyid)
        {                
            //权限检测
            if (Session["userid"] == null)
                return Content("未登录用户不允许使用此功能！");
            using (Data context = new Data())
            {
                var query = (from s in context.Replys
                             where s.ReplyId == replyid
                             select s.Content).Single();
                return Content(query);
            }

        }
        #endregion
    }
}