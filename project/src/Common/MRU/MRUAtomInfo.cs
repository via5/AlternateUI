using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace AUI
{
	abstract class MRUAtomInfo : BasicAtomUIInfo
	{
		class AUIMRUMouseCallbacks : MonoBehaviour, IPointerClickHandler
		{
			private MRUAtomInfo parent_ = null;
			private int index_ = -1;

			public void Set(MRUAtomInfo m, int index)
			{
				parent_ = m;
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


		private const int MaxRecent = 30;

		private readonly MRUAtomUIModifier uiMod_;
		private readonly Logger log_;
		private Transform parent_ = null;
		private UIDynamicPopup popup_ = null;
		private bool stale_ = true;


		protected abstract string GetRecentFile();
		protected abstract string MakeDisplayValue(string entry);
		protected abstract bool OnSelected(string entry);

		public MRUAtomInfo(MRUAtomUIModifier uiMod, Atom a)
			: base(a)
		{
			uiMod_ = uiMod;
			log_ = new Logger(uiMod_.Log.Prefix + "." + Atom.uid);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public List<string> GetRecentEntries()
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

		public void SaveRecentEntries(List<string> list)
		{
			var j = new JSONClass();

			var a = new JSONArray();
			for (int i = 0; i < list.Count; ++i)
				a.Add(new JSONData(list[i]));

			j.Add("recent", a);

			SuperController.singleton.SaveJSON(j, GetRecentFile());
		}

		public void MakeStale()
		{
			stale_ = true;
		}

		public override void Disable()
		{
			DestroyUI();
		}

		public RectTransform CreateUI(Transform parent, string name, Transform prefab)
		{
			parent_ = parent;

			var o = UnityEngine.Object.Instantiate(prefab);

			o.name = name;

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

			o.transform.SetParent(parent, false);
			UpdateList();

			return o.GetComponent<RectTransform>();
		}

		public void DestroyUI()
		{
			if (popup_ == null)
				return;

			foreach (Transform t in parent_)
			{
				if (t.name.StartsWith(popup_.name))
					UnityEngine.Object.Destroy(t.gameObject);
			}
		}

		public void AddRecentEntry(string s)
		{
			if (string.IsNullOrEmpty(s))
				return;

			var list = GetRecentEntries();

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] == s)
					return;
			}

			Log.Verbose($"{this} new recent plugin: {s}");
			list.Insert(0, s);

			while (list.Count > MaxRecent)
				list.RemoveAt(list.Count - 1);

			SaveRecentEntries(list);
			uiMod_.UpdateOthers(this);
		}

		public void RemoveRecentEntry(string s)
		{
			var list = GetRecentEntries();

			var i = list.IndexOf(s);
			if (i >= 0 && i < list.Count)
				DoRemoveEntry(list, i);
		}

		private void RemoveRecentEntry(int index)
		{
			var list = GetRecentEntries();
			DoRemoveEntry(list, index);
		}

		private void DoRemoveEntry(List<string> list, int index)
		{
			if (index < 0 || index >= list.Count)
			{
				Log.Error($"can't remove recent entry {index}, out of bounds, n={list.Count}");
				return;
			}

			Log.Verbose($"removed {list[index]}");
			list.RemoveAt(index);

			SaveRecentEntries(list);
			uiMod_.UpdateOthers(this);

			UpdateList();
		}

		protected void UpdateList()
		{
			var list = popup_.popup;
			var values = GetRecentEntries();

			RemoveHandlers();

			list.useDifferentDisplayValues = true;

			if (values.Count == 0)
			{
				list.numPopupValues = 1;
				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");
			}
			else
			{
				list.numPopupValues = values.Count + 1;

				list.setPopupValue(0, "");
				list.setDisplayPopupValue(0, "");

				for (int i = 0; i < values.Count; ++i)
				{
					list.setPopupValue(i + 1, values[i]);
					list.setDisplayPopupValue(i + 1, MakeDisplayValue(values[i]));
				}
			}

			list.currentValue = list.popupValues[0];

			AlternateUI.Instance.StartCoroutine(CoAddHandlers());
		}

		private void OnRightClick(int index)
		{
			var list = popup_.popup;

			if (index < 0 || index >= list.numPopupValues)
			{
				Log.Error($"bad index {index}");
				return;
			}

			// skip the first blank
			RemoveRecentEntry(index - 1);
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
					if (c.ToString().Contains("AUIMRUMouseCallbacks"))
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
				var c = b.gameObject.AddComponent<AUIMRUMouseCallbacks>();
				if (c == null)
				{
					//Log.Error("failed to add mouse callback");
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

			bool b = OnSelected(s);

			if (!b)
				RemoveRecentEntry(s);
		}
	}
}
