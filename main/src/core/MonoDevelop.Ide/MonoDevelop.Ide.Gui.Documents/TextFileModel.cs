//
// FileDocumentModel.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui.Documents
{
	public class TextFileModel : FileModel
	{
		public Encoding Encoding {
			get => GetRepresentation<TextFileModelRepresentation> ().Encoding;
			set => GetRepresentation<TextFileModelRepresentation> ().Encoding = value;
		}

		public void SetText (string text)
		{
			var rep = GetRepresentation<TextFileModelRepresentation> ();
			rep.SetText (text);
		}

		public string GetText ()
		{
			var rep = GetRepresentation<TextFileModelRepresentation> ();
			return rep.GetText ();
		}

		public Task SetText (TextReader reader)
		{
			var rep = GetRepresentation<TextFileModelRepresentation> ();
			return rep.SetText (reader);
		}

		internal protected override Type RepresentationType => typeof (InternalTextFileModelRepresentation);

		protected abstract class TextFileModelRepresentation : FileModelRepresentation
		{
			Encoding encoding;
			bool useByteOrderMark;

			public Encoding Encoding {
				get { return encoding; }
				set {
					if (encoding != value) {
						encoding = value;
						NotifyChanged ();
					}
				}
			}

			public bool UseByteOrderMark {
				get {
					return useByteOrderMark;
				}
				set {
					if (value != useByteOrderMark) {
						useByteOrderMark = value;
						NotifyChanged ();
					}
				}
			}

			public void SetText (string text)
			{
				OnSetText (text);
				NotifyChanged ();
			}

			public async Task SetText (TextReader reader)
			{
				await OnSetText (reader);
				NotifyChanged ();
			}

			public string GetText ()
			{
				return OnGetText ();
			}

			protected abstract void OnSetText (string text);

			protected abstract string OnGetText ();

			protected virtual Task OnSetText (TextReader reader)
			{
				OnSetText (reader.ReadToEnd ());
				return Task.CompletedTask;
			}

			protected override Stream OnGetContent ()
			{
				var memStream = new MemoryStream ();
				TextFileUtility.WriteText (memStream, GetText (), Encoding, UseByteOrderMark);
				memStream.Position = 0;
				return memStream;
			}

			protected override async Task OnSetContent (Stream content)
			{
				var file = await TextFileUtility.GetTextAsync (content);
				OnSetText (file.Text);
				Encoding = file.Encoding;
				UseByteOrderMark = file.HasByteOrderMark;
			}

			protected override async Task OnCopyFrom (ModelRepresentation other)
			{
				if (other is TextFileModelRepresentation textFile) {
					OnSetText (textFile.GetText ());
					Encoding = textFile.Encoding;
					UseByteOrderMark = textFile.UseByteOrderMark;
					NotifyChanged ();
				} else
					await base.OnCopyFrom (other);
			}

			protected override void OnCreateNew ()
			{
				Encoding = Encoding.UTF8;
			}
		}

		class InternalTextFileModelRepresentation : TextFileModelRepresentation
		{
			string text;

			protected override string OnGetText ()
			{
				return text;
			}

			protected override void OnSetText (string text)
			{
				if (this.text != text) {
					this.text = text;
					HasUnsavedChanges = true;
					NotifyChanged ();
				}
			}

			protected override async Task OnLoad ()
			{
				bool hadContent = text != null;
				var file = await TextFileUtility.ReadAllTextAsync (FilePath);
				text = file.Text;
				Encoding = file.Encoding;
				UseByteOrderMark = file.HasByteOrderMark;
				if (hadContent)
					NotifyChanged ();
			}

			protected override void OnCreateNew ()
			{
				text = "";
				base.OnCreateNew ();
			}

			protected override async Task OnSave ()
			{
				await TextFileUtility.WriteTextAsync (FilePath, text, Encoding, UseByteOrderMark);
			}
		}
	}
}
