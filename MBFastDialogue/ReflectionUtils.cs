using System.Reflection;

namespace MBFastDialogue
{
    public static class ReflectionUtils
    {
        public static T ForceGet<T>(object obj, string fieldName)
        {
            FieldInfo? field = null;
            var baseType = obj.GetType();
            while (field == null)
            {
                field = baseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                baseType = baseType.BaseType;
                if (baseType == null) break;
            }

            return (T)field.GetValue(obj);
        }

        public static T ForceCall<T>(object obj, string methodName, object[] args)
        {
            MethodInfo? method = null;
            var baseType = obj.GetType();
            while (method == null)
            {
                method = baseType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                baseType = baseType.BaseType;
                if (baseType == null) break;
            }
            if (method.ReturnType == typeof(void))
            {
                method.Invoke(obj, args);
                return default!;
            }
            return (T)method.Invoke(obj, args);
        }
    }
}