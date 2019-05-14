using System;
using LeakTest;

namespace MonoDevelop.StressTest
{
	public enum EditorTestRun
	{
		Legacy,
		VSEditor,
		Both,
		Default = Legacy,
	}

	public interface IEditorTestScenario : ITestScenario
	{
		EditorTestRun EditorRunConfiguration { get; }
	}
}
