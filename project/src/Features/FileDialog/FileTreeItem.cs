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
				o_ = FS.Filesystem.Instance.Resolve<FS.IFilesystemContainer>(
					CreateContext(), path_, FS.Filesystem.ResolveDefault | moreFlags);
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
			o_ = null;

			var o = GetFSObject();
			Text = o?.DisplayName ?? "(dead)";

			if (!checkedHasChildren_)
				return;

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

			var dirs = GetSubDirectories(o);

			{
				var cs = GetInternalChildren();

				if (cs != null)
				{
					int i = 0;
					while (i < cs.Count)
					{
						bool found = false;
						var c = cs[i] as FileTreeItem;
						var co = c.GetFSObject();

						if (co != null)
						{
							for (int j = 0; j < dirs.Count; ++j)
							{
								if (co.IsSameObject(dirs[j]))
								{
									found = true;
									break;
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
								c.Refresh(true);

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
				hasChildren_ = (d != null && !d.IsFlattened && HasSubDirectories(d));
				checkedHasChildren_ = true;
			}

			return hasChildren_;
		}

		protected override void GetChildren()
		{
			var d = GetFSObject();

			if (d != null && !d.IsFlattened)
			{
				foreach (var c in GetSubDirectories(d))
					Add(CreateItem(c));
			}
		}

		private FileTreeItem CreateItem(FS.IFilesystemContainer o)
		{
			if (o.UnderlyingCanChange)
			{
				return new FileTreeItem(
					tree_, o.VirtualPath, o.DisplayName, o.Icon);
			}
			else
			{
				return new FileTreeItem(tree_, o);
			}
		}

		private List<FS.IFilesystemContainer> GetSubDirectories(FS.IFilesystemContainer o)
		{
			return o.GetDirectories(CreateContext());
		}

		private bool HasSubDirectories(FS.IFilesystemContainer o)
		{
			return o.HasDirectories(CreateContext());
		}

		private FS.Context CreateContext()
		{
			return FileTree.FileDialog.CreateTreeContext(false);
		}
	}
}
