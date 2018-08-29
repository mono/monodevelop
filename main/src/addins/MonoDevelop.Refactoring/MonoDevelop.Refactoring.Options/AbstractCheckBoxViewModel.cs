//
// AbstractCheckBoxViewModel.cs
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

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace MonoDevelop.Refactoring.Options
{
	internal abstract class AbstractCheckBoxViewModel : AbstractNotifyPropertyChanged
	{
		private readonly string _truePreview;
		private readonly string _falsePreview;
		protected bool _isChecked;

		protected AbstractOptionPreviewViewModel Info { get; }
		public IOption Option { get; }
		public string Description { get; set; }

		internal virtual string GetPreview () => _isChecked ? _truePreview : _falsePreview;

		public AbstractCheckBoxViewModel (IOption option, string description, string preview, AbstractOptionPreviewViewModel info)
			: this (option, description, preview, preview, info)
		{
		}

		public AbstractCheckBoxViewModel (IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info)
		{
			_truePreview = truePreview;
			_falsePreview = falsePreview;

			Info = info;
			Option = option;
			Description = description;
		}

		public abstract bool IsChecked { get; set; }
	}
}
