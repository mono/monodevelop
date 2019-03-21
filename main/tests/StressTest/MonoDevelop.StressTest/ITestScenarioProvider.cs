using System;
namespace MonoDevelop.StressTest
{
	public interface ITestScenarioProvider
	{
		ITestScenario GetTestScenario ();
	}
}
