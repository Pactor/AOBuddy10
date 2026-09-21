using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DbObjects;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Imports.DatabaseController;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    public class ResourceDatabase
    {
        public static IntPtr GetDbObject(DBIdentity identity)
        {
            IntPtr pDatabaseHandler = N3DatabaseHandler_t.Get();
            IntPtr pResourceDatabase = N3DatabaseHandler_t.GetResourceDatabase(pDatabaseHandler);

            return ResourceDatabase_t.GetDbObject(pResourceDatabase, ref identity);
        }

        public static T GetDbObject<T>(DBIdentity identity) where T : DbObject
        {
            IntPtr pDbObj = GetDbObject(identity);

            if(pDbObj == IntPtr.Zero)
                return null;

            return (T)Activator.CreateInstance(typeof(T), pDbObj);
        }

        public static void PutDbBlob(DBIdentity identity, byte[] blobData)
        {
            IntPtr pDatabaseHandler = N3DatabaseHandler_t.Get();
            IntPtr pResourceDatabase = N3DatabaseHandler_t.GetResourceDatabase(pDatabaseHandler);

            ResourceDatabase_t.PutDbBlob(pResourceDatabase, ref identity, blobData, blobData.Length);
        }

        public static void PutDbObject(IntPtr pDbObject)
        {
            IntPtr pDatabaseHandler = N3DatabaseHandler_t.Get();
            IntPtr pResourceDatabase = N3DatabaseHandler_t.GetResourceDatabase(pDatabaseHandler);

            ResourceDatabase_t.PutDbObject(pResourceDatabase, pDbObject);
        }
    }
}