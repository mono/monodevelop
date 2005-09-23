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
	/// The condition builder builds a new codon
	/// </summary>
	public class CodonBuilder
	{
		Assembly assembly;
		string className;
		string codonName;
		
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
		
	}
}
