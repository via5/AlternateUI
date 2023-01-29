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
					//Log.Info(new string(' ', indent * 4) + c.name_);
					//c.Dump(indent + 1);
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
			for (int i = 0; i < nodes.Count; ++i)
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
}
