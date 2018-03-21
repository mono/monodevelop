//
// ViewContent.InfoBar.cs
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public abstract partial class ViewContent
	{
		public virtual InformationArea InfoArea { get; private set; }

		public sealed class InformationArea
		{
			readonly ViewContent owner;
			InformationAreaWidget infoBar;

			public bool IsVisible { get => infoBar != null; }

			internal InformationArea (ViewContent owner)
			{
				this.owner = owner;
			}

			public void Show (string messageMarkup, params InfoButton [] buttons)
			{
				Show (null, messageMarkup, buttons);
			}

			public void Show (IconId image, string messageMarkup, params InfoButton [] buttons)
			{
				if (infoBar != null)
					throw new InvalidOperationException ("Info bar already shown.");
				infoBar = new InformationAreaWidget (image);
				infoBar.SetMessageLabel (messageMarkup);
				if (buttons != null) {
					foreach (var button in buttons) {
						var gtkButton = new Button (button.Text);
						gtkButton.Image = new ImageView (Gtk.Stock.Refresh, IconSize.Button);
						gtkButton.Clicked += delegate {
							try {
								button.ClickAction ();
							} catch (Exception e) {
								LoggingService.LogError ("Error while clicking " + button.Text, e);	
							} finally {
								if (button.AutoHideInfoArea)
									Hide ();
							}
						};
						infoBar.ActionArea.Add (gtkButton);
					}
				}
				ShowInfoBar ();
			}

			void ShowInfoBar ()
			{ 
				owner.EnsureVBoxIsCreated ();
				owner.IsDirty = true;
				// WarnOverwrite = true;
				owner.vbox.PackStart (infoBar, false, false, CHILD_PADDING);
				owner.vbox.ReorderChild (infoBar, 0);
				infoBar.ShowAll ();
				infoBar.QueueDraw ();
				if (owner.WorkbenchWindow != null)
					owner.WorkbenchWindow.ShowNotification = true;

				Shown?.Invoke (this, EventArgs.Empty);
			}

			public void Hide ()
			{
				if (owner.vbox == null || infoBar == null)
					return;
				if (infoBar.Parent == owner.vbox)
					owner.vbox.Remove (infoBar);
				infoBar.Destroy ();
				infoBar = null;

				Hidden?.Invoke (this, EventArgs.Empty);
			}

			public event EventHandler Shown;

			public event EventHandler Hidden;
		}
	}
}
