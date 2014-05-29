//
// UserAgentGeneratorForRepositoryRequestsTests.cs
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

using System;
using System.Net;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UserAgentGeneratorForRepositoryRequestsTests
	{
		UserAgentGeneratorForRepositoryRequests generator;

		FakePackageRepositoryFactoryEvents repositoryFactoryEvents;

		void CreateGenerator ()
		{
			repositoryFactoryEvents = new FakePackageRepositoryFactoryEvents ();
			generator = new UserAgentGeneratorForRepositoryRequests ();
			generator.Register (repositoryFactoryEvents);
		}

		IPackageRepository CreatePackageRepository ()
		{
			return new FakePackageRepository ();
		}

		FakePackageRepositoryWithHttpClientEvents CreatePackageRepositoryThatImplementsIHttpClientEvents ()
		{
			return new FakePackageRepositoryWithHttpClientEvents ();
		}

		void FireRepositoryCreatedEvent (FakePackageRepositoryWithHttpClientEvents clientEvents)
		{
			FireRepositoryCreatedEvent (clientEvents as IPackageRepository);
		}

		void FireRepositoryCreatedEvent (IPackageRepository repository)
		{
			var eventArgs = new PackageRepositoryFactoryEventArgs (repository);
			repositoryFactoryEvents.RaiseRepositoryCreatedEvent (eventArgs);
		}

		WebRequest FireSendingRequestEvent (FakePackageRepositoryWithHttpClientEvents clientEvents)
		{
			var request = new FakeWebRequest ();
			request.Headers = new WebHeaderCollection ();

			var eventArgs = new WebRequestEventArgs (request);
			clientEvents.RaiseSendingRequestEvent (eventArgs);

			return request;
		}

		[Test]
		public void SendingRequest_UserAgentGeneration_UserAgentSetOnRequest ()
		{
			CreateGenerator ();
			var clientEvents = CreatePackageRepositoryThatImplementsIHttpClientEvents ();
			FireRepositoryCreatedEvent (clientEvents);

			WebRequest request = FireSendingRequestEvent (clientEvents);

			string userAgent = request.Headers [HttpRequestHeader.UserAgent];
			Assert.IsTrue (userAgent.StartsWith (BrandingService.ApplicationName), userAgent);
		}

		[Test]
		public void RepositoryCreated_RepositoryDoesNotImplementIHttpClientEvents_NullReferenceExceptionNotThrown ()
		{
			CreateGenerator ();
			IPackageRepository repository = CreatePackageRepository ();

			FireRepositoryCreatedEvent (repository);
		}
	}
}

