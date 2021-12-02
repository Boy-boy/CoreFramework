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
        /// 开启事务
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="publisher"></param>
        /// <param name="autoCommit"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static bool TryBeginTransaction(this IDbConnection dbConnection,
            IMessagePublisher publisher,
            bool autoCommit,
            out ITransaction transaction)
        {
            if (dbConnection == null)
            {
                throw new ArgumentNullException(nameof(dbConnection));
            }
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            var publisherBase = (MessagePublisherBase)publisher;
            transaction = publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
               .GetService<ITransaction>();
            if (transaction == null)
                return false;

            var transactionBase = (TransactionBase)transaction;
            if (dbConnection.State == ConnectionState.Closed)
                dbConnection.Open();
            var dbTransaction = dbConnection.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return true;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="database"></param>
        /// <param name="publisher"></param>
        /// <param name="autoCommit"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static bool TryBeginTransaction(this DatabaseFacade database,
            IMessagePublisher publisher,
            bool autoCommit,
            out ITransaction transaction)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            var publisherBase = (MessagePublisherBase)publisher;
            transaction = publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
               .GetService<ITransaction>();
            if (transaction == null)
                return false;

            var transactionBase = (TransactionBase)transaction;
            var dbTransaction = database.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return true;
        }
    }
}
