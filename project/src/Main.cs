using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AUI
{
	public class AlternateUI
	{
		private static AlternateUI instance_ = null;

		private const string ConfigDir = "Custom/PluginData/AlternateUI";
		private const string ConfigFile = ConfigDir + "/aui.json";
		private const string OldConfigFile = "Custom/PluginData/aui.json";

		private readonly ISys sys_;
		private Logger log_;
		private BasicFeature[] features_ = null;
		private bool inited_ = false;
		private VUI.TimerManager tm_ = null;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public AlternateUI(ISys sys)
		{
			instance_ = this;
			sys_ = sys;
			log_ = new Logger("aui");
			Sys.CreateDirectory(ConfigDir);
			log_.Info($"starting aui {Version.String}");
		}

		public static void Assert(bool b)
		{
			if (!b)
				Instance.Log.ErrorST("assertion failed");
		}

		public static AlternateUI Instance
		{
			get { return instance_; }
		}

		public ISys Sys
		{
			get { return sys_; }
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

		public void StartCoroutine(System.Collections.IEnumerator e)
		{
			Sys.StartCoroutine(e);
		}

		public void Update()
		{
			try
			{
				if (U.DevMode)
				{
					if (Sys.GetKeyUp(UnityEngine.KeyCode.F5))
					{
						if (Sys.GetKey(UnityEngine.KeyCode.LeftShift))
						{
							SuperController.singleton.HardReset();
						}
						else
						{
							SuperController.singleton.ClearMessages();
							ReloadPlugin();
						}

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

			float dt = Sys.GetDeltaTime();

			tm_.TickTimers(dt);
			tm_.CheckTimers();

			Vamos.API.Instance?.DoUpdate(dt);

			for (int i = 0; i < features_.Length; ++i)
			{
				try
				{
					features_[i].Update(dt);
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

			Icons.LoadAll();

			VUI.Root.Init(
				"AUI",
				() => Sys.GetPluginManager(),
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

			var uiReplacements = new List<IFeature>();
			var tweaks = new List<IFeature>();

			for (int i = 0; i < features_.Length; ++i)
			{
				switch (features_[i].FeatureType)
				{
					case BasicFeature.UIReplacement:
					{
						uiReplacements.Add(features_[i]);
						break;
					}

					case BasicFeature.Tweak:
					{
						tweaks.Add(features_[i]);
						break;
					}

					default:
					{
						Log.Error($"bad feature type {features_[i].FeatureType}");
						break;
					}
				}
			}

			CreateHeader("UI replacements");
			CreateFeatureUIs(uiReplacements);

			CreateHeader("Tweaks");
			CreateFeatureUIs(tweaks);
		}

		private void CreateFeatureUIs(List<IFeature> list)
		{
			foreach (var f in list)
			{
				try
				{
					f.CreateUI();
				}
				catch (Exception e)
				{
					Log.Error($"exception in {f.Name} CreateUI:");
					Log.Error(e.ToString());
				}

				var s = Sys.CreateSpacer();
				s.height = 20;
			}
		}

		private void CreateVersion()
		{
			CreateHeader($"AlternateUI {Version.String}");

			var s = Sys.CreateSpacer();
			s.height = 20;

			s = Sys.CreateSpacer(true);
			s.height = 20;
		}

		private void CreateHeader(string text, bool right = false)
		{
			var t = Sys.CreateButton(text, right);
			t.buttonColor = new Color(0, 0, 0, 0);
			t.buttonText.alignment = TextAnchor.MiddleLeft;
			t.buttonText.fontStyle = FontStyle.Bold;
			t.button.interactable = false;
			t.height = 15;
			t.GetComponent<LayoutElement>().minHeight = 15;

			var sp = Sys.CreateSpacer(true);
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

			if (Sys.FileExists(OldConfigFile))
				j = Sys.LoadJSON(OldConfigFile) as JSONClass;
			else if (Sys.FileExists(ConfigFile))
				j = Sys.LoadJSON(ConfigFile) as JSONClass;

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

			Sys.SaveJSON(j, ConfigFile);

			if (Sys.FileExists(OldConfigFile))
				Sys.DeleteFile(OldConfigFile);
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
			Sys.SetPluginEnabled(false);
		}

		public void ReloadPlugin()
		{
			var pui = Sys.GetPluginUI();
			if (pui == null)
				return;

			Log.Verbose("reloading");
			pui.reloadButton?.onClick?.Invoke();
		}

		public string PluginPath
		{
			get { return Sys.GetPluginPath(); }
		}

		private void OnException(Exception e)
		{
			Log.Error(e.ToString());

			var now = Sys.GetRealtimeSinceStartup();

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
	}
}
