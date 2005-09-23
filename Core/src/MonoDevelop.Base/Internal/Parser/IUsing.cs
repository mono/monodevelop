// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.Internal.Parser
{
	public interface IUsing
	{
		IRegion Region {
			get;
		}

		StringCollection Usings {
			get;
		}

		SortedList Aliases {
			get;
		}
	}
}
