using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Nest;

namespace Core.ElasticSearch
{
    public interface IElasticSearchRepositories<T> where T : class
    {
        /// <summary>
        /// 创建索引
        /// </summary>
        CreateIndexResponse CreateIndex(string indexName, int numberOfReplicas = 1, int numberOfShards = 5);

        /// <summary>
        /// 判断索引是否存在
        /// </summary>
        /// <param name="indexName">索引名称</param>
        /// <param name="selector"></param>
        /// <returns></returns>
        bool ExistsIndex(string indexName, Func<IndexExistsDescriptor, IIndexExistsRequest> selector = null);

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="indexName">索引名称</param>
        /// <param name="selector"></param>
        DeleteIndexResponse DeleteIndex(string indexName, Func<DeleteIndexDescriptor, IDeleteIndexRequest> selector = null);

        /// <summary>
        /// 返回一个Must条件集合
        /// </summary>
        /// <returns></returns>
        List<Func<QueryContainerDescriptor<T>, QueryContainer>> Must();

        /// <summary>
        /// 返回一个Should条件集合
        /// </summary>
        /// <returns></returns>
        List<Func<QueryContainerDescriptor<T>, QueryContainer>> Should();

        /// <summary>
        /// 添加Match子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要查询的关键字</param>
        /// <param name="boost"></param>
        void AddMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            string value, double? boost = null);

        /// <summary>
        /// 添加Match子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要查询的关键字</param>
        void AddMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, string value);

        /// <summary>
        /// 添加MultiMatch子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="fields">要查询的列</param>
        /// <param name="value">要查询的关键字</param>
        void AddMultiMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
          string[] fields, string value);


        /// <summary>
        /// 添加MultiMatch子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="fields">例如：f=>new [] {f.xxx, f.xxx}</param>
        /// <param name="value">要查询的关键字</param>
        void AddMultiMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> fields, string value);

        /// <summary>
        /// 添加大于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        void AddGreaterThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string field, double value);

        /// <summary>
        ///  添加大于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void AddGreaterThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value);

        /// <summary>
        /// 添加大于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        void AddGreaterThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string field, double value);

        /// <summary>
        /// 添加大于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void AddGreaterThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value);

        /// <summary>
        /// 添加小于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        void AddLessThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string field, double value);

        /// <summary>
        /// 添加小于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void AddLessThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value);

        /// <summary>
        /// 添加小于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        void AddLessThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string field, double value);

        /// <summary>
        /// 添加小于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void AddLessThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value);

        /// <summary>
        /// 添加一个Term，一个列一个值
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        /// <param name="boost"></param>
        /// <param name="name"></param>
        void AddTerm(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            object value, double? boost = null, string name = null);

        /// <summary>
        /// 添加一个Term，一个列一个值
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        /// <param name="boost"></param>
        /// <param name="name"></param>
        void AddTerm(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, object value, double? boost = null, string name = null);

        /// <summary>
        /// 添加一个Terms，一个列多个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        void AddTerms(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string field, object[] values);

        /// <summary>
        /// 添加一个Terms，一个列多个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        void AddTerms(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, object[] values);
    }
}
