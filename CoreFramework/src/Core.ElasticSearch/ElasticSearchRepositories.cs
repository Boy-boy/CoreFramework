using Nest;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Core.ElasticSearch
{
    public class ElasticSearchRepositories<T> : IElasticSearchRepositories<T>
    where T : class
    {
        protected ElasticClient ElasticClient { get; }
        public ElasticSearchRepositories(IElasticClientFactory elasticClientFactory, string elasticClientName = null)
        {
            if (elasticClientFactory == null)
                throw new ArgumentNullException(nameof(elasticClientFactory));
            ElasticClient = elasticClientFactory.CreateClient(elasticClientName);
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        public CreateIndexResponse CreateIndex(string indexName, int numberOfReplicas = 1, int numberOfShards = 5)
        {
            IIndexState indexState = new IndexState
            {
                Settings = new IndexSettings
                {
                    NumberOfReplicas = numberOfReplicas,
                    NumberOfShards = numberOfShards
                }
            };
            ICreateIndexRequest Selector(CreateIndexDescriptor x) => x.InitializeUsing(indexState).Map<T>(ms => ms.AutoMap());
            CreateIndexResponse response = ElasticClient.Indices.Create(indexName, Selector);
            return response;
        }

        /// <summary>
        /// 判断索引是否存在
        /// </summary>
        /// <param name="indexName">索引名称</param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public bool ExistsIndex(string indexName, Func<IndexExistsDescriptor, IIndexExistsRequest> selector = null)
        {
            var existResponse = ElasticClient.Indices.Exists(indexName, selector);
            return existResponse.Exists;
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="indexName">索引名称</param>
        /// <param name="selector"></param>
        public DeleteIndexResponse DeleteIndex(string indexName, Func<DeleteIndexDescriptor, IDeleteIndexRequest> selector = null)
        {
            var response = ElasticClient.Indices.Delete(indexName, selector);
            return response;
        }

        /// <summary>
        /// 返回一个Must条件集合
        /// </summary>
        /// <returns></returns>
        public List<Func<QueryContainerDescriptor<T>, QueryContainer>> Must()
        {
            return new List<Func<QueryContainerDescriptor<T>, QueryContainer>>();
        }

        /// <summary>
        /// 返回一个Should条件集合
        /// </summary>
        /// <returns></returns>
        public List<Func<QueryContainerDescriptor<T>, QueryContainer>> Should()
        {
            return new List<Func<QueryContainerDescriptor<T>, QueryContainer>>();
        }

        /// <summary>
        /// 添加Match子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要查询的关键字</param>
        /// <param name="boost"></param>
        public void AddMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            string value, double? boost = null)
        {
            musts.Add(d => d.Match(mq => mq.Field(field).Query(value).Boost(boost)));
        }

        /// <summary>
        /// 添加Match子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void AddMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, string value)
        {
            musts.Add(d => d.Match(mq => mq.Field(field).Query(value)));
        }

        /// <summary>
        /// 添加MultiMatch子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="fields">要查询的列</param>
        /// <param name="value">要查询的关键字</param>
        public void AddMultiMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            string[] fields, string value)
        {
            musts.Add(d => d.MultiMatch(mq => mq.Fields(fields).Query(value)));
        }

        /// <summary>
        /// 添加MultiMatch子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="fields">例如：f=>new [] {f.xxx, f.xxx}</param>
        /// <param name="value">要查询的关键字</param>
        public void AddMultiMatch(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> fields, string value)
        {
            musts.Add(d => d.MultiMatch(mq => mq.Fields(fields).Query(value)));
        }

        /// <summary>
        /// 添加大于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        public void AddGreaterThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).GreaterThan(value)));
        }

        /// <summary>
        ///  添加大于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void AddGreaterThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).GreaterThan(value)));
        }

        /// <summary>
        /// 添加大于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        public void AddGreaterThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).GreaterThanOrEquals(value)));
        }

        /// <summary>
        /// 添加大于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void AddGreaterThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).GreaterThanOrEquals(value)));
        }

        /// <summary>
        /// 添加小于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        public void AddLessThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).LessThan(value)));
        }

        /// <summary>
        /// 添加小于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void AddLessThan(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).LessThan(value)));
        }

        /// <summary>
        /// 添加小于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        public void AddLessThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).LessThanOrEquals(value)));
        }

        /// <summary>
        /// 添加小于等于子句
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void AddLessThanEqual(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, double value)
        {
            musts.Add(d => d.Range(mq => mq.Field(field).LessThanOrEquals(value)));
        }


        /// <summary>
        /// 添加一个Term，一个列一个值
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        /// <param name="boost"></param>
        /// <param name="name"></param>
        public void AddTerm(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            object value, double? boost = null, string name = null)
        {
            musts.Add(d => d.Term(field, value, boost, name));
        }

        /// <summary>
        /// 添加一个Term，一个列一个值
        /// </summary>
        /// <param name="musts"></param>
        /// <param name="field">要查询的列</param>
        /// <param name="value">要比较的值</param>
        /// <param name="boost"></param>
        /// <param name="name"></param>
        public void AddTerm(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, object value, double? boost = null, string name = null)
        {
            musts.Add(d => d.Term(field, value, boost, name));
        }

        /// <summary>
        /// 添加一个Terms，一个列多个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        public void AddTerms(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts, string field,
            object[] values)
        {
            musts.Add(d => d.Terms(tq => tq.Field(field).Terms(values)));
        }

        /// <summary>
        /// 添加一个Terms，一个列多个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="musts"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        public void AddTerms(List<Func<QueryContainerDescriptor<T>, QueryContainer>> musts,
            Expression<Func<T, object>> field, object[] values)
        {
            musts.Add(d => d.Terms(tq => tq.Field(field).Terms(values)));
        }
    }
}
