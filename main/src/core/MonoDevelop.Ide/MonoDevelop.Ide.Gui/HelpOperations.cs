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
using Monodoc;
using MonoDevelop.Core.Execution;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Ide.Gui
{
	public class HelpOperations
	{
		ProcessWrapper pw;
		TextWriter outWriter;
		TextWriter errWriter;
		bool firstCall = true;
		bool useExternalMonodoc = false;

		public void ShowHelp (string topic)
		{
			if (topic == null || topic.Trim ().Length == 0)
				return;
	
			if (firstCall)
				CheckExternalMonodoc ();

			if (useExternalMonodoc)
				ShowHelpExternal (topic);
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
								"MonoDoc exited with a exit code = {0}. Error : {1}", 
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
					GettextCatalog.GetString ("You need a newer monodoc to use it externally from monodevelop. Using the integrated help viewer now."));
		}

		void ShowHelpExternal (string topic)
		{
			try {
				if (pw == null || pw.HasExited == true) {
					outWriter = new StringWriter ();
					errWriter = new StringWriter ();
					pw = Runtime.ProcessService.StartProcess (
						"monodoc", "--remote-mode", "", outWriter, errWriter, 
						delegate { 
							if (pw.ExitCode == 0)
								return;

							MessageService.ShowError (
								String.Format (
								"MonoDoc exited with a exit code = {0}.", 
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
		
		//note: this method is very careful to check that the generated URLs exist in MonoDoc
		//because if we send nonexistent URLS to MonoDoc, it shows empty pages
		public string GetHelpUrl (ResolveResult result)
		{
			if (result == null)
				return null;
			
			if (result is AggregatedResolveResult) 
				result = ((AggregatedResolveResult)result).PrimaryResult;
			
			
			if (result is NamespaceResolveResult)
			{
				string namespc = ((NamespaceResolveResult)result).Namespace;
				//verify that the namespace exists in the help tree
				//FIXME: GetHelpXml doesn't seem to work for namespaces, so forced to do full render
				Monodoc.Node dummy;
				if (!String.IsNullOrEmpty (namespc) && ProjectDomService.HelpTree != null && ProjectDomService.HelpTree.RenderUrl ("N:" + namespc, out dummy) != null)
					return "N:" + namespc;
				else return null;
			}
			
			IMember member = null;
			if (result is MethodResolveResult)
				member = ((MethodResolveResult)result).MostLikelyMethod;
			else if (result is MemberResolveResult)
				member = ((MemberResolveResult)result).ResolvedMember;
			
			if (member != null && member.GetMonodocDocumentation () != null)
				return member.HelpUrl;
			
			IReturnType type = result.ResolvedType;
			if (type != null && !String.IsNullOrEmpty (type.FullName)) {
				string t = "T:" + type.FullName;
				if (ProjectDomService.HelpTree != null && ProjectDomService.HelpTree.GetHelpXml (t) != null)
					return t;
			}
			
			return null;
		}
	}
}
