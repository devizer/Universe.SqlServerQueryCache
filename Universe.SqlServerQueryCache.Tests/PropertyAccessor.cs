using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Tests
{
    internal class PropertyAccessor
    {
        public static Func<TClass, TProperty> CreatePropertyGetter<TClass,TProperty>(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            return (Func<TClass, TProperty>)Delegate.CreateDelegate(typeof(Func<TClass, TProperty>), getMethod);
        }
    }
}
