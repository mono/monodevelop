//
// AspNetCoreCertificateManager.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	static class AspNetCoreCertificateManager
	{
		public static bool IsDevelopmentCertificateTrusted { get; private set; }

		/// <summary>
		/// Only supported projects should be checked. If the development certificate
		/// was found to be trusted then do not check again for the current IDE session.
		/// </summary>
		public static bool CheckDevelopmentCertificateIsTrusted (DotNetProject project, SolutionItemRunConfiguration runConfiguration)
		{
			if (IsDevelopmentCertificateTrusted) {
				return false;
			}

			return IsProjectSupported (project, runConfiguration);
		}

		/// <summary>
		/// Only .NET Core 2.1 projects are supported.
		/// Also need .NET Core SDK 2.1 to be installed.
		/// Also check if the project is using https.
		/// </summary>
		public static bool IsProjectSupported (DotNetProject project, SolutionItemRunConfiguration runConfiguration)
		{
			if (!Platform.IsMac) {
				// Only Mac supported currently.
				return false;
			}

			return project.TargetFramework.IsNetCoreApp ("2.1") &&
				IsNetCoreSdk21Installed () &&
				UsingHttps (runConfiguration);
		}

		static bool UsingHttps (SolutionItemRunConfiguration runConfiguration)
		{
			var aspNetCoreRunConfiguration = runConfiguration as AspNetCoreRunConfiguration;
			return aspNetCoreRunConfiguration?.UsingHttps () == true;
		}

		static bool IsNetCoreSdk21Installed ()
		{
			return DotNetCoreSdk.Versions.Any (IsNetCoreSdk21);
		}

		/// <summary>
		/// This checks for .NET Core SDK 2.1 to be installed. Note that the
		/// .NET Core SDK versions is confusing. Here we want the .NET Core 2.1
		/// SDK that supports .NET Core App 2.1. Not the .NET Core 2.1 SDK that
		/// supports .NET Core App 2.0 only.
		/// </summary>
		static bool IsNetCoreSdk21 (DotNetCoreVersion version)
		{
			return version.Major == 2 && version.Minor == 1 && version.Patch >= 300;
		}

		public static async Task TrustDevelopmentCertificate (ProgressMonitor monitor)
		{
			try {
				CertificateCheckResult result = await DotNetCoreDevCertsTool.CheckCertificate (monitor.CancellationToken);
				if (result == CertificateCheckResult.OK) {
					IsDevelopmentCertificateTrusted = true;
					return;
				} else if (result == CertificateCheckResult.Error) {
					// Check failed - Do not try to trust certificate since this
					// will likely also fail.
					return;
				}

				if (ConfirmTrustCertificate (result)) {
					await DotNetCoreDevCertsTool.TrustCertificate (monitor.CancellationToken);
				}

			} catch (OperationCanceledException) {
				throw;
			} catch (Exception ex) {
				LoggingService.LogError ("Error trusting development certificate.", ex);
			}
		}

		static bool ConfirmTrustCertificate (CertificateCheckResult checkResult)
		{
			QuestionMessage message = CreateConfirmMessage (checkResult);

			message.Buttons.Add (AlertButton.Yes);
			message.Buttons.Add (AlertButton.No);

			AlertButton result = MessageService.AskQuestion (message);
			return result == AlertButton.Yes;
		}

		static QuestionMessage CreateConfirmMessage (CertificateCheckResult checkResult)
		{
			if (checkResult == CertificateCheckResult.Untrusted) {
				return new QuestionMessage {
					Text = GettextCatalog.GetString ("HTTPS development certificate is not trusted"),
					SecondaryText = GettextCatalog.GetString ("The HTTPS development certificate will be trusted by running 'dotnet-dev-certs https --trust'. Running this command may prompt you for your password to install the certificate on the system keychain.\n\nDo you want to trust this certificate?"),
				};
			}

			return new QuestionMessage {
				Text = GettextCatalog.GetString ("HTTPS development certificate was not found"),
				SecondaryText = GettextCatalog.GetString ("The HTTPS development certificate will be installed and trusted by running 'dotnet-dev-certs https --trust'. Running this command may prompt you for your password to install the certificate on the system keychain.\n\nDo you want to install and trust this certificate?"),
			};
		}
	}
}
