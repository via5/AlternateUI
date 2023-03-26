using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using UnityEngine;
using UnityEngine.UI;

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
			log_.Info($"starting aui {Version.String}");
		}

		public static void Assert(bool b)
		{
			if (!b)
			{
				AlternateUI.Instance.Log.ErrorST("assertion failed");
			}
		}

		public static AlternateUI Instance
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


		public void Update()
		{
			try
			{
				if (U.DevMode)
				{
					if (Input.GetKeyUp(KeyCode.F5))
					{
						SuperController.singleton.ClearMessages();
						ReloadPlugin();
						return;
					}
				}

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

			Vamos.API.Instance?.DoUpdate(Time.deltaTime);

			for (int i = 0; i < features_.Length; ++i)
			{
				try
				{
					features_[i].Update(Time.deltaTime);
				}
				catch (Exception e)
				{
					Log.Error($"exception in {features_[i].Name} Update:");
					Log.Error(e.ToString());
				}
			}
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
				(s) => Log.Error(s),
				() => CursorProvider.Instance);

			tm_ = new VUI.TimerManager();
			features_ = BasicFeature.CreateAllFeatures();

			LoadConfig();

			for (int i = 0; i < features_.Length; ++i)
			{
				try
				{
					features_[i].Init();
				}
				catch (Exception e)
				{
					Log.Error($"exception in {features_[i].Name} Init:");
					Log.Error(e.ToString());
				}
			}

			SaveConfig();
			CreateUI();
		}

		private void CreateUI()
		{
			CreateVersion();

			for (int i = 0; i < features_.Length; ++i)
			{
				if (i > 0)
				{
					var s = CreateSpacer();
					s.height = 85;
				}

				try
				{
					features_[i].CreateUI();
				}
				catch (Exception e)
				{
					Log.Error($"exception in {features_[i].Name} CreateUI:");
					Log.Error(e.ToString());
				}
			}
		}

		private void CreateVersion()
		{
			var t = CreateButton($"AlternateUI {Version.String}");
			t.buttonColor = new Color(0, 0, 0, 0);
			t.buttonText.alignment = TextAnchor.MiddleLeft;
			t.button.interactable = false;
			t.height = 15;
			t.GetComponent<LayoutElement>().minHeight = 15;

			var sp = CreateSpacer(true);
			sp.height = 50;
			sp.GetComponent<LayoutElement>().minHeight = 50;
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
					{
						try
						{
							features_[i].LoadOptions(o);
						}
						catch (Exception e)
						{
							Log.Error($"exception in {features_[i].Name} Load:");
							Log.Error(e.ToString());
						}
					}
				}
			}
		}

		private void SaveConfig()
		{
			var j = new JSONClass();

			for (int i = 0; i < features_.Length; ++i)
			{
				try
				{
					var o = features_[i].SaveOptions();
					if (o != null)
						j.Add(features_[i].Name, o);
				}
				catch (Exception e)
				{
					Log.Error($"exception in {features_[i].Name} Save:");
					Log.Error(e.ToString());
				}
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
				Vamos.API.Enable("AlternateUI");

				if (features_ != null)
				{
					for (int i = 0; i < features_.Length; ++i)
					{
						try
						{
							features_[i].OnPluginState(true);
						}
						catch (Exception e)
						{
							Log.Error($"exception in {features_[i].Name} OnPluginState(true):");
							Log.Error(e.ToString());
						}
					}
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
					{
						try
						{
							features_[i].OnPluginState(false);
						}
						catch (Exception e)
						{
							Log.Error($"exception in {features_[i].Name} OnPluginState(false):");
							Log.Error(e.ToString());
						}
					}
				}

				Vamos.API.Disable();
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

		public string PluginPath
		{
			get
			{
				// based on MacGruber, which was based on VAMDeluxe, which was
				// in turn based on Alazi

				string id = name.Substring(0, name.IndexOf('_'));
				string filename = manager.GetJSON()["plugins"][id].Value;

				var path = filename.Substring(
					0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

				path = path.Replace('/', '\\');
				if (path.EndsWith("\\"))
					path = path.Substring(0, path.Length - 1);

				return path;
			}
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
