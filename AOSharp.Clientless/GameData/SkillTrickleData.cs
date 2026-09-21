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
	internal static class SkillTrickleData
	{
		private static Dictionary<Stat, float[]> _skillTrickleData;
		internal static Dictionary<Stat, float[]> SkillTrickle
		{
			get
			{
				if (_skillTrickleData == null)
                    LoadSkillTrickleData();

				return _skillTrickleData;
			}
		}

		private static void LoadSkillTrickleData()
		{
			try
			{
				_skillTrickleData = JsonConvert.DeserializeObject<Dictionary<Stat, float[]>>(File.ReadAllText($"GameData\\SkillTrickle.json"));
			}
			catch
			{
				Logger.Error("Failed to load skill trickle data.");
				_skillTrickleData = new Dictionary<Stat, float[]>();
			}
		}
	}
}
