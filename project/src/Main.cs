using System;
using UnityEngine;

namespace AUI
{
	class AlternateUI : MVRScript
	{
		static private AlternateUI instance_ = null;

		private MorphUI.MorphUI mui_ = null;
		private bool inited_ = false;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public AlternateUI()
		{
			instance_ = this;
		}

		static public AlternateUI Instance
		{
			get { return instance_; }
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
				VUI.Root.Init(
					() => manager,
					(s, ps) => string.Format(s, ps),
					(s) => Log.Verbose(s),
					(s) => Log.Info(s),
					(s) => Log.Warning(s),
					(s) => Log.Error(s));

				mui_ = new MorphUI.MorphUI();
				mui_.OnPluginState(true);

				inited_ = true;
			}

			mui_.Update(Time.deltaTime);
		}

		public void OnEnable()
		{
			try
			{
				mui_?.OnPluginState(true);
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
				mui_?.OnPluginState(false);
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
