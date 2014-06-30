//
// CodeSamples.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
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

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public static class CodeSamples
	{
		public static readonly string CSharp = @"#region Studio Style
class Program : IThemeable
{
    static int _I = 1;
    delegate void DoSomething();

    /// <summary>
    /// The quick brown fox jumps over the lazy dog
    /// THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG
    /// </summary>
    static void Main(string[] args)
    {
        string normalStr = ""The time now is approximately "" + DateTime.Now;
        Uri Illegal1Uri = new Uri(""http://packmyboxwith/jugs.html?q=five-dozen&t=liquor"");
        Regex OperatorRegex = new Regex(@""\S#$"", RegexOptions.IgnorePatternWhitespace);

        for (int O = 0; O < 123456789; O++)
        {
            _I += (O % 3) * ((O / 1) ^ 2) - 5;
            if (!OperatorRegex.IsMatch(Illegal1Uri.ToString()))
            {
                // no idea what this does!?
                Console.WriteLine(Illegal1Uri + normalStr);
            }
        }
    }
}
#endregion";
		public static readonly string Web = @"<%@ Page Language=""C#"" Inherits=""System.Web.Mvc.ViewPage<List<Person>>"" %>
<!DOCTYPE html>
<html>
<head>
    <title>Studio Style ASP.NET</title>
</head>
<body>
    <h1>A test page</h1>
    <!-- list of people -->
    <ul class=""people"">
    <% foreach(Person person in Model) { %>
        <li>
            <%: person.Name %> &gt; (<%: Html.ActionLink(""edit"", ""Edit"", new { id = 1 }) %>)
        </li>
    <% } %>
    </ul>
</body>
</html>";
		public static readonly string CSS = @".people {
        font-family: 'Arial Narrow';
        font-size: 100% !important; /* comment */
    }";
		public static readonly string Javascript = @"        // TODO: use jQuery instead
        window.onload = function() {
            for(var i = 0; i < 23; i++) {
                alert(""Hello"");
            }
        }";
		//		public static readonly string Razor=@"@model List<Person>
		//
		//@{
		//    ViewBag.Title = ""Razor"";
		//    Layout = ""~/Views/Shared/_Layout.cshtml"";
		//}
		//
		//<h1>Razor</h1>
		//<ul class=""people"">
		//@foreach(Person person in Model) {
		//    <li>
		//        @person.Name &gt; (@Html.ActionLink(""edit"", ""Edit"", new { id = 1 }))
		//    </li>
		//}
		//</ul>";
		public static readonly string XML = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<!-- this is an example XML file -->
<people xmlns:x=""http://studiostyles.info"">
  <person name=""Jim Jones"" ID=""27"">
    <email html=""yes"">jim@example.invalid</email>
    <address>
      <post>123 Example St, &#160;South Brisbane</post>
      <city>Brisbane</city>
    </address>
    <x:comments>
    <![CDATA[ See? Data. Don't worry about this <tag>. ]]>
    </x:comments>
  </person>
</people>";
	}
}

