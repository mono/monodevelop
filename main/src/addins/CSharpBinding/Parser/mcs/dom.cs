//
// dom.cs: AST tree abstraction for C# compiler
//
// Authors: Marek Safar (marek.safar@gmail.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2008 Novell, Inc
//

using System;
using System.Collections;

namespace Mono.CSharp.Dom
{
	public interface ILocation
	{
		int Row { get; }
		int Column { get; }
	}

	public struct LocationBlock
	{
		readonly ILocation start, end;

		public static readonly LocationBlock Null = new LocationBlock ();

		public LocationBlock (ILocation start, ILocation end)
		{
			this.start = start;
			this.end = end;
		}

		public ILocation Start {
			get {
				return start;
			}
		}

		public ILocation End {
			get {
				return end;
			}
		}
	}

	public interface ICompilationUnit
	{
		IUsingBlock UsingBlock { get; }

		//IEnumerable<IAttribute> GlobalAttributes { get; }

		IType[] Types { get; }

		INamespace[] Namespaces { get; }

		//IEnumerable<Comment> Comments { get; }

		//IEnumerable<DomRegion> FoldingRegions { get; }
	}

	public interface IUsingBlock
	{
		LocationBlock LocationBlock { get; }

		INamespaceImport[] Usings { get; }
		INamespaceImportAlias[] Aliases { get; }
	}

	public interface INamespaceImport
	{
		string Name { get; }
	}

	public interface INamespaceImportAlias : INamespaceImport
	{
		string Value { get; }
	}

	public interface INamespace : IUsingBlock
	{
		string Name { get; }

		IDelegate[] Delegates { get; }
		IType[] Types { get; }
	}

	public interface IType
	{
		ITypeName [] BaseTypes { get; }
		Kind ContainerType { get; }
		string Name { get; }
		LocationBlock MembersBlock { get; }
		int ModFlags { get; }
		ITypeParameter [] TypeParameters { get; }

		IMethod [] Constructors { get; }

		// And the ugly
		ArrayList Constants { get; }
		ArrayList Events { get; }
		ArrayList Fields { get; }
		ArrayList Indexers { get; }
		ArrayList Operators { get; }
		ArrayList Properties { get; }
		ArrayList Methods { get; }
		ArrayList Types { get; }

		ArrayList InstanceConstructors { get; }
		ArrayList Delegates { get; }
	}

	public interface ITypeName
	{
		bool IsNullable { get; }
		bool IsPointer { get; }
		//bool IsArray { get; }

		string Name { get; }
		ITypeName [] TypeArguments { get; }
	}

	public interface ITypeParameter
	{
		//IAttributes [] Attributes { get; }
		ITypeParameterConstraints Constraints { get; }
		string Name { get; }
	}

	public interface ITypeParameterConstraints
	{
		ITypeName Types { get; }

		// TODO: bool IsClass, IsNew, etc.
	}

	public interface ITypeMember
	{
		// IAttributes[] Attributes { get; }
		IType DeclaringType { get; }
		int ModFlags { get; }
		Location Location { get; }
		string Name { get; }
		ITypeName ReturnTypeName { get; }
	}

	public interface IMethod : ITypeMember
	{
		LocationBlock LocationBlock { get; }
		IParameter [] Parameters { get; }
	}

	public interface IDelegate : ITypeMember
	{
		IParameter [] Parameters { get; }
	}

	public interface IProperty : ITypeMember
	{
		IAccessor GetAccessor { get; }
		IAccessor SetAccessor { get; }
	}

	public interface IIndexer : IProperty
	{
		IParameter[] Parameters { get; }
	}

	public interface IEvent : ITypeMember
	{
		IAccessor AddAccessor { get; }
		IAccessor RemoveAccessor { get; }
	}

	public interface IAccessor
	{
		LocationBlock LocationBlock { get; }
		int ModFlags { get; }
	}

	public interface IParameter
	{
//		IAttribute [] Attributes { get; }

		string Name { get; }
		Location Location { get; }
		Parameter.Modifier ModFlags { get; }
		ITypeName TypeName { get; }
	}
}
