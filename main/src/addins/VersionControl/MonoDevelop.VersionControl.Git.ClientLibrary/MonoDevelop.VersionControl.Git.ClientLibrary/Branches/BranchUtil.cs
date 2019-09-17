//
// BranchUtil.cs
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
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	public class CreateTagOptions
	{
		/// <summary>
		/// The name of the new tag.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Use the given tag message.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Make an unsigned, annotated tag object
		/// </summary>
		public bool Annotate { get; set; }

		/// <summary>
		/// Replace an existing tag with the given name (instead of failing)
		/// </summary>
		public bool Force { get; set; }

		/// <summary>
		/// The object that the new tag will refer to, usually a commit. Defaults to HEAD.
		/// </summary>
		public string Commit { get; set; }

		public CreateTagOptions (string name, string Message)
		{
			this.Name = name ?? throw new ArgumentNullException (nameof (name));
			this.Message = Message ?? throw new ArgumentNullException (nameof (Message));
		}
	}

	public static class BranchUtil
	{
		public static async Task<GitLocalBranch> GetCurrentBranchAsync (string rootPath, CancellationToken cancellationToken = default)
		{
			var handler = new GitOutputTrackerCallbackHandler ();
			var arguments = new GitArguments (rootPath);
			arguments.AddArgument ("symbolic-ref");
			arguments.AddArgument ("--short");
			arguments.AddArgument ("HEAD");
			await new GitProcess ().StartAsync (arguments, handler, false, cancellationToken);
			return new GitLocalBranch (handler.Output.TrimEnd ());
		}

		#region Tags
		public static async Task<GitResult> CreateNewTagAsync (string rootPath, CreateTagOptions createTagOptions, CancellationToken cancellationToken = default)
		{
			if (createTagOptions is null)
				throw new ArgumentNullException (nameof (createTagOptions));
			var handler = new GitOutputTrackerCallbackHandler ();
			var arguments = new GitArguments (rootPath);
			arguments.AddArgument ("tag");
			if (createTagOptions.Annotate)
				arguments.AddArgument ("-a");
			if (createTagOptions.Force)
				arguments.AddArgument ("-f");
			if (!string.IsNullOrEmpty (createTagOptions.Message))
				arguments.AddArgument ("\"--message=" + createTagOptions.Message + "\"");

			arguments.AddArgument (createTagOptions.Name);

			if (createTagOptions.Commit != null)
				arguments.AddArgument (createTagOptions.Commit);
			return await new GitProcess ().StartAsync (arguments, handler, false, cancellationToken);
		}

		public static async Task<GitResult> DeleteTagAsync (string rootPath, string tagName, CancellationToken cancellationToken = default)
		{
			var handler = new GitOutputTrackerCallbackHandler ();
			var arguments = new GitArguments (rootPath);
			arguments.AddArgument ("tag");
			arguments.AddArgument ("-d");
			arguments.AddArgument (tagName);
			return await new GitProcess ().StartAsync (arguments, handler, false, cancellationToken);
		}

		class GitTagOutputHandler : GitOutputTrackerCallbackHandler
		{
			public List<string> tags = new List<string> ();

			public override void OnOutput (string line)
			{
				tags.Add (line);
			}
		}

		public static async Task<List<string>> GetAllTagsAsync (string rootPath, CancellationToken cancellationToken = default)
		{
			var handler = new GitTagOutputHandler ();
			var arguments = new GitArguments (rootPath);
			arguments.AddArgument ("tag");
			arguments.AddArgument ("-l");
			var result = await new GitProcess ().StartAsync (arguments, handler, false, cancellationToken);
			if (!result.Success)
				throw new InvalidOperationException (result.ErrorMessage);
			return handler.tags;
		}
		#endregion
	}
}
