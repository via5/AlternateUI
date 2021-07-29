using System.Collections.Generic;

namespace AUI.MorphUI
{
	class Controls : VUI.Panel
	{
		private readonly MorphUI ui_;
		private VUI.IntTextSlider page_ = new VUI.IntTextSlider();
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public Controls(MorphUI ui)
		{
			ui_ = ui;

			var p = new VUI.Panel(new VUI.HorizontalFlow());

			p.Add(new VUI.Label("Page: "));
			p.Add(page_);
			p.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			p.Add(new VUI.ToolButton(">", () => ui_.NextPage()));

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Center);

			page_.ValueChanged += OnPageChanged;
		}

		public void Update()
		{
			ignore_.Do(() =>
			{
				page_.Set(ui_.CurrentPage + 1, 1, ui_.PageCount);
			});
		}

		private void OnPageChanged(int p)
		{
			if (ignore_) return;
			ui_.CurrentPage = page_.Value - 1;
		}
	}


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

			if (morph_ == null)
			{
				Render = false;
			}
			else
			{
				Render = true;
				name_.Text = m.displayName;
				name_.Tooltip.Text =
					$"{m.displayName}\n\n" +
					$"{m.uid}\n\n" +
					$"{m.metaLoadPath}";
			}

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
			if (ignore_ || morph_ == null) return;
			morph_.morphValue = f;
		}

		private void OnReset()
		{
			if (ignore_ || morph_ == null) return;
			morph_.ResetRange();
			slider_.Set(morph_.startValue, morph_.min, morph_.max);
		}

		private void OnDefault()
		{
			if (ignore_ || morph_ == null) return;
			slider_.Value = morph_.startValue;
		}

		private void OnAddRange()
		{
			if (ignore_ || morph_ == null) return;
			slider_.SetRange(slider_.Minimum - 1, slider_.Maximum + 1);
		}

		private void OnResetRange()
		{
			if (ignore_ || morph_ == null) return;
			morph_.ResetRange();
			slider_.SetRange(morph_.min, morph_.max);
		}

		private void OnFavorite(bool b)
		{
			if (ignore_ || morph_ == null) return;
			morph_.favorite = b;
		}
	}


	class MorphUI
	{
		private const int Columns = 3;
		private const int Rows = 5;

		private bool inited_ = false;
		private VUI.Root root_ = null;
		private Controls controls_;
		private VUI.Panel grid_ = new VUI.Panel();
		private List<MorphPanel> panels_ = new List<MorphPanel>();
		private List<DAZMorph> morphs_ = new List<DAZMorph>();
		private List<DAZMorph> filtered_ = new List<DAZMorph>();
		private int page_ = 0;

		public MorphUI()
		{
			controls_ = new Controls(this);
		}

		public void Update()
		{
			if (!inited_ && AlternateUI.Instance.UITransform != null)
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

		public int PerPage
		{
			get { return Columns * Rows; }
		}

		public int PageCount
		{
			get { return filtered_.Count / PerPage + 1; }
		}

		public int CurrentPage
		{
			get
			{
				return page_;
			}

			set
			{
				page_ = value;
				PageChanged();
			}
		}

		public List<DAZMorph> Filtered
		{
			get { return filtered_; }
		}

		public void PreviousPage()
		{
			if (page_ > 0)
			{
				--page_;
				PageChanged();
			}
		}

		public void NextPage()
		{
			if (page_ < (PageCount - 1))
			{
				++page_;
				PageChanged();
			}
		}

		private void PageChanged()
		{
			SetPanels();
			controls_.Update();
		}

		private void SetPanels()
		{
			for (int i = 0; i < panels_.Count; ++i)
			{
				int mi = (page_ * PerPage) + i;

				if (mi < filtered_.Count)
					panels_[i].SetMorph(filtered_[mi]);
				else
					panels_[i].SetMorph(null);
			}
		}

		private void DoInit()
		{
			root_ = new VUI.Root(AlternateUI.Instance.UITransform.GetComponentInChildren<MVRScriptUI>());
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(controls_, VUI.BorderLayout.Top);
			root_.ContentPanel.Add(grid_, VUI.BorderLayout.Center);

			var gl = new VUI.GridLayout(Columns);
			gl.UniformWidth = true;
			gl.Spacing = 5;
			grid_.Layout = gl;

			for (int i = 0; i < Columns * Rows; ++i)
			{
				var p = new MorphPanel();
				panels_.Add(p);
				grid_.Add(p);
			}
		}

		private void DoUpdate()
		{
			var a = SuperController.singleton.GetAtomByUid("A");
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
					Log.Verbose($"filtered {m.uid}");
				}
			}


			Log.Verbose($"filtered {morphs_.Count - filtered_.Count} morphs");
			controls_.Update();
			SetPanels();
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
				Log.Verbose($"dupe {m.uid} {file}");
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
					Log.Error($"{m.uid} has no :/");
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
	}
}
