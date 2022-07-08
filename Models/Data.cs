using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyForum.Models
{
    public class Data : DbContext
    {
        public Data() : base("name=Data")
        {
            DbInterception.Add(new SqliteInterceptor());
        }

        //如果模型改变，则刷新
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var initializer = new SqliteDropCreateDatabaseWhenModelChanges<Data>(modelBuilder);
            Database.SetInitializer(initializer);

        }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Post> Posts { get; set; }
        public virtual DbSet<Reply> Replys { get; set; }
        public virtual DbSet<CheckIn> CheckIns { get; set; }
        public virtual DbSet<StarList> StarLists { get; set; }
    }

    //用户
    public class User
    {
        [Key]
        public string UserId { get; set; }//编号
        [Unique]
        public string UserName { get; set; }//用户名
        public string NickName { get; set; }//昵称，对外展示昵称而非用户名
        public string Password { get; set; }//密码
        public string Head { get; set; }//头像
        public string Disc { get; set; }//自我描述
        public int Lv { get; set; }//等级
        public int Exp { get; set; }//经验
        public int Identity { get; set; }//身份：0为超级管理员，1为管理员，2为普通用户
        public int Access { get; set; }//状态：0正常，1禁止发帖，2禁止发帖回帖，3禁止登录
        public int Posts { get; set; }//发帖数
        public int Replys { get; set; }//回复次数
        public string RegisterTime { get; set; }//注册时间
        public string MailBox{get;set;}//邮箱
        public int MVCGold { get; set; }//MVC金币
        public int Rank { get; set; }//头衔(0无 1黑铁 2黄铜 3白银 4黄金 5铂金 6钻石 7大师 8宗师 9王者 10至尊)
        public string GetRankTime { get; set; }//获取头衔时间
    }

    //帖子
    public class Post
    {
        [Key]
        public string PostId { get; set; }//帖子Id
        public string UserId { get; set; }//发帖者Id
        public string Time { get; set; }//发帖时间
        public string Title { get; set; }//帖子的标题
        public string Content { get; set; }//帖子的内容
        public int Replys { get; set; }//回复次数
        public string LastReplyTime { get; set; }//最后回复时间
        public string LastReplyUserId { get; set; }//最后回复的用户的Id
        public bool IsShown { get; set; }//帖子是否展示
        public int IsTop { get; set; }//是否置顶(0为不置顶，1为置顶)
        public int AccessTime { get; set; }//浏览次数      
        public int Likes { get; set; }//点赞  
        public bool Locked { get; set; }//是否上锁
        public bool Elite { get; set; }//是否精华
        public int Type { get; set; }//无-0 版务-1 二次元-2 游戏-3 小说-4 技术-5 杂谈-6
    }

    //回复
    public class Reply
    {
        [Key]
        public string ReplyId { get; set; }//回复Id
        public string PostId { get; set; }//回复的帖子的Id
        public string UserId { get; set; }//回复者Id
        public string Time { get; set; }//回复时间
        public string Content { get; set; }//回复的内容
    }

    //签到记录
    public class CheckIn
    {
        [Key]
        [Column(Order =1)]
        public string UserId { get; set; }//签到者Id
        [Key]
        [Column(Order = 2)]
        public string CheckInTime { get; set; }//签到时间
    }

    //收藏夹
    public class StarList
    {
        [Key]
        public string UserId { get; set; }//用户Id
        public string Star { get; set; }//收藏（转化为Json串）
    }

    //重写contains
    public class SqliteInterceptor : IDbCommandInterceptor
    {
        private static Regex replaceRegex = new Regex(@"\(CHARINDEX\((.*?),\s?(.*?)\)\)\s*?>\s*?0");
        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {

        }
        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {

        }
        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {

        }
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            ReplaceCharIndexFunc(command);
        }
        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {

        }
        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            ReplaceCharIndexFunc(command);
        }
        private void ReplaceCharIndexFunc(DbCommand command)
        {
            bool isMatch = false;

            var text = replaceRegex.Replace(command.CommandText, (match) =>
            {
                if (match.Success)
                {
                    string paramsKey = match.Groups[1].Value;
                    string paramsColumnName = match.Groups[2].Value;
                    //replaceParams
                    foreach (DbParameter param in command.Parameters)
                    {
                        if (param.ParameterName == paramsKey.Substring(1))
                        {
                            param.Value = string.Format("%{0}%", param.Value);
                            break;
                        }
                    }
                    isMatch = true;
                    return string.Format("{0} LIKE {1}", paramsColumnName, paramsKey);
                }
                else
                    return match.Value;
            });
            if (isMatch)
                command.CommandText = text;
        }
    }
}
