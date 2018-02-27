//
// InfoBarViewContent.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// A view content that handles file changed & file auto save warnings.
	/// </summary>
	public abstract class InfoBarViewContent : ViewContent, IDocumentReloadPresenter
	{
		const uint CHILD_PADDING = 0;

		VBox vbox;
		InfoBar infoBar;

		public bool IsInfoBarVisible {
			get { return infoBar != null; }
		}

		public sealed override Control Control { get { EnsureVBoxIsCreated (); return vbox; } }

		public abstract Gtk.Widget ContentControl { get; }

		void EnsureVBoxIsCreated ()
		{
			if (vbox != null)
				return;
			vbox = new VBox ();
			vbox.SetSizeRequest (32, 32);
			vbox.Accessible.SetShouldIgnore (true);
			Console.WriteLine ("control:" + ContentControl);
			vbox.PackStart (ContentControl, true, true, 0);
			vbox.ShowAll ();
		}

		public async Task Reload ()
		{
			try {
				if (!System.IO.File.Exists (ContentName))
					return;
				await Load (new FileOpenInformation (ContentName) { IsReloadOperation = true });
				WorkbenchWindow.ShowNotification = false;
			} catch (Exception ex) {
				MessageService.ShowError ("Could not reload the file.", ex);
			} finally {
				RemoveInfoBar ();
			}
		}

		public void ShowFileChangedWarning (bool multiple)
		{
			RemoveInfoBar();

			if (infoBar == null) {
				infoBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				infoBar.SetMessageLabel (GettextCatalog.GetString(
					"<b>The file \"{0}\" has been changed outside of {1}.</b>\n" +
					"Do you want to keep your changes, or reload the file from disk?",
					EllipsizeMiddle(ContentName, 50), BrandingService.ApplicationName));

				var b1 = new Button (GettextCatalog.GetString ("_Reload from disk"));
				b1.Image = new ImageView (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += async delegate {
					await Reload();
					WorkbenchWindow.SelectWindow ();
					RemoveInfoBar ();
				};
				infoBar.ActionArea.Add (b1);

				var b2 = new Button (GettextCatalog.GetString ("_Keep changes"));
				b2.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
				b2.Clicked += delegate {
					RemoveInfoBar ();
					WorkbenchWindow.ShowNotification = false;
				};
				infoBar.ActionArea.Add (b2);

				if (multiple) {
					var b3 = new Button (GettextCatalog.GetString ("_Reload all"));
					b3.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
					b3.Clicked += delegate {
						DocumentRegistry.ReloadAllChangedFiles ();
						RemoveInfoBar ();
					};
					infoBar.ActionArea.Add (b3);

					var b4 = new Button (GettextCatalog.GetString ("_Ignore all"));
					b4.Image = new ImageView (Gtk.Stock.Cancel, IconSize.Button);
					b4.Clicked += delegate {
						DocumentRegistry.IgnoreAllChangedFiles ();
						RemoveInfoBar ();
					};
					infoBar.ActionArea.Add (b4);
				}
			}
			ShowInfoBar ();
		}

		public void ShowAutoSaveWarning (string fileName)
		{
			RemoveInfoBar ();
			if (infoBar == null) {
				infoBar = new MonoDevelop.Components.InfoBar (MessageType.Warning);
				infoBar.SetMessageLabel (BrandingService.BrandApplicationName (GettextCatalog.GetString (
						"<b>An autosave file has been found for this file.</b>\n" +
						"This could mean that another instance of MonoDevelop is editing this " +
						"file, or that MonoDevelop crashed with unsaved changes.\n\n" +
						"Do you want to use the original file, or load from the autosave file?")));

				Button b1 = new Button (GettextCatalog.GetString ("_Use original file"));
				b1.Image = new ImageView (Gtk.Stock.Refresh, IconSize.Button);
				b1.Clicked += delegate {
					try {
						AutoSave.RemoveAutoSaveFile (fileName);
						WorkbenchWindow.SelectWindow ();
						Load (fileName);
						WorkbenchWindow.Document.ReparseDocument ();
					} catch (Exception ex) {
						LoggingService.LogError ("Could not remove the autosave file.", ex);
					} finally {
						RemoveInfoBar ();
					}
				};
				infoBar.ActionArea.Add (b1);

				Button b2 = new Button (GettextCatalog.GetString ("_Load from autosave"));
				b2.Image = new ImageView (Gtk.Stock.RevertToSaved, IconSize.Button);
				b2.Clicked += delegate {
					try {
						var content = AutoSave.LoadAndRemoveAutoSave (fileName);
						WorkbenchWindow.SelectWindow ();
						Load (new FileOpenInformation (fileName) {
							ContentText = content.Text,
							Encoding = content.Encoding
						});
						IsDirty = true;
					} catch (Exception ex) {
						LoggingService.LogError ("Could not remove the autosave file.", ex);
					} finally {
						RemoveInfoBar ();
					}

				};
				infoBar.ActionArea.Add (b2);
			}

			ShowInfoBar ();
			ContentControl.Visible = false;
		}

		void ShowInfoBar ()
		{
			IsDirty = true;
			// WarnOverwrite = true;
			EnsureVBoxIsCreated ();
			vbox.PackStart (infoBar, false, false, CHILD_PADDING);
			vbox.ReorderChild (infoBar, 0);
			infoBar.ShowAll ();
			infoBar.QueueDraw ();
			vbox.ShowAll ();
			if (WorkbenchWindow != null)
				WorkbenchWindow.ShowNotification = true;
		}

		public virtual void RemoveInfoBar ()
		{
			if (infoBar != null) {
				if (infoBar.Parent == vbox)
					vbox.Remove (infoBar);
				infoBar.Destroy ();
				infoBar = null;
			}
			if (!ContentControl.Visible)
				ContentControl.Visible = true;
		}

		internal static string EllipsizeMiddle (string str, int truncLen)
		{
			if (str == null) 
				return "";
			if (str.Length <= truncLen) 
				return str;
			
			string delimiter = "...";
			int leftOffset = (truncLen - delimiter.Length) / 2;
			int rightOffset = str.Length - truncLen + leftOffset + delimiter.Length;
			return str.Substring (0, leftOffset) + delimiter + str.Substring (rightOffset);
		}
	}
}