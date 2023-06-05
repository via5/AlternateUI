using SimpleJSON;
using System;
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
		public const int UIReplacement = 0;
		public const int UIChange = 1;
		public const int Tweak = 2;

		private readonly string name_, displayName_;
		private readonly int type_;
		private readonly Logger log_;
		private JSONStorableBool enabledParam_;
		private bool enabled_;
		private bool failed_ = false;

		protected BasicFeature(
			string name, int type, string displayName, bool defaultEnabled)
		{
			name_ = name;
			displayName_ = displayName;
			type_ = type;
			log_ = new Logger("aui." + name_);
			enabled_ = defaultEnabled;

			if (U.DevMode)
				enabled_ = true;

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
				new HairUI.HairUI(),
				new FileDialog.FileDialogFeature(),
				new PluginsUI.PluginsUI(),
				new LightUI.LightUI(),
				new SelectUI.SelectUI(),
				new AddAtomUI.AddAtomUI(),
				new SkinUI.RightClickSkinReload(),
				new SkinUI.SkinMaterialsReset(),
				new LogUI.LogUI(),
				new CuaUI.CuaUI(),

				new Tweaks.EscapeDialogs(),
				new Tweaks.DoubleClickFocus(),
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
				new Tweaks.LoadingIndicator(),
				new Tweaks.SelectAtomOnAdd(),
				new Tweaks.FocusAtomOnAdd()
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

		public int FeatureType
		{
			get { return type_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		private bool CanRun
		{
			get { return enabled_ && !failed_; }
		}

		public abstract string Description { get; }

		public void LoadOptions(JSONClass o)
		{
			if (o.HasKey("enabled"))
			{
				enabled_ = o["enabled"].AsBool;
				enabledParam_.valNoCallback = enabled_;
			}

			if (U.DevMode)
				enabled_ = true;

			try
			{
				DoLoadOptions(o);
			}
			catch (Exception e)
			{
				failed_ = true;
				Log.Error("exception in DoLoadOptions, disabling");
				Log.Error(e.ToString());
			}
		}

		public JSONClass SaveOptions()
		{
			var o = new JSONClass();

			o.Add("enabled", new JSONData(enabled_));

			try
			{
				DoSaveOptions(o);
			}
			catch (Exception e)
			{
				failed_ = true;
				Log.Error("exception in DoSaveOptions, disabling");
				Log.Error(e.ToString());
			}

			return o;
		}

		public void Init()
		{
			Log.Verbose($"init {Name}");

			try
			{
				DoInit();
			}
			catch (Exception e)
			{
				failed_ = true;
				Log.Error("exception in DoInit, disabling");
				Log.Error(e.ToString());
			}

			if (CanRun)
				Enable();
		}

		public void OnPluginState(bool b)
		{
			if (!CanRun)
				return;

			if (b)
				Enable();
			else
				Disable();
		}

		private void Enable()
		{
			Log.Verbose($"enable {Name}");

			try
			{
				DoEnable();
			}
			catch (Exception e)
			{
				failed_ = true;
				Log.Error("exception in DoEnable, disabling");
				Log.Error(e.ToString());
			}
		}

		private void Disable()
		{
			Log.Verbose($"disable {Name}");

			try
			{
				DoDisable();
			}
			catch (Exception e)
			{
				failed_ = true;
				Log.Error("exception in DoDisable, disabling");
				Log.Error(e.ToString());
			}
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
		}

		public void Update(float s)
		{
			if (CanRun)
			{
				try
				{
					DoUpdate(s);
				}
				catch (Exception e)
				{
					failed_ = true;
					Log.Error("exception in DoCreateUI, disabling");
					Log.Error(e.ToString());
				}
			}
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


	abstract class UIReplacementFeature : BasicFeature
	{
		protected UIReplacementFeature(
			string name, string displayName, bool defaultEnabled)
				: base(name, UIReplacement, displayName, defaultEnabled)
		{
		}
	}


	abstract class TweakFeature : BasicFeature
	{
		protected TweakFeature(
			string name, string displayName, bool defaultEnabled)
				: base(name, Tweak, displayName, defaultEnabled)
		{
		}
	}
}
