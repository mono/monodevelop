//
// DocumentViewTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System;
using System.Threading.Tasks;
using IdeUnitTests;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Shell;
using NUnit.Framework;

namespace MonoDevelop.Ide.Gui.Documents
{
	[TestFixture]
	public class DocumentViewVisibilityTests: IdeTestBase
	{
		DocumentManager documentManager;
		DocumentControllerService documentControllerService;
		MockShell shell;

		[SetUp]
		public async Task Setup ()
		{
			documentManager = await Runtime.GetService<DocumentManager> ();
			documentControllerService = await Runtime.GetService<DocumentControllerService> ();
			shell = (MockShell)await Runtime.GetService<IShell> ();
		}

		[Test]
		public async Task WithAttachedViews ()
		{
			var controller = new DocumentViewTestsController ();
			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsTrue (controller.MainView.ContentVisible);
			Assert.IsFalse (controller.Attached1.ContentVisible);
			Assert.IsFalse (controller.Attached2.ContentVisible);

			Assert.AreEqual (1, controller.MainView_VisibleChangeEvents);
			Assert.AreEqual (0, controller.Attached1_VisibleChangeEvents);
			Assert.AreEqual (0, controller.Attached2_VisibleChangeEvents);

			controller.Attached1.SetActive ();

			Assert.IsFalse (controller.MainView.ContentVisible);
			Assert.IsTrue (controller.Attached1.ContentVisible);
			Assert.IsFalse (controller.Attached2.ContentVisible);

			Assert.AreEqual (2, controller.MainView_VisibleChangeEvents);
			Assert.AreEqual (1, controller.Attached1_VisibleChangeEvents);
			Assert.AreEqual (0, controller.Attached2_VisibleChangeEvents);

			controller.Attached2.SetActive ();

			Assert.IsFalse (controller.MainView.ContentVisible);
			Assert.IsFalse (controller.Attached1.ContentVisible);
			Assert.IsTrue (controller.Attached2.ContentVisible);

			Assert.AreEqual (2, controller.MainView_VisibleChangeEvents);
			Assert.AreEqual (2, controller.Attached1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.Attached2_VisibleChangeEvents);

			controller.MainView.SetActive ();

			Assert.IsTrue (controller.MainView.ContentVisible);
			Assert.IsFalse (controller.Attached1.ContentVisible);
			Assert.IsFalse (controller.Attached2.ContentVisible);

			Assert.AreEqual (3, controller.MainView_VisibleChangeEvents);
			Assert.AreEqual (2, controller.Attached1_VisibleChangeEvents);
			Assert.AreEqual (2, controller.Attached2_VisibleChangeEvents);

			await doc.Close (true);
		}

		[Test]
		[TestCase (DocumentViewContainerMode.HorizontalSplit)]
		[TestCase (DocumentViewContainerMode.VerticalSplit)]
		public async Task WithSplits (DocumentViewContainerMode mode)
		{
			var controller = new ContentVisibleEventWithContainerTestController ();
			controller.Mode = mode;

			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			controller.View3.SetActive ();

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			controller.View1.SetActive ();

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			var extraView = new DocumentViewContent (() => new DummyControl ());
			controller.Container.Views.Add (extraView);
			Assert.IsTrue (extraView.ContentVisible);

			controller.Container.Views.Remove (extraView);
			Assert.IsFalse (extraView.ContentVisible);

			var doc2 = await documentManager.OpenDocument (new ContentVisibleEventWithContainerTestController ());
			doc2.Select ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (2, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View3_VisibleChangeEvents);

			await doc.Close (true);
			await doc2.Close (true);
		}

