// 
// ManagePackagesDialog.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
using Gtk;
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	public partial class ManagePackagesDialog : Gtk.Dialog
	{
		ManagePackagesViewModel viewModel;
		IPackageManagementEvents packageManagementEvents;
		string messagesExpanderDefaultLabel;
		
		public ManagePackagesDialog (ManagePackagesViewModel viewModel, IPackageManagementEvents packageManagementEvents)
		{
			this.Build ();
			this.messagesExpanderDefaultLabel = messagesExpander.Label;
			
			this.viewModel = viewModel;
			this.packageManagementEvents = packageManagementEvents;
			AddPackageManagementEventHandlers ();
			LoadViewModels ();
		}
		
		void AddPackageManagementEventHandlers ()
		{
			packageManagementEvents.PackageOperationMessageLogged += PackageOperationMessageLogged;
			packageManagementEvents.PackageOperationError += PackageOperationError;
			packageManagementEvents.PackageOperationsStarting += PackageOperationsStarting;
		}
		
		void RemovePackageManagementEventHandlers ()
		{
			packageManagementEvents.PackageOperationMessageLogged -= PackageOperationMessageLogged;
			packageManagementEvents.PackageOperationError -= PackageOperationError;
			packageManagementEvents.PackageOperationsStarting -= PackageOperationsStarting;
		}

		void PackageOperationMessageLogged (object sender, PackageOperationMessageLoggedEventArgs e)
		{
			AppendMessage (e.Message.ToString ());
		}
		
		void PackageOperationError (object sender, PackageOperationExceptionEventArgs e)
		{
			messagesExpander.Label = e.Exception.Message;
			ResizeMessagesExpanderLabelWidthToFullDialogWidth ();
			AppendMessage (e.Exception.Message);
		}
		
		void PackageOperationsStarting (object sender, EventArgs e)
		{
			ResetMessageExpanderLabelWidthToDefault ();
			messagesExpander.Label = messagesExpanderDefaultLabel;
		}
		
		void AppendMessage (string message)
		{
			TextIter end = this.messagesTextView.Buffer.EndIter;
			this.messagesTextView.Buffer.Insert (ref end, message + "\n");
		}
		
		void LoadViewModels ()
		{
			this.Title = viewModel.Title;
			
			this.availablePackagesWidget.LoadViewModel (viewModel.AvailablePackagesViewModel);
			this.installedPackagesWidget.LoadViewModel (viewModel.InstalledPackagesViewModel);
			this.UpdatedPackagesWidget.LoadViewModel (viewModel.UpdatedPackagesViewModel);
			this.recentPackagesWidget.LoadViewModel (viewModel.RecentPackagesViewModel);
		}
		
		public override void Destroy ()
		{
			viewModel.Dispose ();
			RemovePackageManagementEventHandlers ();
			base.Destroy ();
		}
		
		void MessagesExpanderActivated (object sender, EventArgs e)
		{
			var child = this.VBox [this.messagesExpander] as Box.BoxChild;
			child.Expand = this.messagesExpander.Expanded;
		}
		
		void ResetMessageExpanderLabelWidthToDefault ()
		{
			messagesExpander.LabelWidget.WidthRequest = 0;
		}
		
		void ResizeMessagesExpanderLabelWidthToFullDialogWidth ()
		{
			var label = messagesExpander.LabelWidget as Label;
			label.WidthRequest = GetDialogWidth () - 30;
			label.Wrap = true;
		}
		
		int GetDialogWidth ()
		{
			int width;
			int height;
			GetSize (out width, out height);
			return width;
		}
	}
}

