
using System;
using System.Xml;
using ICSharpCode.PackageManagement;
using MonoDevelop.Projects.Formats.MSBuild;
using NuGet;
using NUnit.Framework;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MSBuildProjectExtensionsTests
	{
		MSBuildProject project;

		void CreateProject ()
		{
			project = new MSBuildProject ();
		}

		void AddImportIfMissingAtBottom (string importFile, string condition = "")
		{
			project.AddImportIfMissing (importFile, ProjectImportLocation.Bottom, condition);
		}

		void AddImportIfMissingAtTop (string importFile, string condition = "")
		{
			project.AddImportIfMissing (importFile, ProjectImportLocation.Top, condition);
		}

		void AssertLastMSBuildImportElementHasProjectAttributeValue (string expectedAttributeValue)
		{
			MSBuildImport import = GetLastMSBuildImportElement ();
			string actualAttributeValue = import.Project;
			Assert.AreEqual (expectedAttributeValue, actualAttributeValue);
		}

		void AssertLastMSBuildImportElementHasCondition (string expectedCondition)
		{
			MSBuildImport import = GetLastMSBuildImportElement ();
			Assert.AreEqual (expectedCondition, import.Condition);
		}

		MSBuildImport GetLastMSBuildImportElement ()
		{
			return project.Imports.LastOrDefault ();
		}

		void AssertFirstMSBuildImportElementHasProjectAttributeValue (string expectedAttributeValue)
		{
			MSBuildImport import = GetFirstMSBuildImportElement ();
			string actualAttributeValue = import.Project;
			Assert.AreEqual (expectedAttributeValue, actualAttributeValue);
		}

		MSBuildImport GetFirstMSBuildImportElement ()
		{
			return project.Imports.FirstOrDefault ();
		}

		void AssertFirstMSBuildImportElementHasCondition (string expectedCondition)
		{
			var import = GetFirstMSBuildImportElement ();
			Assert.AreEqual (expectedCondition, import.Condition);
		}

		[Test]
		public void AddImportIfMissing_RelativeImportFilePathAndBottomOfProject_ImportAddedAsLastImportInProject ()
		{
			CreateProject ();
			string importFile = @"..\packages\Foo.0.1\build\Foo.targets";
			
			AddImportIfMissingAtBottom (importFile);
			
			AssertLastMSBuildImportElementHasProjectAttributeValue (@"..\packages\Foo.0.1\build\Foo.targets");
		}

		[Test]
		public void AddImportIfMissing_AddImportToBottomOfProjectWithCondition_ImportAddedWithCondition ()
		{
			CreateProject ();
			string importFile = @"..\packages\Foo.0.1\build\Foo.targets";
			string condition = "Exists('..\\packages\\Foo.0.1\\build\\Foo.targets')";
			
			AddImportIfMissingAtBottom (importFile, condition);
			
			AssertLastMSBuildImportElementHasCondition (condition);
		}

		[Test]
		public void ImportExists_ImportAlreadyExists_ReturnsTrue ()
		{
			string importFile = @"packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtBottom (importFile);
			
			bool exists = project.ImportExists (importFile);
			
			Assert.IsTrue (exists);
		}

		[Test]
		public void ImportExists_ImportDoesNotExist_ReturnsFalse ()
		{
			CreateProject ();
			string importFile = @"packages\Foo.0.1\build\Foo.targets";
			
			bool exists = project.ImportExists (importFile);
			
			Assert.IsFalse (exists);
		}

		[Test]
		public void ImportExists_DifferentImportExists_ReturnsFalse ()
		{
			string importFile = @"packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtBottom ("different-import.targets");
			
			bool exists = project.ImportExists (importFile);
			
			Assert.IsFalse (exists);
		}

		[Test]
		public void AddImportIfMissing_AddSameImportTwice_ImportOnlyAddedOnceToProject ()
		{
			CreateProject ();
			string import = @"packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtBottom (import);
			
			AddImportIfMissingAtBottom (import);
			
			Assert.AreEqual (1, project.Imports.Count ());
		}

		[Test]
		public void ImportExists_ImportExistsButtWithDifferentCase_ReturnsTrue ()
		{
			CreateProject ();
			string import1 = @"packages\Foo.0.1\build\Foo.targets";
			string import2 = @"packages\Foo.0.1\BUILD\FOO.TARGETS";
			AddImportIfMissingAtBottom (import1);
			
			bool exists = project.ImportExists (import2);
			
			Assert.IsTrue (exists);
		}

		[Test]
		public void RemoveImportIfExists_ImportAlreadyAddedToBottomOfProject_ImportRemoved ()
		{
			CreateProject ();
			string import = @"packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtBottom (import);
			
			project.RemoveImportIfExists (import);
			
			Assert.AreEqual (0, project.Imports.Count ());
		}

		[Test]
		public void RemoveImportIfExists_ImportAlreadyWithDifferentCaseAddedToBottomOfProject_ImportRemoved ()
		{
			CreateProject ();
			string import1 = @"d:\projects\MyProject\packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtBottom (import1);
			string import2 = @"d:\projects\MyProject\packages\Foo.0.1\BUILD\FOO.TARGETS";
			
			project.RemoveImportIfExists (import2);
			
			Assert.AreEqual (0, project.Imports.Count ());
		}

		[Test]
		public void AddImportIfMissing_AddToTopOfProject_ImportAddedAsFirstChildElement ()
		{
			CreateProject ();
			AddImportIfMissingAtBottom ("test.targets");
			string import = @"..\packages\Foo.0.1\build\Foo.targets";
			
			AddImportIfMissingAtTop (import);
			
			AssertFirstMSBuildImportElementHasProjectAttributeValue (@"..\packages\Foo.0.1\build\Foo.targets");
		}

		[Test]
		public void AddImportIfMissing_AddImportToTopOfProject_ImportAddedWithConditionThatChecksForExistenceOfTargetsFile ()
		{
			CreateProject ();
			AddImportIfMissingAtTop ("test.targets");
			string import = @"..\packages\Foo.0.1\build\Foo.targets";
			string condition = "Exists('..\\packages\\Foo.0.1\\build\\Foo.targets')";
			
			AddImportIfMissingAtTop (import, condition);
			
			AssertFirstMSBuildImportElementHasCondition (condition);
		}

		[Test]
		public void AddImportIfMissing_AddToTopOfProjectTwice_ImportAddedOnlyOnce ()
		{
			CreateProject ();
			string import = @"..\packages\Foo.0.1\build\Foo.targets";
			AddImportIfMissingAtTop (import);
			
			AddImportIfMissingAtTop (import);
			
			Assert.AreEqual (1, project.Imports.Count ());
		}
	}
}