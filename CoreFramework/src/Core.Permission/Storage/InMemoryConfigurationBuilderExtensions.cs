namespace Core.Permission.Storage
{
    public static class InMemoryBuilderExtensions
    {
        public static PermissionOptions AddInMemory(this PermissionOptions options)
        {
            options.AddExtensions(new InMemoryOptionsExtensions());
            return options;
        }
    }
}
