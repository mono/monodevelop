using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ${SolutionName};
using ${SolutionName}.Controllers;

namespace ${Namespace}
{
	[TestFixture ()]
	public class ${Name}
	{
		[Test ()]
		public void Index ()
		{
			// Arrange
			HomeController controller = new HomeController ();

			// Act
			ViewResult result = controller.Index () as ViewResult;

			var mvcName = typeof(Controller).Assembly.GetName ();
			var isMono = Type.GetType ("Mono.Runtime") != null;

			var expectedVersion = mvcName.Version.Major;
			var expectedRuntime = isMono? "Mono" : ".NET";

			// Assert
			Assert.AreEqual (expectedVersion, ViewData ["Version"]);
			Assert.AreEqual (expectedRuntime, ViewData ["Runtime"]);
		}
	}
}