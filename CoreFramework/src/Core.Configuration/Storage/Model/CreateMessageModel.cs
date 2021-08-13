namespace Core.Configuration.Storage
{
    public class CreateMessageModel
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public string Description { get; set; } = "";
    }
}
