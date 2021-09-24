using System;
using System.Collections.Generic;
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
		}

		private void DoUpdate()
		{
			if (!inited_)
			{
				VUI.Glue.Set(
					() => manager,
					(s, ps) => string.Format(s, ps),
					(s) => Log.Verbose(s),
					(s) => Log.Info(s),
					(s) => Log.Warning(s),
					(s) => Log.Error(s));

				mui_ = new MorphUI.MorphUI(this);

				mui_.SetAtom(SuperController.singleton.GetAtomByUid("Person"));
				inited_ = true;
			}

			mui_.Update();
		}

		public void OnEnable()
		{
			mui_.OnPluginState(true);
		}

		public void OnDisable()
		{
			mui_.OnPluginState(false);
		}

		public void DisablePlugin()
		{
			enabledJSON.val = false;
		}

		static void Main()
		{
		}
	}
}
