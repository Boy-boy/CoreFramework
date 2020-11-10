using System;
using Core.Json.Newtonsoft;
using System.Collections.Generic;
using System.Linq;

namespace Core.Ddd.Domain.Values
{
    public class ValueObject
    {
        protected virtual IEnumerable<object> GetAtomicValues()
        {
            return GetType().GetProperties().Select(p => p.GetValue(this, null));
        }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }
            var other = (ValueObject)obj;
            var thisValues = GetAtomicValues().GetEnumerator();
            var otherValues = other.GetAtomicValues().GetEnumerator();

            while (thisValues.MoveNext() && otherValues.MoveNext())
            {
                if (ReferenceEquals(thisValues.Current, null) ^ ReferenceEquals(otherValues.Current, null))
                {
                    return false;
                }

                if (thisValues.Current != null && !thisValues.Current.Equals(otherValues.Current))
                {
                    return false;
                }
            }

            return !thisValues.MoveNext() && !otherValues.MoveNext();
        }
        public override int GetHashCode()
        {
            return GetAtomicValues()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }
    }

    public class ValueObject<T> : ValueObject where T : class
    {
        public static T Empty => Activator.CreateInstance<T>();

        public virtual T Clone(bool deSerializeNonPublic = true)
        {
            var cloned = this.ToJson().ToObject<T>(deSerializeNonPublic);
            return cloned;
        }
    }
}
