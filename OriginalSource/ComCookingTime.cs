using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.IO;

namespace CookingTime
{
	/// <summary>
	/// Summary description for ComCookingTime.
	/// </summary>
	public class ComCookingTime : System.ComponentModel.Component
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ComCookingTime(System.ComponentModel.IContainer container)
		{
			/// <summary>
			/// Required for Windows.Forms Class Composition Designer support
			/// </summary>
			container.Add(this);
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	
	
        
		public ComCookingTime()
		{
			/// <summary>
			/// Required for Windows.Forms Class Composition Designer support
			/// </summary>
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}//end of ComCookingTime Constructor

		public class AbstractMeal : IRequiredTime
		{
			private byte   m_ID;
			private string m_Name;
			private string m_Instructions;
			private decimal m_weight;

			public AbstractMeal () 
			{
				m_ID= 0;
				m_Name="None";
				m_Instructions="N/A";
			} //end of AbstractMeal()
            public AbstractMeal(string aName, string aInstruct)
            {
                m_Name = aName;
                m_Instructions = aInstruct;
            }
			public AbstractMeal(byte aMealID, string aName, string aInstruct) 
			{
				m_ID            = aMealID;
				m_Name          = aName;
				m_Instructions = aInstruct;
			}
            public void Dispose()
            {
                m_Name = null;
                m_Instructions = null;
            } //end of Dispose
				
			/********************************************************************
			 * Member: MealTypeID <<Read Only>>
			 * 
			 */
			public byte MealTypeID 
			{
				get{return m_ID;}
			}
			/********************************************************************
			 * Member Name <<Read Only>>
			 * 
			 * 
			 * */
			public string Name 
			{
                set { m_Name = value; }
				get{return m_Name;}
			}
			/********************************************************************
			 * Member: Instructions <<Read Only>>
			 * 
			 * 
			 * */
			public string Instructions
			{
                set { m_Instructions = value; }
				get {return m_Instructions;}
			}
			
			public decimal Weight 
			{
				set {m_weight = value; }
				get {return m_weight; }
			}
		
			/******************************************************************
			 * Method: ToString <<Override>>
			 * 
			 * */
			public override string ToString () 
			{
				return  m_Name;
			}

			public virtual clsTime CalcRequiredTime(decimal aWeight) {return null; }
			

		} //end of AbstractMeal

		public class MealBasic : AbstractMeal
		{
		//Data Fields
		private int m_TimePerPound;
            public MealBasic() :base(){
            }
		public MealBasic(byte aMealID, string aMealName, string aInstruct)
			:base(aMealID, aMealName, aInstruct) 
			{
			}
	
		public MealBasic(byte aMealID, string aMealName,string aInstruct, int aTimePerPound) 
			:base(aMealID, aMealName, aInstruct)
		{
			m_TimePerPound = aTimePerPound;

		}

		//Accessors
		public int TimePerPound 
		{
			get { return m_TimePerPound;}
			set { m_TimePerPound = value; }
		}
			
		//postcondition: returns a Time object with the required time for a meal to be cooked.
		public override clsTime CalcRequiredTime(decimal aWeight) 
		{
			clsTime required = new clsTime();
				
			decimal RequirdTimeMinutes;
			RequirdTimeMinutes = (decimal)aWeight * this.m_TimePerPound;
				
			required.RequiredTime = new TimeSpan(0,(int)RequirdTimeMinutes,0);

			return required;
		}
		} //end of clsMealBasic


		public class MealRange :AbstractMeal
		{
		private int    m_MinTimePerPound;
		private int    m_MaxTimePerPound;
		private decimal m_MaxWeightPounds =0M;
		private decimal m_MinWeightPounds =0M;
		
		//**************************************************************************
		//Constructors
            public MealRange()
                : base()
            {
            }
            public MealRange(byte aMealTypeID, string aMealTypeName, string aInstruct, int aMinTimePerPound, int aMaxTimePerPound)
                : base(aMealTypeID, aMealTypeName, aInstruct)
            {
                m_MinTimePerPound = aMinTimePerPound;
                m_MaxTimePerPound = aMaxTimePerPound;
            }

		public int MinTimePerPound 
		{
			get {return m_MinTimePerPound; }
			set {m_MinTimePerPound = value; }
		} //end of MinTimePerPound
			
		public int MaxTimePerPound 
		{
			get {return m_MaxTimePerPound; }
			set {m_MaxTimePerPound = value; }
		} //end of MaxTimePerPound

		public decimal MinWeightPounds 
		{
			get {return m_MinWeightPounds;}
		}

		public decimal MaxWeightPounds 
		{
			get {return m_MaxWeightPounds;}
		}
			
		public override clsTime CalcRequiredTime(decimal aWeight)
		{
			clsTime required = new clsTime();
			decimal fltMinTimeMinutes;
			decimal fltMaxTimeMinutes;
			fltMaxTimeMinutes = (decimal)aWeight * this.m_MaxTimePerPound;
			fltMinTimeMinutes = (decimal)aWeight * this.m_MinTimePerPound;
			fltMaxTimeMinutes = Decimal.Round(fltMaxTimeMinutes,0);
			required.MinimumTime = new TimeSpan(0,(int)fltMinTimeMinutes,0);
			required.MaximumTime = new TimeSpan(0,(int)fltMaxTimeMinutes,0);

			return required;
 
		} //end of CalcRequiredTime

	} //end of clsMealRange

		public class MealChicken :MealBasic
	{

            public MealChicken() : base() { }
		    public MealChicken(byte aMealID, string aMealName, string aInstruct) 
			    :base(aMealID, aMealName, aInstruct) {
		    }


		/**********************************************************************
		* Calculates the cooking time required for a chicken to be cooked.
		* 
		* */
		public override clsTime CalcRequiredTime(decimal aWeight) 
		{
			clsTime required = new clsTime();

			if(aWeight >= 1.5M && aWeight <= 2.5M) 
			{
				required.MinimumTime = new TimeSpan(1,15,0);
				required.MaximumTime = new TimeSpan(2,0,0);
			} 
			else if(aWeight > 2.5M && aWeight <= 3.5M) 
			{
				required.MinimumTime = new TimeSpan(2,0,0);
				required.MaximumTime = new TimeSpan(3,0,0);
			} 
			else if(aWeight > 3.5M && aWeight <= 4.75M) 
			{
				required.MinimumTime = new TimeSpan(3,0,0);
				required.MaximumTime = new TimeSpan(3,30,0);
			}
			else if(aWeight > 4.75M && aWeight <= 6.00M) 
			{
				required.MinimumTime = new TimeSpan(3,30,0);
				required.MaximumTime = new TimeSpan(4,0,0);
			} 
				
			return required;
		} //end of CalcRequiredTime



	} //end of MealChicken

	public class MealTurkey : MealBasic
	{
        public MealTurkey() : base() { }
		public MealTurkey(byte aMealID, string aName, string aInstruct)
			:base(aMealID,aName, aInstruct)
		{
			
		}
		/**********************************************************************
			 * Calculates the cooking time required for a turkey to be cooked.
			 * 
			 * */
		public override clsTime CalcRequiredTime(decimal aWeight) 
		{
			clsTime required = new clsTime();

			if(aWeight >= 6.0M && aWeight <= 8.0M) 
			{
				required.MinimumTime = new TimeSpan(3,45,0);
				required.MaximumTime = new TimeSpan(4,0,0);
			} 
			else if(aWeight > 8.0M && aWeight <= 10M) 
			{
				required.MinimumTime = new TimeSpan(4,0,0);
				required.MaximumTime = new TimeSpan(4,30,0);
			} 
			else if(aWeight > 10.0M && aWeight <= 12M) 
			{
				required.MinimumTime  = new TimeSpan(4,30,0);
				required.MaximumTime = new TimeSpan(5,0,0);
			} 
			else if(aWeight > 12.0M && aWeight <= 14M) 
			{
				required.MinimumTime = new TimeSpan(5,0,0);
				required.MaximumTime = new TimeSpan(5,15,0);
			}
			else if(aWeight > 14.0M && aWeight <= 16M) 
			{
				required.MinimumTime = new TimeSpan(5,15,0);
				required.MaximumTime = new TimeSpan(6,0,0);
			} 
			else if(aWeight > 16.00M && aWeight <= 18.00M) 
			{
				required.MinimumTime  = new TimeSpan(6,0,0);
				required.MaximumTime = new TimeSpan(6,30,0);
			} 
			else if(aWeight > 18.00M && aWeight <= 20.00M) 
			{
				required.MinimumTime = new TimeSpan(6,30,0);
				required.MaximumTime = new TimeSpan(7,30,0);
			}
			else if(aWeight > 20.00M && aWeight <= 24.00M) 
			{
				required.MinimumTime = new TimeSpan(7,30,0);
				required.MaximumTime = new TimeSpan(9,0,0);
			} 
				
			return required;

		}

	} //end of MealTurkey
	
	public class clsTime
	{
		private TimeSpan m_MinTime;
		private TimeSpan m_MaxTime;


		public clsTime() { }
		
		
		public TimeSpan RequiredTime 
		{
			get {return m_MinTime;}
			set {m_MinTime = value; }
		}
				

		public TimeSpan MinimumTime 
		{
			get { return m_MinTime; }
			set { m_MaxTime = value; }
		} //end of MinimumTime

		public TimeSpan MaximumTime 
		{
			get { return m_MaxTime; }
			set { m_MaxTime = value; }
		} //end of MaximumTime

		public override string ToString() 
		{
//			return "";
			if (m_MinTime.TotalMinutes ==0) { //It must be the Max Time
				if (m_MaxTime.Minutes > 0 )
				{
					return "Cooking time will be " + m_MaxTime.Hours.ToString() + "hrs and " + m_MaxTime.Minutes.ToString();
				} else {
					return "Cooking time will be " + m_MaxTime.Hours.ToString() + "hrs";
				}

			} else {
				return "Cooking time will be between of "  + m_MinTime.Hours.ToString() + " hrs. and "+
					m_MinTime.Minutes.ToString() + " mins. to " +  m_MaxTime.Hours.ToString() + " hrs. and "+
					m_MaxTime.Minutes.ToString() + " mins.";

			}//m_MaxTime ==null
				
		} //end of ToString

	} //public class clsTime

		
	public interface IRequiredTime 
		{
			clsTime CalcRequiredTime(decimal aWeight);
            
		} //end of IRequiredTime

	public struct structWeight
	{
		private const decimal KGSTOPOUNDS = 2.205M;
		private const decimal POUNDSTOKGS =  0.4536M;
    
		private decimal m_entered_weight;
		private enmUnit m_unit;
		public structWeight(decimal aEnteredWeight, enmUnit aUnit)
		{
			m_unit = aUnit;
			m_entered_weight=aEnteredWeight;

		}
		/********************************************************************
			* Member: Weight
			* 
			* */
		public decimal Weight
		{
			get 
			{
				decimal fltWeight;
				if( m_unit == enmUnit.Metric) 
					fltWeight= (m_entered_weight * 2.205M);
				else 
					fltWeight = m_entered_weight;
				
				return fltWeight;
			}
		} //public decimal Weight

	} //public struct structWeight
		
	public enum enmUnit
	{
		Metric=1,Imperial=2
	} //public enum enmUnit

        public abstract class CookingTimeCreator
        {
            public abstract AbstractMeal CreateMeal(String aMealType);
        } //end of CookingTimeCreator
        /**
         * MealCreator 
         * Factory Class to encapsulate object creation.
         * 
         * */
        public class MealCreator : CookingTimeCreator
        {
            public override AbstractMeal CreateMeal(String aMealType)
            {
                if (aMealType.Equals("Chicken"))
                    return new MealChicken();
                if (aMealType.Equals("Turkey"))
                    return new MealTurkey();
                if (aMealType.Equals("Range"))
                    return new MealRange();
                if (aMealType.Equals("Basic"))
                    return new MealBasic();
                
                    return null;

            } //end of CreateMeal
            public AbstractMeal CreateMeal(String aMealType, String aName, String aInstruct)
            {
                AbstractMeal oMeal = CreateMeal(aMealType);
                oMeal.Name = aName;
                oMeal.Instructions = aInstruct;
                return oMeal;
            }
            public AbstractMeal CreateMeal(String aMealType, String aName, String aInstruct, int aMin)
            {
                AbstractMeal oMeal = CreateMeal(aMealType, aName, aInstruct);
                ((MealRange)oMeal).MinTimePerPound = aMin;
                return oMeal;
            }
            public  AbstractMeal CreateMeal(String aMealType, String aName, String aInstruct,int aMin, int aMax)
            {
                AbstractMeal oMeal = CreateMeal(aMealType,aName,aInstruct,aMin);
                ((MealRange)oMeal).MaxTimePerPound = aMax;
                return oMeal;
            }
            public ArrayList GetMeals()
            {
                //Create Meal Type List
                ArrayList aMealTypeList = new ArrayList();
                //Chicken ***********************************************************

                aMealTypeList.Add((object)CreateMeal("Chicken", "Chicken",
                    "Preheat oven to 325F degrees. The chicken will be ready when it's internal temperature is at 180F degrees"));

                //Turkey  ***********************************************************    
                aMealTypeList.Add((object)CreateMeal("Turkey","Turkey", ""));

                //Pork Roast Loin, Leg, Butt *****************************************
                aMealTypeList.Add((object)CreateMeal("Range","Pork Roast Loin, Leg, Butt", "", 40, 45));

                //******************************************************************
                //Beef Roast Standing Rib - Rare *************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Standing Rib - Rare ", "", 18, 20));

                //Beef Roast Standing Rib - Medium **********************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Standing Rib - Rare ", "", 22, 24));

                //Beef Roast Rolled - Rare ******************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Rolled - Rare ", "", 28, 30));

                //Beef Roast Rolled - Medium ****************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Rolled - Medium ", "", 32, 34));

                //Beef Roast Round or Rump - Rare ***********************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Round or Rump - Rare ", "", 20, 22));

                //Beef Roast Round or Rump - Medium *********************************
                aMealTypeList.Add((object)CreateMeal("Range", "Beef Roast Round or Rump - Medium ", "", 25, 30));


                //Smoked Ham - Whole ************************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Smoked Ham - Whole", "", 15, 18));
                //Smoked Ham - Half *************************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Smoked Ham - Half", "", 20));

                //Pork - Smoked Picnic Shoulder *************************************
                aMealTypeList.Add((object)CreateMeal("Range", "Pork - Smoked Picnic Shoulder", "", 30));

                return aMealTypeList;
            } //end of GetMeals				

            public ArrayList GetMealsFromFile()
            {
                System.Collections.ArrayList aList = new ArrayList();
                FileStream ins = new FileStream("meals.xml", System.IO.FileMode.Open);

                BufferedStream bstream = new BufferedStream(ins);

                ins.Close();
                return aList;
            }

            	
        } //end of MealCreator
	} //end of ComCookingTime		
			

} //namespace CookingTime
