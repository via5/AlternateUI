using System;
using System.Collections.Generic;

namespace AUI.MorphUI
{
	class GenderMorphUI
	{
		private float UpdateInterval = 0.025f;
		private const int Columns = 3;
		private const int Rows = 7;

		private readonly PersonMorphUI ui_;
		private readonly string name_;
		private Atom atom_ = null;
		private GenerateDAZMorphsControlUI mui_ = null;
		private VUI.Root root_ = null;
		private Controls controls_;
		private VUI.Panel grid_ = new VUI.Panel();
		private List<MorphPanel> panels_ = new List<MorphPanel>();
		private List<DAZMorph> all_ = new List<DAZMorph>();
		private List<DAZMorph> filtered_ = new List<DAZMorph>();
		private Filter filter_ = new Filter();
		private Categories cats_;
		private int page_ = 0;
		private float createElapsed_ = 1000;
		private bool triedOnce_ = false;
		private float updateElapsed_ = 0;

		public GenderMorphUI(PersonMorphUI ui, string name)
		{
			ui_ = ui;
			name_ = name;
			filter_.Sort = Filter.SortName;
		}

		public PersonMorphUI PersonMorphUI
		{
			get { return ui_; }
		}

		public string GenderName
		{
			get { return name_; }
		}

		public Logger Log
		{
			get { return ui_.Log; }
		}

		public VUI.Root Root
		{
			get { return root_; }
		}

		public Filter Filter
		{
			get { return filter_; }
		}

		public Controls Controls
		{
			get { return controls_; }
		}

		public void Set(Atom a, GenerateDAZMorphsControlUI mui)
		{
			atom_ = a;
			mui_ = mui;

			if (atom_ == null)
				all_ = new List<DAZMorph>();
			else
				all_ = mui_.GetMorphs();

			filter_.Set(all_);
			Refilter();
			cats_?.Update(all_);
			controls_?.UpdateCategories();
		}

		public void Update(float s)
		{
			if (root_ == null)
			{
				createElapsed_ += s;
				if (createElapsed_ > 1)
				{
					createElapsed_ = 0;

					if (!CreateUI())
					{
						Log.Error("will keep retrying");
						triedOnce_ = true;
					}
				}
			}

			updateElapsed_ += s;

			if (root_.Visible)
			{
				if (filter_.IsDirty)
				{
					Refilter();
					updateElapsed_ = UpdateInterval + 1;
				}

				if (updateElapsed_ >= UpdateInterval)
				{
					updateElapsed_ = 0;

					for (int i = 0; i < panels_.Count; ++i)
						panels_[i].Update();
				}

				root_.Update();
			}
			else
			{
				updateElapsed_ = UpdateInterval + 1;
			}
		}

		public int PerPage
		{
			get { return Columns * Rows; }
		}

