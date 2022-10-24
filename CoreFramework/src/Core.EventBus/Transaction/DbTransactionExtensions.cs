using Core.EventBus.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        /// <returns></returns>
        public static ITransaction BeginTransaction(this IDbConnection dbConnection,
            IMessagePublisher publisher)
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
            VerifyStorageServicesAreRegistered(publisherBase.ServiceProvider);
            if (dbConnection.State == ConnectionState.Closed)
                dbConnection.Open();
            var dbTransaction = dbConnection.BeginTransaction();

            var transaction = (TransactionBase)publisherBase.ServiceProvider.GetRequiredService<ITransaction>();

            transaction.DbTransaction = dbTransaction;
            publisherBase.TransactionAccessor.Transaction = transaction;
            return transaction;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="database"></param>
        /// <param name="publisher"></param>
        /// <returns></returns>
        public static ITransaction BeginTransaction(this DatabaseFacade database,
            IMessagePublisher publisher)
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
            VerifyStorageServicesAreRegistered(publisherBase.ServiceProvider);
            var dbTransaction = database.BeginTransaction();

            var transaction = (TransactionBase)publisherBase.ServiceProvider.GetRequiredService<ITransaction>();

            transaction.DbTransaction = dbTransaction;
            publisherBase.TransactionAccessor.Transaction = transaction;
            return transaction;
        }

        private static void VerifyStorageServicesAreRegistered(IServiceProvider service)
        {
            if (service.GetService(typeof(StorageMarkerService)) == null)
                throw new InvalidOperationException("Event storage service not registered");
        }
    }
}
