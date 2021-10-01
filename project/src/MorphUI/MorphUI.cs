using System;
using System.Collections;
using System.Collections.Generic;

namespace AUI.MorphUI
{
	class Categories
	{
		public class Node
		{
			private Node parent_;
			private string name_;
			private List<Node> children_ = null;
			private List<DAZMorph> morphs_ = null;
			private HashSet<string> morphUids_ = null;

			public Node(Node parent, string name)
			{
				parent_ = parent;
				name_ = name;
			}

			public string Name
			{
				get { return name_; }
			}

			public string Path
			{
				get
				{
					string s = name_;
					Node p = parent_;

					while (p != null)
					{
						s = $"{p.Name}/{s}";
						p = p.parent_;
					}

					return s;
				}
			}

			public bool HasChildren
			{
				get { return (children_ != null && children_.Count > 0); }
			}

			public void AddNode(Node n)
			{
				if (children_ == null)
					children_ = new List<Node>();

				children_.Add(n);
			}

			public void RemoveNodeAt(int i)
			{
				if (children_ != null)
					children_.RemoveAt(i);
			}

			public void AddMorph(DAZMorph m)
			{
				if (morphs_ == null)
					morphs_ = new List<DAZMorph>();

				if (morphUids_ == null)
					morphUids_ = new HashSet<string>();

				morphs_.Add(m);
				morphUids_.Add(m.uid);
			}

			public List<DAZMorph> MorphsRecursive()
			{
				var list = new List<DAZMorph>();
				MorphsRecursive(list);
				return list;
			}

			public bool ContainsRecursive(DAZMorph m)
			{
				if (morphUids_ != null)
				{
					if (morphUids_.Contains(m.uid))
						return true;
				}

				if (children_ != null)
				{
					foreach (var c in children_)
					{
						if (c.ContainsRecursive(m))
							return true;
					}
				}

				return false;
			}

			private void MorphsRecursive(List<DAZMorph> list)
			{
				if (morphs_ != null)
				{
					foreach (var m in morphs_)
						list.Add(m);
				}

				if (children_ != null)
				{
					foreach (var c in children_)
						c.MorphsRecursive(list);
				}
			}

			public void Clear()
			{
				if (children_ != null)
					children_.Clear();

				if (morphs_ != null)
					morphs_.Clear();
			}

			public List<Node> NodesRecursive()
			{
				var list = new List<Node>();
				NodesRecursive(list);
				return list;
			}

			private void NodesRecursive(List<Node> list)
			{
				if (children_ != null)
				{
					foreach (var c in children_)
					{
						list.Add(c);
						c.NodesRecursive(list);
					}
				}
			}

			public List<Node> Children
			{
				get { return children_; }
			}

			public List<DAZMorph> Morphs
			{
				get { return morphs_; }
			}

			public void Sort()
			{
				if (children_ != null)
				{
					U.NatSort(children_);

					foreach (var c in children_)
						c.Sort();
				}
			}

			public override string ToString()
			{
				return name_;
			}

			public void Dump(int indent = 0)
			{
				if (children_ == null)
					return;

				foreach (var c in children_)
				{
					Log.Info(new string(' ', indent * 4) + c.name_);
					c.Dump(indent + 1);
				}
			}
		}

		private Node root_ = new Node(null, "");
		private Dictionary<string, string> override_;

		public Categories()
		{
			override_ = new Dictionary<string, string>();
		}

		public Node Root
		{
			get { return root_; }
		}

		public void Update(List<DAZMorph> all)
		{
			root_.Clear();

			var nodes = new Dictionary<string, Node>();

			for (int i = 0; i < all.Count; ++i)
			{
				HandleMorph(nodes, all[i]);
			}


			if (root_.Children != null)
			{
				int i = 0;

				List<Node> morphs = null;

				{
					Node n;
					if (nodes.TryGetValue("morph", out n))
						morphs = n.NodesRecursive();
				}

				if (morphs != null)
				{
					//foreach (var m in morphs)
					//	Log.Info(m);
				}


				while (i < root_.Children.Count)
				{
					var n = root_.Children[i];
					bool removed = false;

					foreach (var nn in morphs)
					{
						if (n.Name == nn.Name)
						{
							root_.RemoveNodeAt(i);

							foreach (var m in n.Morphs)
							{
							//	Log.Info($"moving {m.displayName} from {n.Path} to {nn.Path}");
								nn.AddMorph(m);
							}

							removed = true;
							break;
						}
					}

					if (!removed)
						++i;
				}
			}


			root_.Sort();


			BringToTop(root_.Children, "Pose");
			BringToTop(root_.Children, "Morph");

			//root_.Dump();


			//if (set.Add(lcName))
			//	cats_.Add(name);

			//Log.Info($"{cats_.Count}");
			//foreach (var c in cats_)
			//	Log.Info(c);
		}

