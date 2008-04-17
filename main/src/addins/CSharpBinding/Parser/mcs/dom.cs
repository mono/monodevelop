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

namespace Mono.CSharp.Dom
{
	public interface ILocation
	{
		int Row { get; }
		int Column { get; }
	}

	public interface ILocationBlock
	{
		ILocation Start { get; }
		ILocation End { get; }
	}

	struct LocationBlock : ILocationBlock
	{
		readonly ILocation start, end;
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
		ILocationBlock LocationBlock { get; }

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
		IType[] Types { get; }

		//IDelegate[] Delegates { get; }
	}

	public interface IType
	{
		ITypeName [] BaseTypes { get; }
		Kind ContainerType { get; }
		string Name { get; }
		ILocationBlock LocationBlock { get; }
		int ModFlags { get; }
		ITypeParameter [] TypeParameters { get; }
	}

	public interface ITypeMember
	{
		// TODO:
	}

	public interface ITypeName
	{
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
}
