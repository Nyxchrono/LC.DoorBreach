using System;
using System.Reflection;

namespace Nyxchrono.DoorBreach;

public class Utils
{
    internal const BindingFlags BindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    
    internal static object GetInstanceField(Type type, object instance, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindFlags);
        return field?.GetValue(instance);
    }
    
    internal static T GetInstanceField<T>(Type type, object instance, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindFlags);
        return (T)field?.GetValue(instance);
    }
    
    internal static FieldInfo GetInstanceFieldInfo(object instance, string fieldName)
    {
        return instance.GetType().GetField(fieldName, BindFlags);
    }
    
}