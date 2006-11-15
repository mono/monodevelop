
using System;

namespace VersionControl.Service
{
	public enum VersionStatus
	{
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
		ScheduledIgnore,
		Conflicted
	}
}
