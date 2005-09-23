// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// Indicates that class represents a condition.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
	public sealed class ConditionAttribute : Attribute
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ConditionAttribute()
		{
		}
	}
}
