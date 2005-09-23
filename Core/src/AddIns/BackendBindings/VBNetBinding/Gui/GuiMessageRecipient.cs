// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

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
