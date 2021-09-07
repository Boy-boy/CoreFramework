namespace Core.Permission
{
    public class PermissionResult
    {
        public bool Forbidden { get; private set; }
        public bool Succeeded { get; private set; }

        public static PermissionResult Success()
        {
            return new PermissionResult()
            {
                Succeeded = true
            };
        }

        public static PermissionResult Forbid()
        {
            return new PermissionResult()
            {
                Forbidden = true
            };
        }
    }
}
