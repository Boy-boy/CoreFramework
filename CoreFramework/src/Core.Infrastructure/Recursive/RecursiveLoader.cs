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

            var modelDescriptors = new List<RecursiveModelDescriptor<T>>();
            foreach (var instance in allModels)
            {
                modelDescriptors.Add(new RecursiveModelDescriptor<T>(instance));
            }
            modelDescriptors = modelDescriptors
                .OrderBy(p => p.Instance.Id)
                .ToList();

            var isTrue = true;
            while (isTrue)
            {
                var startModel = modelDescriptors.First();
                newModelDescriptors.Add(startModel);
                startModel?.SetDependencies(modelDescriptors);

                if (modelDescriptors.Count <= 0)
                    isTrue = false;
            }

            return newModelDescriptors;
        }
    }
}
