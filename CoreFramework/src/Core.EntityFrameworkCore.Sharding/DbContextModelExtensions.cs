using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Core.EntityFrameworkCore.Sharding
{
    public static class DbContextModelExtensions
    {
        public static bool ValidateModelIsReadonly(this Model model)
        {
           return model.IsReadonly;
        }

        public static void TryFinalizeModel(this Model model)
        {
            if (!model.ValidateModelIsReadonly())
            {
                model.FinalizeModel();
            }
        }
    }
}
