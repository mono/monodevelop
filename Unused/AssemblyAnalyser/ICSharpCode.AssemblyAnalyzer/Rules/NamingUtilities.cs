// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of NamingUtilities.	
	/// </summary>
	public sealed class NamingUtilities
	{
		/// <summary>
		/// Pascal casing is like 'PascalCase'
		/// </summary>
		public static bool IsPascalCase(string name)
		{
			if (name == null || name.Length == 0) {
				return true;
			}
			return Char.IsUpper(name[0]);
		}
		
		public static string PascalCase(string name)
		{
			if (name == null || name.Length == 0) {
				return name;
			}
			return Char.ToUpper(name[0]) + name.Substring(1);
		}
		
		
		/// <summary>
		/// Camel casing is like 'camelCase'
		/// </summary>
		public static bool IsCamelCase(string name)
		{
			if (name == null || name.Length == 0) {
				return true;
			}
			return Char.IsLower(name[0]);
		}
		
		
		public static string CamelCase(string name)
		{
			if (name == null || name.Length == 0) {
				return name;
			}
			return Char.ToLower(name[0]) + name.Substring(1);
		}
		
		public static bool ContainsUnderscore(string name)
		{
			if (name == null || name.Length == 0) {
				return false;
			}
			return name.IndexOf('_') >= 0;
		}
		
		public static string Combine(string typeName, string memberName)
		{
			return String.Concat(typeName, 
			                     '.',
			                     memberName);
		}
		
	}
}
