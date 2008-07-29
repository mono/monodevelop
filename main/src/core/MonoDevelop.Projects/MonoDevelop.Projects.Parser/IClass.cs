//  IClass.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
		
		SolutionItem SourceProject {
			get;
		}
		
		IRegion Region {
			get;
		}
		
		IRegion BodyRegion {
			get;
		}
		
		// For classes composed by several files, returns all parts of the class
		IClass[] Parts { get; }
		
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
