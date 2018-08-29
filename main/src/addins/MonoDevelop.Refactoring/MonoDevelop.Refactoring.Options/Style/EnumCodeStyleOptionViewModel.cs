//
// EnumCodeStyleOptionViewModel.cs
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace MonoDevelop.Refactoring.Options
{
	/// <summary>
	/// This class represents the view model for a <see cref="CodeStyleOption{T}"/>
	/// that binds to the codestyle options UI.  Note that the T here is expected to be an enum
	/// type.  
	/// 
	/// Important.  The order of the previews and preferences provided should match the order
	/// of enum members of T.  
	/// </summary>
	internal class EnumCodeStyleOptionViewModel<T> : AbstractCodeStyleOptionViewModel
		where T : struct
	{
		static EnumCodeStyleOptionViewModel ()
		{
			Contract.ThrowIfFalse (typeof (T).IsEnum);
		}

		private readonly ImmutableArray<T> _enumValues;
		private readonly ImmutableArray<string> _previews;

		private CodeStylePreference _selectedPreference;
		private NotificationOptionViewModel _selectedNotificationPreference;

		public EnumCodeStyleOptionViewModel (
			PerLanguageOption<CodeStyleOption<T>> option,
			string language,
			string description,
			T [] enumValues,
			string [] previews,
			AbstractOptionPreviewViewModel info,
			OptionSet options,
			string groupName,
			List<CodeStylePreference> preferences)
			: this ((IOption)option, language, description, enumValues, previews, info,
				   options, groupName, preferences)
		{
		}

		public EnumCodeStyleOptionViewModel (
			Option<CodeStyleOption<T>> option,
			string description,
			T [] enumValues,
			string [] previews,
			AbstractOptionPreviewViewModel info,
			OptionSet options,
			string groupName,
			List<CodeStylePreference> preferences)
			: this (option, null, description, enumValues, previews, info,
				   options, groupName, preferences)
		{
		}

		private EnumCodeStyleOptionViewModel (
			IOption option,
			string language,
			string description,
			T [] enumValues,
			string [] previews,
			AbstractOptionPreviewViewModel info,
			OptionSet options,
			string groupName,
			List<CodeStylePreference> preferences)
			: base (option, description, info, options, groupName, preferences)
		{
			Debug.Assert (preferences.Count == enumValues.Length);
			Debug.Assert (previews.Length == enumValues.Length);

			_enumValues = enumValues.ToImmutableArray ();
			_previews = previews.ToImmutableArray ();

			var codeStyleOption = (CodeStyleOption<T>)options.GetOption (new OptionKey (option, language));

			var enumIndex = _enumValues.IndexOf (codeStyleOption.Value);
			if (enumIndex < 0 || enumIndex >= Preferences.Count) {
				enumIndex = 0;
			}

			_selectedPreference = Preferences [enumIndex];

			var notificationViewModel = NotificationPreferences.Single (i => i.Notification.Value == codeStyleOption.Notification.Value);
			_selectedNotificationPreference = NotificationPreferences.Single (p => p.Notification.Value == notificationViewModel.Notification.Value);

			NotifyPropertyChanged (nameof (SelectedPreference));
			NotifyPropertyChanged (nameof (SelectedNotificationPreference));
		}

		public override string GetPreview ()
		{
			var index = Preferences.IndexOf (SelectedPreference);
			return _previews [index];
		}

		public override CodeStylePreference SelectedPreference {
			get => _selectedPreference;

			set {
				if (SetProperty (ref _selectedPreference, value)) {
					var index = Preferences.IndexOf (value);
					var enumValue = _enumValues [index];

					Info.SetOptionAndUpdatePreview (
						new CodeStyleOption<T> (
							enumValue, _selectedNotificationPreference.Notification),
						Option, GetPreview ());
				}
			}
		}

		public override NotificationOptionViewModel SelectedNotificationPreference {
			get => _selectedNotificationPreference;

			set {
				if (SetProperty (ref _selectedNotificationPreference, value)) {
					var index = Preferences.IndexOf (SelectedPreference);
					var enumValue = _enumValues [index];

					Info.SetOptionAndUpdatePreview (
						new CodeStyleOption<T> (
							enumValue, _selectedNotificationPreference.Notification),
						Option, GetPreview ());
				}
			}
		}
	}
}
