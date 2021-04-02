using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Redis
{
    public interface IRedisCache : IDisposable
    {
        /// <summary>
        /// 获取指定的redis数据库
        /// </summary>
        /// <param name="db">默认-1，范围0-15</param>
        /// <returns></returns>
        IDatabase GetDatabase(int db = -1);

        #region String
        T Get<T>(string key, int db = -1);

        object Get(string key, int db = -1);

        Task<object> GetAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        void Set(string key, object value, int db = -1);

        Task SetAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default);

        bool SetExpireTime(string key, DateTime datetime, int db = -1);

        bool Exists(string key, int db = -1);

        bool Remove(string key, int db = -1);

        Task RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        long Increment(string key, int db = -1);

        long Decrement(string key, string value, int db = -1);

        #region List
        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListLeftPush(string redisKey, string redisValue, int db = -1);

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRightPush(string redisKey, string redisValue, int db = -1);

        /// <summary>
        /// 在列表尾部插入数组集合。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRightPush(string redisKey, IEnumerable<string> redisValue, int db = -1);

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素  反序列化成T
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        T ListLeftPop<T>(string redisKey, int db = -1) where T : class;

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素   反序列化为T
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        T ListRightPop<T>(string redisKey, int db = -1) where T : class;

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素   
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        string ListLeftPop(string redisKey, int db = -1);

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素   
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        string ListRightPop(string redisKey, int db = -1);

        /// <summary>
        /// 列表长度
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListLength(string redisKey, int db = -1);

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        IEnumerable<string> ListRange(string redisKey, int db = -1);

        /// <summary>
        /// 根据索引获取指定位置数据
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        IEnumerable<string> ListRange(string redisKey, int start, int stop, int db = -1);

        /// <summary>
        /// 删除List中的元素 并返回删除的个数
        /// </summary>
        /// <param name="redisKey">key</param>
        /// <param name="redisValue">元素</param>
        /// <param name="type">大于零 : 从表头开始向表尾搜索，小于零 : 从表尾开始向表头搜索，等于零：移除表中所有与 VALUE 相等的值</param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRemove(string redisKey, string redisValue, long type = 0, int db = -1);

        /// <summary>
        /// 清空List
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        void ListClear(string redisKey, int db = -1);
        #endregion
    }
}
