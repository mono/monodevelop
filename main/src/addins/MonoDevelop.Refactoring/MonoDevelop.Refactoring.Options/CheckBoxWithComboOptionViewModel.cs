//
// CheckBoxWithComboOptionViewModel.cs
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
	/// This view model binds to a code style option UI that
	/// has a checkbox for selection and a combobox for notification levels.
	/// </summary>
	/// <remarks>
	/// At the features level, this maps to <see cref="CodeStyleOption{T}"/>
	/// </remarks>
	internal class CheckBoxWithComboOptionViewModel : AbstractCheckBoxViewModel
	{
		private NotificationOptionViewModel _selectedNotificationOption;

		public IList<NotificationOptionViewModel> NotificationOptions { get; }

		public CheckBoxWithComboOptionViewModel (IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options, IList<NotificationOptionViewModel> items)
			: this (option, description, preview, preview, info, options, items)
		{
		}

		public CheckBoxWithComboOptionViewModel (IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options, IList<NotificationOptionViewModel> items)
			: base (option, description, truePreview, falsePreview, info)
		{
			NotificationOptions = items;

			var codeStyleOption = ((CodeStyleOption<bool>)options.GetOption (new OptionKey (option, option.IsPerLanguage ? info.Language : null)));
			SetProperty (ref _isChecked, codeStyleOption.Value);

			var notificationViewModel = items.Where (i => i.Notification.Value == codeStyleOption.Notification.Value).Single ();
			SetProperty (ref _selectedNotificationOption, notificationViewModel);
		}

		public override bool IsChecked {
			get {
				return _isChecked;
			}

			set {
				SetProperty (ref _isChecked, value);
				Info.SetOptionAndUpdatePreview (new CodeStyleOption<bool> (_isChecked, _selectedNotificationOption.Notification), Option, GetPreview ());
			}
		}

		public NotificationOptionViewModel SelectedNotificationOption {
			get {
				return _selectedNotificationOption;
			}
			set {
				SetProperty (ref _selectedNotificationOption, value);
				Info.SetOptionAndUpdatePreview (new CodeStyleOption<bool> (_isChecked, _selectedNotificationOption.Notification), Option, GetPreview ());
			}
		}
	}
}
