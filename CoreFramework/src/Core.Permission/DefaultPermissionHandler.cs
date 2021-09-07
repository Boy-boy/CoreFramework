namespace Core.Permission
{
    public class DefaultPermissionHandler : IPermissionHandler
    {
        public PermissionResult Handler(PermissionContext context)
        {
            return PermissionResult.Success();
        }
    }
}
