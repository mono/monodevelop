//
// Program.cs
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
using System.Diagnostics;
using System.IO;
using Mono.Unix.Native;
using Security;

namespace MonoDevelop.AspNetCore.DevCertInstaller
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			try {
				if (ArgumentsMissing (args)) {
					Console.WriteLine ("Arguments missing.");
					return -4;
				}

				if (args [0] == "--setuid") {
					return RunDotNetDevCerts (args [1]);
				} else {
					return Run (args [0], args [1], args [2]);
				}

			} catch (Exception ex) {
				Console.WriteLine (ex.Message);
				return -2;
			}
		}

		static bool ArgumentsMissing (string[] args)
		{
			if (args.Length < 2)
				return true;

			if ((args [0] != "--setuid") && (args.Length < 3))
				return true;

			return false;
		}

		/// <summary>
		/// The Mac's AuthorizationExecuteWithPrivileges runs a process as root but
		/// does not change the user id to be 0. This means that dotnet dev-certs cannot
		/// be directly used since it seems to require the user id to be 0 otherwise
		/// it will try to use sudo to trust the certificates. As a workaround another
		/// console application is run which calls setuid to change the user id to 0
		/// and then runs the dotnet dev-certs.
		/// </summary>
		static int Run (string dotNetCorePath, string monoPath, string prompt)
		{
			var fileName = typeof (MainClass).Assembly.Location;
			var args = new [] { fileName, "--setuid", dotNetCorePath };

			var parameters = new AuthorizationParameters {
				Prompt = prompt,
				PathToSystemPrivilegeTool = "" // Cannot be null otherwise the prompt is not displayed.
			};

			var flags = AuthorizationFlags.ExtendRights |
				AuthorizationFlags.InteractionAllowed |
				AuthorizationFlags.PreAuthorize;

			using (var auth = Authorization.Create (parameters, null, flags)) {
				int result = auth.ExecuteWithPrivileges (
					monoPath,
					AuthorizationFlags.Defaults,
					args);

				if (result != 0) {
					if (Enum.TryParse (result.ToString (), out AuthorizationStatus authStatus)) {
						if (authStatus == AuthorizationStatus.Canceled) {
							Console.WriteLine ("Authorization canceled.");
							return 5;
						} else {
							throw new InvalidOperationException ($"Could not get authorization. {authStatus}");
						}
					}
					throw new InvalidOperationException ($"Could not get authorization. {result}");
				}

				int status;
				if (Syscall.wait (out status) == -1) {
					throw new InvalidOperationException ("Failed to start child process.");
				}

				if (!Syscall.WIFEXITED (status)) {
					throw new InvalidOperationException ("Child process terminated abnormally.");
				}

				int exitCode = Syscall.WEXITSTATUS (status);
				if (exitCode != 0) {
					Console.WriteLine ($"Exit code from dotnet dev-certs: {exitCode}");
				}
				return exitCode;
			}
		}

		static int RunDotNetDevCerts (string dotNetCorePath)
		{
			int result = Syscall.setuid (0);
			if (result != 0) {
				Console.WriteLine ("Unable to set user id to root.");
				return -3;
			}

			using (var process = Process.Start (dotNetCorePath, "dev-certs https --trust")) {
				process.WaitForExit ();
				return process.ExitCode;
			}
		}
	}
}
