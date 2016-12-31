using System.Runtime.InteropServices;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;

namespace Mono.Debugging.Win32
{
	public class CorValRef : CorValRef<CorValue>
	{
		public CorValRef (CorValue val) : base (val)
		{
		}

		public CorValRef (CorValue val, ValueLoader loader) : base (val, loader)
		{
		}

		public CorValRef (ValueLoader loader) : base (loader)
		{
		}
	}

	public class CorValRef<TValue> where TValue : CorValue
	{
		TValue val;
		readonly ValueLoader loader;
		bool needToReload = false;

		public delegate TValue ValueLoader ( );

		public CorValRef (TValue val)
		{
			this.val = val;
		}

		public CorValRef (TValue val, ValueLoader loader)
		{
			this.val = val;
			this.loader = loader;
		}

		public CorValRef (ValueLoader loader)
		{
			this.val = loader ();
			this.loader = loader;
		}

		bool IsAlive ()
		{
			if (val == null)
				return false;
			try {
				// ReSharper disable once UnusedVariable

				// https://msdn.microsoft.com/en-us/library/ms232466(v=vs.110).aspx
				// MSDN says that ICorDebugValue doesn't guarantee that it alives between process Continue and Stop (which occur while evaluating).
				// But in the most of cases the value remains valid.
				// Instead of reloading it every time when evalTimestamp changes we try to call GetExactType (because it pure for the value)
				// and if it fails we reload the value
				var valExactType = val.ExactType;
			}
			catch (COMException e) {
				if (e.ToHResult<HResult> () == HResult.CORDBG_E_OBJECT_NEUTERED) {
					DebuggerLoggingService.LogMessage (string.Format ("Value is out of date: {0}", e.Message));
					return false;
				}
				throw;
			}
			return true;
		}

		public bool IsValid {
			get
			{
				if (needToReload)
					return false;
				return IsAlive ();
			}
		}

		public void Invalidate ()
		{
			needToReload = true;
		}

		public void Reload ()
		{
			if (loader != null) {
				// Obsolete value, get a new one
				var v = loader ();
				if (v != null) {
					val = v;
					needToReload = false;
				}
			}
		}

		public TValue Val {
			get {
				if (!IsValid) {
					Reload ();
				}
				return val;
			}
		}
	}
}
