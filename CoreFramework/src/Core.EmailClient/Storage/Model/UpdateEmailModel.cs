namespace Core.EmailClient.Storage.Model
{
    public class UpdateEmailModel
    {
        public UpdateEmailModel(string id, bool isSend)
        {
            Id = id;
            IsSend = isSend;
        }
        public string Id { get; set; }

        public bool IsSend { get; set; }
    }
}