		public int PageCount
		{
			get
			{
				if (filtered_.Count <= PerPage)
					return 1;
				else
					return filtered_.Count / PerPage + 1;
			}
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

		public Categories Categories
		{
			get { return cats_; }
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
			controls_?.UpdatePage();
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

		private bool CreateUI()
		{
			var rt = mui_.transform.parent;
			if (rt == null)
			{
				if (!triedOnce_)
					Log.Error("no morph ui");

				return false;
			}

			root_ = new VUI.Root(new VUI.TransformUIRootSupport(rt));

			try
			{
				controls_ = new Controls(this);
				cats_ = new Categories();
				cats_.Update(all_);
				controls_?.UpdateCategories();

				root_.ContentPanel.Layout = new VUI.BorderLayout(10);
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

				return true;
			}
			catch (Exception)
			{
				if (root_ != null)
					root_.Visible = false;

				throw;
			}
		}

		public void Enable()
		{
			if (root_ != null)
				root_.Visible = true;
		}

		public void Disable()
		{
			if (root_ != null)
				root_.Visible = false;
		}

		private void Refilter()
		{
			filtered_ = filter_.Process();
			PageChanged();
		}
	}


	class BadAtom : Exception { }


	class PersonMorphUI
	{
		private readonly MorphUI ui_;
		private readonly Atom atom_;
		private readonly GenderMorphUI male_;
		private readonly GenderMorphUI female_;

		public PersonMorphUI(MorphUI ui, Atom a)
		{
			ui_ = ui;
			atom_ = a;

			male_ = new GenderMorphUI(this, "male");
			female_ = new GenderMorphUI(this, "female");

			male_.Set(a, GetMUI(true));
			female_.Set(a, GetMUI(false));
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public Logger Log
		{
			get { return ui_.Log; }
		}

		public MorphUI MorphUI
		{
			get { return ui_; }
		}

		public GenderMorphUI Male
		{
			get { return male_; }
		}

		public GenderMorphUI Female
		{
			get { return female_; }
		}

		public GenderMorphUI[] GenderMorphUIs
		{
			get { return new GenderMorphUI[] { male_, female_ }; }
		}

		public void Update(float s)
		{
			male_.Update(s);
			female_.Update(s);
		}

		private GenerateDAZMorphsControlUI GetMUI(bool male)
		{
			if (atom_ == null)
			{
				Log.Error("no atom");
				throw new BadAtom();
			}

			var cs = atom_.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
			{
				Log.Error("no DAZCharacterSelector");
				throw new BadAtom();
			}

			if (male)
			{
				if (cs.morphsControlMaleUI == null)
				{
					Log.Error("no morphsControlMaleUI");
					throw new BadAtom();
				}

				return cs.morphsControlMaleUI;
			}
			else
			{
				if (cs.morphsControlFemaleUI?.transform == null)
				{
					Log.Error("no morphsControlFemaleUI");
					throw new BadAtom();
				}

				return cs.morphsControlFemaleUI;
			}
		}

		public void Enable()
		{
			male_.Enable();
			female_.Enable();
		}

		public void Disable()
		{
			male_.Disable();
			female_.Disable();
		}
	}


	class MorphUI : BasicFeature
	{
		private SuperController sc_;
		private readonly List<PersonMorphUI> uis_ = new List<PersonMorphUI>();

		public MorphUI()
			: base("morphs", "Morphs UI", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Complete overhaul of the male and female morphs panel.";
			}
		}

		public List<PersonMorphUI> PersonUIs
		{
			get { return uis_; }
		}


		protected override void DoInit()
		{
			sc_ = SuperController.singleton;

			foreach (var a in sc_.GetAtoms())
			{
				if (a.type == "Person")
					Add(a);
			}
		}

		protected override void DoUpdate(float s)
		{
			for (int i = 0; i < uis_.Count; ++i)
				uis_[i].Update(s);
		}

		protected override void DoEnable()
		{
			sc_.onAtomAddedHandlers += Add;
			sc_.onAtomRemovedHandlers += Remove;

			for (int i = 0; i < uis_.Count; ++i)
				uis_[i].Enable();
		}

		protected override void DoDisable()
		{
			sc_.onAtomAddedHandlers -= Add;
			sc_.onAtomRemovedHandlers -= Remove;

			for (int i = 0; i < uis_.Count; ++i)
				uis_[i].Disable();
		}

		private void Add(Atom a)
		{
			if (a.type != "Person")
				return;

			var i = IndexOf(a);

			if (i == -1)
			{
				try
				{
					Log.Verbose($"morphui: new atom {a.uid}");
					uis_.Add(new PersonMorphUI(this, a));
				}
				catch (BadAtom)
				{
					Log.Error($"morphui: bad atom {a.uid}");
				}
			}
			else
			{
				Log.Warning($"morphui: new atom {a.uid} already in list");
			}
		}

		private void Remove(Atom a)
		{
			var i = IndexOf(a);
			if (i == -1)
				return;

			Log.Verbose($"morphui: atom {a.uid} removed");
			uis_[i].Disable();
			uis_.RemoveAt(i);
		}

		private int IndexOf(Atom a)
		{
			for (int i = 0; i < uis_.Count; ++i)
			{
				if (uis_[i].Atom == a)
					return i;
			}

			return -1;
		}
	}
}
