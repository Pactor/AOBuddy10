using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class Buff
    {
        public int Id { get; internal set; }
        public NanoItem NanoItem { get; internal set; }
        public Cooldown Cooldown { get; internal set; }

        public Buff(int id)
        {
            Id = id;

            Cooldown = new Cooldown();

            if (ItemData.Find(id, out NanoItem nanoItem))
                NanoItem = nanoItem;

            //if (DynelManager.LocalPlayer != null)
            //    NanoItem.Cost = GetCost(NanoItem.Cost);
        }

        private int GetCost(int baseCost)
        {
            int costModifier = DynelManager.LocalPlayer.GetStat(Stat.NPCostModifier);

            switch ((Breed)DynelManager.LocalPlayer.GetStat(Stat.Breed))
            {
                case Breed.Nanomage:
                    costModifier = costModifier < 45 ? 45 : costModifier;
                    break;
                case Breed.Atrox:
                    costModifier = costModifier < 55 ? 55 : costModifier;
                    break;
                case Breed.Solitus:
                case Breed.Opifex:
                default:
                    costModifier = costModifier < 50 ? 50 : costModifier;
                    break;
            }

            return (int)(baseCost * ((double)costModifier / 100));
        }
    }


    public class Cooldown
    {
        private long _expireTimeTicks { get; set; }
        public double RemainingTime => GetRemainingTime();

        public void SetExpireTime(float timeInSeconds) => _expireTimeTicks = Utils.SetExpireTimeInTicks(timeInSeconds);

        private double GetRemainingTime() => Utils.GetRemainingTimeInSeconds(_expireTimeTicks);
    }
}
