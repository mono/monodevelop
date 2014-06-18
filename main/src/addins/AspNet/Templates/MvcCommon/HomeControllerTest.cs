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

			// Assert
			Assert.AreEqual ("Welcome to ASP.NET MVC on Mono!", result.ViewData["Message"]);
		}
	}
}