
using System;

namespace MonoDevelop.VersionControl
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
