using System;

namespace CookingTime
{
	/// <summary>
	/// Summary description for structWeight.
	/// A class to convert weight to a base weight.
	/// </summary>
	public struct structWeight
	{
    private const float KGSTOPOUNDS = 2.205F;
    private const float POUNDSTOKGS =  0.4536F;
    
    private float m_entered_weight;
    private enmUnit m_unit;
		public structWeight(float aEnteredWeight, enmUnit aUnit)
		{
			m_unit = aUnit;
      m_entered_weight=aEnteredWeight;

		}
    /********************************************************************
     * Member: Weight
     * 
     * */
    public float Weight{
      get {
        float fltWeight;
        if( m_unit == enmUnit.Metric) 
        {
          fltWeight=  (float)(m_entered_weight * 0.4536);
        } else {
          fltWeight = m_entered_weight;
          //fltWeight= (m_entered_weight * 2.205);
        }
        return fltWeight;
      }
    } 
	}
  public enum enmUnit
  {
    Metric=1,
    Imperial=2
  }
}
