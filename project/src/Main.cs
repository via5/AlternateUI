using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AUI
{
	interface IAlternateUI
	{
		string Name { get; }
		string DisplayName { get; }
		string Description { get; }

		void Load(JSONClass o);
		JSONClass Save();
		void Init();
		void CreateUI();
		void Update(float s);
	}


	public abstract class BasicAlternateUI : IAlternateUI
	{
		private readonly string name_, displayName_;
		private readonly Logger log_;
		private JSONStorableBool enabledParam_;
		private bool enabled_;

		protected BasicAlternateUI(string name, string displayName, bool defaultEnabled)
		{
			name_ = name;
			displayName_ = displayName;
			log_ = new Logger("aui." + name_);
			enabled_ = defaultEnabled;

			enabledParam_ = new JSONStorableBool($"{name_}.enabled", enabled_, (bool b) =>
			{
				if (enabled_ != enabledParam_.val)
				{
					Log.Info($"{Name}: enabled changed to {enabledParam_.val}");

					enabled_ = enabledParam_.val;

					if (enabled_)
						Enable();
					else
						Disable();

					AlternateUI.Instance.Save();
				}
			});
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

		public void Load(JSONClass o)
		{
			if (o.HasKey("enabled"))
			{
				enabled_ = o["enabled"].AsBool;
				enabledParam_.valNoCallback = enabled_;
			}

			DoLoad(o);
		}

		public JSONClass Save()
		{
			var o = new JSONClass();

			o.Add("enabled", new JSONData(enabled_));
			DoSave(o);

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
			Log.Info($"enable {Name}");
			DoEnable();
		}

		private void Disable()
		{
			Log.Info($"disable {Name}");
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

		protected virtual void DoLoad(JSONClass o)
		{
			// no-op
		}

		protected virtual JSONClass DoSave(JSONClass o)
		{
			// no-op
			return null;
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


	class AlternateUI : MVRScript
	{
		private static AlternateUI instance_ = null;

		private const string ConfigDir = "Custom/PluginData/AlternateUI";
		private const string ConfigFile = ConfigDir + "/aui.json";
		private const string OldConfigFile = "Custom/PluginData/aui.json";

		private Logger log_;
		private BasicAlternateUI[] uis_ = null;
		private bool inited_ = false;
		private VUI.TimerManager tm_ = null;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public AlternateUI()
		{
			instance_ = this;
			log_ = new Logger("aui");
			FileManagerSecure.CreateDirectory(ConfigDir);
		}

		static public AlternateUI Instance
		{
			get { return instance_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public T GetUI<T>() where T : class, IAlternateUI
		{
			for (int i = 0; i < uis_.Length; ++i)
			{
				if (uis_[i] is T)
					return uis_[i] as T;
			}

			return null;
		}

		private BasicAlternateUI[] CreateUIs()
		{
			return new BasicAlternateUI[]
			{
				new MorphUI.MorphUI(),
				new SelectUI.SelectUI(),
				new SkinUI.SkinUI(),
				new LogUI.LogUI(),
				new Tweaks.EditMode(),
				new Tweaks.FocusHead(),
				new Tweaks.DisableLoadPosition(),
				new Tweaks.MoveNewLight(),
				new LightUI.LightUI(),
				new PluginsUI.PluginsUI()
			};
		}

		public void Update()
		{
			try
			{
				DoUpdate();
			}
			catch (Exception e)
			{
				OnException(e);
			}
		}

		private void DoUpdate()
		{
			if (!inited_)
			{
				inited_ = true;
				DoInit();
			}

			tm_.TickTimers(Time.deltaTime);
			tm_.CheckTimers();

			for (int i = 0; i < uis_.Length; ++i)
				uis_[i].Update(Time.deltaTime);
		}

		private void DoInit()
		{
			Log.Verbose("init");

			VUI.Root.Init(
				"AUI",
				() => manager,
				(s, ps) => string.Format(s, ps),
				(s) => Log.Verbose(s),
				(s) => Log.Info(s),
				(s) => Log.Warning(s),
				(s) => Log.Error(s));

			tm_ = new VUI.TimerManager();
			uis_ = CreateUIs();

			LoadConfig();

			for (int i = 0; i < uis_.Length; ++i)
				uis_[i].Init();

			SaveConfig();
			CreateUI();
		}

		private void CreateUI()
		{
			for (int i = 0; i < uis_.Length; ++i)
			{
				if (i > 0)
				{
					var s = CreateSpacer();
					s.height = 85;
				}

				uis_[i].CreateUI();
			}
		}

		public string GetConfigFilePath(string filename)
		{
			return ConfigDir + "/" + filename;
		}

		private void LoadConfig()
		{
			Log.Verbose("loading config");

			JSONClass j = null;

			if (FileManagerSecure.FileExists(OldConfigFile))
				j = LoadJSON(OldConfigFile) as JSONClass;
			else if (FileManagerSecure.FileExists(ConfigFile))
				j = LoadJSON(ConfigFile) as JSONClass;

			if (j == null)
				return;

			for (int i = 0; i < uis_.Length; ++i)
			{
				if (j.HasKey(uis_[i].Name))
				{
					var o = j[uis_[i].Name].AsObject;
					if (o != null)
						uis_[i].Load(o);
				}
			}
		}

		private void SaveConfig()
		{
			var j = new JSONClass();

			for (int i = 0; i < uis_.Length; ++i)
			{
				var o = uis_[i].Save();
				if (o != null)
					j.Add(uis_[i].Name, o);
			}

			SaveJSON(j, ConfigFile);

			if (FileManagerSecure.FileExists(OldConfigFile))
				FileManagerSecure.DeleteFile(OldConfigFile);
		}

		public void Save()
		{
			SaveConfig();
		}

		public void OnEnable()
		{
			try
			{
				if (uis_ != null)
				{
					for (int i = 0; i < uis_.Length; ++i)
						uis_[i].OnPluginState(true);
				}
			}
			catch (Exception e)
			{
				OnException(e);
			}
		}

		public void OnDisable()
		{
			try
			{
				if (uis_ != null)
				{
					for (int i = 0; i < uis_.Length; ++i)
						uis_[i].OnPluginState(false);
				}
			}
			catch (Exception e)
			{
				OnException(e);
			}
		}

		public void DisablePlugin()
		{
			enabledJSON.val = false;
		}

		public void ReloadPlugin()
		{
			var pui = GetPluginUI();
			if (pui == null)
				return;

			Log.Verbose("reloading");
			pui.reloadButton?.onClick?.Invoke();
		}

		private MVRPluginUI GetPluginUI()
		{
			Transform p = enabledJSON?.toggle?.transform;

			while (p != null)
			{
				var pui = p.GetComponent<MVRPluginUI>();
				if (pui != null)
					return pui;

				p = p.parent;
			}

			return null;
		}

		private void OnException(Exception e)
		{
			Log.Error(e.ToString());

			var now = Time.realtimeSinceStartup;

			if (now - lastErrorTime_ < 1)
			{
				++errorCount_;
				if (errorCount_ > MaxErrors)
				{
					Log.Error(
						$"more than {MaxErrors} errors in the last " +
						"second, disabling plugin");

					DisablePlugin();
				}
			}
			else
			{
				errorCount_ = 0;
			}

			lastErrorTime_ = now;
		}

		static void Main()
		{
		}
	}
}
