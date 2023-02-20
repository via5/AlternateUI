using SimpleJSON;
using UnityEngine.UI;

namespace AUI
{
	interface IFeature
	{
		string Name { get; }
		string DisplayName { get; }
		string Description { get; }

		void LoadOptions(JSONClass o);
		JSONClass SaveOptions();
		void Init();
		void CreateUI();
		void Update(float s);
	}


	public abstract class BasicFeature : IFeature
	{
		private readonly string name_, displayName_;
		private readonly Logger log_;
		private JSONStorableBool enabledParam_;
		private bool enabled_;

		protected BasicFeature(string name, string displayName, bool defaultEnabled)
		{
			name_ = name;
			displayName_ = displayName;
			log_ = new Logger("aui." + name_);
			enabled_ = defaultEnabled;

			enabledParam_ = new JSONStorableBool($"{name_}.enabled", enabled_, (bool b) =>
			{
				if (enabled_ != enabledParam_.val)
				{
					Log.Verbose($"enabled changed to {enabledParam_.val}");

					enabled_ = enabledParam_.val;

					if (enabled_)
						Enable();
					else
						Disable();

					AlternateUI.Instance.Save();
				}
			});
		}

		public static BasicFeature[] CreateAllFeatures()
		{
			return new BasicFeature[]
			{
				new MorphUI.MorphUI(),
				new ClothingUI.ClothingUI(),
				new FileDialog.FileDialog(),
				new PluginsUI.PluginsUI(),
				new LightUI.LightUI(),
				new SelectUI.SelectUI(),
				new SkinUI.RightClickSkinReload(),
				new SkinUI.SkinMaterialsReset(),
				new LogUI.LogUI(),
				new CuaUI.CuaUI(),
				new Tweaks.EscapeDialogs(),
				new Tweaks.DisableCuaCollision(),
				new Tweaks.SpaceBarFreeze(),
				new Tweaks.RightClickPackagesReload(),
				new Tweaks.QuickSaveScreenshot(),
				new Tweaks.QuickSave(),
				new Tweaks.HideTargetsInVR(),
				new Tweaks.EditMode(),
				new Tweaks.FocusHead(),
				new Tweaks.DisableLoadPosition(),
				new Tweaks.MoveNewLight(),
			};
		}

		public string Name
		{
			get { return name_; }
		}

		public string DisplayName
		{
			get { return displayName_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public abstract string Description { get; }

		public void LoadOptions(JSONClass o)
		{
			if (o.HasKey("enabled"))
			{
				enabled_ = o["enabled"].AsBool;
				enabledParam_.valNoCallback = enabled_;
			}

			DoLoadOptions(o);
		}

		public JSONClass SaveOptions()
		{
			var o = new JSONClass();

			o.Add("enabled", new JSONData(enabled_));
			DoSaveOptions(o);

			return o;
		}

		public void Init()
		{
			Log.Verbose($"init {Name}");
			DoInit();

			if (enabled_)
				Enable();
		}

		public void OnPluginState(bool b)
		{
			if (!enabled_)
				return;

			if (b)
				Enable();
			else
				Disable();
		}

		private void Enable()
		{
			Log.Verbose($"enable {Name}");
			DoEnable();
		}

		private void Disable()
		{
			Log.Verbose($"disable {Name}");
			DoDisable();
		}

		public void CreateUI()
		{
			var a = AlternateUI.Instance;

			var t = a.CreateToggle(enabledParam_);
			t.labelText.text = $"{DisplayName}";

			var ts = new JSONStorableString("text", Description);
			var tt = a.CreateTextField(ts, true);
			tt.GetComponent<LayoutElement>().minHeight = 150;
			tt.height = 150;

			DoCreateUI();
		}

		protected virtual void DoCreateUI()
		{
			// no-op
		}

		public void Update(float s)
		{
			if (enabled_)
				DoUpdate(s);
		}

		protected virtual void DoLoadOptions(JSONClass o)
		{
			// no-op
		}

		protected virtual void DoSaveOptions(JSONClass o)
		{
			// no-op
		}

		protected virtual void DoInit()
		{
			// no-op
		}

		protected virtual void DoUpdate(float s)
		{
			// no-op
		}

		protected virtual void DoEnable()
		{
			// no-op
		}

		protected virtual void DoDisable()
		{
			// no-op
		}
	}
}
