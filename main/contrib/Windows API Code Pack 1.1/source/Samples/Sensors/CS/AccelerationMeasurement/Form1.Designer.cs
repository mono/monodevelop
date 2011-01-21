namespace AccelerationMeasurement
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if( disposing && ( components != null ) )
            {
                components.Dispose( );
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent( )
        {
            this.accelX = new AccelerationMeasurement.AccelerationBar( );
            this.accelY = new AccelerationMeasurement.AccelerationBar( );
            this.accelZ = new AccelerationMeasurement.AccelerationBar( );
            this.label1 = new System.Windows.Forms.Label( );
            this.label2 = new System.Windows.Forms.Label( );
            this.label3 = new System.Windows.Forms.Label( );
            this.availabilityLabel = new System.Windows.Forms.Label( );
            this.SuspendLayout( );
            // 
            // accelX
            // 
            this.accelX.Acceleration = 0F;
            this.accelX.BackColor = System.Drawing.Color.White;
            this.accelX.Location = new System.Drawing.Point( 50, 28 );
            this.accelX.Name = "accelX";
            this.accelX.Size = new System.Drawing.Size( 213, 28 );
            this.accelX.TabIndex = 0;
            this.accelX.Text = "accelerationBar1";
            // 
            // accelY
            // 
            this.accelY.Acceleration = 0F;
            this.accelY.BackColor = System.Drawing.Color.White;
            this.accelY.Location = new System.Drawing.Point( 50, 62 );
            this.accelY.Name = "accelY";
            this.accelY.Size = new System.Drawing.Size( 213, 28 );
            this.accelY.TabIndex = 1;
            this.accelY.Text = "accelerationBar1";
            // 
            // accelZ
            // 
            this.accelZ.Acceleration = 0F;
            this.accelZ.BackColor = System.Drawing.Color.White;
            this.accelZ.Location = new System.Drawing.Point( 50, 96 );
            this.accelZ.Name = "accelZ";
            this.accelZ.Size = new System.Drawing.Size( 213, 28 );
            this.accelZ.TabIndex = 2;
            this.accelZ.Text = "accelerationBar1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point( 12, 37 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 14, 13 );
            this.label1.TabIndex = 6;
            this.label1.Text = "X";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point( 12, 71 );
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size( 14, 13 );
            this.label2.TabIndex = 7;
            this.label2.Text = "Y";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point( 12, 105 );
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size( 14, 13 );
            this.label3.TabIndex = 8;
            this.label3.Text = "Z";
            // 
            // availabilityLabel
            // 
            this.availabilityLabel.AutoSize = true;
            this.availabilityLabel.Location = new System.Drawing.Point( 47, 9 );
            this.availabilityLabel.Name = "availabilityLabel";
            this.availabilityLabel.Size = new System.Drawing.Size( 143, 13 );
            this.availabilityLabel.TabIndex = 9;
            this.availabilityLabel.Text = "Accelerometers available = 0";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 289, 139 );
            this.Controls.Add( this.availabilityLabel );
            this.Controls.Add( this.label3 );
            this.Controls.Add( this.label2 );
            this.Controls.Add( this.label1 );
            this.Controls.Add( this.accelZ );
            this.Controls.Add( this.accelY );
            this.Controls.Add( this.accelX );
            this.Name = "Form1";
            this.Text = "Acceleration Measurement";
            this.Shown += new System.EventHandler( this.Form1_Shown );
            this.ResumeLayout( false );
            this.PerformLayout( );

        }

        #endregion

        private AccelerationBar accelX;
        private AccelerationBar accelY;
        private AccelerationBar accelZ;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label availabilityLabel;


    }
}

