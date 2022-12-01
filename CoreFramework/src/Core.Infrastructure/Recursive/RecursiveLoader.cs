using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Infrastructure.Recursive
{
    public class RecursiveLoader
    {
        public static List<RecursiveModelDescriptor<T>> LoadModels<T>(IEnumerable<T> models)
        where T : IRecursiveModel
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var recursiveModelDescriptors = CreateModelDescriptorsAsync(models);

            return recursiveModelDescriptors;
        }

        private static List<RecursiveModelDescriptor<T>> CreateModelDescriptorsAsync<T>(IEnumerable<T> models)
            where T : IRecursiveModel
        {
            var newModelDescriptors = new List<RecursiveModelDescriptor<T>>();

            var allModels = models.ToList();
            if (!allModels.Any())
                return newModelDescriptors;

            var dtoMap = new Dictionary<object, RecursiveModelDescriptor<T>>();
            foreach (var instance in allModels)
            {
                dtoMap.Add(instance.Id, new RecursiveModelDescriptor<T>(instance));
            }

            foreach (var item in dtoMap.Values)
            {
                if (item.Instance.ParentId == null)
                {
                    newModelDescriptors.Add(item);
                }
                else
                {
                    if (dtoMap.ContainsKey(item.Instance.ParentId))
                    {
                        dtoMap[item.Instance.ParentId].SetNode(item);
                    }
                    else
                    {
                        throw new RecursiveException("error in original data");
                    }
                }
            }
            return newModelDescriptors;
        }
    }
}
