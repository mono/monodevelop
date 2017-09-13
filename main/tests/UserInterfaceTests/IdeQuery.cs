//
// IdeQuery.cs
//
// Author:
//       Manish Sinha <>
//
// Copyright (c) 2015 Xamarin Inc.
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
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Core;

namespace UserInterfaceTests
{
	public static class IdeQuery
	{
		readonly static Func<AppQuery, AppQuery> _defaultWorkbench = c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench");
		readonly static Func<AppQuery, AppQuery> _newFileDialog = c => c.Window ().Marked ("MonoDevelop.Ide.Projects.NewFileDialog");
		readonly static Func<AppQuery, AppQuery> _gitConfigurationDialog = c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog");
		readonly static Func<AppQuery, AppQuery> _editRemoteDialog = c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog");
		readonly static Func<AppQuery, AppQuery> _editBranchDialog = c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditBranchDialog");
		readonly static Func<AppQuery, AppQuery> _textArea = c => c.Window ().Children ().Marked ("Mono.TextEditor.TextArea");
		readonly static Func<AppQuery, AppQuery> _xamarinUpdate = c => c.Marked ("Visual Studio Update");

		readonly static Func<AppQuery, AppQuery> _macRunButton = c => c.Marked ("MonoDevelop.MacIntegration.MainToolbar.RunButton");

		public static Func<AppQuery, AppQuery> RunButton
		{
			get {
				if (Platform.IsMac)
					return _macRunButton;
				throw new NotImplementedException ("Run Button is not implemented for Windows");
			}
		}

		public static Func<AppQuery, AppQuery> DefaultWorkbench
		{
			get {
				return _defaultWorkbench;
			}
		}

		public static Func<AppQuery, AppQuery> NewFileDialog
		{
			get {
				return _newFileDialog;
			}
		}

		public static Func<AppQuery, AppQuery> GitConfigurationDialog
		{
			get {
				return _gitConfigurationDialog;
			}
		}

		public static Func<AppQuery, AppQuery> EditRemoteDialog
		{
			get {
				return _editRemoteDialog;
			}
		}

		public static Func<AppQuery, AppQuery> EditBranchDialog
		{
			get {
				return _editBranchDialog;
			}
		}

		public static Func<AppQuery, AppQuery> TextArea
		{
			get {
				return _textArea;
			}
		}

		public static Func<AppQuery, AppQuery> XamarinUpdate
		{
			get {
				return _xamarinUpdate;
			}
		}
	}
}

