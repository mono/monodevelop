// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client
// Based on: NuGet.Core.Tests/NuGet.Protocol.Tests/HttpSource/HttpSourceAuthenticationHandlerTests.cs

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
	public class HttpSourceAuthenticationHandlerTests
	{
		[Test]
		public void Constructor_WithSourceCredentials_InitializesClientHandler ()
		{
			if (CredentialCache.DefaultNetworkCredentials == null)
				Assert.Ignore ("No default network credentials");

			var packageSource = new Uri ("http://package.source.net");
			var credentials = new NetworkCredential ("user", "password");
			var a = CredentialCache.DefaultNetworkCredentials;

			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());
			var credentialService = Mock.Of<ICredentialService> ();

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			Assert.NotNull (clientHandler.Credentials);

			var actualCredentials = clientHandler.Credentials.GetCredential (packageSource, "Basic");
			Assert.AreEqual (CredentialCache.DefaultNetworkCredentials, actualCredentials);
		}

		[Test]
		public async Task SendAsync_WithUnauthenticatedSource_PassesThru ()
		{
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = new Mock<ICredentialService> (MockBehavior.Strict);
			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService.Object) {
				InnerHandler = GetLambdaMessageHandler (HttpStatusCode.OK)
			};

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.OK, response.StatusCode);
		}

		[Test]
		public async Task SendAsync_WithAcquiredCredentialsOn401_RetriesRequest ()
		{
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Returns (() => Task.FromResult<ICredentials> (new NetworkCredential ()));

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService) {
				InnerHandler = GetLambdaMessageHandler (
					HttpStatusCode.Unauthorized, HttpStatusCode.OK)
			};

			var response = await SendAsync (handler);

			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.OK, response.StatusCode);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		[Test]
		public async Task SendAsync_WithAcquiredCredentialsOn403_RetriesRequest ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Returns (() => Task.FromResult<ICredentials> (new NetworkCredential ()));

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService) {
				InnerHandler = GetLambdaMessageHandler (
					HttpStatusCode.Forbidden, HttpStatusCode.OK)
			};

			// Act
			var response = await SendAsync (handler);

			// Assert
			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.OK, response.StatusCode);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		[Test]
		public async Task SendAsync_WhenTaskCanceledExceptionThrownDuringAcquiringCredentials_Throws ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.ThrowsAsync (new TaskCanceledException ());

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => {
					retryCount++;
					return new HttpResponseMessage (HttpStatusCode.Unauthorized);
				});
			handler.InnerHandler = innerHandler;

			// Act & Assert
			await AssertThrowsAsync<TaskCanceledException> (
				() => SendAsync (handler));

			Assert.AreEqual (1, retryCount);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once);
		}

		[Test]
		public async Task SendAsync_WhenOperationCanceledExceptionThrownDuringAcquiringCredentials_Throws ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var cts = new CancellationTokenSource ();

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.ThrowsAsync (new OperationCanceledException ())
				.Callback (() => cts.Cancel ());

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => {
					retryCount++;
					return new HttpResponseMessage (HttpStatusCode.Unauthorized);
				});
			handler.InnerHandler = innerHandler;

			// Act & Assert
			await AssertThrowsAsync<OperationCanceledException> (
				() => SendAsync (handler));

			Assert.AreEqual (1, retryCount);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once);
		}

		[Test]
		public async Task SendAsync_WithWrongCredentials_StopsRetryingAfter3Times ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Returns (() => Task.FromResult<ICredentials> (new NetworkCredential ()));

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => {
					retryCount++;
					return new HttpResponseMessage (HttpStatusCode.Unauthorized);
				});
			handler.InnerHandler = innerHandler;

			// Act
			var response = await SendAsync (handler);

			// Assert
			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.Unauthorized, response.StatusCode);

			Assert.AreEqual (HttpSourceAuthenticationHandler.MaxAuthRetries + 1, retryCount);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Exactly (HttpSourceAuthenticationHandler.MaxAuthRetries));
		}

		[Test]
		public async Task SendAsync_WithMissingCredentials_Returns401 ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => {
					retryCount++;
					return new HttpResponseMessage (HttpStatusCode.Unauthorized);
				});
			handler.InnerHandler = innerHandler;

			// Act
			var response = await SendAsync (handler);

			// Assert
			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.Unauthorized, response.StatusCode);

			Assert.AreEqual (1, retryCount);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		[Test]
		public async Task SendAsync_WhenCredentialServiceThrows_Returns401 ()
		{
			// Arrange
			var packageSource = new Uri ("http://package.source.net");
			var clientHandler = new DefaultHttpClientHandler (null, new HttpClientSettings ());

			var credentialService = Mock.Of<ICredentialService> ();
			Mock.Get (credentialService)
				.Setup (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()))
				.Throws (new InvalidOperationException ("Credential service failed acquring user credentials"));

			var handler = new HttpSourceAuthenticationHandler (packageSource, clientHandler, credentialService);

			int retryCount = 0;
			var innerHandler = new LambdaMessageHandler (
				_ => {
					retryCount++;
					return new HttpResponseMessage (HttpStatusCode.Unauthorized);
				});
			handler.InnerHandler = innerHandler;

			// Act
			var response = await SendAsync (handler);

			// Assert
			Assert.NotNull (response);
			Assert.AreEqual (HttpStatusCode.Unauthorized, response.StatusCode);

			Assert.AreEqual (1, retryCount);

			Mock.Get (credentialService)
				.Verify (
					x => x.GetCredentialsAsync (
						packageSource,
						It.IsAny<IWebProxy> (),
						CredentialType.RequestCredentials,
						It.IsAny<bool> (),
						It.IsAny<CancellationToken> ()),
					Times.Once ());
		}

		static LambdaMessageHandler GetLambdaMessageHandler (HttpStatusCode statusCode)
		{
			return new LambdaMessageHandler (
				_ => new HttpResponseMessage (statusCode));
		}

		static LambdaMessageHandler GetLambdaMessageHandler (params HttpStatusCode [] statusCodes)
		{
			var responses = new Queue<HttpStatusCode> (statusCodes);
			return new LambdaMessageHandler (
				_ => new HttpResponseMessage (responses.Dequeue ()));
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