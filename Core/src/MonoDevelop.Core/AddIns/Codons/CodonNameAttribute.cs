// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;

namespace MonoDevelop.Core.AddIns.Codons
{
	/// <summary>
	/// Indicates that class represents a codon.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class CodonNameAttribute : Attribute
	{
		string name;
		
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public CodonNameAttribute(string name) 
		{
			this.name = name;
		}
		
		/// <summary>
		/// Returns the name of the codon.
		/// </summary>
		public string Name {
			get { 
				return name; 
			}
			set { 
				name = value; 
			}
		}
	}
}
