using System.Collections.Generic;

namespace AUI
{
	class MorphPanel : VUI.Panel
	{
		private DAZMorph morph_ = null;

		private VUI.Label name_ = new VUI.Label();
		private VUI.FloatTextSlider slider_ = new VUI.FloatTextSlider("0.000");
		private VUI.ToolButton reset_ = new VUI.ToolButton("R");
		private VUI.ToolButton def_ = new VUI.ToolButton("Def");
		private VUI.ToolButton addRange_ = new VUI.ToolButton("+Range");
		private VUI.ToolButton resetRange_ = new VUI.ToolButton("R range");
		private VUI.CheckBox fav_ = new VUI.CheckBox("F");

		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public MorphPanel()
		{
			int buttonsFontSize = 18;

			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.FontSize = 24;

			Padding = new VUI.Insets(5);
			Borders = new VUI.Insets(1);
			Layout = new VUI.VerticalFlow();

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(
				3, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));
			buttons.Add(reset_);
			buttons.Add(def_);
			buttons.Add(addRange_);
			buttons.Add(resetRange_);
			buttons.Add(fav_);

			reset_.FontSize = buttonsFontSize;
			def_.FontSize = buttonsFontSize;
			addRange_.FontSize = buttonsFontSize;
			resetRange_.FontSize = buttonsFontSize;
			fav_.FontSize = buttonsFontSize;

			Add(name_);
			Add(slider_);
			Add(buttons);

			slider_.ValueChanged += OnValue;
			reset_.Clicked += OnReset;
			def_.Clicked += OnDefault;
			addRange_.Clicked += OnAddRange;
			resetRange_.Clicked += OnResetRange;
			fav_.Changed += OnFavorite;
		}

		public void SetMorph(DAZMorph m)
		{
			morph_ = m;

			name_.Text = m.displayName;
			name_.Tooltip.Text =
				$"{m.displayName}\n\n" +
				$"{m.uid}\n\n" +
				$"{m.metaLoadPath}";

			Update();
		}

		public void Update()
		{
			if (morph_ == null) return;

			ignore_.Do(() =>
			{
				slider_.Set(morph_.morphValue, morph_.min, morph_.max);
				fav_.Checked = morph_.favorite;
			});
		}

		private void OnValue(float f)
		{
			if (ignore_) return;
			morph_.morphValue = f;
		}

		private void OnReset()
		{
			if (ignore_) return;
			morph_.ResetRange();
			slider_.Set(morph_.startValue, morph_.min, morph_.max);
		}

		private void OnDefault()
		{
			if (ignore_) return;
			slider_.Value = morph_.startValue;
		}

		private void OnAddRange()
		{
			if (ignore_) return;
			slider_.SetRange(slider_.Minimum - 1, slider_.Maximum + 1);
		}

		private void OnResetRange()
		{
			if (ignore_) return;
			morph_.ResetRange();
			slider_.SetRange(morph_.min, morph_.max);
		}

		private void OnFavorite(bool b)
		{
			if (ignore_) return;
			morph_.favorite = b;
		}
	}


	class AlternateUI : MVRScript
	{
		static private AlternateUI instance_ = null;

		private bool inited_ = false;
		private VUI.Root root_ = null;
		private VUI.Panel panel_ = new VUI.Panel();
		private List<MorphPanel> panels_ = new List<MorphPanel>();
		private List<DAZMorph> morphs_ = new List<DAZMorph>();
		private List<DAZMorph> filtered_ = new List<DAZMorph>();

		public AlternateUI()
		{
			instance_ = this;
		}

		static public AlternateUI Instance
		{
			get { return instance_; }
		}

		private void DoInit()
		{
			VUI.Glue.Set(
				() => manager,
				(s, ps) => string.Format(s, ps),
				(s) => SuperController.LogError(s),
				(s) => SuperController.LogError(s),
				(s) => SuperController.LogError(s),
				(s) => SuperController.LogError(s));

			root_ = new VUI.Root(UITransform.GetComponentInChildren<MVRScriptUI>());
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			int cols = 3;
			int rows = 5;

			var gl = new VUI.GridLayout(cols);
			gl.UniformWidth = true;
			gl.Spacing = 5;
			panel_.Layout = gl;

			for (int i = 0; i < cols * rows; ++i)
			{
				var p = new MorphPanel();
				panels_.Add(p);
				panel_.Add(p);
			}
		}

		public void FixedUpdate()
		{
		}

		public void Update()
		{
			if (!inited_ && UITransform != null)
			{
				DoInit();
				inited_ = true;
				DoUpdate();
			}

			if (root_ != null)
			{
				if (root_.Visible)
				{
					for (int i = 0; i < panels_.Count; ++i)
						panels_[i].Update();
				}

				root_?.Update();
			}
		}

		private void DoUpdate()
		{
			var a = SuperController.singleton.GetAtomByUid("Person");
			var mui = GetMUI(a);
			morphs_ = mui.GetMorphs();

			filtered_.Clear();
			filtered_.Capacity = morphs_.Count;

			// ghetto set
			var set = new Dictionary<string, int>();

			for (int i = 0; i < morphs_.Count; ++i)
			{
				var m = morphs_[i];

				if (ShouldShow(m, set))
				{
					filtered_.Add(m);
				}
				else
				{
					SuperController.LogError($"filtered {m.uid}");
				}

			}

			SuperController.LogError($"filtered {morphs_.Count - filtered_.Count} morphs");


			for (int i = 0; i < panels_.Count; ++i)
				panels_[i].SetMorph(filtered_[i]);
		}

		private bool ShouldShow(DAZMorph m, Dictionary<string, int> set)
		{
			if (m.morphValue != m.startValue)
				return true;

			if (!m.isLatestVersion)
				return false;

			var file = GetFilename(m);
			if (set.ContainsKey(file))
			{
				SuperController.LogError($"dupe {m.uid} {file}");
				return false;
			}
			else
			{
				set.Add(file, 0);
			}

			return true;
		}

		private string GetFilename(DAZMorph m)
		{
			if (m.isInPackage)
			{
				var pos = m.uid.IndexOf(":/");
				if (pos == -1)
				{
					SuperController.LogError(
						$"{m.uid} has no :/");
				}
				else
				{
					return m.uid.Substring(pos + 2);
				}
			}

			return m.uid;
		}

		public GenerateDAZMorphsControlUI GetMUI(Atom atom)
		{
			if (atom == null)
				return null;

			var cs = atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
				return null;

			return cs.morphsControlUI;
		}

		public void OnEnable()
		{
		}

		public void OnDisable()
		{
		}

		static void Main()
		{
		}
	}
}
