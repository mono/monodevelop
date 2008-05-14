using DebuggerLibrary;

using MonoDevelop.Core.Execution;

namespace MonoDevelop.Debugger
{
	class BreakpointEntry: IBreakpoint
	{
		DebuggingService service;
		BreakpointHandle handle;
		
		string file;
		int line;
		
		public BreakpointEntry (DebuggingService service, string file, int line)
		{
			this.service = service;
			this.file = file;
			this.line = line;
		}
		
		public string FileName {
			get { return file; }
		}
		
		public int Line {
			get { return line; }
		}
		
		public BreakpointHandle Handle {
			get { return handle; }
			set { handle = value; }
		}
		
		public bool Enabled {
			get {
				return handle != null && handle.IsEnabled;
			}
			set {
				if (handle == null) return;
				if (value == handle.IsEnabled) return;
				service.EnableBreakpoint (this, value);
			}
		}
	}
}
