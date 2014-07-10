//
// LocationPreviewVisualizer.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Components;
using Mono.Debugging.Client;
using MonoDevelop.Debugger.Converters;
using System.Net;
using System.Globalization;
using System.IO;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Gtk;

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	public class LocationPreviewVisualizer : PreviewVisualizer
	{
		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			return DebuggingService.HasGetConverter<GpsLocation> (val);
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			var location = DebuggingService.GetGetConverter<GpsLocation> (val).GetValue (val);
			var mainBox = new Gtk.VBox ();
			mainBox.PackStart (new Label (GettextCatalog.GetString ("Loading...")));
			using (var stream = new MemoryStream ()) {
				var cancelSource = new CancellationTokenSource ();
				var timer = new Timer (delegate {
					cancelSource.Cancel ();
				}, null, 4000, System.Threading.Timeout.Infinite);
				WebRequestHelper.GetResponseAsync (
					() => (HttpWebRequest)WebRequest.Create ("http://maps.googleapis.com/maps/api/staticmap?&size=500x300&zoom=15&markers=color:blue%7C" +
					location.Longitude.ToString (CultureInfo.InvariantCulture.NumberFormat) + "," +
					location.Latitude.ToString (CultureInfo.InvariantCulture.NumberFormat)),
					null, 
					cancelSource.Token).ContinueWith ((System.Threading.Tasks.Task<HttpWebResponse> arg) => {
					Application.Invoke (delegate {
						mainBox.Remove (mainBox.Children [0]);
						if (arg.Exception == null) {
							var imageView = new ImageView (Xwt.Drawing.Image.FromStream (arg.Result.GetResponseStream ()));
							imageView.Show ();
							mainBox.PackStart (imageView);
							PreviewWindowManager.RepositionWindow ();
						} else {
							mainBox.PackStart (new GenericPreviewVisualizer ().GetVisualizerWidget (val));
							PreviewWindowManager.RepositionWindow ();
							DebuggingService.DebuggerSession.LogWriter (true, "Failed to load map preview: " + arg.Exception.InnerException);
							arg.Exception.Handle ((e) => true);
						}
					});
				});
			}
			mainBox.ShowAll ();
			return mainBox;
		}

		#endregion
	}
}

