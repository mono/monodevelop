// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 2066 $</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Parser
{
	public interface IUsing
	{
		IRegion Region {
			get;
		}

		IEnumerable<string> Usings { get; }
		
		IEnumerable<string> Aliases { get; }
		
		IReturnType GetAlias (string name);
	}
}
