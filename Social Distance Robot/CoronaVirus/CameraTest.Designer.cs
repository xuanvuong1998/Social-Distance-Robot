namespace robot_head
{
    partial class CameraTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbxCameraDevices = new System.Windows.Forms.ComboBox();
            this.cbxResolutions = new System.Windows.Forms.ComboBox();
            this.picCapture = new System.Windows.Forms.PictureBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picCapture)).BeginInit();
            this.SuspendLayout();
            // 
            // cbxCameraDevices
            // 
            this.cbxCameraDevices.Font = new System.Drawing.Font("Microsoft Sans Serif", 17.875F);
            this.cbxCameraDevices.FormattingEnabled = true;
            this.cbxCameraDevices.Location = new System.Drawing.Point(23, 32);
            this.cbxCameraDevices.Name = "cbxCameraDevices";
            this.cbxCameraDevices.Size = new System.Drawing.Size(564, 63);
            this.cbxCameraDevices.TabIndex = 0;
            // 
            // cbxResolutions
            // 
            this.cbxResolutions.Font = new System.Drawing.Font("Microsoft Sans Serif", 17.875F);
            this.cbxResolutions.FormattingEnabled = true;
            this.cbxResolutions.Location = new System.Drawing.Point(681, 32);
            this.cbxResolutions.Name = "cbxResolutions";
            this.cbxResolutions.Size = new System.Drawing.Size(564, 63);
            this.cbxResolutions.TabIndex = 1;
            // 
            // picCapture
            // 
            this.picCapture.Location = new System.Drawing.Point(44, 304);
            this.picCapture.Name = "picCapture";
            this.picCapture.Size = new System.Drawing.Size(1435, 925);
            this.picCapture.TabIndex = 2;
            this.picCapture.TabStop = false;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(31, 149);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(340, 109);
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(525, 149);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(340, 109);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // CameraTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1659, 1272);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.picCapture);
            this.Controls.Add(this.cbxResolutions);
            this.Controls.Add(this.cbxCameraDevices);
            this.Name = "CameraTest";
            this.Text = "CameraTest";
            ((System.ComponentModel.ISupportInitialize)(this.picCapture)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbxCameraDevices;
        private System.Windows.Forms.ComboBox cbxResolutions;
        private System.Windows.Forms.PictureBox picCapture;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnSave;
    }
}