using Core.EmailClient.Storage.Model;

namespace Core.EmailClient.Storage
{
    public interface IEmailStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<string> AddAsync(AddEmailModel model, object dbTransaction = null, CancellationToken cancellationToken = default);

        Task<int> UpdateAsync(UpdateEmailModel model, CancellationToken cancellationToken = default);

        Task<List<EmailMessage>> GetAsync(QueryEmailModel query, CancellationToken cancellationToken = default);

        Task<EmailMessage> GetAsync(string id, CancellationToken cancellationToken = default);
    }
}
