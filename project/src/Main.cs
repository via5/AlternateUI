using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using UnityEngine;

namespace AUI
{
	class AlternateUI : MVRScript
	{
		private static AlternateUI instance_ = null;

		private const string ConfigDir = "Custom/PluginData/AlternateUI";
		private const string ConfigFile = ConfigDir + "/aui.json";
		private const string OldConfigFile = "Custom/PluginData/aui.json";

		private Logger log_;
		private BasicFeature[] features_ = null;
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

		public T GetFeature<T>() where T : class, IFeature
		{
			for (int i = 0; i < features_.Length; ++i)
			{
				if (features_[i] is T)
					return features_[i] as T;
			}

			return null;
		}

		private BasicFeature[] CreateUIs()
		{
			return new BasicFeature[]
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

			for (int i = 0; i < features_.Length; ++i)
				features_[i].Update(Time.deltaTime);
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
			features_ = CreateUIs();

			LoadConfig();

			for (int i = 0; i < features_.Length; ++i)
				features_[i].Init();

			SaveConfig();
			CreateUI();
		}

		private void CreateUI()
		{
			for (int i = 0; i < features_.Length; ++i)
			{
				if (i > 0)
				{
					var s = CreateSpacer();
					s.height = 85;
				}

				features_[i].CreateUI();
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

			for (int i = 0; i < features_.Length; ++i)
			{
				if (j.HasKey(features_[i].Name))
				{
					var o = j[features_[i].Name].AsObject;
					if (o != null)
						features_[i].Load(o);
				}
			}
		}

		private void SaveConfig()
		{
			var j = new JSONClass();

			for (int i = 0; i < features_.Length; ++i)
			{
				var o = features_[i].Save();
				if (o != null)
					j.Add(features_[i].Name, o);
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
				if (features_ != null)
				{
					for (int i = 0; i < features_.Length; ++i)
						features_[i].OnPluginState(true);
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
				if (features_ != null)
				{
					for (int i = 0; i < features_.Length; ++i)
						features_[i].OnPluginState(false);
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
