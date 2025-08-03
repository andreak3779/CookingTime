using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using CT = CookingTime.ComCookingTime;

namespace CookingTime
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class frmCookingTime : System.Windows.Forms.Form
	{
    #region FormMembers

    private System.Windows.Forms.Label lblCookingTime;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.ListBox listChoice;
    private System.Windows.Forms.ToolBar tBarCookingTime;
    private System.Windows.Forms.ToolBarButton btnCalculate;
    private System.Windows.Forms.ToolBarButton btnReset;
    private System.Windows.Forms.ToolBarButton toolBarButton1;
    private System.Windows.Forms.ToolBarButton toolBarButton2;
    private System.Windows.Forms.ToolBarButton btnExit;
    private System.Windows.Forms.Label lblCookingType;
    private System.Windows.Forms.Label lblCookingInstr;
    private System.ComponentModel.IContainer components;
  #endregion
        private CT.MealCreator m_MealCreator;
    private ArrayList m_Meals;
    private System.Windows.Forms.ToolTip toolTipCookingTime;
    private System.Windows.Forms.ErrorProvider errCookingTime;
    private System.Windows.Forms.MainMenu mainMenu1;
    private System.Windows.Forms.MenuItem mnuFile;
    private System.Windows.Forms.MenuItem mnuCalculate;
    private System.Windows.Forms.MenuItem mnuHelp;
    private System.Windows.Forms.MenuItem mnuHelpHelp;
    private System.Windows.Forms.MenuItem mnuAbout;
    private System.Windows.Forms.MenuItem menuItem6;
    private System.Windows.Forms.MenuItem mnuCookingTime;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.MenuItem mnuFileExit;
    private System.Windows.Forms.StatusBar statusBarCookingTime;
    private CT.AbstractMeal m_Meal;
	private System.Windows.Forms.GroupBox groupBox1;
	private System.Windows.Forms.NumericUpDown numWeight;
	private System.Windows.Forms.GroupBox grpUnits;
	private System.Windows.Forms.RadioButton radioKgs;
	private System.Windows.Forms.RadioButton radioPounds;
	private System.Windows.Forms.Label label1;
	private System.Windows.Forms.HelpProvider hlpCookingTime;
	private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.Timer timer1;
    private bool m_isClosing=false;

		public frmCookingTime()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            m_MealCreator = new CT.MealCreator();
			

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(frmCookingTime));
			this.lblCookingTime = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.listChoice = new System.Windows.Forms.ListBox();
			this.statusBarCookingTime = new System.Windows.Forms.StatusBar();
			this.toolTipCookingTime = new System.Windows.Forms.ToolTip(this.components);
			this.tBarCookingTime = new System.Windows.Forms.ToolBar();
			this.btnCalculate = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
			this.btnReset = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton2 = new System.Windows.Forms.ToolBarButton();
			this.btnExit = new System.Windows.Forms.ToolBarButton();
			this.lblCookingType = new System.Windows.Forms.Label();
			this.lblCookingInstr = new System.Windows.Forms.Label();
			this.errCookingTime = new System.Windows.Forms.ErrorProvider();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.mnuFile = new System.Windows.Forms.MenuItem();
			this.mnuFileExit = new System.Windows.Forms.MenuItem();
			this.mnuCalculate = new System.Windows.Forms.MenuItem();
			this.mnuCookingTime = new System.Windows.Forms.MenuItem();
			this.mnuHelp = new System.Windows.Forms.MenuItem();
			this.mnuHelpHelp = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.mnuAbout = new System.Windows.Forms.MenuItem();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.numWeight = new System.Windows.Forms.NumericUpDown();
			this.grpUnits = new System.Windows.Forms.GroupBox();
			this.radioKgs = new System.Windows.Forms.RadioButton();
			this.radioPounds = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.hlpCookingTime = new System.Windows.Forms.HelpProvider();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numWeight)).BeginInit();
			this.grpUnits.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblCookingTime
			// 
			this.lblCookingTime.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblCookingTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.lblCookingTime.Location = new System.Drawing.Point(128, 184);
			this.lblCookingTime.Name = "lblCookingTime";
			this.lblCookingTime.Size = new System.Drawing.Size(464, 24);
			this.lblCookingTime.TabIndex = 3;
			this.lblCookingTime.Text = "CookingTime";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 184);
			this.label3.Name = "label3";
			this.label3.TabIndex = 4;
			this.label3.Text = "Cooking Time:";
			// 
			// listChoice
			// 
			this.listChoice.CausesValidation = false;
			this.listChoice.Location = new System.Drawing.Point(8, 64);
			this.listChoice.Name = "listChoice";
			this.listChoice.ScrollAlwaysVisible = true;
			this.listChoice.Size = new System.Drawing.Size(272, 108);
			this.listChoice.Sorted = true;
			this.listChoice.TabIndex = 1;
			this.listChoice.SelectedIndexChanged += new System.EventHandler(this.listChoice_SelectedIndexChanged);
			// 
			// statusBarCookingTime
			// 
			this.statusBarCookingTime.Location = new System.Drawing.Point(0, 299);
			this.statusBarCookingTime.Name = "statusBarCookingTime";
			this.statusBarCookingTime.Size = new System.Drawing.Size(640, 22);
			this.statusBarCookingTime.TabIndex = 7;
			// 
			// tBarCookingTime
			// 
			this.tBarCookingTime.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																							   this.btnCalculate,
																							   this.toolBarButton1,
																							   this.btnReset,
																							   this.toolBarButton2,
																							   this.btnExit});
			this.tBarCookingTime.CausesValidation = false;
			this.tBarCookingTime.DropDownArrows = true;
			this.tBarCookingTime.Name = "tBarCookingTime";
			this.tBarCookingTime.ShowToolTips = true;
			this.tBarCookingTime.Size = new System.Drawing.Size(640, 39);
			this.tBarCookingTime.TabIndex = 9;
			this.tBarCookingTime.Validating += new System.ComponentModel.CancelEventHandler(this.frmCookingTime_Validating);
			this.tBarCookingTime.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.tBarCookingTime_ButtonClick);
			// 
			// btnCalculate
			// 
			this.btnCalculate.Text = "Calculate";
			// 
			// toolBarButton1
			// 
			this.toolBarButton1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// btnReset
			// 
			this.btnReset.Text = "Reset";
			// 
			// toolBarButton2
			// 
			this.toolBarButton2.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// btnExit
			// 
			this.btnExit.Text = "Exit";
			// 
			// lblCookingType
			// 
			this.lblCookingType.Location = new System.Drawing.Point(16, 48);
			this.lblCookingType.Name = "lblCookingType";
			this.lblCookingType.Size = new System.Drawing.Size(128, 16);
			this.lblCookingType.TabIndex = 11;
			this.lblCookingType.Text = "Your Cooking a:";
			this.lblCookingType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblCookingInstr
			// 
			this.lblCookingInstr.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblCookingInstr.Location = new System.Drawing.Point(8, 232);
			this.lblCookingInstr.Name = "lblCookingInstr";
			this.lblCookingInstr.Size = new System.Drawing.Size(584, 48);
			this.lblCookingInstr.TabIndex = 4;
			this.lblCookingInstr.Text = "lblCookingInstr";
			// 
			// errCookingTime
			// 
			this.errCookingTime.DataMember = null;
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.mnuFile,
																					  this.mnuCalculate,
																					  this.mnuHelp});
			// 
			// mnuFile
			// 
			this.mnuFile.Index = 0;
			this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuFileExit});
			this.mnuFile.Text = "File";
			// 
			// mnuFileExit
			// 
			this.mnuFileExit.Index = 0;
			this.mnuFileExit.Text = "Exit";
			this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
			// 
			// mnuCalculate
			// 
			this.mnuCalculate.Index = 1;
			this.mnuCalculate.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.mnuCookingTime});
			this.mnuCalculate.Text = "Calculate";
			// 
			// mnuCookingTime
			// 
			this.mnuCookingTime.Index = 0;
			this.mnuCookingTime.Text = "Cooking Time";
			// 
			// mnuHelp
			// 
			this.mnuHelp.Index = 2;
			this.mnuHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					this.mnuHelpHelp,
																					this.menuItem6,
																					this.mnuAbout});
			this.mnuHelp.Text = "Help";
			// 
			// mnuHelpHelp
			// 
			this.mnuHelpHelp.Index = 0;
			this.mnuHelpHelp.Text = "Help";
			// 
			// menuItem6
			// 
			this.menuItem6.Index = 1;
			this.menuItem6.Text = "-";
			// 
			// mnuAbout
			// 
			this.mnuAbout.Index = 2;
			this.mnuAbout.Text = "About";
			this.mnuAbout.Click += new System.EventHandler(this.mnuAbout_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 216);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(216, 16);
			this.label2.TabIndex = 12;
			this.label2.Text = "Cooking Instructions:";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.AddRange(new System.Windows.Forms.Control[] {
																					this.numWeight,
																					this.grpUnits,
																					this.label1});
			this.groupBox1.Location = new System.Drawing.Point(296, 64);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(304, 96);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "It Weighs";
			// 
			// numWeight
			// 
			this.numWeight.DecimalPlaces = 3;
			this.numWeight.Increment = new System.Decimal(new int[] {
																		1,
																		0,
																		0,
																		65536});
			this.numWeight.Location = new System.Drawing.Point(96, 24);
			this.numWeight.Maximum = new System.Decimal(new int[] {
																	  20,
																	  0,
																	  0,
																	  0});
			this.numWeight.Name = "numWeight";
			this.numWeight.Size = new System.Drawing.Size(88, 20);
			this.numWeight.TabIndex = 1;
			this.numWeight.Value = new System.Decimal(new int[] {
																	1,
																	0,
																	0,
																	65536});
			this.numWeight.Enter += new System.EventHandler(this.numWeight_Enter);
			// 
			// grpUnits
			// 
			this.grpUnits.Controls.AddRange(new System.Windows.Forms.Control[] {
																				   this.radioKgs,
																				   this.radioPounds});
			this.grpUnits.Location = new System.Drawing.Point(192, 16);
			this.grpUnits.Name = "grpUnits";
			this.grpUnits.Size = new System.Drawing.Size(104, 72);
			this.grpUnits.TabIndex = 2;
			this.grpUnits.TabStop = false;
			this.grpUnits.Text = "Units";
			// 
			// radioKgs
			// 
			this.radioKgs.CausesValidation = false;
			this.radioKgs.Location = new System.Drawing.Point(8, 40);
			this.radioKgs.Name = "radioKgs";
			this.radioKgs.Size = new System.Drawing.Size(80, 24);
			this.radioKgs.TabIndex = 1;
			this.radioKgs.Text = "Kilograms";
			// 
			// radioPounds
			// 
			this.radioPounds.CausesValidation = false;
			this.radioPounds.Location = new System.Drawing.Point(8, 16);
			this.radioPounds.Name = "radioPounds";
			this.radioPounds.Size = new System.Drawing.Size(80, 24);
			this.radioPounds.TabIndex = 0;
			this.radioPounds.Text = "Pounds";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Cooking Weight:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// frmCookingTime
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(640, 321);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.groupBox1,
																		  this.label2,
																		  this.lblCookingInstr,
																		  this.lblCookingType,
																		  this.tBarCookingTime,
																		  this.statusBarCookingTime,
																		  this.listChoice,
																		  this.label3,
																		  this.lblCookingTime});
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Menu = this.mainMenu1;
			this.Name = "frmCookingTime";
			this.Text = "Cooking Time Tool";
			this.Load += new System.EventHandler(this.frmCookingTime_Load);
			this.Validating += new System.ComponentModel.CancelEventHandler(this.frmCookingTime_Validating);
			this.groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numWeight)).EndInit();
			this.grpUnits.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new frmCookingTime());
		}


    private void tBarCookingTime_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e) {
    
      
      switch(e.Button.Text){
        case "Calculate":
          DoCalculate();
          break;

        case "Reset":
          ResetForm();
          break;

        case "Exit":
          m_isClosing = true;
          Close();
          break;

      }
    }
    /*********************************************************************
     * Method: DoCalculate()
     * 
     * */
    private void DoCalculate() 
    {
      if (!CheckForm())
      {
		CT.clsTime Required = new CT.clsTime();
        GetFormData();
        Required = ((CT.IRequiredTime)m_Meal).CalcRequiredTime(m_Meal.Weight);
		lblCookingTime.Text = Required.ToString();
      } //end of if
      return;
    } //private void DoCalculate()
    /*********************************************************************
     * Method: ResetForm()
     * 
     * Destroys an objects in the form and clears the previous selections.
     * */
    private void ResetForm() 
    {
      listChoice.ClearSelected();
      ClearForm();
    } //end of ResetForm() 
    /*********************************************************************
     * Method: GetFormData()
     * Retrieve the data entered by the user from the form. 
     * */
    private void GetFormData() 
    {    
         
      //Cooking Units
      CT.enmUnit tmpUnit;
      if (radioKgs.Checked) 
        tmpUnit = CT.enmUnit.Metric;
      else
		tmpUnit = CT.enmUnit.Imperial;

      //Convert the weight to the proper weight.
      CT.structWeight oWeight;
      oWeight = new CT.structWeight((decimal) numWeight.Value,tmpUnit);
      
	  m_Meal.Weight = oWeight.Weight;
      return;
    }//end of GetFormData()
    /*********************************************************************
     * Event: frmCookingTime_Load
     * 
     * */
    private void frmCookingTime_Load(object sender, System.EventArgs e)
    {

      m_Meals = m_MealCreator.GetMeals();
      //Retrieve the MealList
      listChoice.Items.AddRange(m_Meals.ToArray());
      ResetForm();
    } //end of frmCookingTime_Load
    /*********************************************************************
     * Event: listChoice_SelectedIndexChanged()
     * 
     * */
    private void listChoice_SelectedIndexChanged(object sender, System.EventArgs e)
    {	
		int i = m_Meals.IndexOf(listChoice.SelectedItem);
		m_Meal = ((CT.AbstractMeal)m_Meals[i]);  
 	    ClearForm();
		numWeight.Focus();
         
      return; 
    } //end of listChoice_SelectedIndexChanged
    /*********************************************************************
     * Event frmCookingTime_Validating()
     * 
     * Check if there are all the required controls have been filled.
     * 
     * */
    private void frmCookingTime_Validating(object sender, System.ComponentModel.CancelEventArgs e)
    {
      
      if (m_isClosing == false)
      {
        e.Cancel = CheckForm();
        if(e.Cancel == true) 
			statusBarCookingTime.Text = "Errors found";
        else 
			statusBarCookingTime.Text = "";

      } //end of if
    } //end of frmCookingTime_Validating
	private void ClearForm() 
	{
		radioKgs.Checked = false;
		radioPounds.Checked = true;
		lblCookingTime.Text = "";
		numWeight.Value = 0.1M;	
	} //end of ClearForm
	//*********************************************************************************
    private bool CheckForm() 
    {
      bool bolReturn = false;
      //Clear all errors
      errCookingTime.SetError(listChoice,"");
      errCookingTime.SetError(grpUnits,"");
      errCookingTime.SetError(numWeight,"");
      statusBarCookingTime.Text = "Validating";

      //Check if the Meal Type has been selected
      if(listChoice.SelectedIndex <0 ) 
      {
        errCookingTime.SetError(listChoice,"Please select something you would like to cook.");
        bolReturn = true;
      }

      if(radioPounds.Checked == false && radioKgs.Checked == false) 
      {
        errCookingTime.SetError(grpUnits,"Please select a either Pound or Kilograms");
        bolReturn = true;
      }
      
      if(numWeight.Value <= 0) 
      {        
        errCookingTime.SetError(numWeight,"Please Enter a weight for the thing your cooking.");
       bolReturn = true;
      }

      if(bolReturn) 
        statusBarCookingTime.Text = "Errors found";
      else 
        statusBarCookingTime.Text = "";
 
      return bolReturn;
    } //end of CheckForm()
    /*********************************************************************
     * Event mnuAbout_Click
     * Display the frmAbout Form.
     * */
    private void mnuAbout_Click(object sender, System.EventArgs e)
    {
      frmAbout oAbout = new frmAbout();
     
      oAbout.ShowDialog(this);
    } //end of mnuAbout_Click
	//*********************************************************************
    private void mnuFileExit_Click(object sender, System.EventArgs e)
    {
      m_isClosing = true;
      
      Close();
    } //end of mnuFileExit_Click
	//*********************************************************************
	private void numWeight_Enter(object sender, System.EventArgs e)	{
			
		numWeight.Select(0,numWeight.Text.Length);

	} //end of numWeight_Enter 

		
	public void CheckWeight(decimal aWeight) 
	{
		
				
//			string sMessage ="";
//				
//			if (aWeight > MaxWeightPounds && m_MaxWeightPounds > 0) 
//			{	
//				sMessage ="The weight of the " +m_Name+ " is too large to calculate a cooking time";
//			} 
//			else if (aWeight < m_MinWeightPounds && m_MinWeightPounds > 0) 
//			{
//				sMessage ="The weight of the " +m_Name+ " is below the minimum weight to calculate a cooking time";
//			}
				
	} //end of CheckWeight
    
	}
}