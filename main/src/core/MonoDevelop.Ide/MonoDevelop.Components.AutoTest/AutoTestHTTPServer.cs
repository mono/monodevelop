//
// AutoTestHTTPServer.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Net;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestHTTPServer
	{
		HttpListener listener;
		bool running;

		public AutoTestHTTPServer ()
		{
			listener = new HttpListener ();
			listener.Prefixes.Add ("http://localhost:12698/AutoTest/");
		}

		private class UTF8StringWriter : StringWriter
		{
			public override Encoding Encoding {
				get {
					return Encoding.UTF8;
				}
			}
		}

		public async void Start ()
		{
			listener.Start ();

			running = true;
			while (running) {
				HttpListenerContext context = await listener.GetContextAsync ();

				AppQuery q = new AppQuery ();
				XmlDocument doc = q.ExecuteAndGenerateXml ();

				string msg;
				using (var sw = new UTF8StringWriter ()) {
					using (var xw = XmlWriter.Create (sw, new XmlWriterSettings {Indent = true})) {
						doc.WriteTo (xw);
					}

					msg = sw.ToString ();
				}
					
				context.Response.ContentLength64 = Encoding.UTF8.GetByteCount (msg);
				context.Response.StatusCode = (int)HttpStatusCode.OK;

				using (Stream s = context.Response.OutputStream) {
					using (StreamWriter sw = new StreamWriter (s)) {
						await sw.WriteAsync (msg);
					}
				}
			}
		}

		public void Stop ()
		{
			running = false;
			listener.Stop ();
		}
	}
}

