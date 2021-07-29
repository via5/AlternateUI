using System.Collections.Generic;

namespace AUI.MorphUI
{
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
