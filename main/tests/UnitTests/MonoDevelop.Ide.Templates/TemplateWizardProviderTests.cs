//
// TemplateWizardProviderTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using NUnit.Framework;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class TemplateWizardProviderTests
	{
		TestableTemplateWizardProvider provider;
		TestableTemplateWizard wizard;
		ProjectCreateParameters parameters;
		SolutionTemplate template;

		void CreateProvider ()
		{
			template = new SolutionTemplate ("id", "name", "icon");
			template.Wizard = "wizard-id";
			parameters = new ProjectCreateParameters ();
			wizard = new TestableTemplateWizard ();
			provider = new TestableTemplateWizardProvider ();
			wizard = provider.Wizard;
		}

		TestableWizardPage AddOneWizardPage ()
		{
			return wizard.AddWizardPage ();
		}

		[Test]
		public void MoveFirst_TemplateHasNoWizard_ReturnsFalse ()
		{
			CreateProvider ();
			template.Wizard = null;

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsFalse (result);
		}

		[Test]
		public void MoveFirst_TemplateHasWizardButNoWizardExists_ReturnsFalse ()
		{
			CreateProvider ();
			template.Wizard = "wizard-id";
			provider.Wizard = null;

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsFalse (result);
		}

		[Test]
		public void MoveFirst_OneWizardPage_ReturnsTrue ()
		{
			CreateProvider ();
			WizardPage page = AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsTrue (result);
			Assert.IsTrue (provider.IsFirstPage);
			Assert.IsTrue (provider.IsLastPage);
			Assert.AreEqual (1, provider.CurrentPageNumber);
			Assert.AreEqual (wizard, provider.CurrentWizard);
			Assert.AreEqual (page, provider.CurrentWizardPage);
			Assert.AreEqual (1, wizard.LastPageRequested);
		}

		[Test]
		public void MoveFirst_TwoWizardPages_IsLastPageIsFalse ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsTrue (result);
			Assert.IsTrue (provider.IsFirstPage);
			Assert.IsFalse (provider.IsLastPage);
		}

		[Test]
		public void MoveFirst_OneWizardPage_WizardParametersUpdated ()
		{
			CreateProvider ();
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsTrue (result);
			Assert.AreEqual (parameters, wizard.Parameters);
		}

		[Test]
		public void MoveFirst_OneWizardPage_WizardSupportedParametersUpdated ()
		{
			CreateProvider ();
			template.SupportedParameters = "MySupportedParameter";
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);
			bool supported = wizard.IsSupportedParameter ("MySupportedParameter");
			bool notSupported = wizard.IsSupportedParameter ("Unknown");

			Assert.IsTrue (result);
			Assert.IsTrue (supported);
			Assert.IsFalse (notSupported);
		}

		[Test]
		public void MoveFirst_OneWizardPageNoSupportedParametersDefined_UnknownParameterNotSupportedByWizard ()
		{
			CreateProvider ();
			template.SupportedParameters = null;
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);
			bool supported = wizard.IsSupportedParameter ("Unknown");

			Assert.IsTrue (result);
			Assert.IsFalse (supported);
		}

		[Test]
		public void MoveFirst_OneWizardPage_WizardDefaultParametersUpdated ()
		{
			CreateProvider ();
			template.DefaultParameters = "MyDefaultParameter=Test";
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);
			string value = wizard.Parameters ["MyDefaultParameter"];

			Assert.IsTrue (result);
			Assert.AreEqual ("Test", value);
		}

		[Test]
		public void MovePrevious_MoveFirstWithOneWizardPage_ReturnsFalse ()
		{
			CreateProvider ();
			template.DefaultParameters = "MyDefaultParameter=Test";
			AddOneWizardPage ();
			bool moveFirstResult = provider.MoveToFirstPage (template, parameters);

			bool result = provider.MoveToPreviousPage ();

			Assert.IsTrue (moveFirstResult);
			Assert.IsFalse (result);
		}

		[Test]
		public void MoveFirst_MoveFirstForOneWizardThenMoveFirstForTemplateWithoutWizard_StateReset ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			template.Wizard = null;

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsFalse (result);
			Assert.IsFalse (provider.HasWizard);
			Assert.IsNull (provider.CurrentWizard);
			Assert.IsNull (provider.CurrentWizardPage);
			Assert.AreEqual (0, provider.CurrentPageNumber);
			Assert.IsFalse (provider.IsFirstPage);
			Assert.IsFalse (provider.IsLastPage);
		}

		[Test]
		public void MoveNext_TwoWizardPages_CanMoveToNextPage ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			WizardPage page2 = AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);

			bool result = provider.MoveToNextPage ();

			Assert.IsTrue (result);
			Assert.IsFalse (provider.IsFirstPage);
			Assert.IsTrue (provider.IsLastPage);
			Assert.AreEqual (page2, provider.CurrentWizardPage);
			Assert.AreEqual (2, provider.CurrentPageNumber);
		}

		[Test]
		public void MoveNext_ThreeWizardPagesMoveToSecondPage_AfterMovingNextIsLastPageIsFalse ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			AddOneWizardPage ();
			AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);

			bool result = provider.MoveToNextPage ();

			Assert.IsTrue (result);
			Assert.IsFalse (provider.IsFirstPage);
			Assert.IsFalse (provider.IsLastPage);
		}

		[Test]
		public void MovePrevious_TwoWizardPagesOnSecondPage_CanMoveToPreviousPage ()
		{
			CreateProvider ();
			WizardPage page1 = AddOneWizardPage ();
			AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();

			bool result = provider.MoveToPreviousPage ();

			Assert.IsTrue (result);
			Assert.IsTrue (provider.IsFirstPage);
			Assert.IsFalse (provider.IsLastPage);
			Assert.AreEqual (page1, provider.CurrentWizardPage);
			Assert.AreEqual (1, provider.CurrentPageNumber);
		}

		[Test]
		public void MovePrevious_ThreeWizardPagesOneLastPage_IsFirstPageIsFalse ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			AddOneWizardPage ();
			AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();
			provider.MoveToNextPage ();

			bool result = provider.MoveToPreviousPage ();

			Assert.IsTrue (result);
			Assert.IsFalse (provider.IsFirstPage);
			Assert.IsFalse (provider.IsLastPage);
		}

		[Test]
		public void MoveFirst_OneWizardPageMovePastLastPageAndDispose_WizardPageIsDisposed ()
		{
			CreateProvider ();
			TestableWizardPage page = AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();

			provider.Dispose ();

			Assert.IsTrue (page.IsDisposed);
		}

		[Test]
		public void MoveFirst_TwoWizardPagesMovePastLastPageAndDispose_BothPagesDisposed ()
		{
			CreateProvider ();
			TestableWizardPage page1 = AddOneWizardPage ();
			TestableWizardPage page2 = AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();

			provider.Dispose ();

			Assert.IsTrue (page1.IsDisposed);
			Assert.IsTrue (page2.IsDisposed);
		}

		[Test]
		public void MovePrevious_MoveToSecondPageAndThenBackToFirst_FirstPageIsNotRequestedFromWizardAgain ()
		{
			CreateProvider ();
			TestableWizardPage page1 = AddOneWizardPage ();
			TestableWizardPage page2 = AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();
			wizard.Pages.Clear ();

			bool result = provider.MoveToPreviousPage ();

			Assert.IsTrue (result);
			Assert.AreEqual (page1, provider.CurrentWizardPage);
			Assert.IsFalse (page1.IsDisposed);
			Assert.IsFalse (page2.IsDisposed);
		}

		[Test]
		public void MovePrevious_MoveToSecondPageAndThenBackToFirstAndBackToSecondAgain_SecondPageIsNotRequestedFromWizardAgain ()
		{
			CreateProvider ();
			TestableWizardPage page1 = AddOneWizardPage ();
			TestableWizardPage page2 = AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToNextPage ();
			wizard.Pages.Clear ();
			provider.MoveToPreviousPage ();

			bool result = provider.MoveToNextPage ();

			Assert.IsTrue (result);
			Assert.AreEqual (page2, provider.CurrentWizardPage);
			Assert.IsFalse (page1.IsDisposed);
			Assert.IsFalse (page2.IsDisposed);
		}

		[Test]
		public void MoveFirst_MoveFirstForOneWizardThenMoveFirstForDifferentTemplate_PageFromSecondWizardDisplayed ()
		{
			CreateProvider ();
			AddOneWizardPage ();
			provider.MoveToFirstPage (template, parameters);
			provider.MoveToPreviousPage ();
			wizard.Pages.Clear ();
			TestableWizardPage newPage = AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);

			Assert.IsTrue (result);
			Assert.AreEqual (newPage, provider.CurrentWizardPage);
		}

		[Test]
		public void MoveFirst_OneWizardPageAndTwoSupportedParametersSeparatedBySemiColons_WizardSupportedParametersUpdated ()
		{
			CreateProvider ();
			template.SupportedParameters = "MySupportedParameter1; MySupportedParameter2";
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);
			bool supported1 = wizard.IsSupportedParameter ("MySupportedParameter1");
			bool supported2 = wizard.IsSupportedParameter ("MySupportedParameter2");
			bool notSupported = wizard.IsSupportedParameter ("Unknown");

			Assert.IsTrue (result);
			Assert.IsTrue (supported1);
			Assert.IsTrue (supported2);
			Assert.IsFalse (notSupported);
		}

		[Test]
		public void MoveFirst_OneWizardPageAndTwoDefaultParametersSeparatedBySemiColons_WizardDefaultParametersUpdated ()
		{
			CreateProvider ();
			template.DefaultParameters = "MyDefaultParameter1=Test1; MyDefaultParameter2=Test2";
			AddOneWizardPage ();

			bool result = provider.MoveToFirstPage (template, parameters);
			string value1 = wizard.Parameters ["MyDefaultParameter1"];
			string value2 = wizard.Parameters ["MyDefaultParameter2"];

			Assert.IsTrue (result);
			Assert.AreEqual ("Test1", value1);
			Assert.AreEqual ("Test2", value2);
		}
	}
}