		[Test]
		public async Task WithTabs ()
		{
			var controller = new ContentVisibleEventWithContainerTestController ();
			controller.Mode = DocumentViewContainerMode.Tabs;

			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (0, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (0, controller.View3_VisibleChangeEvents);

			controller.View3.SetActive ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (0, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			controller.View1.SetActive ();

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View3_VisibleChangeEvents);

			var extraView = new DocumentViewContent (() => new DummyControl ());
			controller.Container.Views.Add (extraView);
			Assert.IsFalse (extraView.ContentVisible);

			var doc2 = await documentManager.OpenDocument (new ContentVisibleEventWithContainerTestController ());
			doc2.Select ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (2, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View3_VisibleChangeEvents);

			await doc.Close (true);
			await doc2.Close (true);
		}

		[Test]
		public async Task SwitchMode ()
		{
			var controller = new ContentVisibleEventWithContainerTestController ();
			controller.Mode = DocumentViewContainerMode.Tabs;

			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (0, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (0, controller.View3_VisibleChangeEvents);

			controller.Container.CurrentMode = DocumentViewContainerMode.HorizontalSplit;

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			controller.Container.CurrentMode = DocumentViewContainerMode.VerticalSplit;

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			Assert.AreEqual (1, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View3_VisibleChangeEvents);

			controller.Container.CurrentMode = DocumentViewContainerMode.Tabs;

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			Assert.AreEqual (2, controller.View1_VisibleChangeEvents);
			Assert.AreEqual (1, controller.View2_VisibleChangeEvents);
			Assert.AreEqual (2, controller.View3_VisibleChangeEvents);

			await doc.Close (true);
		}

		[Test]
		public async Task TabContainerCollectionOperations ()
		{
			var controller = new ContentVisibleEventWithContainerTestController ();
			controller.Mode = DocumentViewContainerMode.Tabs;

			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);

			var newView = new DocumentViewContent (() => new DummyControl ());
			Assert.IsFalse (newView.ContentVisible);

			controller.Container.Views [1] = newView;
			Assert.IsTrue (newView.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);

			controller.Container.Views.RemoveAt (1);
			Assert.IsFalse (newView.ContentVisible);

			controller.View1.SetActive ();
			Assert.IsTrue (controller.View1.ContentVisible);

			controller.Container.Views.Clear ();
			Assert.IsFalse (controller.View1.ContentVisible);
		}

		[Test]
		[TestCase (DocumentViewContainerMode.HorizontalSplit)]
		[TestCase (DocumentViewContainerMode.VerticalSplit)]
		public async Task SplitContainerCollectionOperations (DocumentViewContainerMode mode)
		{
			var controller = new ContentVisibleEventWithContainerTestController ();
			controller.Mode = mode;

			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			var view = await controller.GetDocumentView ();

			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View2.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			var newView = new DocumentViewContent (() => new DummyControl ());
			Assert.IsFalse (newView.ContentVisible);

			controller.Container.Views [1] = newView;
			Assert.IsTrue (newView.ContentVisible);
			Assert.IsFalse (controller.View2.ContentVisible);

			controller.Container.Views.RemoveAt (1);
			Assert.IsFalse (newView.ContentVisible);
			Assert.IsTrue (controller.View1.ContentVisible);
			Assert.IsTrue (controller.View3.ContentVisible);

			controller.Container.Views.Clear ();
			Assert.IsFalse (controller.View1.ContentVisible);
			Assert.IsFalse (controller.View3.ContentVisible);
		}
	}

	class DocumentViewTestsController: DocumentController
	{
		public DocumentViewContent MainView = new DocumentViewContent (() => new DummyControl ());
		public DocumentViewContent Attached1 = new DocumentViewContent (() => new DummyControl ());
		public DocumentViewContent Attached2 = new DocumentViewContent (() => new DummyControl ());

		public int MainView_VisibleChangeEvents;
		public int Attached1_VisibleChangeEvents;
		public int Attached2_VisibleChangeEvents;

		protected override Task<DocumentView> OnInitializeView ()
		{
			MainView.ContentVisibleChanged += (s,a) => MainView_VisibleChangeEvents++;
			Attached1.ContentVisibleChanged += (s, a) => Attached1_VisibleChangeEvents++;
			Attached2.ContentVisibleChanged += (s, a) => Attached2_VisibleChangeEvents++;

			var view = MainView;
			view.AttachedViews.Add (Attached1);
			view.AttachedViews.Add (Attached2);
			return Task.FromResult<DocumentView> (view);
		}
	}

	class ContentVisibleEventWithContainerTestController : DocumentController
	{
		public DocumentViewContainer Container = new DocumentViewContainer ();
		public DocumentViewContent View1 = new DocumentViewContent (() => new DummyControl ());
		public DocumentViewContent View2 = new DocumentViewContent (() => new DummyControl ());
		public DocumentViewContent View3 = new DocumentViewContent (() => new DummyControl ());

		public int View1_VisibleChangeEvents;
		public int View2_VisibleChangeEvents;
		public int View3_VisibleChangeEvents;

		public DocumentViewContainerMode Mode { get; set; }

		protected override Task<DocumentView> OnInitializeView ()
		{
			View1.ContentVisibleChanged += (s, a) => View1_VisibleChangeEvents++;
			View2.ContentVisibleChanged += (s, a) => View2_VisibleChangeEvents++;
			View3.ContentVisibleChanged += (s, a) => View3_VisibleChangeEvents++;

			Container.SupportedModes = DocumentViewContainerMode.Tabs | DocumentViewContainerMode.HorizontalSplit | DocumentViewContainerMode.VerticalSplit;
			Container.CurrentMode = Mode;
			Container.Views.Add (View1);
			Container.Views.Add (View2);
			Container.Views.Add (View3);
			View2.SetActive ();
			return Task.FromResult<DocumentView> (Container);
		}
	}
}
