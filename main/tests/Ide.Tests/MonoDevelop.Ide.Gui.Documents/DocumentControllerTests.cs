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

namespace MonoDevelop.Ide.Gui.Documents
{
	[TestFixture]
	public class DocumentControllerTests: IdeTestBase
	{
		DocumentManager documentManager;
		DocumentControllerService documentControllerService;
		MockShell shell;

		[SetUp]
		public async Task Setup ()
		{
			documentManager = await Runtime.GetService<DocumentManager> ();
			documentControllerService = await Runtime.GetService<DocumentControllerService> ();
			shell = (MockShell) await Runtime.GetService<IShell> ();
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
}
