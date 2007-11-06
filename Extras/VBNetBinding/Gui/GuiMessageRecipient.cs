//  GuiMessageRecipient.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Windows.Forms;

namespace VBBinding
{
	[Serializable()]
	class GuiMessageRecipient : IMessageRecipient
	{
		class StatusForm : Form
		{
			Label statusLabel;
			
			public StatusForm()
			{
				this.Text = "VB.DOC status";
				this.ControlBox = false;
				this.StartPosition = FormStartPosition.CenterScreen;
				this.ShowInTaskbar = false;
				
				this.Size = new System.Drawing.Size(400, 50);
				
				statusLabel = new Label();
				statusLabel.Dock = DockStyle.Fill;
				statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
				Controls.Add(statusLabel);
			}
			
			public void Status(string statusMessage)
			{
				statusLabel.Text = statusMessage;
				Application.DoEvents();
			}
		}
		
		StatusForm messageForm;
		
		public GuiMessageRecipient()
		{
			messageForm = new StatusForm();
			messageForm.Show();
		}
		
		public void Finished()
		{
			messageForm.Close();
		}
		
		public void DisplayStatusMessage(string message)
		{
			messageForm.Status(message);
		}
		
		public void DisplayErrorMessage(string message)
		{
			// message doesn't work in this app domain
			System.Windows.Forms.MessageBox.Show(message);
		}
	}
}
