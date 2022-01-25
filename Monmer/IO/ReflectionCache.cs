using System.Reflection;

namespace Monmer.IO
{
    internal static class ReflectionCache<T> where T : Enum
    {
        private static readonly Dictionary<T, Type> cache = new Dictionary<T, Type>();

        public static int Count => cache.Count;

        static ReflectionCache()
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                ReflectionCacheAttribute attribute = field.GetCustomAttribute<ReflectionCacheAttribute>();
                if (attribute == null) continue;

                cache.Add((T)field.GetValue(null), attribute.Type);
            }
        }

        public static object CreateInstance(T key, object obj = null)
        {
            if (cache.TryGetValue(key, out Type type))
            {
                return Activator.CreateInstance(type);
            }

            return obj;
        }

        public static ISerializable CreateSerializable(T key, byte[] data)
        {
            if (cache.TryGetValue(key, out Type type))
            {
                return data.AsSerializable(type);
            }

            return null;
        }
    }

}
