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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Documents
{
	[RequireService (typeof(DocumentControllerService))]
	public class DocumentControllerExtensionTests : TestBase
	{
		public TestCaseData [] Filters = {
			new TestCaseData (new ExportDocumentControllerExtensionAttribute(), false).SetName ("Unspecified"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = "*" }, true).SetName ("FileExtension=*"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".foo" }, false).SetName ("FileExtension=foo"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".test" }, true).SetName ("FileExtension=.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = "test" }, false).SetName ("FileExtension=test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".foo, .bar" }, false).SetName ("FileExtensions=.foo,.bar"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".foo, .test" }, true).SetName ("FileExtensions=.foo,.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = "" }, false).SetName ("FileExtensions=empty"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "*" }, true).SetName ("MimeType=*"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "application/bar" }, false).SetName ("MimeType=application_bar"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "application/test" }, true).SetName ("MimeType=application_test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "application/bar, application/foo" }, false).SetName ("MimeTypes=foo,bar"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "application/bar, application/test" }, true).SetName ("MimeTypes=bar,test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { MimeType = "" }, false).SetName ("MimeTypes=empty"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "*" }, true).SetName ("FileName=*"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "base.foo" }, false).SetName ("FileName=base.foo"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "foo.test" }, true).SetName ("FileName=foo.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "test" }, false).SetName ("FileName=test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "base.foo, base.bar" }, false).SetName ("FileNames=base.foo,base.bar"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "base.foo, foo.test" }, true).SetName ("FileNames=base.foo,foo.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "foo.*" }, true).SetName ("FileNames=foo.*"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "fo?.*" }, true).SetName ("FileNames=fo?.*"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "?oo.test" }, true).SetName ("FileNames=?oo.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "*.test" }, true).SetName ("FileNames=*.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "*.t, *.te, *.tes" }, false).SetName ("FileNames=*.t, *.te, *.tes"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "*.t, *.te, *.tes,*.test" }, true).SetName ("FileNames=*.t, *.te, *.tes,*.test"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FilePattern = "" }, false).SetName ("FileNames=empty"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".test",MimeType = "application/bar" }, true).SetName ("FileExtension=.test,Type=bar"),
			new TestCaseData (new ExportDocumentControllerExtensionAttribute { FileExtension = ".car",MimeType = "application/test" }, true).SetName ("FileExtension=.car,Type=test"),
		};

		[Test, TestCaseSource("Filters")]
		public async Task FileAttributeFilters (ExportDocumentControllerExtensionAttribute attribute, bool matches)
		{
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attribute, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", "application/test", null));

					var ext = controller.GetContent<ProducerExtension> ();
					if (matches)
						Assert.IsNotNull (ext);
					else
						Assert.IsNull (ext);
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attribute);
			}
		}

		[Test]
		public async Task AttachControllerExtension ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test2"
			};
			try {
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (TestExtension<ExtensionThatMatches>));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (TestExtension<ExtensionThatDoesntMatch>));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					Assert.AreEqual (1, TestExtension<ExtensionThatMatches>.InstancesCreated);
					Assert.AreEqual (0, TestExtension<ExtensionThatMatches>.InstancesDisposed);

					Assert.AreEqual (0, TestExtension<ExtensionThatDoesntMatch>.InstancesCreated);
					Assert.AreEqual (0, TestExtension<ExtensionThatDoesntMatch>.InstancesDisposed);
				}

				Assert.AreEqual (1, TestExtension<ExtensionThatMatches>.InstancesCreated);
				Assert.AreEqual (1, TestExtension<ExtensionThatMatches>.InstancesDisposed);

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
			}
		}

		[Test]
		public async Task FileExtensionActivationDeactivation ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.bar", null, null));

					Assert.AreEqual (0, ProducerExtension.InstancesCreated);

					controller.FilePath = "foo.test";
					Assert.AreEqual (1, ProducerExtension.InstancesCreated);
					Assert.AreEqual (0, ProducerExtension.InstancesDisposed);
					Assert.IsNotNull (controller.GetContent<ProducerExtension> ());

					controller.FilePath = "foo.var";
					Assert.AreEqual (1, ProducerExtension.InstancesCreated);
					Assert.AreEqual (1, ProducerExtension.InstancesDisposed);
					Assert.IsNull (controller.GetContent<ProducerExtension> ());
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
			}
		}

		[Test]
		public async Task KeepExtensionInstanceOnRefresh ()
		{
			// When reloading extensions, if an extension still applies it should be reused, not re-created

			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test1, .test2"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test1"
			};
			var attr3 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test2"
			};
			try {
				TestExtension<Test1>.ResetCounters ();
				TestExtension<Test2>.ResetCounters ();
				TestExtension<Test3>.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (TestExtension<Test1>));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (TestExtension<Test2>));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr3, typeof (TestExtension<Test3>));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test1", null, null));

					var instance = controller.GetContent<TestExtension<Test1>> ();
					Assert.IsNotNull (instance);
					Assert.AreEqual (1, TestExtension<Test1>.LiveExtensions.Count ());

					Assert.IsNotNull (controller.GetContent<TestExtension<Test2>> ());
					Assert.AreEqual (1, TestExtension<Test2>.LiveExtensions.Count ());

					Assert.IsNull (controller.GetContent<TestExtension<Test3>> ());
					Assert.AreEqual (0, TestExtension<Test3>.LiveExtensions.Count ());

					controller.FilePath = "foo.test2";

					Assert.AreSame (instance, controller.GetContent<TestExtension<Test1>> ());
					Assert.AreEqual (1, TestExtension<Test1>.LiveExtensions.Count ());

					Assert.IsNull (controller.GetContent<TestExtension<Test2>> ());
					Assert.AreEqual (0, TestExtension<Test2>.LiveExtensions.Count ());

					Assert.IsNotNull (controller.GetContent<TestExtension<Test3>> ());
					Assert.AreEqual (1, TestExtension<Test3>.LiveExtensions.Count ());

					controller.FilePath = "foo.test1";

					Assert.AreSame (instance, controller.GetContent<TestExtension<Test1>> ());
					Assert.AreEqual (1, TestExtension<Test1>.LiveExtensions.Count ());

					Assert.IsNotNull (controller.GetContent<TestExtension<Test2>> ());
					Assert.AreEqual (1, TestExtension<Test2>.LiveExtensions.Count ());

					Assert.IsNull (controller.GetContent<TestExtension<Test3>> ());
					Assert.AreEqual (0, TestExtension<Test3>.LiveExtensions.Count ());
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr3);
			}
		}

		[Test]
		public async Task OwnerActivationDeactivation ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (OwnerConditionedExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, new MyWorkspaceObject ()));

					Assert.IsNotNull (controller.GetContent<OwnerConditionedExtension> ());
					Assert.AreEqual (1, OwnerConditionedExtension.LiveExtensions.Count);

					controller.Owner = null;

					Assert.IsNull (controller.GetContent<OwnerConditionedExtension> ());
					Assert.AreEqual (0, OwnerConditionedExtension.LiveExtensions.Count);

					controller.Owner = new MyWorkspaceObject ();

					Assert.IsNotNull (controller.GetContent<OwnerConditionedExtension> ());
					Assert.AreEqual (1, OwnerConditionedExtension.LiveExtensions.Count);
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
			}
		}

		[Test]
		public async Task ExtensionDependingOnExtension ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = "*"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (ProducerExtension));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (ConsumerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					Assert.AreEqual (1, ProducerExtension.InstancesCreated);
					Assert.AreEqual (0, ProducerExtension.InstancesDisposed);

					Assert.AreEqual (2, ConsumerExtension.InstancesCreated);
					Assert.AreEqual (1, ConsumerExtension.InstancesDisposed);

					Assert.IsNotNull (controller.GetContent<ProducerExtension> ());
					Assert.IsNotNull (controller.GetContent<ConsumerExtension> ());

					// If Producer extension is removed, consumer should also be removed

					controller.FilePath = "foo.txt";

					Assert.IsNull (controller.GetContent<ProducerExtension> ());
					Assert.IsNull (controller.GetContent<ConsumerExtension> ());
					Assert.AreEqual (1, ProducerExtension.InstancesDisposed);
					Assert.AreEqual (0, ConsumerExtension.LiveExtensions.Count);
				}

				Assert.AreEqual (0, ProducerExtension.LiveExtensions.Count);
				Assert.AreEqual (0, ConsumerExtension.LiveExtensions.Count);

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
			}
		}

		[Test]
		[TestCase (typeof(ProducerExtension), true)]
		[TestCase (typeof (IProducer), true)]
		[TestCase (typeof (IProducer2), true)]
		[TestCase (typeof (IProducer3), true)]
		[TestCase (typeof (string), false)]
		[TestCase (typeof (Producer4), true)]
		[TestCase (typeof (Producer5), true)]
		public async Task GetContent (Type type, bool found)
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					if (found)
						Assert.IsNotNull (controller.GetContent (type));
					else
						Assert.IsNull (controller.GetContent (type));
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
			}
		}

		[Test]
		[TestCase (typeof (ProducerExtension), 2)]
		[TestCase (typeof (IProducer), 2)]
		[TestCase (typeof (IProducer2), 2)]
		[TestCase (typeof (IProducer3), 4)]
		[TestCase (typeof (string), 0)]
		[TestCase (typeof (Producer4), 3)] // one from the controller, two from extensions
		[TestCase (typeof (Producer5), 6)]
		public async Task GetContents (Type type, int count)
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = "*"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (ProducerExtension));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					Assert.AreEqual (count, controller.GetContents (type).Count ());
				}
			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
			}
		}

		[Test]
		public async Task OwnerChangedNotification ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					var owner = new MyWorkspaceObject ();
					await controller.Initialize (new FileDescriptor ("foo.test", null, owner));

					var ext = controller.GetContent<ProducerExtension> ();
					Assert.IsNotNull (ext);
					Assert.AreSame (owner, ext.KnownOwner);

					var owner2 = new MyWorkspaceObject ();
					controller.Owner = owner2;
					Assert.AreSame (owner2, ext.KnownOwner);
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
			}
		}

		[Test]
		public async Task ContentChangedNotification ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			try {
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (TestExtension<Test1>));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (TestExtension<Test2>));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					TestExtension<Test1>.ResetCounters ();
					TestExtension<Test2>.ResetCounters ();

					controller.NotifyContentChanged ();

					Assert.AreEqual (1, TestExtension<Test1>.ContentChangedCount);
					Assert.AreEqual (1, TestExtension<Test2>.ContentChangedCount);
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
			}
		}

		[Test]
		public async Task ProjectReloadCapabilityOverride ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute { FileExtension = ".full, .full-unsaved-default, .full-unsaved-default-none" };
			var attr2 = new ExportDocumentControllerExtensionAttribute { FileExtension = ".unsaved, .full-unsaved-default, .full-unsaved-default-none" };
			var attr3 = new ExportDocumentControllerExtensionAttribute { FileExtension = ".none, .full-unsaved-default-none" };
			var attr4 = new ExportDocumentControllerExtensionAttribute { FileExtension = ".default, .full-unsaved-default" };
			try {
				ProducerExtension.ResetCounters ();
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (FullReloadExtension));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (UnsavedDataReloadExtension));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr3, typeof (NoReloadExtension));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr4, typeof (ProducerExtension));

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.noext", null, null));

					// No extensions

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.Full, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					// Extension with ProjectReloadCapability.Full

					controller.FilePath = "foo.full";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.Full, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					// Extension with ProjectReloadCapability.UnsavedData

					controller.FilePath = "foo.unsaved";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					// Extension with ProjectReloadCapability.None

					controller.FilePath = "foo.none";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					// Extension with default

					controller.FilePath = "foo.default";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.Full, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					// Extensions with Full, UnsavedData and default

					controller.FilePath = "foo.full-unsaved-default";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.UnsavedData, controller.ProjectReloadCapability);

					// Extensions with Full, UnsavedData, Default and none

					controller.FilePath = "foo.full-unsaved-default-none";

					controller.SetReloadCapability (ProjectReloadCapability.None);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.Full);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);

					controller.SetReloadCapability (ProjectReloadCapability.UnsavedData);
					Assert.AreEqual (ProjectReloadCapability.None, controller.ProjectReloadCapability);
				}

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr3);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr4);
			}
		}

		[Test]
		public async Task StatusSerialization ()
		{
			var attr1 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = ".test"
			};
			var attr2 = new ExportDocumentControllerExtensionAttribute {
				FileExtension = "*"
			};
			try {
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr1, typeof (StatusTest1));
				IdeServices.DocumentControllerService.RegisterControllerExtension (attr2, typeof (StatusTest2));
				Properties storedStatus = null;

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null));

					StatusTest1.LiveExtensions [0].Status = "status1";
					StatusTest2.LiveExtensions [0].Status = "status2";

					controller.FilePath = "foo.txt";

					Assert.AreEqual (0, StatusTest1.LiveExtensions.Count);
					storedStatus = controller.GetDocumentStatus ();

					// Status should be properly restored when reactivating extension
					controller.FilePath = "foo.test";
					Assert.AreEqual (1, StatusTest1.LiveExtensions.Count);
					Assert.AreEqual ("status1", StatusTest1.LiveExtensions [0].Status);
				}

				Assert.AreEqual (0, StatusTest1.LiveExtensions.Count);
				Assert.AreEqual (0, StatusTest2.LiveExtensions.Count);

				using (var controller = new TestControllerWithExtension ()) {
					await controller.Initialize (new FileDescriptor ("foo.test", null, null), storedStatus);

					// Even though the StatusTest1 extension was disposed when the status was retrieved,
					// its status was stored before disposing, so it should be available now.
					Assert.AreEqual ("status1", StatusTest1.LiveExtensions [0].Status);
					Assert.AreEqual ("status2", StatusTest2.LiveExtensions [0].Status);
				}

				Assert.AreEqual (0, ProducerExtension.LiveExtensions.Count);
				Assert.AreEqual (0, ConsumerExtension.LiveExtensions.Count);

			} finally {
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr1);
				IdeServices.DocumentControllerService.UnregisterControllerExtension (attr2);
			}
		}
	}

	class TestControllerWithExtension: FileDocumentController
	{
		protected override object OnGetContent (Type type)
		{
			if (type == typeof (Producer4))
				return new Producer4 ();
			return null;
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			if (type != typeof (Producer4) && type != typeof (Producer5))
				yield break;

			foreach (var c in base.OnGetContents (type))
				yield return c;

			if (type == typeof (Producer5)) {
				yield return new Producer5 ();
				yield return new Producer5 ();
			}
		}

		ProjectReloadCapability capability;
		public void SetReloadCapability (ProjectReloadCapability cap)
		{
			capability = cap;
		}

		internal protected override ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return capability;
		}
	}

	class TestExtension<T>: DocumentControllerExtension
	{
		public static List<TestExtension<T>> LiveExtensions = new List<TestExtension<T>> ();

		public static int InstancesCreated;
		public static int InstancesDisposed;
		public static int ContentChangedCount;

		public string Status { get; set; }

		public TestExtension ()
		{
			InstancesCreated++;
			LiveExtensions.Add (this);
		}

		public override void Dispose ()
		{
			InstancesDisposed++;
			LiveExtensions.Remove (this);
			base.Dispose ();
		}

		public static void ResetCounters ()
		{
			LiveExtensions.Clear ();
			InstancesCreated = 0;
			InstancesDisposed = 0;
			ContentChangedCount = 0;
		}

		public override Task Initialize (Properties status)
		{
			if (status != null)
				SetDocumentStatus (status);
			return base.Initialize (status);
		}

		public override Properties GetDocumentStatus ()
		{
			var props = new Properties ();
			props.Set ("MyStatus", Status);
			return props;
		}

		public override void SetDocumentStatus (Properties properties)
		{
			Status = properties.Get<string> ("MyStatus");
		}

		protected override void OnContentChanged ()
		{
			ContentChangedCount++;
			base.OnContentChanged ();
		}
	}

	// Consumer / Producer

	interface IProducer
	{
	}

	interface IProducer2
	{
	}

	interface IProducer3
	{
	}

	class Producer4
	{
	}

	class Producer5
	{
	}

	class ProducerExtension : TestExtension<ProducerExtension>, IProducer
	{
		class MyProducer : IProducer2, IProducer3
		{
		}

		public override Task Initialize (Properties status)
		{
			KnownOwner = Controller.Owner;
			return base.Initialize (status);
		}

		protected override object OnGetContent (Type type)
		{
			if (type == typeof (IProducer2))
				return new MyProducer ();
			if (type == typeof (Producer4))
				return new Producer4 ();
			return base.OnGetContent (type);
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			var res = base.OnGetContents (type);
			if (type == typeof (IProducer3))
				return res.Concat (new object [] { new MyProducer (), new MyProducer () });
			else if (type == typeof (Producer5))
				return res.Concat (new object [] { new Producer5 (), new Producer5 () });
			else
				return res;
		}

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();
			KnownOwner = Controller.Owner;
		}

		public WorkspaceObject KnownOwner { get; set; }
	}

	class ConsumerExtension: TestExtension<ConsumerExtension>
	{
		public override Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult(controller.GetContent<IProducer> () != null);
		}
	}

	class OwnerConditionedExtension : TestExtension<OwnerConditionedExtension>
	{
		public override Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult (controller.Owner?.GetType() == typeof(MyWorkspaceObject));
		}
	}

	class MyWorkspaceObject : WorkspaceObject
	{
		protected override string OnGetBaseDirectory ()
		{
			throw new NotImplementedException ();
		}

		protected override string OnGetItemDirectory ()
		{
			throw new NotImplementedException ();
		}

		protected override string OnGetName ()
		{
			throw new NotImplementedException ();
		}
	}

	class FullReloadExtension : TestExtension<OwnerConditionedExtension>
	{
		public override ProjectReloadCapability ProjectReloadCapability => ProjectReloadCapability.Full;
	}

	class UnsavedDataReloadExtension : TestExtension<OwnerConditionedExtension>
	{
		public override ProjectReloadCapability ProjectReloadCapability => ProjectReloadCapability.UnsavedData;
	}

	class NoReloadExtension : TestExtension<OwnerConditionedExtension>
	{
		public override ProjectReloadCapability ProjectReloadCapability => ProjectReloadCapability.None;
	}

	class StatusTest1: TestExtension<StatusTest1>
	{

	}

	class StatusTest2 : TestExtension<StatusTest2>
	{

	}

	// Placeholder types

	class ExtensionThatMatches { }
	class ExtensionThatDoesntMatch { }
	class Test1 { }
	class Test2 { }
	class Test3 { }
}
