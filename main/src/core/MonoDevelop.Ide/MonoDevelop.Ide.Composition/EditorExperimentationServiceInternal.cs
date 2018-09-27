using System.Composition;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Utilities;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IExperimentationServiceInternal))]
	internal class EditorExperimentationServiceInternal : IExperimentationServiceInternal
	{
		public bool IsCachedFlightEnabled (string flightName) => flightName == "CompletionAPI";
	}

	[ExportWorkspaceService (typeof (IExperimentationService), ServiceLayer.Host), Shared]
	class ExperimentationService : IExperimentationService
	{
		public bool IsExperimentEnabled (string experimentName) => true;
	}
}
