using UnityEngine;

namespace AUI.CuaUI
{
	class CuaAtomInfo : MRUAtomInfo
	{
		private const string RecentFile = "aui.cua.recent.json";
		private const string ListName = "aui.cua.recent";

		private CustomUnityAssetLoader cua_ = null;
		private CustomUnityAssetLoaderUI ui_ = null;
		private JSONStorableUrl url_ = null;

		public CuaAtomInfo(CuaUIModifier uiMod, Atom a)
			: base(uiMod, a)
		{
		}

		public override bool Enable()
		{
			if (!GetInfo())
				return false;

			cua_.RegisterAssetLoadedCallback(OnAssetLoaded);

			DestroyUI();
			CreateUI();

			return true;
		}

		public override void Disable()
		{
			if (cua_ != null)
				cua_.DeregisterAssetLoadedCallback(OnAssetLoaded);

			base.Disable();
		}

		public override bool IsLike(BasicAtomUIInfo other)
		{
			return true;
		}

		protected override string GetRecentFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(RecentFile);
		}

		protected override string MakeDisplayValue(string entry)
		{
			return U.PrettyFilename(entry);
		}

		protected override bool OnSelected(string entry)
		{
			url_.val = entry;
			return true;
		}

		private bool GetInfo()
		{
			if (Atom.UITransform == null)
			{
				Log.Verbose($"no UITransform");
				return false;
			}

			cua_ = Atom.GetComponentInChildren<CustomUnityAssetLoader>();
			if (cua_ == null)
			{
				Log.Verbose($"no CustomUnityAssetLoader");
				return false;
			}

			url_ = cua_.GetUrlJSONParam("assetUrl");
			if (url_ == null)
			{
				Log.Error($"no assetUrl parameter");
				return false;
			}

			ui_ = Atom.UITransform.GetComponentInChildren<CustomUnityAssetLoaderUI>();
			if (ui_ == null)
			{
				Log.Verbose($"no CustomUnityAssetLoaderUI");
				return false;
			}

			if (ui_.fileBrowseButton == null)
			{
				Log.Verbose($"no fileBrowseButton");
				return false;
			}

			if (ui_.clearButton == null)
			{
				Log.Verbose($"no clearButton");
				return false;
			}

			DestroyUI();
			CreateUI();

			return true;
		}

		private Transform GetParent()
		{
			return ui_.fileBrowseButton.transform.parent;
		}

		private void CreateUI()
		{
			RectTransform rt = CreateUI(
				GetParent(), ListName,
				AlternateUI.Instance.Sys.GetPluginManager().configurablePopupPrefab);

			GetParent().SetAsLastSibling();
			rt.GetComponent<UIDynamicPopup>().popup.popupPanel.transform.SetAsFirstSibling();
			rt.GetComponent<UIDynamicPopup>().popup.buttonParent?.transform?.SetAsFirstSibling();

			rt.transform.SetAsFirstSibling();

			var ce = ui_.fileBrowseButton.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x + 250, rt.offsetMin.y - 5);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 570, rt.offsetMax.y + 5);
		}

		private void OnAssetLoaded()
		{
			AddRecentEntry(url_.val);
			UpdateList();
		}
	}


	class CuaUIModifier : MRUAtomUIModifier
	{
		private readonly CuaUI parent_;

		public CuaUIModifier(CuaUI parent)
			: base("aui.cua")
		{
			parent_ = parent;
		}

		public override void Update(float s)
		{
			base.Update(s);
		}

		protected override bool ValidAtom(Atom a)
		{
			return (a.type == "CustomUnityAsset");
		}

		protected override BasicAtomUIInfo CreateAtomInfo(Atom a)
		{
			return new CuaAtomInfo(this, a);
		}
	}


	class CuaUI : TweakFeature
	{
		private readonly CuaUIModifier uiMod_;

		public CuaUI()
			: base("cua", "Custom Unity Asset UI", true)
		{
			uiMod_ = new CuaUIModifier(this);
		}

		public override string Description
		{
			get
			{
				return
					"Adds a recent list to the Custom Unity Asset UI.";
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
