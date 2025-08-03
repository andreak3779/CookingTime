using System;

namespace CookingTime
{
	/// <summary>
	/// Summary description for MealType.
	/// The MealType class describes something that is cooked.
	/// 
	/// </summary>
	public class clsMealType
	{
    private byte m_MealTypeID;
    private string m_MealTypeName;
    private string m_MealInstructions;
    private int m_MinTimePerPound;
    private int m_MaxTimePerPound;

    public clsMealType () 
    {
      m_MealTypeID= 0;
      m_MealTypeName="None";
      m_MealInstructions="N/A";
    }
		public clsMealType ( byte aMealTypeID,string aMealTypeName) :this()
		{
      
			m_MealTypeID = aMealTypeID;
      m_MealTypeName = aMealTypeName;
		}
    
    /**************************************************************************
     * 
     * 
     * */
    public clsMealType ( byte aMealTypeID,
        string aMealTypeName, 
        int aMinTimePerPound,
        int aMaxTimePerPound)
      :this()
    {
      
      m_MealTypeID = aMealTypeID;
      m_MealTypeName = aMealTypeName;
      m_MinTimePerPound = aMinTimePerPound;
      m_MaxTimePerPound = aMaxTimePerPound;
    }
    /**************************************************************************
     * 
     * 
     * */
    public clsMealType ( byte aMealTypeID,
      string aMealTypeName, 
      int aTimePerPound)
      :this()
    {
      
      m_MealTypeID = aMealTypeID;
      m_MealTypeName = aMealTypeName;
      m_MinTimePerPound = aTimePerPound;
      
    }
    /********************************************************************
     * Member: MealTypeID <<Read Only>>
     * 
     */
    public byte MealTypeID {
      get{return m_MealTypeID;}
    }
    /********************************************************************
     * Member Name <<Read Only>>
     * 
     * 
     * */
    public string Name {
      get{return m_MealTypeName;}
    }
    public string Instructions
    {
      get {return m_MealInstructions;}

    }
    /********************************************************************
     * Method: CalcCookingTime()
     * 
     * 
     * */
    public clsTime CalcCookingTime(float aWeight) 
    {

      clsTime oCookingTime;
      if (m_MinTimePerPound == 0) 
      {
        oCookingTime = new clsTime(m_MealTypeID,aWeight);
      } 
      else if (m_MaxTimePerPound == 0) 
      {
        oCookingTime = new clsTime(m_MealTypeID,aWeight,m_MinTimePerPound);     
      } 
      else 
      {
        oCookingTime = new clsTime(m_MealTypeID,aWeight,m_MinTimePerPound,m_MaxTimePerPound);
      }
      return oCookingTime;
    }

//    public TimeSpan CalcCookingTime(float aWeight) 
//    {
//
//      TimeSpan tmpTimeSpan;
//      //Turkey 
//      /* (0.1334)(x) + 1.54
//       * */
//      switch (m_MealTypeID) {
//        case 1: //Chicken
//            tmpTimeSpan = new TimeSpan(0,120,0);
//
//          break;
//        case 2: //Chicken and Dressing
//            tmpTimeSpan = new TimeSpan(0,160,0);
//          break;
//        case 3: //Pork Roast
//            tmpTimeSpan = new TimeSpan(0,170,0);
//          break;
//        default:
//            tmpTimeSpan = new TimeSpan(0,12,0);
//          break;
//
//      }
//      return tmpTimeSpan;
//    }

    /******************************************************************
     * Method: ToString <<Override>>
     * 
     * */
    public override string ToString () 
    {
      return m_MealTypeName;
    }

    /*********************************************************************
     * Method: Dispose()
     * 
     * */
    public void Dispose() 
    {
      m_MealTypeName = null;
      m_MealInstructions=null;
    }
    /******************************************************************
     * Method: GetMeals 
     * 
     * 
     * */


    /**********************************************************************
       * Calculates the cooking time required for a turkey to be cooked.
       * 
       * */
    private void CalcTimeRange(byte aMealType, float aWeight) 
    {
      TimeSpan tsMinTimePerPound, tsMaxTimePerPound;
      switch (aMealType) 
      {
        case 1: //Chicken

        break;

        default:

        break;

      }

    }
    /*****************************************************************************
     * Testing static helper function.
     * 
     * History:
     * awk 2005-may-18 - 
     * 
     * */
    public static int GetMeals(System.Collections.ArrayList aMealTypeList) 
    {
		int i=0;
      //Create Meal Type List
      clsMealType oMealType;
      //Chicken
      oMealType = new clsMealType(++i,"Chicken");

      oMealType.m_MealInstructions = 
        "Preheat oven to 325F degrees. The chicken will be ready when"+ 
        "it's internal temperature is at 180F degrees";
      aMealTypeList.Add((object)oMealType);
      //Turkey      
      oMealType = new clsMealType(++i,"Turkey");
      aMealTypeList.Add((object)oMealType);
      //Pork Roast Loin, Leg, Butt
      oMealType = new clsMealType(++i,"Pork Roast Loin, Leg, Butt",40,45);
      aMealTypeList.Add((object)oMealType);
      //******************************************************************
      //Beef Roast Standing Rib - Rare 
      oMealType = new clsMealType(++i,"Beef Roast Standing Rib - Rare ",18,20);
      aMealTypeList.Add((object)oMealType);
      //Beef Roast Standing Rib - Medium 
      oMealType = new clsMealType(++i,"Beef Roast Standing Rib - Rare ",22,24);
      aMealTypeList.Add((object)oMealType);
      //Beef Roast Rolled - Rare 
      oMealType = new clsMealType(++i,"Beef Roast Rolled - Rare ",28,30);
      aMealTypeList.Add((object)oMealType);
      //Beef Roast Rolled - Medium 
      oMealType = new clsMealType(++i,"Beef Roast Rolled - Medium ",32,34);
      aMealTypeList.Add((object)oMealType);
      //Beef Roast Round or Rump - Rare 
      oMealType = new clsMealType(++i,"Beef Roast Round or Rump - Rare ",20,22);
      aMealTypeList.Add((object)oMealType);
      //Beef Roast Round or Rump - Medium 
      oMealType = new clsMealType(++i,"Beef Roast Round or Rump - Medium ",25,30);
      aMealTypeList.Add((object)oMealType);
      //******************************************************************
      //Turkey      
      oMealType = new clsMealType(++i,"Turkey");
      aMealTypeList.Add((object)oMealType);
      //Smoked Ham - Whole
      oMealType = new clsMealType(++i,"Smoked Ham - Whole", 15, 18);
      aMealTypeList.Add((object)oMealType);
      //Smoked Ham - Half
      oMealType = new clsMealType(++i,"Smoked Ham - Half",20);
      aMealTypeList.Add((object)oMealType);

      //Pork - Smoked Picnic Shoulder
      oMealType = new clsMealType(++i,"Pork - Smoked Picnic Shoulder",30);
      aMealTypeList.Add((object)oMealType);
      return 1;
    }
  }
}
