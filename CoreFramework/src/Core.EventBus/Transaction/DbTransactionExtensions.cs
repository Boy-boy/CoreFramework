using System;
using Core.EventBus.Messaging;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace Core.EventBus.Transaction
{
    public static class DbTransactionExtensions
    {
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
            var transactionBase = (TransactionBase)publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
                .GetService<ITransaction>();
            if (transactionBase == null) return null;
            if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();
            var dbTransaction = dbConnection.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return transactionBase;
        }

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
            var transactionBase = (TransactionBase)publisherBase.ServiceScopeFactory.CreateScope().ServiceProvider
                .GetService<ITransaction>();
            if (transactionBase == null) return null;
            var dbTransaction = database.BeginTransaction();
            transactionBase.DbTransaction = dbTransaction;
            transactionBase.AutoCommit = autoCommit;
            ((MessagePublisherBase)publisher).TransactionAccessor.Transaction = transactionBase;
            return transactionBase;
        }
    }
}
