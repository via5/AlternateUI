using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AUI.PluginsUI
{
	class PluginsUI : BasicFeature
	{
		private const int MaxRecent = 30;

		private const float DeferredCheckInterval = 2;
		private const float ChangedCheckInterval = 2;

		private readonly List<AtomInfo> deferred_ = new List<AtomInfo>();
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
					"Adds a recent plugins list to the Plugins UI for all " +
					"atoms.";
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
			if (deferred_.Count == 0)
				return;

			deferredElapsed_ += s;
			if (deferredElapsed_ >= DeferredCheckInterval)
			{
				deferredElapsed_ = 0;

				bool okay = true;

				for (int i = 0; i < deferred_.Count; ++i)
				{
					if (deferred_[i] == null)
						continue;

					if (deferred_[i].Enable())
					{
						Log.Verbose($"deferred atom {deferred_[i]} now okay");
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
					Log.Verbose($"all deferred atoms okay");
					deferred_.Clear();
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

			Log.Verbose($"{ai} new recent plugin: {p.lastUrl}");
			list.Insert(0, p.lastUrl);

			while (list.Count > MaxRecent)
				list.RemoveAt(list.Count - 1);

			ai.SaveRecentPlugins(list);
			UpdateOthers(ai);
		}

		public void RemoveRecent(AtomInfo ai, string s)
		{
			if (string.IsNullOrEmpty(s))
				return;

			var list = ai.GetRecentPlugins();

			list.RemoveAll((i) => (i == s));
			Log.Verbose($"{ai} removed {s}");

			ai.SaveRecentPlugins(list);
			UpdateOthers(ai);
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

		private void UpdateOthers(AtomInfo like)
		{
			for (int i = 0; i < atoms_.Count; ++i)
			{
				var a = atoms_[i];
				if (a == like)
					continue;

				if (a.IsLike(like))
					a.MakeStale();
			}
		}

		private bool ValidAtom(Atom a)
		{
			if (a.uid == "[CameraRig]")
				return false;

			return true;
		}

		private List<Atom> GetAtoms()
		{
			var list = new List<Atom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (ValidAtom(a))
					list.Add(a);
			}

			return list;
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;

			CheckScene();
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;

			foreach (var a in atoms_)
				a.Disable();

			atoms_.Clear();
			deferred_.Clear();
		}

		private void CheckScene()
		{
			atoms_.Clear();
			deferred_.Clear();

			foreach (var a in GetAtoms())
				TryAddAtom(a);
		}

		private void TryAddAtom(Atom a)
		{
			var ai = new AtomInfo(this, a);

			if (ai.Enable())
			{
				atoms_.Add(ai);
			}
			else
			{
				Log.Verbose($"{ai}: deferring");
				deferred_.Add(ai);
			}
		}

		private void OnAtomAdded(Atom a)
		{
			if (ValidAtom(a))
				TryAddAtom(a);
		}

		private void OnAtomRemoved(Atom a)
		{
			for (int i = 0; i < atoms_.Count; ++i)
			{
				if (atoms_[i].Atom == a)
				{
					atoms_[i].Disable();
					atoms_.RemoveAt(i);
					break;
				}
			}
		}

		private void OnSceneLoaded()
		{
			CheckScene();
		}
	}
}
