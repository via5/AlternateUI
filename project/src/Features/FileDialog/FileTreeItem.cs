using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileTreeItem
	{
		FS.IFilesystemObject Object { get; }
		void SetFlags(int f);
	}


	class FileTreeItem : VUI.TreeView.Item, IFileTreeItem
	{
		private readonly FileTree tree_;
		private readonly string path_;

		private bool checkedHasChildren_ = false;
		private bool hasChildren_ = false;
		private FS.IFilesystemContainer o_ = null;
		private FS.IFilesystemContainer hardObject_ = null;

		public FileTreeItem(FileTree tree, FS.IFilesystemContainer c)
			: this(tree, c.VirtualPath, c.DisplayName, c.Icon)
		{
			hardObject_ = c;
		}

		public FileTreeItem(FileTree tree, string path, string displayName, VUI.Icon icon)
			: base(displayName)
		{
			tree_ = tree;
			path_ = path;
			icon?.GetTexture(t => Icon = t);
		}

		public override string ToString()
		{
			return $"FileTreeItem({path_})";
		}

		protected override string GetTooltip()
		{
			return Object?.Tooltip ?? "";
		}

		public FileTree FileTree
		{
			get { return tree_; }
		}

		public FS.IFilesystemObject Object
		{
			get { return GetFSObject(); }
		}

		public string Path
		{
			get { return path_; }
		}

		public FS.IFilesystemContainer GetFSObject(int moreFlags = 0)
		{
			if (hardObject_ != null)
				return hardObject_;

			if (o_ == null)
			{
				FS.Instrumentation.Start(FS.I.FTIGetFSObject);
				{
					o_ = FS.Filesystem.Instance.Resolve<FS.IFilesystemContainer>(
						CreateContext(), path_, FS.Filesystem.ResolveDefault | moreFlags);
				}
				FS.Instrumentation.End();
			}

			return o_;
		}

		public virtual void SetFlags(int f)
		{
			bool visible = true;

			var o = GetFSObject();
			if (o == null)
				return;

			if (o.ChildrenVirtual && Bits.IsSet(f, FileTree.Writeable))
				visible = false;
			else if (o.IsFlattened && Bits.IsAnySet(f, FileTree.FlattenDirectories | FileTree.Writeable))
				visible = false;

			Visible = visible;
		}

		public void Refresh(bool recursive)
		{
			var cx = CreateContext();
			RefreshInternal(cx, recursive);
		}

		private void RefreshInternal(FS.Context cx, bool recursive)
		{
			o_ = null;

			var o = GetFSObject();
			Text = o?.DisplayName ?? "(dead)";

			checkedHasChildren_ = false;

			if (!Expanded)
			{
				Clear();
				UpdateToggle();
				return;
			}

			if (o == null || !HasChildren)
			{
				Clear();
				return;
			}

			var dirs = GetDirectories(cx, o);

			var names = new Dictionary<string, FS.IFilesystemContainer>();
			foreach (var d in dirs)
				names.Add(d.Name, d);

			{
				var cs = GetInternalChildren();

				if (cs != null)
				{
					int i = 0;
					while (i < cs.Count)
					{
						bool found = false;
						var c = cs[i] as FileTreeItem;

						// assume that two objects can only be the same if they
						// have the same name; this can give false negative,
						// which isn't a problem except for performance, but it
						// won't give false positives

						FS.IFilesystemContainer samePath;
						if (names.TryGetValue(c.Text, out samePath))
						{
							var co = c.GetFSObject();

							if (co != null)
							{
								if (co.IsSameObject(samePath))
								{
									found = true;
								}
							}
						}

						if (!found)
						{
							Remove(c);
						}
						else
						{
							if (recursive)
								c.RefreshInternal(cx, true);

							++i;
						}
					}
				}
			}

			{
				var cs = GetInternalChildren();

				if (cs != null)
				{
					for (int i = 0; i < dirs.Count; ++i)
					{
						if (i >= cs.Count || !(cs[i] as FileTreeItem).GetFSObject().IsSameObject(dirs[i]))
							Insert(i, CreateItem(dirs[i]));
					}
				}
			}
		}

		protected override bool GetHasChildren()
		{
			if (!checkedHasChildren_)
			{
				var d = GetFSObject();
				var cx = CreateContext();

				hasChildren_ = (d != null && !d.IsFlattened && HasDirectories(cx, d));
				checkedHasChildren_ = true;
			}

			return hasChildren_;
		}

		protected override void GetChildren()
		{
			var d = GetFSObject();

			if (d != null && !d.IsFlattened)
			{
				foreach (var c in GetDirectories(CreateContext(), d))
					Add(CreateItem(c));
			}
		}

		private FileTreeItem CreateItem(FS.IFilesystemContainer o)
		{
			FileTreeItem item;

			FS.Instrumentation.Start(FS.I.FTICreateItem);
			{
				if (o.UnderlyingCanChange)
				{
					item = new FileTreeItem(
						tree_, o.VirtualPath, o.DisplayName, o.Icon);
				}
				else
				{
					item = new FileTreeItem(tree_, o);
				}
			}
			FS.Instrumentation.End();

			return item;
		}

		private List<FS.IFilesystemContainer> GetDirectories(
			FS.Context cx, FS.IFilesystemContainer o)
		{
			List<FS.IFilesystemContainer> list;

			FS.Instrumentation.Start(FS.I.FTIGetDirectories);
			{
				list = o.GetDirectories(cx);
			}
			FS.Instrumentation.End();

			return list;
		}

		private bool HasDirectories(FS.Context cx, FS.IFilesystemContainer o)
		{
			bool b = false;

			FS.Instrumentation.Start(FS.I.FTIHasDirectories);
			{
				b = o.HasDirectories(cx);
			}
			FS.Instrumentation.End();

			return b;
		}

		private FS.Context CreateContext()
		{
			return FileTree.FileDialog.CreateTreeContext(false);
		}
	}
}
