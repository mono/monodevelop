// 
// EncodingComboBox.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using CustomControls.Controls;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Platform
{
	public class EncodingBox : ComboBox
	{
		TextEncoding [] encodings;
		bool showAutoDetected;
		
		public EncodingBox (bool showAutoDetected)
		{
			this.showAutoDetected = showAutoDetected;
			
			DropDownStyle = ComboBoxStyle.DropDownList;
			
			Populate (false);
			SelectedEncodingId = null;
		}
		
		public string SelectedEncodingId {
			get {
				int selectedIndex = SelectedIndex;
				if (selectedIndex < 0 || (showAutoDetected && selectedIndex == 0))
					return null;
				
				return encodings [showAutoDetected ? selectedIndex - 1 : selectedIndex].Id;
			}
			set {
				if (!String.IsNullOrEmpty (value))
					for (int i = 0; i < encodings.Length; i++)
						if (encodings [i].Id == value) {
							SelectedIndex = showAutoDetected ? i + 1 : i;
							return;
						}
				
				SelectedIndex = 0;
			}
		}
		
		void Populate (bool clear)
		{	
			encodings = TextEncoding.ConversionEncodings;
			if (encodings == null || encodings.Length == 0)
				encodings = new TextEncoding [] { TextEncoding.GetEncoding (TextEncoding.DefaultEncoding) };
			
			BeginUpdate ();
					
			if (clear)
				Items.Clear ();
			
			if (showAutoDetected)
				Items.Add (GettextCatalog.GetString ("Auto Detected"));
			
			foreach (var encoding in TextEncoding.ConversionEncodings)
				Items.Add (String.Format ("{0} ({1})", encoding.Name, encoding.Id));
			
			Items.Add (GettextCatalog.GetString ("Add or Remove..."));
			
			EndUpdate ();
		}
		
		protected override void OnSelectedIndexChanged (EventArgs e)
		{
			base.OnSelectedIndexChanged (e);
			
			// 'Add or Remove...' option
			if (Items.Count > 0 && SelectedIndex == Items.Count -1) {
				using (var encodingsForm = new EncodingSelectionForm ()) {
					if (encodingsForm.ShowDialog (Parent) == DialogResult.OK) {
						TextEncoding.ConversionEncodings = encodingsForm.SelectedEncodings;
						Populate (true);
					}
				}
				
				SelectedIndex = 0;
			}
		}
	}
}

