namespace Core.Configuration.Storage
{
    public class ModifyMessageModel
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public string Description { get; set; } = "";
    }
}
