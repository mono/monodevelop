
using System;
using System.IO;

namespace MonoDevelop.Core
{
	public delegate void OperationHandler (IAsyncOperation op);
	
	public interface IAsyncOperation
	{
		void Cancel ();
		void WaitForCompleted ();
		bool IsCompleted { get; }
		bool Success { get; }

		event OperationHandler Completed;
	}
}
