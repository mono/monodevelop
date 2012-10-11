// 
// FeedbackService.cs
//  
// Author:
//       lluis <${AuthorEmail}>
// 
// Copyright (c) 2011 lluis
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using System.Net;
using System.IO;
using System.Xml;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Extensions;
using System.Reflection;
using System.Linq;
using Mono.Addins;

namespace MonoDevelop.Ide
{
	static class FeedbackService
	{
		static bool sending;
		static object sendingLock = new object ();
		static FeedbackDialog currentFeedbackDialog;
		static Lazy<string> feedbackUrl;

		internal static bool Enabled {
			get { return !string.IsNullOrEmpty (feedbackUrl.Value); }
		}

		static FeedbackService ()
		{
			feedbackUrl = new Lazy<string> (() => {
				var node = AddinManager.GetExtensionNodes<ServiceUrlExtensionNode> ("/MonoDevelop/Ide/FeedbackService")
					.FirstOrDefault ();
				return node != null? node.Url : null;
			});
		}
		
		public static bool IsFeedbackWindowVisible {
			get { return currentFeedbackDialog != null && currentFeedbackDialog.Visible; }
		}
		
		public static void ShowFeedbackWindow ()
		{
			if (string.IsNullOrEmpty (feedbackUrl.Value))
				return;

			if (currentFeedbackDialog == null) {
				var p = FeedbackPositionGetter ();
				currentFeedbackDialog = new FeedbackDialog (p.X, p.Y);
				currentFeedbackDialog.Show ();
				currentFeedbackDialog.Destroyed += delegate {
					currentFeedbackDialog = null;
				};
			} else
				currentFeedbackDialog.Show ();
		}
		
		internal static Func<Gdk.Point> FeedbackPositionGetter { get; set; }
		
		public static int FeedbacksSent {
			get { return PropertyService.Get<int> ("MonoDevelop.Feedback.Count", 0); }
		}
		
		public static string ReporterEMail {
			get { return PropertyService.Get<string> ("MonoDevelop.Feedback.Email"); }
		}
		
		static FilePath FeedbackFile {
			get { return UserProfile.Current.LocalConfigDir.Combine ("Feedback.xml"); }
		}
		
		public static void SendFeedback (string email, string body)
		{
			PropertyService.Set ("MonoDevelop.Feedback.Count", FeedbacksSent + 1);
			PropertyService.Set ("MonoDevelop.Feedback.Email", email);
			PropertyService.SaveProperties ();
			
			string header = "MonoDevelop: " + BuildVariables.PackageVersionLabel + "\n";
			
			Type t = Type.GetType ("Mono.Runtime");
			if (t != null) {
				try {
					string ver = (string) t.InvokeMember ("GetDisplayName", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic, null, null, null);
					header += "Runtime: Mono " + ver + "\n";
				} catch {
					header += "Runtime: Mono (detection failed)\n";
				}
			} else {
				header += "Runtime: Microsoft .NET v" + Environment.Version + "\n";
			}
			
			string os = Platform.IsMac ? "Mac OSX" : (Platform.IsWindows ? "Windows" : "Linux");
			header += "Operating System: " + os + " (" + Environment.OSVersion + ")\n";
			header += "Distributor: " + PropertyService.Get <string> ("MonoDevelop.Distributor", "Xamarin") + "\n";
			header += SystemInformation.GetTextDescription ();
			body = header + "\n" + body;
			
			lock (sendingLock) {
				// Append the feedback entry to the end of the file
				XmlDocument doc = LoadFeedbackDoc ();
				if (doc.DocumentElement == null)
					doc.AppendChild (doc.CreateElement ("Feedbacks"));
				
				var f = doc.CreateElement ("Feedback");
				f.SetAttribute ("email", email);
				f.InnerText = body;
				doc.DocumentElement.AppendChild (f);
				try {
					if (!Directory.Exists (FeedbackFile.ParentDirectory))
						Directory.CreateDirectory (FeedbackFile.ParentDirectory);
					doc.Save (FeedbackFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Could not save feedback file", ex);
				}
			}
			SendPendingFeedback ();
		}
		
		public static void SendPendingFeedback ()
		{
			if (string.IsNullOrEmpty (feedbackUrl.Value))
				return;

			lock (sendingLock) {
				if (sending)
					return;
				
				XmlDocument doc = LoadFeedbackDoc ();
				if (doc.DocumentElement == null)
					return;
				XmlElement fe = doc.DocumentElement.FirstChild as XmlElement;
				if (fe != null) {
					sending = true;
					InternalSendFeedback (fe);
				}
			}
		}
		
		static XmlDocument LoadFeedbackDoc ()
		{
			XmlDocument doc = new XmlDocument ();
			try {
				if (File.Exists (FeedbackFile))
					doc.Load (FeedbackFile);
			} catch (Exception ex) {
				LoggingService.LogError ("Could not load feedback file", ex);
			}
			return doc;
		}
		
		static void InternalSendFeedback (XmlElement feedbackElem)
		{
			string email = feedbackElem.GetAttribute ("email");
			string body = feedbackElem.InnerText;
			var request = (HttpWebRequest) HttpWebRequest.Create (feedbackUrl.Value + "?m=" + email);
			request.Method = "POST";
			request.BeginGetRequestStream (delegate (IAsyncResult res) {
				HandleGetRequestStream (res, request, body);
			}, null);
		}
		
		static void HandleGetRequestStream (IAsyncResult res, HttpWebRequest request, string body)
		{
			try {
				Stream s = request.EndGetRequestStream (res);
				using (var sw = new StreamWriter (s)) {
					sw.Write (body);
				}
				WebResponse resp = request.GetResponse ();
				s = resp.GetResponseStream ();
				string result;
				using (var sr = new StreamReader (s))
					result = sr.ReadToEnd ();
				if (result != "OK")
					throw new Exception (result);
			}
			catch (Exception ex) {
				LoggingService.LogError ("Feedback submission failed", ex);
				lock (sendingLock) {
					sending = false;
				}
				return;
			}
			LoggingService.LogInfo ("Feedback successfully sent");
			lock (sendingLock) {
				XmlDocument doc = LoadFeedbackDoc ();
				if (doc.DocumentElement != null) {
					var first = doc.DocumentElement.FirstChild;
					doc.DocumentElement.RemoveChild (first);
					doc.Save (FeedbackFile);
					
					XmlElement fe = doc.DocumentElement.FirstChild as XmlElement;
					if (fe != null)
						InternalSendFeedback (fe);
					else
						sending = false;
				}
				else
					sending = false;
			}
		}
	}
	
	[StartupHandlerExtension]
	class FeedbackSender: CommandHandler
	{
		protected override void Run ()
		{
			FeedbackService.SendPendingFeedback ();
		}
	}
}

