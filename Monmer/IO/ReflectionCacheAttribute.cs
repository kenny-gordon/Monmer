namespace Monmer.IO
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class ReflectionCacheAttribute : Attribute
    {
        public Type Type { get; }

        public ReflectionCacheAttribute(Type type)
        {
            Type = type;
        }
    }

}
