namespace AUI
{
	class AlternateUI : MVRScript
	{
		static private AlternateUI instance_ = null;

		private MorphUI.MorphUI mui_ = new MorphUI.MorphUI();
		private bool inited_ = false;

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
			U.Safe(() =>
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

					mui_.SetAtom(SuperController.singleton.GetAtomByUid("Person"));
					inited_ = true;
				}

				mui_.Update();
			});
		}

		public void OnEnable()
		{
		}

		public void OnDisable()
		{
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
