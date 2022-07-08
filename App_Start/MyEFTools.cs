using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace MyForum.App_Start
{
    //https://www.tnblog.net/aojiancc2/article/details/2752
    public static class MyEFTools
    {
        public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> query, string sort, string sortway)
        {
            if (!string.IsNullOrEmpty(sort))
            {
                //第一步要拿到排序字段的类型
                Type propertyType = typeof(TSource).GetProperty(sort).PropertyType;
                //通过反射拿到方法
                var method = typeof(MyEFTools).GetMethod(sortway == "asc" ? "DealAsc" : "DealDesc");
                //给反射拿到的方法提供泛型
                method = method.MakeGenericMethod(typeof(TSource), propertyType);
                //反射调用方法
                IQueryable<TSource> result = (IQueryable<TSource>)method.Invoke(null, new object[] { query, sort });
                return result;
            }
            return query;
        }

        /// <summary>
        /// 处理升序排序
        /// 通过一个方法中转实现类型的传递
        /// </summary>
        public static IQueryable<TSource> DealAsc<TSource, M>(IQueryable<TSource> query, string sort)
        {
            return query.OrderBy(OrderLamdba<TSource, M>(query, sort));
        }

        /// <summary>
        /// 处理降序排序
        /// 通过一个方法中转实现类型的传递
        /// </summary>
        public static IQueryable<TSource> DealDesc<TSource, M>(IQueryable<TSource> query, string sort)
        {
            return query.OrderByDescending(OrderLamdba<TSource, M>(query, sort));
        }

        static Expression<Func<TSource, M>> OrderLamdba<TSource, M>(IQueryable<TSource> query, string sort)
        {
            var left = Expression.Parameter(typeof(TSource), "a");
            var body = Expression.Property(left, sort);
            Expression<Func<TSource, M>> lamdba = Expression.Lambda<Func<TSource, M>>(body, left);
            return lamdba;
        }
    }

}