using System;
using System.Collections.Generic;
using System.Text;

namespace AUI.MorphUI
{
	class Filter
	{
		public List<DAZMorph> Process(List<DAZMorph> morphs)
		{
			var list = new List<DAZMorph>();
			list.Capacity = morphs.Count;

			// ghetto set
			var set = new Dictionary<string, int>();

			for (int i = 0; i < morphs.Count; ++i)
			{
				var m = morphs[i];

				if (ShouldShow(m, set))
				{
					list.Add(m);
				}
				else
				{
					Log.Verbose($"filtered {m.uid}");
				}
			}

			return list;
		}

		private bool ShouldShow(DAZMorph m, Dictionary<string, int> set)
		{
			if (m.morphValue != m.startValue)
				return true;

			if (!m.isLatestVersion)
				return false;

			var file = GetFilename(m);
			if (set.ContainsKey(file))
			{
				Log.Verbose($"dupe {m.uid} {file}");
				return false;
			}
			else
			{
				set.Add(file, 0);
			}

			return true;
		}

		private string GetFilename(DAZMorph m)
		{
			if (m.isInPackage)
			{
				var pos = m.uid.IndexOf(":/");
				if (pos == -1)
				{
					Log.Error($"{m.uid} has no :/");
				}
				else
				{
					return m.uid.Substring(pos + 2);
				}
			}

			return m.uid;
		}
	}
}
