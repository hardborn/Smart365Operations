
namespace Feeling.GIS.Map.Core
{
   using System;
   using System.Collections.Generic;
   using System.Runtime.Serialization;

   [Serializable]
   public class MapRoute : ISerializable, IDeserializationCallback

   {

      public readonly List<PointLatLng> Points;

      public string Name;

      public object Tag;

      public PointLatLng? From
      {
         get
         {
            if(Points.Count > 0)
            {
               return Points[0];
            }

            return null;
         }
      }

      public PointLatLng? To
      {
         get
         {
            if(Points.Count > 1)
            {
               return Points[Points.Count - 1];
            }

            return null;
         }
      }

      public MapRoute(List<PointLatLng> points, string name)
      {
         Points = new List<PointLatLng>(points);
         Points.TrimExcess();

         Name = name;
      }

      public double Distance
      {
         get
         {
            double distance = 0.0;

            if(From.HasValue && To.HasValue)
            {
               for(int i = 1; i < Points.Count; i++)
               {
                  distance += MapsManager.Instance.GetDistance(Points[i - 1], Points[i]);
               }
            }

            return distance;
         }
      }

#if !PocketPC
      #region ISerializable Members

      private PointLatLng[] deserializedPoints;

      public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
      {
         info.AddValue("Name", this.Name);
         info.AddValue("Tag", this.Tag);
         info.AddValue("Points", this.Points.ToArray());
      }

      protected MapRoute(SerializationInfo info, StreamingContext context)
      {
         this.Name = info.GetString("Name");
         this.Tag = Extensions.GetValue<object>(info, "Tag", null);
         this.deserializedPoints = Extensions.GetValue<PointLatLng[]>(info, "Points");
         this.Points = new List<PointLatLng>();
      }

      #endregion

      #region IDeserializationCallback Members
     
      public virtual void OnDeserialization(object sender)
      {
         Points.AddRange(deserializedPoints);
         Points.TrimExcess();
      }

      #endregion
#endif
   }
}
