
using System;

namespace Stetic
{
	public interface IEditableObject
	{
		bool CanCopy { get; }
		bool CanCut { get; }
		bool CanPaste { get; }
		bool CanDelete { get; }
		
		void Copy ();
		void Cut ();
		void Paste ();
		void Delete ();
	}
}
