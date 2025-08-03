using System;


namespace CookingTime
{
	/// <summary>
	/// Summary description for clsTime.
	/// This will contain the minimum and maximum time.
	/// </summary>
  public class clsTime
	{
    private TimeSpan m_MinTime;
    private TimeSpan m_MaxTime;
		public clsTime()
		{
			//
			// TODO: Add constructor logic here
			//
		}
    /**********************************************************************
     * Constructor
     * 
     * sets the clsTime based on a MealType.
     * */
    public clsTime(byte aMealType, float aWeight) 
    {
      
    }

    public clsTime(byte aMealTypeID,float aWeight,int aCookingTimePerPound) 
    :this(aMealTypeID, aWeight) { 


    }
    public clsTime(byte aMealTypeID,float aWeight,int aMinTimePerPound,int aMaxTimePerPound ) 
      :this(aMealTypeID, aWeight) 
    { 


    }

    /***********************************************************************
     * Member: MinimumTime
     * 
     * */
    public TimeSpan MinimumTime 
    {
      get {return m_MinTime; }
      set {m_MaxTime = value; }
    }
    /***********************************************************************
     * Member: MaximumTime
     * 
     * */
    public TimeSpan MaximumTime 
    {
      get {return m_MaxTime; }
      set {m_MaxTime = value; }
    }

    /**********************************************************************
     * Calculates the cooking time required for a chicken to be cooked.
     * 
     * */
    private void CalcTimeChicken(float aWeight) 
    {


      if(aWeight >= 1.5 && aWeight <= 2.5) 
      {
        m_MinTime = new TimeSpan(1,15,0);
        m_MaxTime = new TimeSpan(2,0,0);
      } 
      else if(aWeight > 2.5 && aWeight <= 3.5) 
      {
        m_MinTime = new TimeSpan(2,0,0);
        m_MaxTime = new TimeSpan(3,0,0);
      } 
      else if(aWeight > 3.5 && aWeight <= 4.75) 
      {
        m_MinTime = new TimeSpan(3,0,0);
        m_MaxTime = new TimeSpan(3,30,0);
      }
      else if(aWeight > 4.75 && aWeight <= 6.00) 
      {
        m_MinTime = new TimeSpan(3,30,0);
        m_MaxTime = new TimeSpan(4,0,0);
      } 
      else 
      {
        throw new Exception("Weight of Chicken out of range.");
      }
    }

    /**********************************************************************
     * Calculates the cooking time required for a turkey to be cooked.
     * 
     * */
    private void CalcTimeTurkey(float aWeight) 
    {


      if(aWeight >= 6.0 && aWeight <= 8.0) 
      {
        m_MinTime = new TimeSpan(3,45,0);
        m_MaxTime = new TimeSpan(4,0,0);
      } 
      else if(aWeight > 8.0 && aWeight <= 10) 
      {
        m_MinTime = new TimeSpan(4,0,0);
        m_MaxTime = new TimeSpan(4,30,0);
      } 
      else if(aWeight > 10.0 && aWeight <= 12) 
      {
        m_MinTime = new TimeSpan(4,30,0);
        m_MaxTime = new TimeSpan(5,0,0);
      } 
      else if(aWeight > 12.0 && aWeight <= 14) 
      {
        m_MinTime = new TimeSpan(5,0,0);
        m_MaxTime = new TimeSpan(5,15,0);
      }
      else if(aWeight > 14.0 && aWeight <= 16) 
      {
        m_MinTime = new TimeSpan(5,15,0);
        m_MaxTime = new TimeSpan(6,0,0);
      } 
      else if(aWeight > 16.00 && aWeight <= 18.00) 
      {
        m_MinTime = new TimeSpan(6,0,0);
        m_MaxTime = new TimeSpan(6,30,0);
      } 
      else if(aWeight > 18.00 && aWeight <= 20.00) 
      {
        m_MinTime = new TimeSpan(6,30,0);
        m_MaxTime = new TimeSpan(7,30,0);
      }
      else if(aWeight > 20.00 && aWeight <= 24.00) 
      {
        m_MinTime = new TimeSpan(7,30,0);
        m_MaxTime = new TimeSpan(9,0,0);
      } 
      else 
      {
        throw new Exception("Weight of Turkey out of range.");
      }
    }

	
   

  }
}
