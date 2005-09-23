// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using System.Collections.Specialized;
using ICSharpCode.SharpRefactory.Parser;

namespace MonoDevelop.Internal.Parser
{
	public interface ICompilationUnitBase
	{
		bool ErrorsDuringCompile {
			get;
			set;
		}

		ErrorInfo[] ErrorInformation {
			get;
			set;
		}
		
		object Tag {
			get;
			set;
		}
	}
}
