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
	/// <summary>
	///  Indicates that field should be treated as a xml attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited=true)]
	public sealed class XmlMemberAttributeAttribute : Attribute
	{
		string name;
		bool   isRequired;
		
		public XmlMemberAttributeAttribute(string name)
		{
			this.name  = name;
			isRequired = false;
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public bool IsRequired {
			get {
				return isRequired;
			}
			set {
				isRequired = value;
			}
		}
	}
}
