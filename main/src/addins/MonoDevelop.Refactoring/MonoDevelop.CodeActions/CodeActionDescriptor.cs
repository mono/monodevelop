//
// CodeActionDescriptor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.CodeRefactorings;
using MonoDevelop.Core;

namespace MonoDevelop.CodeActions
{
	/// <summary>
	/// This class wraps a roslyn <see cref="ICodeRefactoringProvider"/> and adds required meta data to it.
	/// </summary>
	class CodeActionDescriptor
	{
		readonly Type codeActionType;
		ICodeRefactoringProvider instance;

		/// <summary>
		/// Gets the identifier string.
		/// </summary>
		internal string IdString {
			get {
				return codeActionType.FullName;
			}
		}

		/// <summary>
		/// Gets the display name for this action.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the language for this action.
		/// </summary>
		public string Language { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this code action is enabled by the user.
		/// </summary>
		/// <value><c>true</c> if this code action is enabled; otherwise, <c>false</c>.</value>
		public bool IsEnabled {
			get {
				return PropertyService.Get ("ContextActions." + Language + "." + IdString, true);
			}
			set {
				PropertyService.Set ("ContextActions." + Language + "." + IdString, value);
			}
		}

		internal CodeActionDescriptor (string name, string language, Type codeActionType)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException ("name");
			if (string.IsNullOrEmpty (language))
				throw new ArgumentNullException ("language");
			if (codeActionType == null)
				throw new ArgumentNullException ("codeActionType");
			Name = name;
			Language = language;
			this.codeActionType = codeActionType;
		}

		/// <summary>
		/// Gets the roslyn code action provider.
		/// </summary>
		public ICodeRefactoringProvider GetProvider ()
		{
			if (instance == null)
				instance = (ICodeRefactoringProvider)Activator.CreateInstance (codeActionType);
			return instance;
		}
	}
}
