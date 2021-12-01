namespace Core.Permission
{
    public class PermissionResult
    {
        public bool Forbidden { get; private init; }
        public bool Succeeded { get; private init; }

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
