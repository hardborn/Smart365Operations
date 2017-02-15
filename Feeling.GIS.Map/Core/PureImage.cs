
namespace Feeling.GIS.Map.Core
{
   using System;
   using System.IO;

   public abstract class PureImageProxy
   {
      abstract public PureImage FromStream(Stream stream);
      abstract public bool Save(Stream stream, PureImage image);
   }

   public abstract class PureImage : IDisposable
   {
      public MemoryStream Data;

      #region IDisposable Members

      abstract public void Dispose();

      #endregion
   }
}
