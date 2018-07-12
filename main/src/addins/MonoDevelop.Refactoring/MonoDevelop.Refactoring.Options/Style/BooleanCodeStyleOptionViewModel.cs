//
// BooleanCodeStyleOptionViewModel.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Refactoring.Options
{
	/// <summary>
	/// This class represents the view model for a <see cref="CodeStyleOption{T}"/>
	/// that binds to the codestyle options UI.
	/// </summary>
	internal class BooleanCodeStyleOptionViewModel : AbstractCodeStyleOptionViewModel
	{
		private readonly string _truePreview;
		private readonly string _falsePreview;

		private CodeStylePreference _selectedPreference;
		private NotificationOptionViewModel _selectedNotificationPreference;

		public BooleanCodeStyleOptionViewModel (
			IOption option,
			string description,
			string truePreview,
			string falsePreview,
			AbstractOptionPreviewViewModel info,
			OptionSet options,
			string groupName,
			List<CodeStylePreference> preferences = null,
			List<NotificationOptionViewModel> notificationPreferences = null)
			: base (option, description, info, options, groupName, preferences, notificationPreferences)
		{
			_truePreview = truePreview;
			_falsePreview = falsePreview;

			var codeStyleOption = ((CodeStyleOption<bool>)options.GetOption (new OptionKey (option, option.IsPerLanguage ? info.Language : null)));
			_selectedPreference = Preferences.Single (c => c.IsChecked == codeStyleOption.Value);

			var notificationViewModel = NotificationPreferences.Single (i => i.Notification.Value == codeStyleOption.Notification.Value);
			_selectedNotificationPreference = NotificationPreferences.Single (p => p.Notification.Value == notificationViewModel.Notification.Value);

			NotifyPropertyChanged (nameof (SelectedPreference));
			NotifyPropertyChanged (nameof (SelectedNotificationPreference));
		}

		public override CodeStylePreference SelectedPreference {
			get => _selectedPreference;

			set {
				if (SetProperty (ref _selectedPreference, value)) {
					Info.SetOptionAndUpdatePreview (new CodeStyleOption<bool> (_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview ());
				}
			}
		}

		public override NotificationOptionViewModel SelectedNotificationPreference {
			get => _selectedNotificationPreference;

			set {
				if (SetProperty (ref _selectedNotificationPreference, value)) {
					Info.SetOptionAndUpdatePreview (new CodeStyleOption<bool> (_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview ());
				}
			}
		}

		public override string GetPreview ()
			=> SelectedPreference.IsChecked ? _truePreview : _falsePreview;
	}
}
