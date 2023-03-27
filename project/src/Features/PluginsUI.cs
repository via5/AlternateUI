using System.Collections.Generic;
using UnityEngine;

namespace AUI.PluginsUI
{
	class AtomInfo : MRUAtomInfo
	{
		private const string RecentFileFormat = "aui.plugins.recent.{0}.json";
		private const string ListName = "aui.plugins.recent";

		public class Plugin
		{
			public readonly MVRPluginUI ui;
			public string lastUrl;

			public Plugin(MVRPluginUI ui)
			{
				this.ui = ui;
				this.lastUrl = ui.urlText.text;
			}
		}

		private readonly PluginsUIModifier uiMod_;
		private MVRPluginManager pm_ = null;
		private MVRPluginManagerUI ui_ = null;
		private List<Plugin> plugins_ = new List<Plugin>();

		public AtomInfo(PluginsUIModifier uiMod, Atom a)
			: base(uiMod, a)
		{
			uiMod_ = uiMod;
		}

		public bool Valid
		{
			get { return (ui_ != null); }
		}

		private static string GetCategory(Atom a)
		{
			string cat = a.category.Trim();
			string s = "";

			for (int i = 0; i < cat.Length; ++i)
			{
				if (char.IsLetterOrDigit(cat[i]))
					s += cat[i];
			}

			if (s == "")
				s = "None";

			return s;
		}

		public override bool IsLike(BasicAtomUIInfo other)
		{
			return GetCategory(Atom) == GetCategory(other.Atom);
		}

		protected override string GetRecentFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				string.Format(RecentFileFormat, GetCategory(Atom)));
		}

		protected override bool OnSelected(string s)
		{
			Log.Verbose(s);

			var p = pm_.CreatePlugin();
			p.pluginURLJSON.val = s;

			if (p.scriptControllers.Count == 0)
			{
				Log.Error("failed to load plugin, removing from recent list");
				return false;
			}

			return true;
		}

		protected override string MakeDisplayValue(string p)
		{
			return U.PrettyFilename(p);
		}

		public override bool Enable()
		{
			if (!GetInfo())
				return false;

			DestroyUI();
			CreateUI();

			return true;
		}

		private bool GetInfo(bool log = true)
		{
			pm_ = Atom.GetStorableByID("PluginManager") as MVRPluginManager;
			if (pm_ == null)
			{
				//if (log)
				//	Log.Verbose($"{this}: no PluginManager");

				return false;
			}

			if (pm_.UITransform == null)
			{
				//if (log)
				//	Log.Verbose($"{this}: no UITransform");

				return false;
			}

			ui_ = pm_.UITransform.GetComponentInChildren<MVRPluginManagerUI>();
			if (ui_ == null)
			{
				//if (log)
				//	Log.Verbose($"{this} no MVRPluginManagerUI");

				return false;
			}

			return true;
		}

		public void CheckChanged()
		{
			var panel = pm_.pluginListPanel;
			var childCount = panel.childCount;
			bool changed = false;

			if (childCount != plugins_.Count)
			{
				var newList = new List<Plugin>();

				for (int j = 0; j < childCount; ++j)
				{
					var ui = panel.GetChild(j)?.GetComponent<MVRPluginUI>();
					if (ui == null)
					{
						Log.Info("new plugin has no ui");
						continue;
					}

					if (ValidPlugin(ui))
						newList.Add(new Plugin(ui));
				}

				if (AddRecentDiff(newList))
					changed = true;

				plugins_ = newList;
			}
			else
			{
				for (int j = 0; j < plugins_.Count; ++j)
				{
					var p = plugins_[j];

					if (p.ui.urlText.text != p.lastUrl)
					{
						if (ValidPlugin(p.ui))
						{
							p.lastUrl = p.ui.urlText.text;
							AddRecentEntry(p.lastUrl);
							changed = true;
						}
					}
				}
			}

			if (changed)
				UpdateList();
		}

		private bool ValidPlugin(MVRPluginUI ui)
		{
			// plugins that failed to load don't have a MVRScriptControllerUI
			//
			// this is important because loading a plugin from the mru that's
			// been deleted will be removed from the list in
			// OnRecentSelection(), but without this check, it'll be picked back
			// up immediately by CheckChanged() because the url has changed
			//
			// so if the plugin changed but doesn't have a
			// MVRScriptControllerUI, consider it failed and don't add it to the
			// mru

			var scui = ui.GetComponentInChildren<MVRScriptControllerUI>();
			return (scui != null);
		}

		private bool AddRecentDiff(List<Plugin> newList)
		{
			bool changed = false;

			for (int i = 0; i < newList.Count; ++i)
			{
				if (newList[i].lastUrl == "")
					continue;

				bool found = false;

				for (int j = 0; j < plugins_.Count; ++j)
				{
					if (plugins_[j].lastUrl == newList[i].lastUrl)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					AddRecentEntry(newList[i].lastUrl);
					changed = true;
				}
			}

			return changed;
		}

		private Transform GetParent()
		{
			return ui_.addPluginButton.transform.parent;
		}

		private void CreateUI()
		{
			// the popup list is below the panel in some places like
			// ControlCore
			ui_.pluginListPanel.transform.parent.parent.SetAsFirstSibling();

			foreach (Transform pp in ui_.pluginListPanel)
			{
				var ui = pp.GetComponent<MVRPluginUI>();
				if (ui == null)
				{
					Log.Error("plugin has no ui");
					continue;
				}

				plugins_.Add(new Plugin(ui));
			}

			RectTransform rt = CreateUI(GetParent(), ListName, pm_.configurablePopupPrefab);

			rt.transform.SetSiblingIndex(ui_.addPluginButton.transform.GetSiblingIndex());

			var ce = ui_.addPluginButton.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x + 220, rt.offsetMin.y - 5);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 850, rt.offsetMax.y + 5);
		}

		public override string ToString()
		{
			return $"{GetCategory(Atom)}.{Atom.uid}";
		}
	}


	class PluginsUIModifier : MRUAtomUIModifier
	{
		private const float ChangedCheckInterval = 1;

		private readonly PluginsUI parent_;
		private float changedElapsed_ = 0;

		public PluginsUIModifier(PluginsUI parent)
			: base("aui.plugins")
		{
			parent_ = parent;
		}

		public override void Update(float s)
		{
			base.Update(s);
			CheckChanged(s);
		}

		private void CheckChanged(float s)
		{
			changedElapsed_ += s;
			if (changedElapsed_ >= ChangedCheckInterval)
			{
				changedElapsed_ = 0;

				for (int i = 0; i < Atoms.Count; ++i)
					(Atoms[i] as AtomInfo).CheckChanged();
			}
		}

		protected override bool ValidAtom(Atom a)
		{
			if (a.uid == "[CameraRig]")
				return false;

			return true;
		}

		protected override BasicAtomUIInfo CreateAtomInfo(Atom a)
		{
			return new AtomInfo(this, a);
		}
	}


	class PluginsUI : TweakFeature
	{
		private readonly PluginsUIModifier uiMod_;

		public PluginsUI()
			: base("plugins", "Plugins UI", true)
		{
			uiMod_ = new PluginsUIModifier(this);
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

		protected override void DoEnable()
		{
			uiMod_.Enable();
		}

		protected override void DoDisable()
		{
			uiMod_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			base.DoUpdate(s);
			uiMod_.Update(s);
		}
	}
}
