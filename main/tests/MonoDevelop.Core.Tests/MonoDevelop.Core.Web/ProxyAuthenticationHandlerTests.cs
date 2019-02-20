// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client
// Based on: NuGet.Core.Tests/NuGet.Protocol.Tests/HttpSource/ProxyAuthenticationHandlerTests.cs

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace MonoDevelop.Core.Web
{
	[TestFixture]
	public class ProxyAuthenticationHandlerTests
	{
		static readonly Uri ProxyAddress = new Uri ("http://127.0.0.1:8888/");

		[Test]
		public async Task SendAsync_WithUnauthenticatedProxy_PassesThru ()
		{
			var defaultClientHandler = GetDefaultClientHandler ();

			var service = Mock.Of<ICredentialService> ();
			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ()) {
				InnerHandler = GetLambdaHandler (HttpStatusCode.OK)
			};

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.OK, response.StatusCode);
		}

		[Test]
		public async Task SendAsync_WithMissingCredentials_Returns407 ()
		{
			var defaultClientHandler = GetDefaultClientHandler ();

			var service = Mock.Of<ICredentialService> ();
			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ()) {
				InnerHandler = GetLambdaHandler (HttpStatusCode.ProxyAuthenticationRequired)
			};

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);

			Mock.Get (service)
				.Verify (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		[Test]
		public async Task SendAsync_WithAcquiredCredentials_RetriesRequest ()
		{
			var defaultClientHandler = GetDefaultClientHandler ();

			var service = Mock.Of<ICredentialService> ();
			Mock.Get (service)
				.Setup (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Returns (() => Task.FromResult<ICredentials> (new NetworkCredential ()));

			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ());

			var responses = new Queue<HttpStatusCode> (
				new [] { HttpStatusCode.ProxyAuthenticationRequired, HttpStatusCode.OK });
			var innerHandler = new LambdaMessageHandler (
				_ => new HttpResponseMessage (responses.Dequeue ()));
			handler.InnerHandler = innerHandler;

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.OK, response.StatusCode);

			Mock.Get (service)
				.Verify (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
				Times.Once ());
		}

		[Test]
		public async Task SendAsync_WhenCancelledDuringAcquiringCredentials_Throws ()
		{
			// Arrange
			var defaultClientHandler = GetDefaultClientHandler ();

			var cts = new CancellationTokenSource ();

			var service = Mock.Of<ICredentialService> ();
			Mock.Get (service)
				.Setup (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.ThrowsAsync (new TaskCanceledException ())
				.Callback (() => cts.Cancel ());

			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ());

			var responses = new Queue<HttpStatusCode> (
				new [] { HttpStatusCode.ProxyAuthenticationRequired, HttpStatusCode.OK });
			var innerHandler = new LambdaMessageHandler (
				_ => new HttpResponseMessage (responses.Dequeue ()));
			handler.InnerHandler = innerHandler;

			// Act
			await AssertThrowsAsync<TaskCanceledException> (
				() => SendAsync (handler, cancellationToken: cts.Token));

			Mock.Get (service)
				.Verify (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
				Times.Once);
		}

		[Test]
		public async Task SendAsync_WithWrongCredentials_StopsRetryingAfter3Times ()
		{
			var defaultClientHandler = GetDefaultClientHandler ();

			var service = Mock.Of<ICredentialService> ();
			Mock.Get (service)
				.Setup (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Returns (() => Task.FromResult<ICredentials> (new NetworkCredential ()));

			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ());

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => { retryCount++; return new HttpResponseMessage (HttpStatusCode.ProxyAuthenticationRequired); });
			handler.InnerHandler = innerHandler;

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);

			Assert.AreEqual (ProxyAuthenticationHandler.MaxAuthRetries, retryCount);

			Mock.Get (service)
				.Verify (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Exactly (2));
		}

		[Test]
		public async Task SendAsync_WhenCredentialServiceThrows_Returns407 ()
		{
			var defaultClientHandler = GetDefaultClientHandler ();

			var service = Mock.Of<ICredentialService> ();
			Mock.Get (service)
				.Setup (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Throws (new InvalidOperationException ("Credential service failed acquiring credentials"));

			var handler = new ProxyAuthenticationHandler (defaultClientHandler, service, new ProxyCache ()) {
				InnerHandler = GetLambdaHandler (HttpStatusCode.ProxyAuthenticationRequired)
			};

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);

			Mock.Get (service)
				.Verify (
					x => x.GetCredentialsAsync (
						ProxyAddress,
						It.IsAny<IWebProxy> (),
						CredentialType.ProxyCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		static HttpClientHandler GetDefaultClientHandler ()
		{
			var proxy = new TestProxy (ProxyAddress);
			return new HttpClientHandler { Proxy = proxy };
		}

		static LambdaMessageHandler GetLambdaHandler (HttpStatusCode statusCode)
		{
			return new LambdaMessageHandler (
				_ => new HttpResponseMessage (statusCode));
		}

		static async Task<HttpResponseMessage> SendAsync (
			HttpMessageHandler handler,
			HttpRequestMessage request = null,
			CancellationToken cancellationToken = default (CancellationToken))
		{
			using (var client = new HttpClient (handler)) {
				return await client.SendAsync (request ?? new HttpRequestMessage (HttpMethod.Get, "http://foo"), cancellationToken);
			}
		}

		static async Task AssertThrowsAsync<T> (Func<Task> handler)
		{
			try {
				await handler ();
				Assert.Fail ("Exception not thrown");
			} catch (Exception ex) {
				Assert.IsInstanceOf<T> (ex);
			}
		}
	}
}
