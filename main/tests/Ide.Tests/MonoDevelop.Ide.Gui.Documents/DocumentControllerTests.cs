//
// DocumentControllerTests.cs
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
using IdeUnitTests;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Shell;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnitTests;
using MonoDevelop.Components;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Documents
{
	[TestFixture]
	public class DocumentControllerTests : IdeTestBase
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
		public async Task ControllerDisposal ()
		{
			var controller = new RootDisposableTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);

			Assert.IsNotNull (controller.Child1);
			Assert.IsNotNull (controller.Child2);
			Assert.IsNull (controller.Child1.Control);
			Assert.IsNull (controller.Child2.Control);

			Assert.AreEqual (1, shell.Windows.Count);
			await shell.Windows [0].RootView.Show ();

			Assert.IsNotNull (controller.Child1.Control);
			Assert.IsNotNull (controller.Child2.Control);

			await doc.Close (true);

			Assert.AreEqual (1, controller.DisposeCount);
			Assert.AreEqual (1, controller.Child1.DisposeCount);
			Assert.AreEqual (1, controller.Child2.DisposeCount);
			Assert.AreEqual (1, controller.Child1.Control.DisposeCount);
			Assert.AreEqual (1, controller.Child2.Control.DisposeCount);
		}

		static object [] LoadControllers = {
			new LoadTestController (),
			new LoadTestControllerWithInnerView (),
			new LoadTestControllerWithContainer ()
		};

		[Test]
		[TestCaseSource (nameof(LoadControllers))]
		public async Task LoadAndReload (LoadTestController controller)
		{
			await controller.Initialize (new ModelDescriptor ());
			var doc = await documentManager.OpenDocument (controller);

			Assert.AreEqual (0, controller.Loaded);
			Assert.AreEqual (0, controller.Reloaded);

			if (controller.InnerController != null) {
				Assert.AreEqual (0, controller.InnerController.Loaded);
				Assert.AreEqual (0, controller.InnerController.Reloaded);
			}

			await controller.GetDocumentView ();

			Assert.AreEqual (0, controller.Loaded);
			Assert.AreEqual (0, controller.Reloaded);

			if (controller.InnerController != null) {
				Assert.AreEqual (0, controller.InnerController.Loaded);
				Assert.AreEqual (0, controller.InnerController.Reloaded);
			}

			await doc.ForceShow ();

			Assert.AreEqual (1, controller.Loaded);
			Assert.AreEqual (0, controller.Reloaded);

			if (controller.InnerController != null) {
				Assert.AreEqual (1, controller.InnerController.Loaded);
				Assert.AreEqual (0, controller.InnerController.Reloaded);
			}

			await doc.Reload ();

			Assert.AreEqual (1, controller.Loaded);
			Assert.AreEqual (1, controller.Reloaded);

			if (controller.InnerController != null) {
				Assert.AreEqual (1, controller.InnerController.Loaded);
				Assert.AreEqual (0, controller.InnerController.Reloaded);
			}

			await doc.Reload ();

			Assert.AreEqual (1, controller.Loaded);
			Assert.AreEqual (2, controller.Reloaded);

			if (controller.InnerController != null) {
				Assert.AreEqual (1, controller.InnerController.Loaded);
				Assert.AreEqual (0, controller.InnerController.Reloaded);
			}
		}

		[Test]
		public async Task ContentChangedEvent ()
		{
			var controller = new ContentTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var theContent = new SomeContent ();

			int contentChangedEvents = 0;

			controller.ContentChanged += (s, a) => {
				contentChangedEvents++;
			};

			Assert.AreEqual (0, contentChangedEvents);

			controller.AddContent (theContent);

			Assert.AreEqual (1, contentChangedEvents);

			controller.RemoveContent (theContent);

			Assert.AreEqual (2, contentChangedEvents);
		}

		[Test]
		public async Task RunWhenContentAdded ()
		{
			var controller = new ContentTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var theContent = new SomeContent ();

			int totalEvents = 0;
			int contentAddedEvents = 0;
			int additionalContentAddedEvents = 0;

			var r1 = controller.RunWhenContentAdded<SomeContent> (c => {
				totalEvents++;
				if (c == theContent)
					contentAddedEvents++;
			});

			Assert.AreEqual (0, totalEvents);
			Assert.AreEqual (0, contentAddedEvents);
			Assert.AreEqual (0, additionalContentAddedEvents);

			controller.AddContent (theContent);

			Assert.AreEqual (1, totalEvents);
			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (0, additionalContentAddedEvents);

			controller.RunWhenContentAdded<SomeContent> (c => {
				totalEvents++;
				if (c == theContent)
					additionalContentAddedEvents++;
			});

			Assert.AreEqual (2, totalEvents);
			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (1, additionalContentAddedEvents);

			controller.RemoveContent (theContent);

			Assert.AreEqual (2, totalEvents);
			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (1, additionalContentAddedEvents);

			controller.AddContent (theContent);

			Assert.AreEqual (4, totalEvents);
			Assert.AreEqual (2, contentAddedEvents);
			Assert.AreEqual (2, additionalContentAddedEvents);

			var oldContent = theContent;
			theContent = new SomeContent ();
			controller.ReplaceContent (oldContent, theContent);

			Assert.AreEqual (6, totalEvents);
			Assert.AreEqual (3, contentAddedEvents);
			Assert.AreEqual (3, additionalContentAddedEvents);

			r1.Dispose ();

			controller.RemoveContent (theContent);
			theContent = new SomeContent ();
			controller.AddContent (theContent);

			Assert.AreEqual (7, totalEvents);
			Assert.AreEqual (3, contentAddedEvents);
			Assert.AreEqual (4, additionalContentAddedEvents);
		}

		[Test]
		public async Task RunWhenContentRemoved ()
		{
			var controller = new ContentTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var theContent = new SomeContent ();

			int totalEvents = 0;
			int contentEvents = 0;
			int additionalContentEvents = 0;

			var r1 = controller.RunWhenContentRemoved<SomeContent> (c => {
				totalEvents++;
				if (c == theContent)
					contentEvents++;
			});

			Assert.AreEqual (0, totalEvents);
			Assert.AreEqual (0, contentEvents);
			Assert.AreEqual (0, additionalContentEvents);

			controller.AddContent (theContent);

			Assert.AreEqual (0, totalEvents);
			Assert.AreEqual (0, contentEvents);
			Assert.AreEqual (0, additionalContentEvents);

			controller.RunWhenContentRemoved<SomeContent> (c => {
				totalEvents++;
				if (c == theContent)
					additionalContentEvents++;
			});

			Assert.AreEqual (0, totalEvents);
			Assert.AreEqual (0, contentEvents);
			Assert.AreEqual (0, additionalContentEvents);

			controller.RemoveContent (theContent);

			Assert.AreEqual (2, totalEvents);
			Assert.AreEqual (1, contentEvents);
			Assert.AreEqual (1, additionalContentEvents);

			controller.AddContent (theContent);

			Assert.AreEqual (2, totalEvents);
			Assert.AreEqual (1, contentEvents);
			Assert.AreEqual (1, additionalContentEvents);

			var newContent = new SomeContent ();
			controller.ReplaceContent (theContent, newContent);
			theContent = newContent;

			Assert.AreEqual (4, totalEvents);
			Assert.AreEqual (2, contentEvents);
			Assert.AreEqual (2, additionalContentEvents);

			r1.Dispose ();

			controller.RemoveContent (theContent);

			Assert.AreEqual (5, totalEvents);
			Assert.AreEqual (2, contentEvents);
			Assert.AreEqual (3, additionalContentEvents);
		}

		[Test]
		public async Task RunWhenContentAddedOrRemoved ()
		{
			var controller = new ContentTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var addedContent = new SomeContent ();
			var removedContent = addedContent;

			int totalEvents = 0;
			int contentAddedEvents = 0;
			int contentRemovedEvents = 0;
			int additionalContentAddedEvents = 0;
			int additionalContentRemovedEvents = 0;

			var r1 = controller.RunWhenContentAddedOrRemoved<SomeContent> (
				added => {
					totalEvents++;
					if (added == addedContent)
						contentAddedEvents++;
				},
				removed => {
					totalEvents++;
					if (removed == removedContent)
						contentRemovedEvents++;
				}
			);

			Assert.AreEqual (0, totalEvents);
			Assert.AreEqual (0, contentAddedEvents);
			Assert.AreEqual (0, contentRemovedEvents);
			Assert.AreEqual (0, additionalContentAddedEvents);
			Assert.AreEqual (0, additionalContentRemovedEvents);

			controller.AddContent (addedContent);

			Assert.AreEqual (1, totalEvents);
			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (0, contentRemovedEvents);
			Assert.AreEqual (0, additionalContentAddedEvents);
			Assert.AreEqual (0, additionalContentRemovedEvents);

			controller.RunWhenContentAddedOrRemoved<SomeContent> (
				added => {
					totalEvents++;
					if (added == addedContent)
						additionalContentAddedEvents++;
				},
				removed => {
					totalEvents++;
					if (removed == removedContent)
						additionalContentRemovedEvents++;
				}
			);

			Assert.AreEqual (2, totalEvents);
			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (0, contentRemovedEvents);
			Assert.AreEqual (1, additionalContentAddedEvents);
			Assert.AreEqual (0, additionalContentRemovedEvents);

			controller.RemoveContent (addedContent);

			Assert.AreEqual (1, contentAddedEvents);
			Assert.AreEqual (1, contentRemovedEvents);
			Assert.AreEqual (1, additionalContentAddedEvents);
			Assert.AreEqual (1, additionalContentRemovedEvents);
			Assert.AreEqual (4, totalEvents);

			controller.AddContent (addedContent);

			Assert.AreEqual (6, totalEvents);
			Assert.AreEqual (2, contentAddedEvents);
			Assert.AreEqual (1, contentRemovedEvents);
			Assert.AreEqual (2, additionalContentAddedEvents);
			Assert.AreEqual (1, additionalContentRemovedEvents);

			addedContent = new SomeContent ();
			controller.ReplaceContent (removedContent, addedContent);
			removedContent = addedContent;

			Assert.AreEqual (10, totalEvents);
			Assert.AreEqual (3, contentAddedEvents);
			Assert.AreEqual (2, contentRemovedEvents);
			Assert.AreEqual (3, additionalContentAddedEvents);
			Assert.AreEqual (2, additionalContentRemovedEvents);

			r1.Dispose ();

			controller.RemoveContent (addedContent);
			controller.AddContent (addedContent);

			Assert.AreEqual (12, totalEvents);
			Assert.AreEqual (3, contentAddedEvents);
			Assert.AreEqual (2, contentRemovedEvents);
			Assert.AreEqual (4, additionalContentAddedEvents);
			Assert.AreEqual (3, additionalContentRemovedEvents);
		}
	}

	class DummyControl : Control
	{
		public int DisposeCount;

		protected override void Dispose (bool disposing)
		{
			DisposeCount++;
			base.Dispose (disposing);
		}
	}

	class RootDisposableTestController : DocumentController
	{
		public int DisposeCount;

		public ChildDisposableTestController Child1 { get; set; }
		public ChildDisposableTestController Child2 { get; set; }

		protected override void OnDispose ()
		{
			DisposeCount++;
			base.OnDispose ();
		}

		protected override async Task<DocumentView> OnInitializeView ()
		{
			var container = new DocumentViewContainer ();

			Child1 = new ChildDisposableTestController ();
			await Child1.Initialize (new ModelDescriptor ());
			var view1 = await Child1.GetDocumentView ();
			container.Views.Add (view1);

			Child2 = new ChildDisposableTestController ();
			await Child2.Initialize (new ModelDescriptor ());
			var view2 = await Child2.GetDocumentView ();
			container.Views.Add (view2);

			return container;
		}
	}

	class ChildDisposableTestController : DocumentController
	{
		public int DisposeCount;

		public DummyControl Control { get; set; }

		protected override Task<DocumentView> OnInitializeView ()
		{
			return Task.FromResult<DocumentView> (new DocumentViewContent (ControlCreator));
		}

		private Control ControlCreator ()
		{
			Control = new DummyControl ();
			return Control;
		}

		protected override void OnDispose ()
		{
			DisposeCount++;
			base.OnDispose ();
		}
	}

	public class LoadTestController : DocumentController
	{
		public int Loaded;
		public int Reloaded;

		public LoadTestController InnerController;

		protected override Task OnLoad (bool reloading)
		{
			if (reloading)
				Reloaded++;
			else
				Loaded++;
			return base.OnLoad (reloading);
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			return new DummyControl ();
		}
	}

	class LoadTestControllerWithInnerView : LoadTestController
	{
		protected override async Task<DocumentView> OnInitializeView ()
		{
			InnerController = new LoadTestController ();
			await InnerController.Initialize (null);
			return await InnerController.GetDocumentView ();
		}
	}

	class LoadTestControllerWithContainer : LoadTestController
	{
		protected override async Task<DocumentView> OnInitializeView ()
		{
			var container = new DocumentViewContainer ();
			InnerController = new LoadTestController ();
			await InnerController.Initialize (null);
			container.Views.Add (await InnerController.GetDocumentView ());
			return container;
		}
	}

	class ContentTestController: DocumentController
	{
		List<object> content = new List<object> ();

		public void AddContent (object ob)
		{
			content.Add (ob);
			NotifyContentChanged ();
		}

		public void RemoveContent (object ob)
		{
			content.Remove (ob);
			NotifyContentChanged ();
		}

		public void ReplaceContent (object oldOb, object newOb)
		{
			content.Remove (oldOb);
			content.Add (newOb);
			NotifyContentChanged ();
		}

		protected override object OnGetContent (Type type)
		{
			var c = content.FirstOrDefault (ob => type.IsInstanceOfType (ob));
			if (c != null)
				return c;
			return base.OnGetContent (type);
		}
	}

	class SomeContent
	{
	}
}
