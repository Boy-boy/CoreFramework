using System;
using Core.EventBus.Messaging;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace Core.EventBus.Transaction
{
    public static class DbTransactionExtensions
    {
        /// <summary>
        /// 开启事务，返回值若为空，表示未启用持久化机制
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="publisher"></param>
        /// <param name="autoCommit"></param>
        /// <returns></returns>
        public static ITransaction BeginTransaction(this IDbConnection dbConnection,
            IMessagePublisher publisher, bool autoCommit = false)
        {
            if (dbConnection == null)
            {
                throw new ArgumentException(nameof(dbConnection));
            }
            if (publisher == null)
            {
                throw new ArgumentException(nameof(publisher));
            }

            var publisherBase = (MessagePublisherBase)publisher;
            var transaction = publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
                .GetService<ITransaction>();
            if (transaction == null) return null;

            var transactionBase = (TransactionBase)transaction;
            if (dbConnection.State == ConnectionState.Closed)
                dbConnection.Open();
            var dbTransaction = dbConnection.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return transactionBase;
        }

        /// <summary>
        /// 开启事务，返回值若为空，表示未启用持久化机制
        /// </summary>
        /// <param name="database"></param>
        /// <param name="publisher"></param>
        /// <param name="autoCommit"></param>
        /// <returns></returns>
        public static ITransaction BeginTransaction(this DatabaseFacade database,
            IMessagePublisher publisher, bool autoCommit = false)
        {
            if (database == null)
            {
                throw new ArgumentException(nameof(database));
            }
            if (publisher == null)
            {
                throw new ArgumentException(nameof(publisher));
            }
            var publisherBase = (MessagePublisherBase)publisher;
            var transaction = publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
                .GetService<ITransaction>();
            if (transaction == null) return null;

            var transactionBase = (TransactionBase)transaction;
            var dbTransaction = database.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return transactionBase;
        }
    }
}
