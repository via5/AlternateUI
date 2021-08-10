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

				if (m.uid.ToLower().Contains("acicia"))
					Log.Info("!!! " + Path + " " + m.displayName);
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


	class MorphUI
	{
		private const int Columns = 3;
		private const int Rows = 7;

		private Atom atom_ = null;
		private VUI.Root root_ = null;
		private Controls controls_;
		private VUI.Panel grid_ = new VUI.Panel();
		private List<MorphPanel> panels_ = new List<MorphPanel>();
		private List<DAZMorph> all_ = new List<DAZMorph>();
		private List<DAZMorph> filtered_ = new List<DAZMorph>();
		private Filter filter_ = new Filter();
		private Categories cats_;
		private int page_ = 0;

		public MorphUI()
		{
			//filter_.Dupes = Filter.SamePathDupes | Filter.SimilarDupes;
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

		public void SetAtom(Atom a)
		{
			atom_ = a;

			if (atom_ == null)
				all_ = new List<DAZMorph>();
			else
				all_ = GetMUI(atom_).GetMorphs();

			filter_.Set(all_);
			Refilter();
			cats_?.Update(all_);
			controls_?.UpdateCategories();
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

		private void CreateUI()
		{
			root_ = new VUI.Root(AlternateUI.Instance.UITransform.GetComponentInChildren<MVRScriptUI>());
			controls_ = new Controls(this);
			cats_ = new Categories();
			cats_.Update(all_);
			controls_?.UpdateCategories();

			root_.ContentPanel.Layout = new VUI.BorderLayout(10);



			//var t = new VUI.TreeView();
			//
			//for (int i=0; i<25; ++i)
			//	t.RootItem.Add(new VUI.TreeView.Item($"{i}"));
			//
			//for (int i=0; i<10; ++i)
			//	t.RootItem.Children[0].Add(new VUI.TreeView.Item($"0-{i}"));
			//
			//for (int i = 0; i < 20; ++i)
			//	t.RootItem.Children[2].Add(new VUI.TreeView.Item($"2-{i}"));
			//
			//
			//root_.ContentPanel.Add(t, VUI.BorderLayout.Center);



			//var cb = new VUI.ComboBox<string>();
			//for (int i=0; i<50; ++i)
			//	cb.AddItem($"{i}");
			//
			//root_.ContentPanel.Add(cb, VUI.BorderLayout.Top);
			//VUI.TimerManager.Instance.CreateTimer(
			//	1, () => { VUI.Utilities.DumpComponentsAndDown(cb.MainObject); });




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
