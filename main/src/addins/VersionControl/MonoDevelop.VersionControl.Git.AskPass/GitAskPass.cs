//
// GitAskPass.cs
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

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;

namespace GitAskPass
{
	class GitAskPass
	{
		public static int Main (string [] args)
		{
			if (args.Length < 1) {
				Console.WriteLine ("No arguments specified.");
				return 1;
			}

			string pipe = Environment.GetEnvironmentVariable ("MONODEVELOP_GIT_ASKPASS_PIPE");
			if (string.IsNullOrEmpty (pipe)) {
				Console.WriteLine ("No arguments specified.");
				return 1;
			}
			try {
				using (var client = new NamedPipeClientStream (".", pipe, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation)) {
					client.Connect ();
					var writer = new StreamStringReadWriter (client);
					if (!ArgumentHandler.Handle (args [0], writer)) {
						Console.WriteLine ("can't handle : " + args [0]);
						return 2;
					}
					Console.WriteLine (writer.ReadLine ());
				}
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				return -1;
			}
			return 0;
		}
	}
}
