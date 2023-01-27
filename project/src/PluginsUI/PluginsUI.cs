using SimpleJSON;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AUI.PluginsUI
{
	class PluginsUI : BasicAlternateUI
	{
		class Plugin
		{
			public readonly MVRPluginUI ui;
			public string lastUrl;

			public Plugin(MVRPluginUI ui)
			{
				this.ui = ui;
				this.lastUrl = ui.urlText.text;
			}
		}

		class AtomInfo
		{
			public readonly Atom a;
			public MVRPluginManager pm = null;
			public MVRPluginManagerUI ui = null;
			public List<Plugin> plugins = new List<Plugin>();

			public AtomInfo(Atom a)
			{
				this.a = a;
			}

			public bool Valid
			{
				get { return (ui != null); }
			}
		}

		private const float DeferredCheckInterval = 1;
		private const float ChangedCheckInterval = 1;
		private const string ListName = "aui.plugins.recent";
		private const string RecentFileFormat = "Custom/PluginData/aui.plugins.recent.{0}.json";

		private AtomInfo[] deferred_ = null;
		private readonly List<AtomInfo> atoms_ = new List<AtomInfo>();
		private float deferredElapsed_ = 0;
		private float changedElapsed_ = 0;


		public PluginsUI()
			: base("plugins", "Plugins UI", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Adds a recent plugins list to the Plugins UI.";
			}
		}

		private string GetRecentFile(Atom a)
		{
			return string.Format(RecentFileFormat, a.type);
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

					if (EnableForAtom(deferred_[i], false))
					{
						Log.Info($"deferred atom {deferred_[i].a.uid} now okay");
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
				{
					var panel = atoms_[i].pm.pluginListPanel;
					var childCount = panel.childCount;

					if (childCount != atoms_[i].plugins.Count)
					{
						Log.Info($"{atoms_[i].a.uid} changed");

						var newList = new List<Plugin>();

						for (int j = 0; j < childCount; ++j)
						{
							var ui = panel.GetChild(j)?.GetComponent<MVRPluginUI>();
							if (ui == null)
							{
								Log.Info("new plugin as no ui");
								continue;
							}

							newList.Add(new Plugin(ui));
						}

						AddRecentDiff(atoms_[i], atoms_[i].plugins, newList);
						atoms_[i].plugins = newList;
					}
					else
					{
						for (int j = 0; j < atoms_[i].plugins.Count; ++j)
						{
							var p = atoms_[i].plugins[j];

							if (p.ui.urlText.text != p.lastUrl)
							{
								p.lastUrl = p.ui.urlText.text;
								AddRecent(atoms_[i], p);
							}
						}
					}
				}
			}
		}

		private void AddRecentDiff(AtomInfo ai, List<Plugin> oldList, List<Plugin> newList)
		{
			for (int i = 0; i < newList.Count; ++i)
			{
				if (newList[i].lastUrl == "")
					continue;

				bool found = false;

				for (int j = 0; j < oldList.Count; ++j)
				{
					if (oldList[j].lastUrl == newList[i].lastUrl)
					{
						found = true;
						break;
					}
				}

				if (!found)
					AddRecent(ai, newList[i]);
			}
		}

		private void AddRecent(AtomInfo ai, Plugin p)
		{
			if (p.lastUrl == "")
				return;

			Log.Info($"{ai.a.type} new recent plugin: {p.lastUrl}");

			var list = new List<string>(PluginsForAtom(ai.a));

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] == p.lastUrl)
					return;
			}

			list.Insert(0, p.lastUrl);

			var j = new JSONClass();

			var a = new JSONArray();
			for (int i = 0; i < list.Count; ++i)
				a.Add(new JSONData(list[i]));

			j.Add("recent", a);

			SuperController.singleton.SaveJSON(j, GetRecentFile(ai.a));
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
				var i = GetAtomInfo(a);

				if (!i.Valid)
				{
					Log.Error($"{i.a.uid}: deferring");
					deferred.Add(i);
					continue;
				}

				EnableForAtom(i);
			}

			if (deferred.Count > 0)
				deferred_ = deferred.ToArray();
		}

		private AtomInfo GetAtomInfo(Atom a, bool log=true)
		{
			var i = new AtomInfo(a);

			i.pm = a.GetStorableByID("PluginManager") as MVRPluginManager;
			if (i.pm == null)
			{
				if (log)
					Log.Error($"{a.uid}: no PluginManager");

				return i;
			}

			if (i.pm.UITransform == null)
			{
				if (log)
					Log.Error($"{a.uid}: no UITransform");

				return i;
			}

			i.ui = i.pm.UITransform.GetComponentInChildren<MVRPluginManagerUI>();
			if (i.ui == null)
			{
				if (log)
					Log.Error($"{a.uid} no MVRPluginManagerUI");

				return i;
			}

			return i;
		}

		private bool EnableForAtom(AtomInfo i, bool log=true)
		{
			Remove(i);
			Add(i);

			atoms_.Add(i);

			return true;
		}

		protected override void DoDisable()
		{
			foreach (var a in GetAtoms())
			{
				var i = GetAtomInfo(a);

				if (!i.Valid)
					continue;

				Remove(i);
			}
		}

		private void Add(AtomInfo i)
		{
			Log.Info($"{i.a.uid} {i.a.type}");

			foreach (Transform pp in i.ui.pluginListPanel)
			{
				var ui = pp.GetComponent<MVRPluginUI>();
				if (ui == null)
				{
					Log.Info("plugin as no ui");
					continue;
				}

				i.plugins.Add(new Plugin(ui));
			}

			var parent = i.ui.addPluginButton.transform.parent;

			var o = UnityEngine.Object.Instantiate(i.pm.configurablePopupPrefab);

			o.name = ListName;

			var p = o.GetComponent<UIDynamicPopup>();
			p.label = "Add recent";
			p.popup.onValueChangeHandlers += (s) =>
			{
				OnRecentSelection(i, s);
				p.popup.currentValueNoCallback = "";
			};

			o.transform.SetParent(parent, false);

			var ce = i.ui.addPluginButton.GetComponent<RectTransform>();
			var rt = o.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x + 220, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 850, rt.offsetMax.y);

			Fill(i.a, p.popup);
		}

		private void OnRecentSelection(AtomInfo ai, string s)
		{
			if (string.IsNullOrEmpty(s.Trim()))
				return;

			Log.Info(s);

			var p = ai.pm.CreatePlugin();
			p.pluginURLJSON.val = s;
		}

		private void Remove(AtomInfo i)
		{
			var parent = i.ui.addPluginButton.transform.parent;

			foreach (Transform t in parent)
			{
				if (t.name.StartsWith(ListName))
					UnityEngine.Object.Destroy(t.gameObject);
			}
		}

		private void Fill(Atom a, UIPopup list)
		{
			var ps = PluginsForAtom(a);

			Log.Info($"{ps.Length}");

			list.useDifferentDisplayValues = true;

			if (ps.Length == 0)
			{
				list.numPopupValues = 1;
				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");
			}
			else
			{
				list.numPopupValues = ps.Length + 1;

				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");

				for (int i = 0; i < ps.Length; ++i)
				{
					list.setPopupValue(i + 1, ps[i]);
					list.setDisplayPopupValue(i + 1, GetDisplayName(ps[i]));
				}
			}

			list.currentValue = list.popupValues[0];
		}

		private string GetDisplayName(string p)
		{
			string packageRe = @"\w+\.(\w+)\.\d+:\/";
			string csRe = @".*\/([^\/]*\.cs)";
			string cslistRe = @".*\/([^\/]*\.cslist)";
			string dllRE = @".*\/([^\/]*\.dll)";

			var packageCs = new Regex(packageRe + csRe);
			var packageCslist = new Regex(packageRe + cslistRe);
			var packageDll = new Regex(packageRe + dllRE);

			var cs = new Regex(csRe);
			var cslist = new Regex(cslistRe);
			var dll = new Regex(dllRE);

			var packageRes = new Regex[]
			{
				packageCs, packageCslist, packageDll
			};

			var fileRes = new Regex[]
			{
				cs, cslist, dll
			};

			for (int i = 0; i < packageRes.Length; ++i)
			{
				var m = packageRes[i].Match(p);
				if (m != null)
				{
					if (m.Groups.Count == 2)
						return m.Groups[1].Value;
					else if (m.Groups.Count == 3)
						return m.Groups[1] + ":" + m.Groups[2];

					break;
				}
			}

			for (int i = 0; i < fileRes.Length; ++i)
			{
				var m = fileRes[i].Match(p);
				if (m != null)
				{
					if (m.Groups.Count == 2)
						return m.Groups[1].Value;

					break;
				}
			}

			Log.Error($"can't parse plugin path '{p}'");
			return p;
		}

		private string[] PluginsForAtom(Atom a)
		{
			var list = new List<string>();

			var j = SuperController.singleton.LoadJSON(GetRecentFile(a));

			var o = j?.AsObject;
			if (o != null)
			{
				if (o.HasKey("recent"))
				{
					var r = o["recent"].AsArray;
					if (r != null)
					{
						foreach (var n in r.Childs)
							list.Add(n.Value);
					}
				}
			}

			return list.ToArray();
		}
	}
}
