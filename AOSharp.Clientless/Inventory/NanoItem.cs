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
	public class NanoItem : ItemBase
	{
		public int Cost;
		public NanoLine NanoLine;
		public NanoSchool NanoSchool;
		public int NCU;
		public int StackingOrder;
		public int Range;

		[JsonIgnore]
		public double TotalTime => TotalTimeInTicks / 100f;
		[JsonIgnore]
		public double AttackDelay => AttackDelayInTicks / 100f;
		[JsonIgnore]
		public double RechargeDelay => RechargeDelayInTicks / 100f;

		[JsonProperty]
		internal int TotalTimeInTicks;
		[JsonProperty]
		internal int AttackDelayInTicks;
		[JsonProperty]
		internal int RechargeDelayInTicks;

		public NanoItem(int id, int ql) : base(id, ql) { }
	}
}