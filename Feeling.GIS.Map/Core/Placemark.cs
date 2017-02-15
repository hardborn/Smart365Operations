
namespace Feeling.GIS.Map.Core
{
 
   public class Placemark
   {
      public GeoCoderStatusCode Status = GeoCoderStatusCode.Unknow;
      public string XmlData;

      string address;

     
      public string Address
      {
         get
         {
            return address;
         }
         internal set
         {
            address = value;
         }
      }

    
      public int Accuracy;

      public string ThoroughfareName;
      public string LocalityName;
      public string PostalCodeNumber;
      public string CountryName;
      public string CountryNameCode;
      public string AdministrativeAreaName;
      public string SubAdministrativeAreaName;

      public Placemark(string address)
      {
         this.address = address;
      }
     
      protected virtual bool ParseAddress()
      {
         return false;
      }
   }
}
