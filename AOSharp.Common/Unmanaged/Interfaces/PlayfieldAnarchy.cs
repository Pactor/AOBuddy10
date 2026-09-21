using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DbObjects;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    
    public class PlayfieldAnarchy
    {
        private IntPtr _pointer;
        
        
        /*public static PlayfieldAnarchy Get(uint playfieldId)
        {
            IntPtr pPlayfieldAnarchy = PlayfieldAnarchy_t.Get(playfieldId);
            if (pPlayfieldAnarchy != IntPtr.Zero)
                return new PlayfieldAnarchy(pPlayfieldAnarchy);
            
            return null;
        }
        */
        
        private PlayfieldAnarchy(IntPtr pointer)
        {
            _pointer = pointer;
        }

        public bool IsShadowlandPF()
        {
            return PlayfieldAnarchy_t.IsShadowlandPF(_pointer);
        }

        public bool AreVehiclesAllowed()
        {
            return PlayfieldAnarchy_t.AreVehiclesAllowed(_pointer);
        }

        public PlayfieldDistrictInfo GetDistrictInfo()
        {
            return new PlayfieldDistrictInfo(PlayfieldAnarchy_t.GetDistrictInfo(_pointer));
        }

        public LandControlMap GetLandControlMap()
        {
            return new LandControlMap(PlayfieldAnarchy_t.GetLandControlMap(_pointer));
        }

        public int GetPFWorldXPos()
        {
            return PlayfieldAnarchy_t.GetPFWorldXPos(_pointer);
        }

        public int GetPFWorldZPos()
        {
            return PlayfieldAnarchy_t.GetPFWorldZPos(_pointer);
        }

        public Vector3 GetSafePos()
        {
            return PlayfieldAnarchy_t.GetSafePos(_pointer);
        }
        
        public bool IsGrid()
        {
            return PlayfieldAnarchy_t.IsGrid(_pointer);
        }
    }
}
