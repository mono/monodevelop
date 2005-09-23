// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;

namespace MonoDevelop.Internal.Parser
{
	public interface IRegion: IComparable
	{
		int BeginLine {
			get;
		}

		int BeginColumn {
			get;
		}

		int EndColumn {
			get;
			set;
		}

		int EndLine {
			get;
			set;
		}
		
		string FileName {
			get;
			set;
		}

		bool IsInside(int row, int column);
	}
}
