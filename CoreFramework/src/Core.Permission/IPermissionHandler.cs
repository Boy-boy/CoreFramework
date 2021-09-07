namespace Core.Permission
{
    public interface IPermissionHandler
    {
        PermissionResult Handler(PermissionContext context);
    }
}
