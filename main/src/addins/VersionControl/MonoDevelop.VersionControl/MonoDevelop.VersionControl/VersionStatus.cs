
using System;

namespace MonoDevelop.VersionControl
{
	[Flags]
	public enum VersionStatus
	{
		Unmodified       = 0x00000000,
		Unversioned      = 0x00000001,
		Ignored          = 0x00000002,
		LockRequired     = 0x00000004,  // A lock is required to edit this file
		LockOwned        = 0x00000008,  // File locked by the current user
		Locked           = 0x00000010,  // File locked by another user
		
		Modified         = 0x00010000,
		ScheduledAdd     = 0x00020000,
		ScheduledDelete  = 0x00040000,
		ScheduledReplace = 0x00080000,
		ScheduledIgnore  = 0x00100000,
		Missing          = 0x00200000,
		Conflicted       = 0x00400000,
		
		LocalChangesMask = 0x3fff0000,
	}
}
