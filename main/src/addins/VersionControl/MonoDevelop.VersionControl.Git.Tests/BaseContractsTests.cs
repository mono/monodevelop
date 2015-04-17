//
// BaseContractsTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public abstract class BaseAPIContractsTests
	{
		/// <summary>
		/// Gets the path to the assemblies to load and test against.
		/// </summary>
		/// <value>The assembly path.</value>
		protected abstract string[] AssemblyPaths { get; }

		/// <summary>
		/// Items to ignore in additional to the constraints imposed.
		/// </summary>
		/// <value>The allowed types.</value>
		protected abstract string[] AllowedUITypes { get; }

		static bool MethodIsOverrideableAndNotOverridden (MethodInfo m)
		{
			return (m.IsVirtual || m.IsAbstract) && !m.IsFinal && m.GetBaseDefinition() != m;
		}

		protected bool HasAbstractOrVirtualMethods (Type t)
		{
			return t.IsAbstract || t.GetMethods ().Any (MethodIsOverrideableAndNotOverridden) ||
				t.GetProperties ().Any (p => MethodIsOverrideableAndNotOverridden (p.GetMethod) || MethodIsOverrideableAndNotOverridden (p.SetMethod));
		}

		protected bool IsToolboxItem (Type t)
		{
			return t.CustomAttributes.All (attr => attr.AttributeType != typeof(System.ComponentModel.ToolboxItemAttribute));
		}

		IEnumerable<string> NoVisibleUI (Assembly asm)
		{
			var gtkType = typeof(Gtk.Widget);
			// TODO: Check Mac types too.

			var uiTypes =
				asm.ExportedTypes
				.Where (gtkType.IsAssignableFrom)
				.Where (IsToolboxItem)
				.Where (t => !HasAbstractOrVirtualMethods(t))
				.Where (t => !AllowedUITypes.Contains (t.FullName));
			
			return uiTypes.Select (t => t.FullName);
		}

		[Test]
		public void TestNoVisibleUI ()
		{
			var uiTypes = Enumerable.Empty<string> ();
			foreach (var path in AssemblyPaths) {
				uiTypes = uiTypes.Concat (NoVisibleUI (Assembly.LoadFrom (path)));
			}
			Assert.True (!uiTypes.Any (), "The following UI types should not be public:\n" + String.Join ("\n", uiTypes));
		}
	}

	[TestFixture]
	public class VersionControlContractTests : BaseAPIContractsTests
	{
		string path = Path.Combine ("..", "AddIns", "VersionControl", "MonoDevelop.VersionControl.dll");
		protected override string[] AssemblyPaths {
			get { return new[] { path }; }
		}

		readonly string[] allowedUITypes = {};
		protected override string[] AllowedUITypes {
			get { return allowedUITypes; }
		}
	}
}

