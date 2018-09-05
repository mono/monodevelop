//
// AbstractCodeStyleOptionViewModel.cs
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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.Options
{
	/// <summary>
	/// This class acts as a base for any view model that  binds to the codestyle options UI.
	/// </summary>
	/// <remarks>
	/// This supports databinding of: 
	/// Description
	/// list of CodeStyle preferences
	/// list of Notification preferences 
	/// selected code style preference
	/// selected notification preference
	/// plus, styling for visual elements.
	/// </remarks>
	internal abstract class AbstractCodeStyleOptionViewModel : AbstractNotifyPropertyChanged
	{
		protected AbstractOptionPreviewViewModel Info { get; }
		public IOption Option { get; }

		public string Description { get; set; }
		public double DescriptionMargin { get; set; } = 12d;
		public string GroupName { get; set; }
		public string GroupNameAndDescription { get; set; }
		public List<CodeStylePreference> Preferences { get; set; }
		public List<NotificationOptionViewModel> NotificationPreferences { get; set; }

		public abstract CodeStylePreference SelectedPreference { get; set; }
		public abstract string GetPreview ();

		public virtual NotificationOptionViewModel SelectedNotificationPreference {
			get { return NotificationPreferences.First (); }
			set { }
		}

		public AbstractCodeStyleOptionViewModel (
			IOption option,
			string description,
			AbstractOptionPreviewViewModel info,
			OptionSet options,
			string groupName,
			List<CodeStylePreference> preferences = null,
			List<NotificationOptionViewModel> notificationPreferences = null)
		{
			Info = info;
			Option = option;
			Description = description;
			Preferences = preferences ?? GetDefaultPreferences ();
			NotificationPreferences = notificationPreferences ?? GetDefaultNotifications ();
			GroupName = groupName;
			GroupNameAndDescription = $"{groupName}, {description}";
		}

		private static List<NotificationOptionViewModel> GetDefaultNotifications ()
		{
			return new List<NotificationOptionViewModel>
			{
				new NotificationOptionViewModel(NotificationOption.None, "issues-hide"),
				new NotificationOptionViewModel(NotificationOption.Suggestion, "issues-suggestion"),
				new NotificationOptionViewModel(NotificationOption.Warning, "issues-warning"),
				new NotificationOptionViewModel(NotificationOption.Error, "issues-error")
			};
		}

		private static List<CodeStylePreference> GetDefaultPreferences ()
		{
			return new List<CodeStylePreference>
			{
				new CodeStylePreference(GettextCatalog.GetString("Yes"), isChecked: true),
				new CodeStylePreference(GettextCatalog.GetString("No"), isChecked: false),
			};
		}
	}
}
