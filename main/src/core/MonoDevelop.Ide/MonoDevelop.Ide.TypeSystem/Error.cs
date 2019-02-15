//
// Error.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 mkrueger
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.TypeSystem
{
	/// <summary>
	/// Enum that describes the type of an error.
	/// </summary>
	[Obsolete("Use Visual Studio Editor APIs")]
	public enum ErrorType
	{
		Unknown,
		Error,
		Warning
	}

	/// <summary>
	/// Descibes an error during parsing.
	/// </summary>
	[Serializable]
	[Obsolete("Use Visual Studio Editor APIs")]
	public class Error
	{
		readonly ErrorType errorType;
		readonly string message;
		readonly DocumentRegion region;

		/// <summary>
		/// The type of the error.
		/// </summary>
		public ErrorType ErrorType { get { return errorType; } }

		/// <summary>
		/// The error description.
		/// </summary>
		public string Message { get { return message; } }

		/// <summary>
		/// The id of the error.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// The region of the error.
		/// </summary>
		public DocumentRegion Region { get { return region; } }

		/// <summary>
		/// Gets or sets the tag.
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='region'>
		/// The region of the error.
		/// </param>
		public Error (ErrorType errorType, string message, DocumentRegion region)
		{
			this.errorType = errorType;
			this.message = message;
			this.region = region;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='location'>
		/// The location of the error.
		/// </param>
		public Error (ErrorType errorType, string message, DocumentLocation location)
		{
			this.errorType = errorType;
			this.message = message;
			this.region = new DocumentRegion (location, location);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		public Error (ErrorType errorType, string message, int line, int column) : this (errorType, message, new DocumentLocation (line, column)) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		public Error (ErrorType errorType, string message)
		{
			this.errorType = errorType;
			this.message = message;
			this.region = DocumentRegion.Empty;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='region'>
		/// The region of the error.
		/// </param>
		public Error (ErrorType errorType, string id, string message, DocumentRegion region)
		{
			this.errorType = errorType;
			this.Id = id;
			this.message = message;
			this.region = region;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		/// <param name='location'>
		/// The location of the error.
		/// </param>
		public Error (ErrorType errorType, string id, string message, DocumentLocation location)
		{
			this.errorType = errorType;
			this.Id = id;
			this.message = message;
			this.region = new DocumentRegion (location, location);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		public Error (ErrorType errorType, string id, string message, int line, int column) : this (errorType, id, message, new DocumentLocation (line, column)) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
		/// </summary>
		/// <param name='errorType'>
		/// The error type.
		/// </param>
		/// <param name='message'>
		/// The description of the error.
		/// </param>
		public Error (ErrorType errorType, string id, string message)
		{
			this.errorType = errorType;
			this.Id = id;
			this.message = message;
			this.region = DocumentRegion.Empty;
		}
	}
}

