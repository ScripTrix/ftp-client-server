using System;

namespace Server.Util
{
    public static class EnumParser
    {
        public static T Parse<T>(string value)
        {
            T enumNode = default;
            try
            {
                enumNode = (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception ex)
            {
                enumNode = default;
            }
            return enumNode;
        }
    }
}
