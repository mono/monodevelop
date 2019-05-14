using System;
namespace LeakTest
{
	public interface ITestScenarioProvider
	{
		ITestScenario GetTestScenario ();
	}
}
