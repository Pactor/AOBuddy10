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
	public class DummyItem : ItemBase
	{
		public DummyItem(int id, int ql) : base(id, ql) { }
	}
}