using System;
using System.Collections.Generic;

namespace Core.Infrastructure.Recursive
{
    public class RecursiveModelDescriptor<T>
    where T : IRecursiveModel
    {
        public T Instance { get; set; }

        public IReadOnlyList<RecursiveModelDescriptor<T>> Nodes => _nodes;

        private readonly List<RecursiveModelDescriptor<T>> _nodes;

        public RecursiveModelDescriptor(T instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance)); ;
            _nodes = new List<RecursiveModelDescriptor<T>>();
        }

        public void SetDependencies(List<RecursiveModelDescriptor<T>> models)
        {
            if (models == null)
                return;

            var recursiveModelDescriptors = models.FindAll(m => m.Instance.ParentId != null && m.Instance.ParentId.Equals(Instance.Id));
            _nodes.AddRange(recursiveModelDescriptors);

            models.Remove(this);
            foreach (var recursiveModelDescriptor in recursiveModelDescriptors)
            {
                models.Remove(recursiveModelDescriptor);
            }

            foreach (var recursiveModelDescriptor in recursiveModelDescriptors)
            {
                recursiveModelDescriptor.SetDependencies(models);
            }
        }
    }
}
