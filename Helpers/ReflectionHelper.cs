using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiniDemo.Helpers
{
    public static class ReflectionHelper
    {
        public static TAttribute GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
          where TAttribute : Attribute
        {
            //Get attribute on the member
            if (memberInfo.IsDefined(typeof(TAttribute), inherit))
            {
                return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
            }

            return defaultValue;
        }
    }
}
