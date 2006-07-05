// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.Parser
{
	public interface IClass : IDecoration
	{
		string FullyQualifiedName {
			get;
		}
		
		string Namespace {
			get;
		}
		
		ClassType ClassType {
			get;
		}		
		
		ICompilationUnit CompilationUnit {
			get;
		}
		
		IRegion Region {
			get;
		}
		
		IRegion BodyRegion {
			get;
		}
		
		/* Reasoning behind the 'null' isGeneric indication: since most classes
		   are not generic, it is best not to create string collections to hold
		   type parameters and use the 'null' value to indicate that a type is
		   not generic */
		/// <summary>
		/// Contains a set of formal parameters to a generic type. 
		/// <p>If this property returns null or an empty collection, the type is
		/// not generic.</p>
		/// </summary>
		GenericParameterList GenericParameters {
			get;
		}
		
		ReturnTypeList BaseTypes {
			get;
		}
		
		ClassCollection InnerClasses {
			get;
		}

		FieldCollection Fields {
			get;
		}

		PropertyCollection Properties {
			get;
		}

		IndexerCollection Indexer {
			get;
		}

		MethodCollection Methods {
			get;
		}

		EventCollection Events {
			get;
		}

		object DeclaredIn {
			get;
		}
	}
}
