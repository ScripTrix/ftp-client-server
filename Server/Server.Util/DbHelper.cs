using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Util
{
    public static class DbHelper
    {
        public static T Convert<T>(object obj)
        {
            if (obj is null || obj is DBNull || obj == DBNull.Value)
            {
                return default(T);
            }
            var result = (T)obj;
            return result;
        }
    }
}
