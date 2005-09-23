// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.Internal.Parser
{
	public interface ICompilationUnit : ICompilationUnitBase
	{
		IUsingCollection Usings {
			get;
		}
		
		AttributeSectionCollection Attributes {
			get;
		}
		
		ClassCollection Classes {
			get;
		}
		
		CommentCollection MiscComments {
			get;
		}
		
		CommentCollection DokuComments {
			get;
		}
		
		TagCollection TagComments {
			get;
		}
		
		ArrayList FoldingRegions {
			get;
		}
	}
}
