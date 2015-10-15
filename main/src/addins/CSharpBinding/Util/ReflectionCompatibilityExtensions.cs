using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp
{
	[Flags]
	internal enum BindingFlags
	{
		Default = 0,
		Instance = 1,
		Static = 2,
		Public = 4,
		NonPublic = 8,
	}

	internal static class ReflectionCompatibilityExtensions
	{
		public static object[] GetCustomAttributes(this Type type, bool inherit)
		{
			return type.GetTypeInfo().GetCustomAttributes(inherit).ToArray();
		}

		public static MethodInfo GetMethod(this Type type, string name)
		{
			return type.GetTypeInfo().DeclaredMethods.FirstOrDefault(m => m.Name == name);
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().DeclaredMethods.FirstOrDefault(m => (m.Name == name) && IsConformWithBindingFlags(m, bindingFlags));
		}

		public static MethodInfo GetMethod(this Type type, string name, Type[] types)
		{
			return type.GetTypeInfo().DeclaredMethods.FirstOrDefault(
				m => (m.Name == name) && TypesAreEqual(m.GetParameters().Select(p => p.ParameterType).ToArray(), types));
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingFlags, object binder, Type[] types, object modifiers)
		{
			return type.GetTypeInfo().DeclaredMethods.FirstOrDefault(
				m => (m.Name == name) && IsConformWithBindingFlags(m, bindingFlags) && TypesAreEqual(m.GetParameters().Select(p => p.ParameterType).ToArray(), types));
		}

		public static IEnumerable<MethodInfo> GetMethods(this Type type)
		{
			return type.GetTypeInfo().DeclaredMethods;
		}

		public static IEnumerable<MethodInfo> GetMethods(this Type type, string name)
		{
			return type.GetTypeInfo().DeclaredMethods.Where(m => m.Name == name);
		}

		public static IEnumerable<MethodInfo> GetMethods(this Type type, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().DeclaredMethods.Where(m => IsConformWithBindingFlags(m, bindingFlags));
		}

		public static FieldInfo GetField(this Type type, string name)
		{
			return type.GetTypeInfo().DeclaredFields.FirstOrDefault(f => f.Name == name);
		}

		public static FieldInfo GetField(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().DeclaredFields.FirstOrDefault(f => (f.Name == name) && IsConformWithBindingFlags(f, bindingFlags));
		}

		public static PropertyInfo GetProperty(this Type type, string name)
		{
			return type.GetTypeInfo().DeclaredProperties.FirstOrDefault(p => p.Name == name);
		}

		public static IEnumerable<PropertyInfo> GetProperties(this Type type)
		{
			return type.GetTypeInfo().DeclaredProperties;
		}

		private static bool TypesAreEqual(Type[] memberTypes, Type[] searchedTypes)
		{
			if (((memberTypes == null) || (searchedTypes == null)) && (memberTypes != searchedTypes))
				return false;

			if (memberTypes.Length != searchedTypes.Length)
				return false;

			for (int i = 0; i < memberTypes.Length; i++)
			{
				if (memberTypes[i] != searchedTypes[i])
					return false;
			}

			return true;
		}

		private static bool IsConformWithBindingFlags(MethodBase method, BindingFlags bindingFlags)
		{
			if (method.IsPublic && !bindingFlags.HasFlag(BindingFlags.Public))
				return false;
			if (method.IsPrivate && !bindingFlags.HasFlag(BindingFlags.NonPublic))
				return false;
			if (method.IsStatic && !bindingFlags.HasFlag(BindingFlags.Static))
				return false;
			if (!method.IsStatic && !bindingFlags.HasFlag(BindingFlags.Instance))
				return false;

			return true;
		}

		private static bool IsConformWithBindingFlags(FieldInfo method, BindingFlags bindingFlags)
		{
			if (method.IsPublic && !bindingFlags.HasFlag(BindingFlags.Public))
				return false;
			if (method.IsPrivate && !bindingFlags.HasFlag(BindingFlags.NonPublic))
				return false;
			if (method.IsStatic && !bindingFlags.HasFlag(BindingFlags.Static))
				return false;
			if (!method.IsStatic && !bindingFlags.HasFlag(BindingFlags.Instance))
				return false;

			return true;
		}
	}
}
