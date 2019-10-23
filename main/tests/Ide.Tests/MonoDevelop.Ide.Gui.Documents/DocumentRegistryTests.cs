//
// DocumentRegistryTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using System.IO;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Core.Logging;

namespace MonoDevelop.Ide.Gui.Documents
{
	[TestFixture]
	public class DocumentRegistryTests : IdeTestBase
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
		public async Task TestVSTSBug8608 ()
		{
			var controller = new RootDisposableTestController ();
			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);

			var path = Path.GetTempFileName ();
			var logger = new LoggingServiceTestsLogger ();
			try {
				LoggingService.AddLogger (logger);
				DocumentRegistry.Add (doc);
				controller.DocumentTitle = "New Title";
				Assert.AreEqual (0, logger.Messages.Count);
			} finally {
				DocumentRegistry.Remove (doc);
				LoggingService.RemoveLogger (logger.Name);
				File.Delete (path);
			}
		}


		[Test]
		public async Task IgnoreAllChangedFilesTest ()
		{
			var controller = new FileDocumentController ();
			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			controller.FilePath = Path.GetTempFileName ();

			try {
				DocumentRegistry.Add (doc);
				File.WriteAllText (controller.FilePath, "test");
				doc.IsDirty = doc.Window.ShowNotification = true;
				await DocumentRegistry.IgnoreAllChangedFiles ();
				Assert.IsFalse (doc.Window.ShowNotification);
			} finally {
				DocumentRegistry.Remove (doc);
				File.Delete (controller.FilePath);
			}
		}

		[Test]
		public async Task ReloadChangedFilesTest ()
		{
			var controller = new FileDocumentController ();
			await controller.Initialize (new ModelDescriptor ());

			var doc = await documentManager.OpenDocument (controller);
			controller.FilePath = Path.GetTempFileName ();
			bool reloaded = false;
			doc.Reloaded += delegate {
				reloaded = true;
			};
			try {
				DocumentRegistry.Add (doc);
				File.WriteAllText (controller.FilePath, "test");
				await DocumentRegistry.ReloadAllChangedFiles ();
				await Runtime.RunInMainThread (delegate {
					Assert.IsTrue (reloaded);
				});
			} finally {
				DocumentRegistry.Remove (doc);
				File.Delete (controller.FilePath);
			}
		}



		class LoggingServiceTestsLogger : ILogger
		{
			readonly object lockObj = new object ();
			readonly List<Tuple<LogLevel, string>> messages = new List<Tuple<LogLevel, string>> ();

			public IReadOnlyList<Tuple<LogLevel, string>> Messages => messages;

			public void Log (LogLevel level, string message)
			{
				lock (lockObj)
					messages.Add (new Tuple<LogLevel, string> (level, message));
			}

			public EnabledLoggingLevel EnabledLevel => EnabledLoggingLevel.All;

			public string Name => "Logging tests logger";
		}
	}
}
