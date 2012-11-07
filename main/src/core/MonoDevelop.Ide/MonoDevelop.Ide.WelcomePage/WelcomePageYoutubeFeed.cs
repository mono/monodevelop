//
// WelcomePageYoutubeFeed.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using Gtk;
using System;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.WelcomePage
{
	class WelcomePageYoutubeFeed : WelcomePageNewsFeed
	{
		const string RegexString = "[^\\s]* for this session: (Coming Soon|[^\\s]*) ";

		public WelcomePageYoutubeFeed (string title, string newsUrl, string id, int limit = 5): base (title, newsUrl, id, limit)
		{
		}

		protected override IEnumerable<Widget> OnLoadNews (XElement news)
		{
			var channel = news.Element ("channel");
			foreach (var child in channel.Elements ("item")) {
				var name = child.Element ("title").Value;
				var link = child.Element ("link").Value;
				var date = child.Element ("pubDate").Value;

				var descHtml = child.Element ("description").Value;

				int hackIndex = descHtml.IndexOf ("<span>");
				descHtml = descHtml.Substring (hackIndex + "<span>".Length);
				hackIndex = descHtml.IndexOf ("</span>");
				descHtml = descHtml.Substring (0, hackIndex);
				descHtml = RemoveDescriptionCruft (descHtml);

				yield return new WelcomePageFeedItem (name, link, descHtml, date);
			}
		}

		string RemoveDescriptionCruft (string description)
		{
			Regex regex = new Regex (RegexString);
			string result = regex.Replace (description, "");

			if (result == description)
				return result;
			return RemoveDescriptionCruft (result);
		}
	}
}

