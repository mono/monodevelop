//
// ServiceProviderTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class ServiceProviderTests
	{
		[SetUp]
		public void Setup ()
		{
			TestService.Created = false;
			TestService.Initialized = false;
			TestService2.Initialized2 = false;
		}

		[Test]
		public async Task GetService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s = await serviceProvider.GetService<TestService> ();

			Assert.IsTrue (TestService.Created);
			Assert.IsTrue (TestService.Initialized);
		}

		[Test]
		public async Task GetService_NotImplementingIService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s = await serviceProvider.GetService<ServiceObject> ();
			Assert.IsNotNull (s);
		}

		[Test]
		public async Task GetServiceConcurrent ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s1 = serviceProvider.GetService<TestService> ();
			var s2 = serviceProvider.GetService<TestService> ();

			await s1;
			await s2;

			Assert.IsTrue (TestService.Created);
			Assert.IsTrue (TestService.Initialized);
			Assert.AreSame (s1.Result, s2.Result);
		}

		[Test]
		public async Task GetServiceConcurrent_NotImplementingIService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s1 = serviceProvider.GetService<ServiceObject> ();
			var s2 = serviceProvider.GetService<ServiceObject> ();

			await s1;
			await s2;

			Assert.AreSame (s1.Result, s2.Result);
		}

		[Test]
		public async Task DefaultServiceOverride ()
		{
			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.RegisterServiceType<TestService, TestService2> ();
			var s = await serviceProvider.GetService<TestService> ();
			Assert.IsInstanceOfType (typeof (TestService2), s);

			Assert.IsTrue (TestService.Created);
			Assert.IsFalse (TestService.Initialized);
			Assert.IsTrue (TestService2.Initialized2);
		}

		[Test]
		public async Task DefaultServiceOverride_NotImplementingIService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.RegisterServiceType<ServiceObject, ServiceObject2> ();
			var s = await serviceProvider.GetService<ServiceObject> ();
			Assert.IsInstanceOfType (typeof (ServiceObject2), s);
		}

		[Test]
		public async Task DefaultServiceOverrideWithInstance ()
		{
			var t2 = new TestService2 ();
			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.RegisterService<TestService> (t2);

			var s = await serviceProvider.GetService<TestService> ();
			Assert.AreSame (t2, s);

			Assert.IsTrue (TestService.Created);
			Assert.IsFalse (TestService.Initialized);
			Assert.IsFalse (TestService2.Initialized2);
		}

		[Test]
		public async Task DefaultServiceOverrideWithInstance_NotImplementingIService ()
		{
			var t2 = new ServiceObject2 ();
			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.RegisterService<ServiceObject> (t2);

			var s = await serviceProvider.GetService<ServiceObject> ();
			Assert.AreSame (t2, s);
		}

		[Test]
		public async Task PeekService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s = serviceProvider.PeekService<TestService> ();
			Assert.IsNull (s);

			Assert.IsFalse (TestService.Created);
			Assert.IsFalse (TestService.Initialized);

			s = await serviceProvider.GetService<TestService> ();

			Assert.IsTrue (TestService.Created);
			Assert.IsTrue (TestService.Initialized);

			var s2 = serviceProvider.PeekService<TestService> ();
			Assert.AreSame (s, s2);
		}

		[Test]
		public async Task PeekService_NotImplementingIService ()
		{
			var serviceProvider = new BasicServiceProvider ();
			var s = serviceProvider.PeekService<ServiceObject> ();
			Assert.IsNull (s);

			s = await serviceProvider.GetService<ServiceObject> ();
			Assert.IsNotNull (s);

			var s2 = serviceProvider.PeekService<ServiceObject> ();
			Assert.AreSame (s, s2);
		}

		[Test]
		public async Task WhenInitialized ()
		{
			TestService gotService = null;

			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.WhenServiceInitialized<TestService> (ns => {
				gotService = ns;
			});

			Assert.IsFalse (TestService.Created);
			Assert.IsFalse (TestService.Initialized);
			Assert.IsNull (gotService);

			var s = await serviceProvider.GetService<TestService> ();

			Assert.IsTrue (TestService.Created);
			Assert.IsTrue (TestService.Initialized);

			// Add a bit of delay since callbacks are not immediately invoked after service initialization
			await Task.Delay (100);

			Assert.IsNotNull (gotService);
			Assert.AreSame (s, gotService);
		}

		[Test]
		public async Task WhenInitialized_NotImplementingIService ()
		{
			ServiceObject gotService = null;

			var serviceProvider = new BasicServiceProvider ();
			serviceProvider.WhenServiceInitialized<ServiceObject> (ns => {
				gotService = ns;
			});

			Assert.IsNull (gotService);

			var s = await serviceProvider.GetService<ServiceObject> ();

			// Add a bit of delay since callbacks are not immediately invoked after service initialization
			await Task.Delay (100);

			Assert.IsNotNull (gotService);
			Assert.AreSame (s, gotService);
		}

		[Test]
		public async Task WhenInitializedCancel ()
		{
			bool gotService1 = false;
			bool gotService1b = false;
			bool gotService2 = false;
			bool gotService2b = false;

			var serviceProvider = new BasicServiceProvider ();
			var r1 = serviceProvider.WhenServiceInitialized<TestService> (ns => {
				gotService1 = true;
			});
			var r1b = serviceProvider.WhenServiceInitialized<TestService> (ns => {
				gotService1b = true;
			});

			Assert.IsFalse (TestService.Created);
			Assert.IsFalse (TestService.Initialized);

			r1.Dispose ();

			var task = serviceProvider.GetService<TestService> ();
			var r2 = serviceProvider.WhenServiceInitialized<TestService> (ns => {
				gotService2 = true;
			});
			var r2b = serviceProvider.WhenServiceInitialized<TestService> (ns => {
				gotService2b = true;
			});
			r2.Dispose ();

			await task;

			// Add a bit of delay since callbacks are not immediately invoked after service initialization
			await Task.Delay (50);

			Assert.IsTrue (TestService.Created);
			Assert.IsTrue (TestService.Initialized);
			Assert.IsFalse (gotService1);
			Assert.IsFalse (gotService2);
			Assert.IsTrue (gotService1b);
			Assert.IsTrue (gotService2b);
		}

		[Test]
		public async Task WhenInitializedCancel_NotImplementingIService ()
		{
			bool gotService1 = false;
			bool gotService1b = false;
			bool gotService2 = false;
			bool gotService2b = false;

			var serviceProvider = new BasicServiceProvider ();
			var r1 = serviceProvider.WhenServiceInitialized<ServiceObject> (ns => {
				gotService1 = true;
			});
			var r1b = serviceProvider.WhenServiceInitialized<ServiceObject> (ns => {
				gotService1b = true;
			});

			r1.Dispose ();

			var task = serviceProvider.GetService<ServiceObject> ();
			var r2 = serviceProvider.WhenServiceInitialized<ServiceObject> (ns => {
				gotService2 = true;
			});
			var r2b = serviceProvider.WhenServiceInitialized<ServiceObject> (ns => {
				gotService2b = true;
			});
			r2.Dispose ();

			await task;

			// Add a bit of delay since callbacks are not immediately invoked after service initialization
			await Task.Delay (50);

			Assert.IsFalse (gotService1);
			Assert.IsTrue (gotService2);
			Assert.IsTrue (gotService1b);
			Assert.IsTrue (gotService2b);
		}

		[Test]
		public async Task DisposeServiceProvider ()
		{
			var serviceProvider = new BasicServiceProvider ();

			// Useless but it shouldn't crash
			serviceProvider.RegisterService (typeof (string), "");

			var s1 = await serviceProvider.GetService<TestService> ();
			var s2 = await serviceProvider.GetService<ServiceObject> ();
			var s3 = await serviceProvider.GetService<string> ();

			await serviceProvider.Dispose ();

			Assert.IsTrue (s1.Disposed);
			Assert.IsTrue (s2.Disposed);
		}
	}

	[DefaultServiceImplementation]
	class TestService : Service
	{
		public static bool Initialized;
		public static bool Created;

		public bool Disposed;

		public TestService ()
		{
			Created = true;
		}

		protected override async Task OnInitialize (ServiceProvider serviceProvider)
		{
			await Task.Delay (100);
			Initialized = true;
		}

		protected override async Task OnDispose ()
		{
			await Task.Delay (50);
			Disposed = true;
		}
	}

	class TestService2 : TestService
	{
		public static bool Initialized2;

		public TestService2 ()
		{
			Created = true;
		}

		protected override async Task OnInitialize (ServiceProvider serviceProvider)
		{
			await Task.Delay (100);
			Initialized2 = true;
		}
	}

	[DefaultServiceImplementation]
	class ServiceObject : IDisposable
	{
		public bool Disposed;

		public void Dispose ()
		{
			Disposed = true;
		}
	}

	class ServiceObject2 : ServiceObject
	{
	}
}