// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.Projects.Parser
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
	
	public class ErrorInfo
	{
		public int Line;
		public int Col;
		public string Message;
		
		public ErrorInfo(int line, int col, string message)
		{
			Line = line;
			Col = col;
			Message = message;
		}
	}
}
