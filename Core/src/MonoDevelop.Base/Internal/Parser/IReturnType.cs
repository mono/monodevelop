// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
namespace MonoDevelop.Internal.Parser
{
	public interface IReturnType: IComparable
	{
		string FullyQualifiedName {
			get;
		}

		string Name {
			get;
		}

		string Namespace {
			get;
		}

		int PointerNestingLevel {
			get;
		}
		int ArrayCount { // ArrayDimensions.Length
			get;
		}
		int[] ArrayDimensions {
			get;
		}
		
		object DeclaredIn {
			get;
		}
		
	}
}
