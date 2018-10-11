using System;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.AspNetCore
{
	internal static class Counters
	{
		public static Counter UsersPublishToFolder = InstrumentationService.CreateCounter ("Users that publish to a folder", "Publish To Folder", id: "Publish.PublishToFolder");
		public static Counter UsersPublishToFolderSuccess = InstrumentationService.CreateCounter ("Times publish to a folder successfully", "Publish To Folder", id: "Publish.PublishToFolderSuccess");
		public static Counter UsersPublishToFolderFailed = InstrumentationService.CreateCounter ("Times that publish to a folder fails", "Publish To Folder", id: "Publish.PublishToFolderFails");
		//		public static Counter OpenedFromFile = InstrumentationService.CreateCounter ("Times opened from file", "Build Ouptut", id: "BuildOutput.OpenedFromFile");
		//		public static Counter SavedToFile = InstrumentationService.CreateCounter ("Times saved to file", "Build Output", id: "BuildOutput.SavedToFile");

		//		public static TimerCounter<BuildOutputCounterMetadata> ProcessBuildLog = InstrumentationService.CreateTimerCounter<BuildOutputCounterMetadata> ("Process binlog file", "Build Output", id: "BuildOutput.ProcessBuildLog");
		//		public static TimerCounter SearchBuildLog = InstrumentationService.CreateTimerCounter ("Search binlog", "Build Output", id: "BuildOutput.SearchBuildLog");
	}

	internal class BuildOutputCounterMetadata : CounterMetadata
	{
		public MSBuildVerbosity Verbosity {
			get => GetProperty<MSBuildVerbosity> ("Verbosity");
			set => SetProperty (value);
		}

		public int BuildCount {
			get => GetProperty<int> ("BuildCount");
			set => SetProperty (value);
		}

		public int RootNodesCount {
			get => GetProperty<int> ("RootNodesCount");
			set => SetProperty (value);
		}

		public long OnDiskSize {
			get => GetProperty<long> ("OnDiskSize");
			set => SetProperty (value);
		}
	}
}
