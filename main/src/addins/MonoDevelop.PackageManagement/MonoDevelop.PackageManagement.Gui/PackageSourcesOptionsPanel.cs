// 
// PackageSourcesOptionPanel.cs
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
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.PackageManagement.Gui
{
	public class PackageSourcesOptionsPanel : OptionsPanel
	{
		PackageManagementViewModels viewModels;
		PackageSourcesWidget packageSourcesWidget;

		public override Gtk.Widget CreatePanelWidget()
		{
			viewModels = new PackageManagementViewModels ();
			viewModels.RegisteredPackageSourcesViewModel.Load ();
			
			packageSourcesWidget = new PackageSourcesWidget (viewModels.RegisteredPackageSourcesViewModel);
			return packageSourcesWidget;
		}

		/// <summary>
		/// Check that mono can encrypt package source passwords. This can fail if the
		/// "~/.config/.mono/keypairs" directory has incorrect permissions.
		/// 
		/// The keypairs directory will be created by the HttpWebRequest when a 
		/// request is made to url using https. The keypairs directory created has the
		/// wrong permissions so NuGet will fail to encrypt any passwords.
		/// 
		/// Use the following to fix the permissions:
		/// 
		/// chmod u=rwx,go= keypairs
		/// 
		/// This check is done here instead of in ApplyChanges so the user is presented
		/// with a slightly better error message and the user can try to fix the problem
		/// without losing their changes.
		/// </summary>
		public override bool ValidateChanges ()
		{
			if (Platform.IsWindows) {
				return true;
			}

			try {
				if (AnyPasswordsToBeEncrypted ()) {
					CheckPasswordEncryptionIsWorking ();
				}
			} catch (CryptographicException ex) {
				LoggingService.LogError ("Unable to encrypt NuGet Package Source passwords.", ex);

				MessageService.ShowMessage (
					GettextCatalog.GetString ("Unable to encrypt Package Source passwords."),
					GetEncryptionFailureMessage (ex));

				return false;
			}
			return true;
		}

		bool AnyPasswordsToBeEncrypted ()
		{
			return viewModels
				.RegisteredPackageSourcesViewModel
				.PackageSourceViewModels
				.Any (packageSource => packageSource.HasPassword ());
		}

		/// <summary>
		/// Try encrypting some data the same way NuGet does when it 
		/// encrypts passwords in the NuGet.config file.
		/// 
		/// If the ~/.config/.mono/keypairs directory 
		/// has incorrect permissions or has a corrupt key value pair then
		/// ProtectedData.Protect (...) will throw an exception.
		/// </summary>
		void CheckPasswordEncryptionIsWorking ()
		{
			var userData = new byte [] { 0xFF };
			ProtectedData.Protect (userData, null, DataProtectionScope.CurrentUser);
		}

		string GetEncryptionFailureMessage (Exception ex)
		{
			if (ex.InnerException != null) {
				return ex.InnerException.Message;
			}

			return ex.Message;
		}

		public override void ApplyChanges()
		{
			try {
				if (packageSourcesWidget.HasPackageSourcesOrderChanged) {
					viewModels.RegisteredPackageSourcesViewModel.Save (
						packageSourcesWidget.GetOrderedPackageSources ());
				} else {
					viewModels.RegisteredPackageSourcesViewModel.Save ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to save NuGet.config changes", ex);
				MessageService.ShowError (
					GettextCatalog.GetString ("Unable to save package source changes.{0}{0}{1}",
					Environment.NewLine,
					GetSaveNuGetConfigFileErrorMessage ()));
			}
		}

		/// <summary>
		/// Returns a non-Windows specific error message instead of the one NuGet returns.
		/// 
		/// NuGet returns a Windows specific error:
		/// 
		/// "DeleteSection" cannot be called on a NullSettings. This may be caused on account of 
		/// insufficient permissions to read or write to "%AppData%\NuGet\NuGet.config".
		/// </summary>
		string GetSaveNuGetConfigFileErrorMessage ()
		{
			string path = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
				"NuGet",
				"NuGet.config");
			return GettextCatalog.GetString ("Unable to read or write to \"{0}\".", path);
		}

		public override void Dispose ()
		{
			if (packageSourcesWidget != null) {
				packageSourcesWidget.Dispose ();
			}
		}
	}
}

