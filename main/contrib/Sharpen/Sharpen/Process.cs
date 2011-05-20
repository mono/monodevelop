using System;
using System.Diagnostics;

namespace Sharpen
{
	public class SystemProcess
	{
		Process proc;
		
		public SystemProcess ()
		{
		}
		
		public SystemProcess (Process proc)
		{
			this.proc = proc;
		}
		
		public static SystemProcess Start (ProcessStartInfo psi)
		{
			var p = Process.Start (psi);
			return new SystemProcess (p);
		}
		
		public virtual InputStream GetInputStream()
		{
			return InputStream.Wrap (proc.StandardOutput.BaseStream);
		}

		public virtual OutputStream GetOutputStream()
		{
			return OutputStream.Wrap (proc.StandardInput.BaseStream);
		}

		public virtual InputStream GetErrorStream()
		{
			return InputStream.Wrap (proc.StandardError.BaseStream);
		}

		public virtual int ExitValue()
		{
			return proc.ExitCode;
		}
		
		public virtual void Destroy()
		{
			if (!proc.HasExited) {
				try {
					proc.Kill ();
				} catch (InvalidOperationException) {
					// Already exited. Do nothing
				}
			}
		}

		public virtual int WaitFor()
		{
			proc.WaitForExit ();
			return proc.ExitCode;
		}
	}
}

