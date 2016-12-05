using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SaveAnywhere
{
    class TypeUtils
    {
        /// <summary>
        /// Flags to get all static/instance fields of all access rights for the type called on, and public/protects for inherited members.
        /// </summary>
        public static readonly BindingFlags DefaultFlags = 
              BindingFlags.IgnoreCase
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Get all static/instance fields of all access types for the given instance.
        /// </summary>
        public static readonly BindingFlags DeclaredGrantingFlags =
              BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.IgnoreCase
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly;


        public static object CreateGenericList(Type listType)
        {
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
        }

        public static bool HasCustomAttribute<TAttribute>(PropertyInfo property)
        {
            return (property?.GetCustomAttribute(typeof(TAttribute)) != null);
        }

        public static bool HasCustomAttribute<TAttribute>(Type type)
        {
            return (type?.GetCustomAttribute(typeof(TAttribute)) != null);
        }

        public static bool IsType<T>(object o)
        {
            if (o != null)
            {
                return (o.GetType() == typeof(T));
            }
            return false;
        }

        public static Type[] GetGenericArgTypes(Type t)
        {
            if (t.IsArray)
            {
                string[] parts = t.FullName.Split(new char[] { '[' });
                if (parts.Length > 0)
                {
                    return new Type[] { Type.GetType(parts[0]) };
                }
            }

            var genericTypeArgs = t.GetTypeInfo().GenericTypeArguments;
            if (genericTypeArgs.Length > 0)
            {
                return genericTypeArgs;
            }
            return new Type[0];
        }

        public static bool IsEnumerableOfType(object o, string typename)
        {
            Type type = o.GetType();
            if (IsEnurmerableType(type))
            {
                if (o is Array)
                {
                    string[] parts = type.Name.Split(new char[] { '[' });
                    return (parts[0] == typename);
                }
                Debug.Assert(type.GetTypeInfo().GenericTypeArguments.Length > 0, "Object of type: " + type.Name + " does not contain any generic arguments.");
                return (type.GetTypeInfo().GenericTypeArguments[0].Name == typename);
            }
            return false;
        }

        public static bool IsEnumerableOfType(object o, Type t)
        {
            return IsEnumerableOfType(o, t.Name);
        }

        public static bool IsCollectionOfType(object o, Type t)
        {
            Type otype = o.GetType();
            if (IsCollectionType(otype))
            {
                if (o is Array)
                {
                    string[] parts = otype.Name.Split(new char[] { '[' });
                    return (parts[0] == t.Name);
                }

                Debug.Assert(otype.GetTypeInfo().GenericTypeArguments.Length > 0, "Object of type: " + otype.Name + " does not contain any generic arguments.");
                return (otype.GetTypeInfo().GenericTypeArguments[0] == t);
            }
            return false;
        }

        public static bool IsEnurmerableType(Type type)
        {
            return (type.GetInterface("IEnumerable") != null);
        }

        public static bool IsCollectionType(Type type)
        {
            return (type.GetInterface("ICollection") != null);
        }

        public static Type ResolveTypeFromAssembly(Assembly assembly, string objectName)
        {
            Type type = null;

            // TODO: only do this once and store it as a member of wherever this method is moved to
            var namespaces = GetAssemblyNamespaces(assembly);
            foreach (var nspace in namespaces)
            {
                type = Type.GetType(nspace + "." + objectName);
                if (type != null)
                {
                    break;
                }
            }
            return type;
        }

        public static IEnumerable<string> GetAssemblyNamespaces(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(t => t.Namespace)
                .Where(n => n != null)
                .Distinct();
        }

        public static List<FieldInfo> GetAllFieldsUpHierarchy(object instance)
        {
            var fieldInfo = new List<FieldInfo>();

            Type currentType = instance.GetType();
            while (currentType != typeof(Object))
            {
                // Get all fields of all access types declared by this type.
                fieldInfo.AddRange(instance.GetType().GetFields(DeclaredGrantingFlags));
                currentType = currentType.BaseType;
            }
            return fieldInfo;
        }

        public static FieldInfo GetFieldUpHierachy(string fieldName, object instance)
        {
            var fields = GetAllFieldsUpHierarchy(instance);
            foreach (var field in fields)
            {
                if (field.Name == fieldName)
                {
                    return field;
                }
            }
            return null;
        }

        public static List<FieldInfo> FilterFieldsByType(Type type, FieldInfo[] fields)
        {
            var fieldsOfType = new List<FieldInfo>();
            foreach (var field in fields)
            {
                if (field.FieldType == type)
                {
                    fieldsOfType.Add(field);
                }
            }
            return fieldsOfType;
        }

        public static FieldInfo GetField(string fieldName, Type type, object instance)
        {
            var filteredFields = FilterFieldsByType(type, instance.GetType().GetFields(DefaultFlags));
            foreach (var f in filteredFields)
            {
                if (f.Name == fieldName)
                {
                    return f;
                }
            }
            return null;
        }

        public static FieldInfo GetField(string fieldName, object instance)
        {
            return instance.GetType().GetField(fieldName, DefaultFlags);
        }

        public static FieldInfo GetPrivateField(string fieldName, object instance)
        {
            return instance.GetType().GetField(fieldName, DeclaredGrantingFlags);
        }

        public static object GetPrivateFieldData(string fieldName, object instance)
        {
            return GetPrivateField(fieldName, instance)?.GetValue(instance);
        }

        public static object GetFieldData(string fieldName, object instance)
        {
            return GetField(fieldName, instance)?.GetValue(instance);
        }

        public static T GetNativeField<T, Instance>(Instance instance, string fieldName)
        {
            FieldInfo fieldInfo = typeof(Instance).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)fieldInfo.GetValue(instance);
        }

        public static void SetFieldData(string fieldName, object instance, object value)
        {
            GetField(fieldName, instance)?.SetValue(instance, value);
        }
    }
}
