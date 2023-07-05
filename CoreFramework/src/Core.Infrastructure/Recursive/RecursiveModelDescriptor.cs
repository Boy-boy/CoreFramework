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
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _nodes = new List<RecursiveModelDescriptor<T>>();
        }

        public void SetNode(RecursiveModelDescriptor<T> model)
        {
            if (model == null)
                return;
            _nodes.Add(model);
        }
    }
}
