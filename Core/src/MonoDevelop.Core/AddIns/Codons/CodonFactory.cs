// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Codons
{
	/// <summary>
	/// Creates a new <code>ICodon</code> object.
	/// </summary>
	public class CodonFactory
	{
		Hashtable codonHashtable = new Hashtable();
		
		/// <remarks>
		/// Adds a new builder to this factory. After the builder is added
		/// codons from the builder type can be created by the factory
		/// </remarks>
		/// <exception cref="DuplicateCodonException">
		/// Is thrown when a codon builder with the same <code>CodonName</code>
		/// was already inserted
		/// </exception>
		public void AddCodonBuilder(CodonBuilder builder)
		{
			if (codonHashtable[builder.CodonName] != null) {
				throw new DuplicateCodonException(builder.ClassName, builder.CodonName);
			}
			codonHashtable[builder.CodonName] = builder;
		}
		
		/// <remarks>
		/// Creates a new <code>ICodon</code> object using  <code>codonNode</code>
		/// as a mark of which builder to take for creation.
		/// </remarks>
		public ICodon CreateCodon(AddIn addIn, XmlNode codonNode)
		{
			CodonBuilder builder = codonHashtable[codonNode.Name] as CodonBuilder;
			
			if (builder != null) {
				return builder.BuildCodon(addIn);
			}
			
			throw new CodonNotFoundException(String.Format("no codon builder found for <{0}>", codonNode.Name));
		}
	}
}
