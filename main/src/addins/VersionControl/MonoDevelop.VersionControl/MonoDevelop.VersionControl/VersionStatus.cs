
using System;

namespace MonoDevelop.VersionControl
{
	[Flags]
	public enum VersionStatus
	{
		Unversioned      = 0x00000000,
		Versioned        = 0x00000001,
		Ignored          = 0x00000002,
		LockRequired     = 0x00000004,  // A lock is required to edit this file
		LockOwned        = 0x00000008,  // File locked by the current user
		Locked           = 0x00000010,  // File locked by another user
		
		Modified         = 0x00000100,
		ScheduledAdd     = 0x00000200,
		ScheduledDelete  = 0x00000400,
		ScheduledReplace = 0x00000800,
		ScheduledIgnore  = 0x00001000,
		Missing          = 0x00002000,
		Conflicted       = 0x00004000,
		
		LocalChangesMask = 0x0000ff00,
	}
}
