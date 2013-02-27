//
// HelpOperations.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.Diagnostics;
using Monodoc;
using MonoDevelop.Core.Execution;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.Ide
{
	public class HelpOperations
	{
		ProcessWrapper pw;
		TextWriter outWriter;
		TextWriter errWriter;
		bool firstCall = true;
		bool useExternalMonodoc = false;

		ProcessStartInfo GetStartPlatformSpecificMonoDoc (string topic, params string[] extraArgs)
		{
			var builder = new ProcessArgumentBuilder ();
			extraArgs = extraArgs ?? new string[0];

			if (Platform.IsMac) {
				var url = topic != null ? "monodoc://" + System.Web.HttpUtility.UrlEncode (topic) : null;
				var mdapp = new FilePath (typeof (HelpOperations).Assembly.Location).ParentDirectory.Combine ("..", "..", "..", "MonoDoc.app").FullPath;
				if (Directory.Exists (mdapp)) {
					builder.AddQuoted ("-a", mdapp, url, "--args");
					AddDirArgs (builder);
					builder.AddQuoted (extraArgs);
				} else {
					builder.AddQuoted (url);
					builder.AddQuoted (extraArgs);
				}
				return new ProcessStartInfo ("open", builder.ToString ());
			} else if (Platform.IsWindows) {
				string mdapp = new FilePath (typeof (HelpOperations).Assembly.Location).ParentDirectory.Combine ("windoc", "WinDoc.exe").FullPath;
				if (topic != null)
					builder.AddQuoted ("--url", topic);
				AddDirArgs (builder);
				builder.AddQuoted (extraArgs);
				if (File.Exists (mdapp)) {
					return new System.Diagnostics.ProcessStartInfo {
						FileName = mdapp,
						Arguments = builder.ToString (),
						WorkingDirectory = Path.GetDirectoryName (mdapp),
					};
				}
			}

			return null;
		}

		public void ShowHelp (string topic)
		{
			if (topic == null || string.IsNullOrWhiteSpace (topic))
				return;

			var psi = GetStartPlatformSpecificMonoDoc (topic, null);
			if (psi != null) {
				Process.Start (psi);
				return;
			}
	
			if (firstCall)
				CheckExternalMonodoc ();

			if (useExternalMonodoc)
				ShowHelpExternal (topic);
		}

		public void SearchHelpFor (string searchTerm)
		{
			var searchArgs = new string[] { "--search", searchTerm };
			var psi = GetStartPlatformSpecificMonoDoc (null, searchArgs);
			if (psi == null) {
				var pb = new ProcessArgumentBuilder ();
				pb.AddQuoted (searchArgs);
				AddDirArgs (pb);
				psi = new System.Diagnostics.ProcessStartInfo {
					FileName = "monodoc",
					UseShellExecute = true,
					Arguments = pb.ToString (),
				};
			}

			Process.Start (psi);
		}

		public bool CanShowHelp (string topic)
		{
			return topic != null;
		}

		public void ShowDocs (string path)
		{
			ShowDocs (path, null);
		}

		public void ShowDocs (string path, string topic)
		{
			if (path == null)
				return;

			string[] args = null;
			if (Platform.IsMac) {
				args = new[] { '+' + path };
			} else if (Platform.IsWindows) {
				args = new[] { "--docdir", path };
			}

			var psi = GetStartPlatformSpecificMonoDoc (topic, args);
			if (psi != null)
				Process.Start (psi);
		}

		public bool CanShowDocs (string path)
		{
			return path != null && path.Length != 0 && Directory.Exists (path);
		}

		void CheckExternalMonodoc ()
		{
			firstCall = false;
			try {
				outWriter = new StringWriter ();
				errWriter = new StringWriter ();
				pw = Runtime.ProcessService.StartProcess (
					"monodoc", "--help", "", outWriter, errWriter, 
					delegate { 
						if (pw.ExitCode != 0) 
							MessageService.ShowError (
								String.Format (
								"MonoDoc exited with exit code {0}. Error : {1}", 
								pw.ExitCode, errWriter.ToString ()));
						pw = null;
					}, true);

				pw.WaitForOutput ();
				if (outWriter.ToString ().IndexOf ("--about") > 0)
					useExternalMonodoc = true;
				pw = null;
			} catch (Exception e) {
				MessageService.ShowError (String.Format (
					"Could not start monodoc : {0}", e.ToString ()));
			}

			if (!useExternalMonodoc)
				MessageService.ShowError (
					BrandingService.BrandApplicationName (GettextCatalog.GetString ("You need a newer monodoc to use it externally from MonoDevelop. Using the integrated help viewer now.")));
		}
		
		string DirArgs {
			get {
				var pb = new ProcessArgumentBuilder ();
				AddDirArgs (pb);
				return pb.ToString ();
			}
		}

		void AddDirArgs (ProcessArgumentBuilder pb)
		{
			foreach (var dir in HelpService.Sources)
				pb.AddQuotedFormat ("--docdir={0}", dir);
		}

		void ShowHelpExternal (string topic)
		{
			try {
				if (pw == null || pw.HasExited == true) {
					outWriter = new StringWriter ();
					errWriter = new StringWriter ();
					
					pw = Runtime.ProcessService.StartProcess (
						"monodoc", "--remote-mode" + DirArgs, "", outWriter, errWriter, 
						delegate { 
							if (pw.ExitCode == 0)
								return;

							MessageService.ShowError (
								String.Format (
								"MonoDoc exited with exit code {0}.", 
								pw.ExitCode, errWriter.ToString ()));
							pw = null;
						}, true);
				}

				if (pw != null && !pw.HasExited) {
					pw.StandardInput.WriteLine (topic);
					Console.WriteLine (outWriter.ToString ());
					Console.WriteLine (errWriter.ToString ());
				}
			} catch (Exception e) {
				MessageService.ShowException (e);
				useExternalMonodoc = false;
			}
		}
		
		public bool CanShowHelp (ResolveResult result)
		{
			try {
				return CanShowHelp (HelpService.GetMonoDocHelpUrl (result));
			} catch (Exception e) {
				LoggingService.LogError ("Error while trying to get monodoc help.", e);
				return false;
			}
		}
	}
}
