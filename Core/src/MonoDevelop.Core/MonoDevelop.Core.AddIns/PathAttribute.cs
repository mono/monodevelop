// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;

namespace MonoDevelop.Core.AddIns
{
	[AttributeUsage(AttributeTargets.Field, Inherited=true)]
	public sealed class PathAttribute : Attribute
	{
		/// <summary>
		/// Constructs a new instance.
		/// </summary>
		public PathAttribute()
		{
		}
	}
}
