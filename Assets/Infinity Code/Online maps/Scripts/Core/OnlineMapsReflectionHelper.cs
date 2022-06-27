/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Helper class for compatibility of reflection on WSA.
/// </summary>
public static class OnlineMapsReflectionHelper
{
    private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

    /// <summary>
    /// Checks whether the type is anonymous.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>True - type is anonymous, false - otherwise</returns>
    public static bool CheckIfAnonymousType(Type type)
    {
        if (type == null) throw new ArgumentNullException("type");

        return IsGenericType(type)
            && (type.Name.Contains("AnonymousType") || type.Name.Contains("AnonType"))
            && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && (GetAttributes(type) & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
    }

    /// <summary>
    /// Gets the attributes associated with the Type.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Attributes of type.</returns>
    public static TypeAttributes GetAttributes(Type type)
    {
#if !NETFX_CORE
        return type.Attributes;
#else
        return type.GetTypeInfo().Attributes;
#endif
    }

    /// <summary>
    /// Get OnlineMapsDescription attribute from Enum.
    /// </summary>
    /// <param name="value">Enum value</param>
    /// <returns>OnlineMapsDescription attribute</returns>
    public static string GetEnumDescription(Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());

        OnlineMapsDescriptionAttribute[] attributes = (OnlineMapsDescriptionAttribute[])fi.GetCustomAttributes(typeof(OnlineMapsDescriptionAttribute), false);

        if (attributes.Length > 0) return attributes[0].Description;
        return value.ToString();
    }


    /// <summary>
    /// Searches for the fields defined for the current Type, using the specified binding constraints.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.</param>
    /// <returns>An array of FieldInfo objects representing all fields defined for the current Type that match the specified binding constraints.</returns>
    public static IEnumerable<FieldInfo> GetFields(Type type, BindingFlags bindingAttr = DefaultLookup)
    {
#if !NETFX_CORE
        return type.GetFields(bindingAttr);
#else
        return type.GetTypeInfo().DeclaredFields;
#endif
    }

    /// <summary>
    /// Returns an array of Type objects that represent the type arguments of a generic type or the type parameters of a generic type definition.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>An array of Type objects that represent the type arguments of a generic type. Returns an empty array if the current type is not a generic type.</returns>
    public static Type[] GetGenericArguments(Type type)
    {
#if !NETFX_CORE
        return type.GetGenericArguments();
#else
        return type.GetTypeInfo().GenericTypeArguments;
#endif
    }

    /// <summary>
    /// Searches for the public members with the specified name.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="name">The String containing the name of the public members to get. </param>
    /// <returns>An array of MemberInfo objects representing the public members with the specified name, if found; otherwise, an empty array.</returns>
    public static MemberInfo GetMember(Type type, string name)
    {
#if !NETFX_CORE
        MemberInfo[] infos = type.GetMember(name);
        if (infos.Length > 0) return infos[0];
        return null;
#else
        IEnumerable<MemberInfo> members = type.GetTypeInfo().DeclaredMembers;
        foreach (MemberInfo member in members) if (member.Name == name) return member;
        return null;
#endif
    }

    /// <summary>
    /// searches for the members defined for the current Type, using the specified binding constraints.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.</param>
    /// <returns>An array of MemberInfo objects representing all members defined for the current Type that match the specified binding constraints.</returns>
    public static IEnumerable<MemberInfo> GetMembers(Type type, BindingFlags bindingAttr = DefaultLookup)
    {
#if !NETFX_CORE
        return type.GetMembers(bindingAttr);
#else
        return type.GetTypeInfo().DeclaredMembers;
#endif
    }

    /// <summary>
    /// Gets a MemberTypes value indicating the type of the member — method, constructor, event, and so on.
    /// </summary>
    /// <param name="member">MemberInfo</param>
    /// <returns>MemberTypes value</returns>
    public static MemberTypes GetMemberType(MemberInfo member)
    {
#if !NETFX_CORE
        return member.MemberType;
#else
        if (member is PropertyInfo) return MemberTypes.Property;
        if (member is FieldInfo) return MemberTypes.Field;
        if (member is MethodInfo) return MemberTypes.Method;
        if (member is EventInfo) return MemberTypes.Event;
        if (member is ConstructorInfo) return MemberTypes.Constructor;
        return MemberTypes.All;
#endif
    }

    /// <summary>
    /// Searches for the public method with the specified name.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="name">The String containing the name of the public method to get. </param>
    /// <returns>A MethodInfo object representing the public method with the specified name, if found; otherwise, null.</returns>
    public static MethodInfo GetMethod(Type type, string name)
    {
#if !NETFX_CORE
        return type.GetMethod(name);
#else
        return type.GetTypeInfo().GetDeclaredMethod(name);
#endif
    }

    /// <summary>
    /// Searches for the specified public method whose parameters match the specified argument types.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="name">The String containing the name of the public method to get. </param>
    /// <param name="types">An array of Type objects representing the number, order, and type of the parameters for the method to get.</param>
    /// <returns>A MethodInfo object representing the public method whose parameters match the specified argument types, if found; otherwise, null.</returns>
    public static MethodInfo GetMethod(Type type, string name, Type[] types)
    {
#if !NETFX_CORE
        return type.GetMethod(name, types);
#else
        var methods = type.GetTypeInfo().GetDeclaredMethods(name);
        foreach(var m in methods)
        {
            var parms = m.GetParameters();
            if (parms != null && parms.Length == types.Length && parms[0].ParameterType == typeof(string))
            {
                bool success = true;
                for(int i = 0; i < parms.Length; i++)
                {
                    if (parms[i].ParameterType != types[i])
                    {
                        success = false;
                        break;
                    }
                }
                if (success) return m;
            }
        }
        return null;
#endif
    }

    /// <summary>
    /// Returns all the public properties of the current Type.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>An array of PropertyInfo objects representing all public properties of the current Type.</returns>
    public static PropertyInfo[] GetProperties(Type type)
    {
#if !NETFX_CORE
        return type.GetProperties();
#else
        return type.GetTypeInfo().DeclaredProperties.ToArray();
#endif
    }

    /// <summary>
    /// Gets a value indicating whether the Type is a class; that is, not a value type or interface.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>True if the Type is a class; otherwise, false.</returns>
    public static bool IsClass(Type type)
    {
#if !NETFX_CORE
        return type.IsClass;
#else
        return type.GetTypeInfo().IsClass;
#endif
    }

    /// <summary>
    /// Gets a value indicating whether the current type is a generic type.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>True if the current type is a generic type; otherwise, false.</returns>
    public static bool IsGenericType(Type type)
    {
#if !NETFX_CORE
        return type.IsGenericType;
#else
        return type.GetTypeInfo().IsGenericType;
#endif
    }

    /// <summary>
    /// Gets a value indicating whether the Type is a value type.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>True if the Type is a value type; otherwise, false.</returns>
    public static bool IsValueType(Type type)
    {
#if !NETFX_CORE
        return type.IsValueType;
#else
        return type.GetTypeInfo().IsValueType;
#endif
    }
}