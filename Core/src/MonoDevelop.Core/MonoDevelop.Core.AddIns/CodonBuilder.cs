// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.ComponentModel;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// The condition builder builds a new codon
	/// </summary>
	public class CodonBuilder
	{
		Assembly assembly;
		string className;
		string codonName;
		
		static Hashtable allowedChildNodes = new Hashtable ();
		static Hashtable descriptions = new Hashtable ();
		static string[] emptyArray = new string [0];
		
		/// <summary>
		/// Initializes a new CodonBuilder instance with beeing
		/// className the name of the condition class and assembly the
		/// assembly in which the class is defined.
		/// </summary>
		public CodonBuilder(string className, Assembly assembly)
		{
			this.assembly  = assembly;
			this.className = className;
			
			// get codon name from attribute
			CodonNameAttribute codonNameAttribute = (CodonNameAttribute)Attribute.GetCustomAttribute(assembly.GetType(ClassName), typeof(CodonNameAttribute));
			codonName = codonNameAttribute.Name;
		}
		
		/// <summary>
		/// Returns the className the name of the condition class;
		/// </summary>
		public string ClassName {
			get {
				return className;
			}
		}
		
		internal Type CodonType {
			get {
				return assembly.GetType (ClassName);
			}
		}
		
		/// <summary>
		/// Returns the name of the codon (it is used to determine which xml element
		/// represents which codon.
		/// </summary>
		public string CodonName {
			get {
				return codonName;
			}
		}
		
		/// <summary>
		/// Returns a newly build <code>ICodon</code> object.
		/// </summary>
		public ICodon BuildCodon(AddIn addIn)
		{
			ICodon codon;
			try {
				// create instance (ignore case)
				codon = (ICodon)assembly.CreateInstance(ClassName, true);
				
				// set default values
				codon.AddIn = addIn;
			} catch (Exception) {
				codon = null;
			}
			return codon;
		}
		
		internal static string[] GetAllowedChildNodes (Type type)
		{
			string[] nodes = (string[]) allowedChildNodes [type];
			if (nodes != null)
				return nodes;

			object[] ats = type.GetCustomAttributes (typeof(CategoryAttribute), false);
			if (ats.Length > 0) {
				nodes = ((CategoryAttribute)ats[0]).Category.Split (',');
				for (int n=0; n<nodes.Length; n++)
					nodes [n] = nodes [n].Trim ();
				return nodes;
			} else
				nodes = emptyArray;
				
			allowedChildNodes [type] = nodes;
			return nodes;
		}
		
		internal static string GetDescription (Type type)
		{
			string desc = (string) descriptions [type];
			if (desc != null)
				return desc;
			
			object[] dats = type.GetCustomAttributes (typeof(DescriptionAttribute), false);
			if (dats.Length > 0)
				desc = ((DescriptionAttribute)dats[0]).Description;
			else
				desc = "";
			descriptions [type] = desc;
			return desc;
		}
	}
}
