using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace CookingTime
{
	/// <summary>
	/// Summary description for frmAbout.
	/// </summary>
	public class frmAbout : System.Windows.Forms.Form
	{
    private System.Windows.Forms.Button btnClose;
    private System.Windows.Forms.Label lblAboutLine1;
    private System.Windows.Forms.Label lblAboutLine2;
    private System.Windows.Forms.Label lblAboutLine3;
    private System.Windows.Forms.LinkLabel linkAboutResume;
    private System.Windows.Forms.LinkLabel linkAboutEmail;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public frmAbout()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
      lblAboutLine1.Text = Application.ProductName + " " + Application.ProductVersion.ToString();
      lblAboutLine2.Text = Application.CompanyName;
      lblAboutLine3.Text = "2005";

      linkAboutResume.Links.Add(0,linkAboutResume.Text.Length,
        "http://members.shaw.ca/akaplen/");
      linkAboutEmail.Links.Add(0,linkAboutEmail.Text.Length,
      "mailto:akaplen@shaw.ca");
    }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(frmAbout));
			this.btnClose = new System.Windows.Forms.Button();
			this.lblAboutLine1 = new System.Windows.Forms.Label();
			this.lblAboutLine2 = new System.Windows.Forms.Label();
			this.lblAboutLine3 = new System.Windows.Forms.Label();
			this.linkAboutResume = new System.Windows.Forms.LinkLabel();
			this.linkAboutEmail = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// btnClose
			// 
			this.btnClose.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnClose.Location = new System.Drawing.Point(376, 160);
			this.btnClose.Name = "btnClose";
			this.btnClose.TabIndex = 0;
			this.btnClose.Text = "Close";
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// lblAboutLine1
			// 
			this.lblAboutLine1.Location = new System.Drawing.Point(16, 24);
			this.lblAboutLine1.Name = "lblAboutLine1";
			this.lblAboutLine1.Size = new System.Drawing.Size(432, 16);
			this.lblAboutLine1.TabIndex = 1;
			this.lblAboutLine1.Text = "label1";
			// 
			// lblAboutLine2
			// 
			this.lblAboutLine2.Location = new System.Drawing.Point(16, 48);
			this.lblAboutLine2.Name = "lblAboutLine2";
			this.lblAboutLine2.Size = new System.Drawing.Size(432, 16);
			this.lblAboutLine2.TabIndex = 2;
			this.lblAboutLine2.Text = "label1";
			// 
			// lblAboutLine3
			// 
			this.lblAboutLine3.Location = new System.Drawing.Point(16, 72);
			this.lblAboutLine3.Name = "lblAboutLine3";
			this.lblAboutLine3.Size = new System.Drawing.Size(432, 24);
			this.lblAboutLine3.TabIndex = 3;
			this.lblAboutLine3.Text = "label2";
			// 
			// linkAboutResume
			// 
			this.linkAboutResume.Location = new System.Drawing.Point(16, 104);
			this.linkAboutResume.Name = "linkAboutResume";
			this.linkAboutResume.Size = new System.Drawing.Size(424, 23);
			this.linkAboutResume.TabIndex = 4;
			this.linkAboutResume.TabStop = true;
			this.linkAboutResume.Text = "Download my resume.";
			// 
			// linkAboutEmail
			// 
			this.linkAboutEmail.Location = new System.Drawing.Point(16, 136);
			this.linkAboutEmail.Name = "linkAboutEmail";
			this.linkAboutEmail.Size = new System.Drawing.Size(296, 23);
			this.linkAboutEmail.TabIndex = 5;
			this.linkAboutEmail.TabStop = true;
			this.linkAboutEmail.Text = "Email Me";
			this.linkAboutEmail.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkAboutEmail_LinkClicked);
			// 
			// frmAbout
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(472, 206);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.linkAboutEmail,
																		  this.linkAboutResume,
																		  this.lblAboutLine3,
																		  this.lblAboutLine2,
																		  this.lblAboutLine1,
																		  this.btnClose});
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "frmAbout";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About Cooking Time Tool";
			this.ResumeLayout(false);

		}
		#endregion

    private void btnClose_Click(object sender, System.EventArgs e)
    {
      this.Close();
    }

    private void linkAboutEmail_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
    {
       //System.Inter
        //e.Link.LinkData.ToString()
    }
	}
}