		private void BringToTop(List<Node> nodes, string name)
		{
			for (int i=0; i<nodes.Count; ++i)
			{
				if (nodes[i].Name == name)
				{
					var temp = nodes[i];
					nodes.RemoveAt(i);
					nodes.Insert(0, temp);
					return;
				}
			}
		}

		private string GetRegion(DAZMorph m)
		{
			if (m.resolvedRegionName != "")
				return m.resolvedRegionName;
			else
				return m.region;
		}

		private void HandleMorph(Dictionary<string, Node> map, DAZMorph m)
		{
			var name = GetRegion(m);
			name = name.Replace('\\', '/');
			name = name.Trim(new char[] { '/' });

			var lcName = name.ToLower();

			int start = 0;
			int slash = -1;
			string fullName = "";
			Node parent = root_;

			do
			{
				slash = name.IndexOf('/', start);
				if (slash == -1)
					slash = name.Length;

				var cn = name.Substring(start, slash - start);

				if (cn.Length > 0 || fullName == "")
				{
					if (fullName != "")
						fullName += "/";

					fullName += cn.ToLower();

					Node n;
					if (!map.TryGetValue(fullName, out n))
					{
						n = new Node(parent, cn);
						map.Add(fullName, n);
						parent.AddNode(n);
					}

					n.AddMorph(m);

					parent = n;
				}

				start = slash + 1;
			} while (slash < name.Length);
		}
	}


	class GenderMorphUI
	{
		private float UpdateInterval = 0.025f;
		private const int Columns = 3;
		private const int Rows = 7;

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

		public GenderMorphUI()
		{
			filter_.Sort = Filter.SortName;
		}

		public VUI.Root Root
		{
			get { return root_; }
		}

		public Filter Filter
		{
			get { return filter_; }
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
					Refilter();

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
				updateElapsed_ = 1000;
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

		public void OnPluginState(bool b)
		{
			if (root_ != null)
				root_.Visible = b;
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
		private Atom atom_ = null;
		private GenderMorphUI male_ = new GenderMorphUI();
		private GenderMorphUI female_ = new GenderMorphUI();

		public PersonMorphUI(Atom a)
		{
			atom_ = a;
			male_.Set(a, GetMUI(true));
			female_.Set(a, GetMUI(false));
		}

		public Atom Atom
		{
			get { return atom_; }
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

		public void OnPluginState(bool b)
		{
			male_.OnPluginState(b);
			female_.OnPluginState(b);
		}
	}


	class MorphUI
	{
		private SuperController sc_;
		private readonly List<PersonMorphUI> uis_ = new List<PersonMorphUI>();

		public MorphUI()
		{
			sc_ = SuperController.singleton;

			foreach (var a in sc_.GetAtoms())
			{
				if (a.type == "Person")
					Add(a);
			}
		}

		public void Update(float s)
		{
			for (int i = 0; i < uis_.Count; ++i)
				uis_[i].Update(s);
		}

		public void OnPluginState(bool b)
		{
			if (b)
			{
				sc_.onAtomAddedHandlers += Add;
				sc_.onAtomRemovedHandlers += Remove;
			}
			else
			{
				sc_.onAtomAddedHandlers -= Add;
				sc_.onAtomRemovedHandlers -= Remove;
			}

			for (int i = 0; i < uis_.Count; ++i)
				uis_[i].OnPluginState(b);
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
					uis_.Add(new PersonMorphUI(a));
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
			uis_[i].OnPluginState(false);
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
