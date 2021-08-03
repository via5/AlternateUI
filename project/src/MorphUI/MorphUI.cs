using System.Collections.Generic;

namespace AUI.MorphUI
{
	class MorphUI
	{
		private const int Columns = 3;
		private const int Rows = 5;

		private Atom atom_ = null;
		private VUI.Root root_ = null;
		private Controls controls_;
		private VUI.Panel grid_ = new VUI.Panel();
		private List<MorphPanel> panels_ = new List<MorphPanel>();
		private List<DAZMorph> all_ = new List<DAZMorph>();
		private List<DAZMorph> filtered_ = new List<DAZMorph>();
		private Filter filter_ = new Filter();
		private int page_ = 0;

		public MorphUI()
		{
			controls_ = new Controls(this);
			filter_.Dupes = Filter.SamePathDupes | Filter.SimilarDupes;
			filter_.Sort = Filter.SortName;
		}

		public Filter Filter
		{
			get { return filter_; }
		}

		public void SetAtom(Atom a)
		{
			atom_ = a;

			if (atom_ == null)
				all_ = new List<DAZMorph>();
			else
				all_ = GetMUI(atom_).GetMorphs();

			filter_.Set(all_);
			Refilter();
		}

		public void Update()
		{
			if (root_ == null)
			{
				if (AlternateUI.Instance.UITransform == null)
					return;

				CreateUI();
			}

			if (root_.Visible)
			{
				if (filter_.IsDirty)
					Refilter();

				for (int i = 0; i < panels_.Count; ++i)
					panels_[i].Update();

				root_.Update();
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
			page_ = U.Clamp(page_, 0, PageCount - 1);
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

		private void CreateUI()
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

			PageChanged();
		}

		private void Refilter()
		{
			filtered_ = filter_.Process();
			Log.Verbose($"filtered {all_.Count - filtered_.Count} morphs");
			PageChanged();
		}

		private GenerateDAZMorphsControlUI GetMUI(Atom atom)
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
