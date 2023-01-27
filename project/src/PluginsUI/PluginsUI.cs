using SimpleJSON;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AUI.PluginsUI
{
	class PluginsUI : BasicAlternateUI
	{
		private const float DeferredCheckInterval = 1;
		private const float ChangedCheckInterval = 1;

		private AtomInfo[] deferred_ = null;
		private readonly List<AtomInfo> atoms_ = new List<AtomInfo>();
		private float deferredElapsed_ = 0;
		private float changedElapsed_ = 0;

		private readonly Regex[] packageRegexes_;
		private readonly Regex[] fileRegexes_;

		public PluginsUI()
			: base("plugins", "Plugins UI", true)
		{
			string packageRe = @"\w+\.(\w+)\.\d+:\/";
			string csRe = @".*\/([^\/]*\.cs)$";
			string cslistRe = @".*\/([^\/]*\.cslist)$";
			string dllRE = @".*\/([^\/]*\.dll)$";

			var packageCs = new Regex(packageRe + csRe, RegexOptions.IgnoreCase);
			var packageCslist = new Regex(packageRe + cslistRe, RegexOptions.IgnoreCase);
			var packageDll = new Regex(packageRe + dllRE, RegexOptions.IgnoreCase);

			var cs = new Regex(csRe, RegexOptions.IgnoreCase);
			var cslist = new Regex(cslistRe, RegexOptions.IgnoreCase);
			var dll = new Regex(dllRE, RegexOptions.IgnoreCase);

			packageRegexes_ = new Regex[]
			{
				packageCs, packageCslist, packageDll
			};

			fileRegexes_ = new Regex[]
			{
				cs, cslist, dll
			};
		}

		public override string Description
		{
			get
			{
				return
					"Adds a recent plugins list to the Plugins UI.";
			}
		}

		protected override void DoInit()
		{
		}

		protected override void DoUpdate(float s)
		{
			CheckDeferred(s);
			CheckChanged(s);
		}

		private void CheckDeferred(float s)
		{
			if (deferred_ == null)
				return;

			deferredElapsed_ += s;
			if (deferredElapsed_ >= DeferredCheckInterval)
			{
				deferredElapsed_ = 0;

				bool okay = true;

				for (int i = 0; i < deferred_.Length; ++i)
				{
					if (deferred_[i] == null)
						continue;

					if (deferred_[i].Enable())
					{
						Log.Info($"deferred atom {deferred_[i]} now okay");
						atoms_.Add(deferred_[i]);
						deferred_[i] = null;
					}
					else
					{
						okay = false;
					}
				}

				if (okay)
				{
					Log.Info($"all deferred atoms okay");
					deferred_ = null;
				}
			}
		}

		private void CheckChanged(float s)
		{
			changedElapsed_ += s;
			if (changedElapsed_ >= ChangedCheckInterval)
			{
				changedElapsed_ = 0;

				for (int i = 0; i < atoms_.Count; ++i)
					atoms_[i].CheckChanged();
			}
		}

		public void AddRecent(AtomInfo ai, AtomInfo.Plugin p)
		{
			if (p.lastUrl == "")
				return;

			var list = ai.GetRecentPlugins();

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] == p.lastUrl)
					return;
			}

			Log.Info($"{ai} new recent plugin: {p.lastUrl}");
			list.Insert(0, p.lastUrl);

			ai.SaveRecentPlugins(list);
		}

		public void RemoveRecent(AtomInfo ai, string s)
		{
			if (string.IsNullOrEmpty(s))
				return;

			var list = ai.GetRecentPlugins();

			list.RemoveAll((i) => (i == s));
			Log.Info($"{ai} removed {s}");

			ai.SaveRecentPlugins(list);
		}

		public string GetPluginDisplayName(string p)
		{
			for (int i = 0; i < packageRegexes_.Length; ++i)
			{
				var m = packageRegexes_[i].Match(p);
				if (m != null && m.Success)
				{
					if (m.Groups.Count == 2)
						return m.Groups[1].Value;
					else if (m.Groups.Count == 3)
						return m.Groups[1] + ":" + m.Groups[2];

					break;
				}
			}

			for (int i = 0; i < fileRegexes_.Length; ++i)
			{
				var m = fileRegexes_[i].Match(p);
				if (m != null && m.Success)
				{
					if (m.Groups.Count == 2)
						return m.Groups[1].Value;

					break;
				}
			}

			Log.Error($"can't parse plugin path '{p}'");
			return p;
		}

		private List<Atom> GetAtoms()
		{
			var list = new List<Atom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.uid == "[CameraRig]")
					continue;

				list.Add(a);
			}

			return list;
		}

		protected override void DoEnable()
		{
			var deferred = new List<AtomInfo>();

			foreach (var a in GetAtoms())
			{
				var ai = new AtomInfo(this, a);

				if (ai.Enable())
				{
					atoms_.Add(ai);
				}
				else
				{
					Log.Error($"{ai}: deferring");
					deferred.Add(ai);
				}
			}

			if (deferred.Count > 0)
				deferred_ = deferred.ToArray();
		}

		protected override void DoDisable()
		{
			foreach (var a in atoms_)
				a.Disable();
		}
	}
}
