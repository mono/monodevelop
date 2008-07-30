// HelpViewer.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2007 Todd Berman
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
//
//

using System;
using System.IO;
using System.Text;

using Gtk;
using Monodoc;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.WebBrowser;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{

	public class HelpViewer : AbstractViewContent
	{
		IWebBrowser html_viewer;
		Widget widget;

		public override bool IsViewOnly {
			get { return true; }
		}

		public override Gtk.Widget Control {
			get { return widget; }
		}

		public override string ContentName {
			get { return GettextCatalog.GetString ("Documentation"); }
		}
		
		public override void Dispose ()
		{
			widget.Destroy ();
			base.Dispose ();
		}
		
		public static bool CanLoadHelpViewer {
			get { return WebBrowserService.CanGetWebBrowser; }
		}
		
		public HelpViewer ()
		{
			if (WebBrowserService.CanGetWebBrowser) {
				html_viewer = WebBrowserService.GetWebBrowser ();
				html_viewer.LinkClicked += LinkClicked;
				html_viewer.LinkStatusChanged += onLinkMessage;
				widget = (Widget) html_viewer;
			} else {
				Label lab = new Label (GettextCatalog.GetString ("The help viewer could not be loaded, because an embedded web browser is not available."));
				widget = lab;
			}
			Control.ShowAll ();
		}

		void onLinkMessage (object sender, StatusMessageChangedEventArgs args)
		{
			IdeApp.Workbench.StatusBar.ShowMessage (args.Message ?? string.Empty);
		}
		
		void LinkClicked (object o, LocationChangingEventArgs args)
		{
			LoadUrl (args.NextLocation);
			args.SuppressChange =  true;
		}
		
		public void LoadNode (string nodeText, Node node, string url)
		{
			if (html_viewer == null)
				return;
			try {
				string tempFile = buildTempDocDirectory (nodeText);
				html_viewer.LoadUrl (string.Format ("file://{0}#{1}", tempFile, string.Empty));
			} catch (Exception e) {
				LoadErrorHtml (e, url);
			}
		}
		
		public void LoadErrorHtml (Exception e, string missingUrl)
		{
			LoggingService.LogError ("Could not load help url {0}\n{1}", missingUrl, e.ToString ());
			html_viewer.LoadHtml (GettextCatalog.GetString("{0}Error: the help topic '{2}' could not be loaded.{1}",
			                                               "<html><body><h1>",
			                                               "</h1></body></html>",
			                                               missingUrl));
		}
 
		public void LoadUrl (string url)
		{
			if (html_viewer == null || url.StartsWith("#"))
				return;
			Node node;
			string res = Services.DocumentationService.HelpTree.RenderUrl (url, out node);
			LoadNode (res, node, url);
		}
		
		string buildTempDocDirectory (string fileText)
		{
			if (fileText == null)
				throw new ArgumentNullException ("fileText");
			
			string tempDir = Path.GetTempFileName ();
			File.Delete (tempDir);
			Directory.CreateDirectory (tempDir);
			
			StringBuilder builder = new StringBuilder ();
			builder.Append ("<html><body>");
			ProcessImages (fileText, builder, tempDir);
			builder.Append ("</body></html>");
			
			using (StreamWriter writer = File.CreateText (Path.Combine (tempDir, "index.html"))) {
				writer.Write (builder.ToString ());
			}
			return Path.Combine (tempDir, "index.html"); //FIXME: and the anchor too
		}
		
		void ProcessImages (string input, StringBuilder output, string tempDir)
		{
			int imageIndex = 0;
			int pos = 0;
			bool inCDATA = false;
			bool inComment = false;
			int previousCaptureEndedAt = 0;
			
			while (pos < (input.Length - 9)) {
				//escape from comments
				if (inComment) {
					while (pos < input.Length - 3) {
						if (input[pos] == '-' && input[pos+1] == '-' && input [pos+2] == '>') {
							pos += 2;
							break;
						}
						pos++;
					}
					continue;
				}
				//escape from CDATA
				else if (inCDATA) {
					while (pos < input.Length - 3) {
						if (input[pos] == ']' && input[pos+1] == ']' && input [pos+2] == '>') {
							pos += 2;
							break;
						}
						pos++;
					}
					continue;
				}
				//HTML/XML/SGML tags
				else if (input[pos] == '<') {
					//enter CDATA or comment
					if (input[pos+1] == '!') {
						if (input[pos+2] == '-' && input [pos+3] == '-') {
							inComment = true;
							pos += 4;
							continue;
						} else if (input.Substring (pos+2, 7) ==  "[CDATA[") {						
							pos += 9;
							continue;
						}
					}
					//IMG tags; what we're looking for!
					else if (input[pos+1] == 'i' && input [pos+2] == 'm' && input [pos+3] == 'm' ){
						//WriteImage
						//string url = "";
						//string path = Path.Combine (tempDir, "image" + imageIndex);
						//WriteImage (path, url);
						imageIndex++;
						System.Console.WriteLine(input.Substring (pos, 10));
					}
				}
				pos++;
			}
			output.Append (input.Substring (previousCaptureEndedAt));
		}
		
		void WriteImage (string path, string url)
		{
			using (Stream s = Services.DocumentationService.HelpTree.GetImage (url)) {
				using (FileStream fs = new FileStream (path, FileMode.Create)) {
					byte[] buffer = new byte [8192];
					int n = 0;
					while ((n = s.Read (buffer, 0, 8192)) != 0)
						fs.Write (buffer, 0, n);
				}
			}
		}

		public override void Load (string s)
		{
		}

	}

}
