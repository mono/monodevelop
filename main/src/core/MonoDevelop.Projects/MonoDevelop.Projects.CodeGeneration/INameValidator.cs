// 
// INameValidator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Projects.CodeGeneration
{
	public interface INameValidator
	{
		ValidationResult ValidateName (IDomVisitable visitable, string name);
	}
	
	public class ValidationResult 
	{
		public static ValidationResult Valid = new ValidationResult (true, false, MonoDevelop.Core.GettextCatalog.GetString ("Name is valid"));
		
		public static ValidationResult CreateError (string error)
		{
			return new ValidationResult (false, true, error);
		}
		
		public static ValidationResult CreateWarning (string warning)
		{
			return new ValidationResult (true, true, warning);
		}
		
		public bool IsValid {
			get;
			set;
		}
		
		public bool HasWarning {
			get;
			set;
		}
		
		public string Message {
			get;
			set;
		}
		
		protected ValidationResult (bool isValid, bool hasWarning, string message)
		{
			this.IsValid    = isValid;
			this.HasWarning = hasWarning;
			this.Message    = message;
		}
	}
	
}
