// 
// ProjectLoadProgressMonitor.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011 Xamarin Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Ide.Gui
{
	public class GtkProjectLoadProgressMonitor : WrappedProgressMonitor, IProjectLoadProgressMonitor
	{
		MigrationType? Migration {
			get; set;
		}
		
		public MonoDevelop.Projects.Solution CurrentSolution { get; set; }

		public GtkProjectLoadProgressMonitor (IProgressMonitor monitor)
			: base (monitor)
		{
			
		}
		
		public MigrationType ShouldMigrateProject ()
		{
			if (!IdeApp.IsInitialized)
				return MigrationType.Ignore;

			if (Migration.HasValue)
				return Migration.Value;

			var buttonBackupAndMigrate = new AlertButton (GettextCatalog.GetString ("Back up and migrate"));
			var buttonMigrate = new AlertButton (GettextCatalog.GetString ("Migrate"));
			var buttonIgnore = new AlertButton (GettextCatalog.GetString ("Ignore"));
			var response = MessageService.AskQuestion (
				GettextCatalog.GetString ("Migrate Project?"),
				BrandingService.BrandApplicationName (GettextCatalog.GetString (
					"One or more projects must be migrated to a new format. " +
					"After migration, it will not be able to be opened in " +
					"older versions of MonoDevelop.\n\n" +
					"If you choose to back up the project before migration, a copy of the project " +
					"file will be saved in a 'backup' directory in the project directory.")),
				buttonIgnore, buttonMigrate, buttonBackupAndMigrate);

			// If we get an unexpected response, the default should be to *not* migrate
			if (response == buttonBackupAndMigrate)
				Migration = MigrationType.BackupAndMigrate;
			else if (response == buttonMigrate)
				Migration = MigrationType.Migrate;
			else 
				Migration = MigrationType.Ignore;

			return Migration.Value;
		}
	}
}
