//
// XmlDocumentTransformTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
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
using System.Xml;
using NUnit.Framework;
using Microsoft.Web.XmlTransform;
using System.IO;
using System.Text;

namespace MonoDevelop.PackageManagement.Tests
{
	/// <summary>
	/// Tests that patches applied to Microsoft's XML Document Transformation (XDT)
	/// library for Mono issues are working.
	/// </summary>
	[TestFixture]
	public class XmlDocumentTransformTests
	{
		string RunTransform (string input, string xdt)
		{
			using (var transformation = new XmlTransformation (xdt, isTransformAFile: false, logger: null)) {
				using (var document = new XmlTransformableDocument ()) {
					document.PreserveWhitespace = true;

					document.Load (new StringReader (input));

					bool succeeded = transformation.Apply(document);
					if (succeeded) {
						var writer = new StringWriter ();
						document.Save (writer);
						return writer.ToString ();
					}
					return null;
				}
			}
		}

		/// <summary>
		/// XDT change:
		/// 
		/// https://github.com/mrward/xdt/commit/b2c3b5383d589c3f79650a0cab93f88a8741b057
		/// </summary>
		[Test]
		public void RemoveTransformShouldRemoveExistingXmlElements ()
		{
			string input =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
 <runtime>
  <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
      <assemblyIdentity name=""System.Web.WebPages"" publicKeyToken=""31bf3856ad364e35""/>
      <bindingRedirect oldVersion=""0.0.0.0-0.0.0.0"" newVersion=""0.0.0.0""/>
    </dependentAssembly>
  </assemblyBinding>
 </runtime>
</configuration>
";

				string xdt = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
 <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly xdt:Transform=""Remove""
          xdt:Locator=""Condition(./_defaultNamespace:assemblyIdentity/@name='System.Web.WebPages')"" >
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>";

			string output = RunTransform (input, xdt);

			Assert.IsFalse (output.Contains ("WebPages"), output);

			// Sanity check that the transform is returning something.
			Assert.IsTrue (output.Contains ("assemblyBinding"), output);
		}

		/// <summary>
		/// XDT change:
		/// 
		/// https://github.com/mrward/xdt/commit/9ae2e113dc4140fa7da853436f42547795cfebb5
		/// </summary>
		[Test]
		public void SetAttributesTransformShouldSetAttribute ()
		{
			string input =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
	<connectionStrings>
		<!-- Example connection to a SQL Server Database on localhost. -->
		<add name=""MyDB""
		     connectionString=""""
		     providerName=""System.Data.SqlClient""/>
	</connectionStrings>
	<appSettings>
		<add key=""Setting1"" value=""Very""/>
		<add key=""Setting2"" value=""Easy""/>
 	</appSettings>
</configuration>
";

			string xdt = 
@"<?xml version=""1.0""?>
<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
	<connectionStrings>
		<add name=""MyDB""
			connectionString=""value for the deployed Web.config file""
			xdt:Transform=""SetAttributes"" xdt:Locator=""Match(name)""/>
		<add name=""AWLT"" connectionString=""newstring""
			providerName=""newprovider""
			xdt:Transform=""Insert"" />
	</connectionStrings>
</configuration>";

			string output = RunTransform (input, xdt);

			Assert.IsTrue (output.Contains ("deployed Web.config file"), output);
		}
	}
}

