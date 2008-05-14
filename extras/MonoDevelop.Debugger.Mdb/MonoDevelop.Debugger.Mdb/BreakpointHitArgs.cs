using System;

namespace MonoDevelop.Debugger
{
	public class BreakpointHitArgs : EventArgs
	{
		string filename;
		int linenumber;
		
		public BreakpointHitArgs (string filename, int linenumber)
		{
			this.filename = filename;
			this.linenumber = linenumber;
		}
		
		public string Filename {
			get {
				return filename;
			}
		}
		
		public int LineNumber {
			get {
				return linenumber;
			}
		}
	}
}