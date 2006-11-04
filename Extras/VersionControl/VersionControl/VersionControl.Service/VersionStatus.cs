
using System;

namespace VersionControl.Service
{
	public enum VersionStatus
	{
		Unknown,
		Unversioned,
		UnversionedIgnored,
		Missing,
		Obstructed,
		
		Unchanged,
		Protected,
		
		Modified,
		ScheduledAdd,
		ScheduledDelete,
		ScheduledReplace,
		Conflicted
	}
}
