// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 979 $</version>
// </file>

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Converts elements not supported by C# to their C# representation.
	/// Not all elements are converted here, most simple elements (e.g. StopStatement)
	/// are converted in the output visitor.
	/// </summary>
	public class ToCSharpConvertVisitor : AbstractAstTransformer
	{
		// The following conversions should be implemented in the future:
		//   Public Event EventName(param As String) -> automatic delegate declaration
		
	}
}
