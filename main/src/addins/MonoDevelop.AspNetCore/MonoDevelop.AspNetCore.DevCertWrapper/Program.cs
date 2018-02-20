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
using Mono.Unix.Native;
using System.Diagnostics;

namespace MonoDevelop.AspNetCore.DevCertWrapper
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			try {
				string dotNetCorePath = ParseArguments (args);
				return Run (dotNetCorePath);
			} catch (Exception ex) {
				Console.WriteLine (ex.Message);
				return -2;
			}
		}

		static string ParseArguments (string[] args)
		{
			if (args.Length > 0) {
				return args [0];
			}
			throw new InvalidOperationException ("Argument missing: Path to .NET Core runtime");
		}

		static int Run (string dotNetCorePath)
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
