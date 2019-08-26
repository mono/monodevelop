//
// ArgumentHandler.cs
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
using System.Text.RegularExpressions;

namespace GitAskPass
{
	class ArgumentHandler
	{
		static Regex passPhraseRegex = new Regex("Enter passphrase for key \\'(.*)\\':\\s?", RegexOptions.Compiled);
		static Regex passwordPrompt = new Regex("(.*)\\'s password:\\s?", RegexOptions.Compiled);

		internal static bool Handle (string command, StreamStringReadWriter writer) => 
			HandleUsernameRequest (command, writer) || 
			HandlePasswordRequest (command, writer) || 
			HandleConnecting (command, writer) ||
			HandleSSHPassphrase (command, writer) ||
			HandleSSHPassword (command, writer) ||
			Error (command, writer);

		static bool Error (string command, StreamStringReadWriter writer)
		{
			writer.WriteLine ("Error");
			writer.WriteLine (command);
			return false;
		}

		static bool HandleSSHPassword (string command, StreamStringReadWriter writer)
		{
			var match = passwordPrompt.Match (command);
			if (match.Success) {
				writer.WriteLine ("SSHPassword");
				writer.WriteLine (match.Groups [1].Value);
				return true;
			}
			return false;
		}

		static bool HandleSSHPassphrase (string command, StreamStringReadWriter writer)
		{
			var match = passPhraseRegex.Match (command);
			if (match.Success) {
				writer.WriteLine ("SSHPassPhrase");
				writer.WriteLine (match.Groups [1].Value);
				return true;
			}
			return false;
		}

		static bool HandleConnecting (string command, StreamStringReadWriter writer)
		{
			if (command.Contains ("Are you sure you want to continue connecting")) {
				writer.WriteLine ("Continue connecting");
				return true;
			}
			return false;
		}

		static bool HandleUsernameRequest (string command, StreamStringReadWriter writer)
		{
			if (command.StartsWith ("Username", StringComparison.InvariantCultureIgnoreCase)) {
				var url = ParseUrl (command);
				writer.WriteLine ("Username");
				writer.WriteLine (url);
				return true;
			}
			return false;
		}

		static bool HandlePasswordRequest (string command, StreamStringReadWriter writer)
		{
			if (command.StartsWith ("Password", StringComparison.InvariantCultureIgnoreCase)) {
				var url = ParseUrl (command);
				writer.WriteLine ("Password");
				writer.WriteLine (url);
				return true;
			}
			return false;
		}

		static string ParseUrl (string command)
		{
			var idx1 = command.IndexOf ('\'');
			var idx2 = command.LastIndexOf ('\'');
			var url = idx1 >= 0 && idx2 >= 0 ? command.Substring (idx1 + 1, idx2 - idx1 - 1) : "unknown";
			return url;
		}
	}
}
