using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AUI.PluginsUI
{
	class AtomInfo
	{
		class AUIPluginsUIMouseCallbacks : MonoBehaviour, IPointerClickHandler
		{
			private AtomInfo parent_ = null;
			private int index_ = -1;

			public void Set(AtomInfo p, int index)
			{
				parent_ = p;
				index_ = index;
			}

			public void OnPointerClick(PointerEventData d)
			{
				try
				{
					if (parent_ == null)
						return;

					if (d.button == PointerEventData.InputButton.Right)
						parent_.OnRightClick(index_);
				}
				catch (Exception e)
				{
					SuperController.LogError(e.ToString());
				}
			}
		}


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

		private readonly PluginsUI pui_;
		private readonly Atom a_;
		private readonly Logger log_;
		private MVRPluginManager pm_ = null;
		private MVRPluginManagerUI ui_ = null;
		private List<Plugin> plugins_ = new List<Plugin>();
		private UIDynamicPopup popup_ = null;
		private bool stale_ = true;

		public AtomInfo(PluginsUI ui, Atom a)
		{
			pui_ = ui;
			a_ = a;
			log_ = new Logger($"aui.plugins[{a.uid}]");
		}

		public Atom Atom
		{
			get { return a_; }
		}

		public Logger Log
		{
			get { return log_; }
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

		public bool IsLike(AtomInfo other)
		{
			return GetCategory(a_) == GetCategory(other.a_);
		}

		public void MakeStale()
		{
			stale_ = true;
		}

		public string GetRecentFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				string.Format(RecentFileFormat, GetCategory(a_)));
		}

		public List<string> GetRecentPlugins()
		{
			var list = new List<string>();

			if (FileManagerSecure.FileExists(GetRecentFile()))
			{
				var j = SuperController.singleton.LoadJSON(GetRecentFile());

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
			}

			return list;
		}

		public void SaveRecentPlugins(List<string> list)
		{
			var j = new JSONClass();

			var a = new JSONArray();
			for (int i = 0; i < list.Count; ++i)
				a.Add(new JSONData(list[i]));

			j.Add("recent", a);

			SuperController.singleton.SaveJSON(j, GetRecentFile());
		}

		public bool Enable()
		{
			if (!GetInfo())
				return false;

			DestroyUI();
			CreateUI();

			return true;
		}

		public void Disable()
		{
			DestroyUI();
		}

		private bool GetInfo(bool log = true)
		{
			pm_ = a_.GetStorableByID("PluginManager") as MVRPluginManager;
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
							AddRecent(p);
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
					AddRecent(newList[i]);
					changed = true;
				}
			}

			return changed;
		}

		private void AddRecent(Plugin p)
		{
			pui_.AddRecent(this, p);
		}

		private void RemoveRecent(string s)
		{
			pui_.RemoveRecent(this, s);
			UpdateList();
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

			var o = UnityEngine.Object.Instantiate(pm_.configurablePopupPrefab);

			o.name = ListName;

			popup_ = o.GetComponent<UIDynamicPopup>();
			popup_.label = "Add recent:";
			popup_.popup.onOpenPopupHandlers += () =>
			{
				if (stale_)
				{
					stale_ = false;
					UpdateList();
				}
			};
			popup_.popup.onValueChangeHandlers += (s) =>
			{
				OnRecentSelection(s);
				popup_.popup.currentValueNoCallback = "";
			};

			o.transform.SetParent(GetParent(), false);
			o.transform.SetSiblingIndex(ui_.addPluginButton.transform.GetSiblingIndex());

			var ce = ui_.addPluginButton.GetComponent<RectTransform>();
			var rt = o.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x + 220, rt.offsetMin.y - 5);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 850, rt.offsetMax.y + 5);

			UpdateList();
		}

		private void DestroyUI()
		{
			foreach (Transform t in GetParent())
			{
				if (t.name.StartsWith(ListName))
					UnityEngine.Object.Destroy(t.gameObject);
			}
		}

		private void UpdateList()
		{
			var ps = GetRecentPlugins();
			var list = popup_.popup;

			RemoveHandlers();

			list.useDifferentDisplayValues = true;

			if (ps.Count == 0)
			{
				list.numPopupValues = 1;
				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");
			}
			else
			{
				list.numPopupValues = ps.Count + 1;

				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");

				for (int i = 0; i < ps.Count; ++i)
				{
					list.setPopupValue(i + 1, ps[i]);
					list.setDisplayPopupValue(i + 1, pui_.GetPluginDisplayName(ps[i]));
				}
			}

			list.currentValue = list.popupValues[0];

			AlternateUI.Instance.StartCoroutine(CoAddHandlers());
		}

		private void OnRightClick(int index)
		{
			var list = popup_.popup;

			if (index < 0 || index >= list.numPopupValues)
				return;

			RemoveRecent(list.popupValues[index]);
		}

		private Transform PopupParent
		{
			get
			{
				if (popup_.popup.buttonParent != null)
					return popup_.popup.buttonParent;
				else
					return popup_.popup.popupPanel;
			}
		}

		private void RemoveHandlers()
		{
			foreach (var b in PopupParent.GetComponentsInChildren<Button>())
			{
				foreach (var c in b.gameObject.GetComponents<Component>())
				{
					if (c.ToString().Contains("AUIPluginsUIMouseCallbacks"))
						UnityEngine.Object.Destroy(c);
				}
			}
		}

		private System.Collections.IEnumerator CoAddHandlers()
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			AddHandlers();
		}

		private void AddHandlers()
		{
			int i = 0;

			foreach (var b in PopupParent.GetComponentsInChildren<Button>())
			{
				var c = b.gameObject.AddComponent<AUIPluginsUIMouseCallbacks>();
				if (c == null)
				{
					Log.Error("failed to add mouse callback");
					continue;
				}

				c.Set(this, i);
				++i;
			}
		}

		private void OnRecentSelection(string s)
		{
			if (string.IsNullOrEmpty(s.Trim()))
				return;

			Log.Verbose(s);

			var p = pm_.CreatePlugin();
			p.pluginURLJSON.val = s;

			if (p.scriptControllers.Count == 0)
			{
				Log.Error("failed to load plugin, removing from recent list");
				RemoveRecent(s);
			}
		}

		public override string ToString()
		{
			return $"{GetCategory(a_)}.{a_.uid}";
		}
	}
}
