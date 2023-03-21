using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AUI.FS
{
	class VirtualDirectory : BasicFilesystemContainer, IDirectory
	{
		private HashSet<IFilesystemContainer> dirs_ = null;
		private List<IFilesystemContainer> sortedDirs_ = null;
		private List<string> tooltip_ = null;
		private Context lastContext_ = null;
		private bool merged_ = false;

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent,
			IFilesystemContainer content)
				: this(fs, parent, content.Name)
		{
			if (content != null)
				Add(content);
		}

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent, string name)
				: base(fs, parent, name)
		{
		}

		public override string ToString()
		{
			return $"VirtualDirectory({VirtualPath})";
		}

		private IFilesystemContainer SingleContent
		{
			get
			{
				if (dirs_ == null || dirs_.Count != 1)
					return null;
				else
					return dirs_.First();
			}
		}

		public override void ClearCache()
		{
			base.ClearCache();

			if (dirs_ != null)
			{
				foreach (var d in dirs_)
					d.ClearCache();

				sortedDirs_ = null;
			}

			merged_ = false;
		}

		protected override string GetDisplayName()
		{
			return SingleContent?.Name ?? Name;
		}

		public override string Tooltip
		{
			get
			{
				RequireAllDirectories(lastContext_);

				var s = base.Tooltip;

				if (dirs_ != null && dirs_.Count > 0)
				{
					if (tooltip_ == null)
						tooltip_ = new List<string>();
					else
						tooltip_.Clear();

					MakeTooltip(lastContext_, SortedDirs(lastContext_), U.DevMode);

					string dirSources = "";

					for (int i = 0; i < Math.Min(tooltip_.Count, 20); ++i)
						dirSources += $"\n  - {tooltip_[i]}";

					if (tooltip_.Count > 20)
						dirSources += $"\n  - +{tooltip_.Count - 20} more";

					s += $"\nSources:{dirSources}";
				}

				return s;
			}
		}

		private bool RequireAllDirectories(Context cx)
		{
			if (merged_)
				return false;

			if (cx == null)
			{
				Log.Error($"{this}: trying to merge, but no context");
				return false;
			}

			if (cx.MergePackages)
			{
				if (ParentPackage == null && !IsInternal)
					return MergePackages(cx);
			}

			return false;
		}

		private List<IFilesystemContainer> SortedDirs(Context cx)
		{
			RequireAllDirectories(cx);

			if (sortedDirs_ == null && dirs_ != null)
			{
				sortedDirs_ = new List<IFilesystemContainer>(dirs_);
				sortedDirs_.Sort((a, b) =>
				{
					var ap = a.ParentPackage;
					var bp = b.ParentPackage;

					if (ap == null && bp != null)
					{
						return -1;
					}
					else if (ap != null && bp == null)
					{
						return 1;
					}
					else
					{
						return U.CompareNatural(a.VirtualPath, b.VirtualPath);
					}
				});
			}

			return sortedDirs_;
		}

		private void MakeTooltip(
			Context cx, List<IFilesystemContainer> dirs, bool devMode)
		{
			if (dirs == null)
				return;

			foreach (var d in dirs)
			{
				string s = "";

				if (devMode)
				{
					s += d.ToString();
				}
				else
				{
					var p = d.ParentPackage;

					if (p == null)
						s += d.VirtualPath;
					else
						s += p.DisplayName;
				}

				tooltip_.Add(s);

				if (d is VirtualDirectory)
					MakeTooltip(cx, (d as VirtualDirectory).SortedDirs(cx), devMode);
			}
		}

		public void Add(IFilesystemContainer c)
		{
			if (dirs_ == null)
				dirs_ = new HashSet<IFilesystemContainer>();

			if (!dirs_.Contains(c))
			{
				dirs_.Add(c);
				sortedDirs_ = null;
			}
		}

		public void AddRange(IEnumerable<IFilesystemContainer> c)
		{
			if (dirs_ == null)
				dirs_ = new HashSet<IFilesystemContainer>();

			foreach (var cc in c)
				Add(cc);

			sortedDirs_ = null;
		}

		public override DateTime DateCreated
		{
			get
			{
				var c = SingleContent;
				if (c == null)
					return DateTime.MaxValue;
				else
					return c.DateCreated;
			}
		}

		public override DateTime DateModified
		{
			get
			{
				var c = SingleContent;
				if (c == null)
					return DateTime.MaxValue;
				else
					return c.DateModified;
			}
		}

		public override VUI.Icon Icon
		{
			get
			{
				if (HasRealDir())
					return Icons.Get(Icons.Directory);
				else
					return Icons.Get(Icons.Package);
			}
		}

		public override bool CanPin
		{
			get { return true; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool AlreadySorted
		{
			get { return false; }
		}

		public override bool IsInternal
		{
			get
			{
				if (dirs_ != null)
				{
					foreach (var d in dirs_)
					{
						if (!d.IsInternal)
							return false;
					}
				}

				return true;
			}
		}


		public override string MakeRealPath()
		{
			return "";
		}

		public override string DeVirtualize()
		{
			if (dirs_ != null)
			{
				foreach (var d in dirs_)
				{
					if (!d.Virtual && d.ParentPackage == null)
					{
						var rp = d.DeVirtualize();
						if (rp != "")
							return rp;
					}
				}
			}

			return base.DeVirtualize();
		}

		public override bool IsSameObject(IFilesystemObject o)
		{
			if (o == null)
				return false;

			return (o.VirtualPath == VirtualPath);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			lastContext_ = cx;
			RequireAllDirectories(cx);

			var list = new HashSet<IFilesystemContainer>();

			if (dirs_ != null)
			{
				// see DoGetFiles() below
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "");

				foreach (var d in dirs_)
				{
					var ds = d.GetDirectories(cx2);
					if (ds != null)
						list.UnionWith(ds);
				}
			}

			var map = new Dictionary<string, IFilesystemContainer>();
			Merge(list, map);

			var list2 = new List<IFilesystemContainer>();
			foreach (var ss in map)
				list2.Add(ss.Value);

			return list2;
		}

		protected override bool DoHasDirectories(Context cx)
		{
			lastContext_ = cx;

			if (HasDirectoriesInternal(cx))
				return true;

			// try again with all directories
			// todo: skip the ones already tried
			if (RequireAllDirectories(cx))
			{
				if (HasDirectoriesInternal(cx))
					return true;
			}

			return false;
		}

		private bool HasDirectoriesInternal(Context cx)
		{
			if (dirs_ == null)
				return false;

			// see DoGetFiles() below
			var cx2 = new Context(
				"", null, cx.PackagesRoot,
				Context.NoSort, Context.NoSortDirection, cx.Flags, "");

			foreach (var d in dirs_)
			{
				if (d.HasDirectories(cx2))
					return true;
			}

			return false;
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			lastContext_ = cx;
			RequireAllDirectories(cx);

			var list = new List<IFilesystemObject>();

			if (dirs_ != null)
			{
				// this needs to get the raw files, not filtered, so get a new
				// context with the same flags only
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "");

				foreach (var d in dirs_)
				{
					var fs = d.GetFiles(cx2);
					if (fs != null)
						list.AddRange(fs);
				}
			}

			return list;
		}

		private void Merge(
			HashSet<IFilesystemContainer> list,
			Dictionary<string, IFilesystemContainer> map)
		{
			foreach (var d in list)
			{
				// todo; a VirtualPackageDirectory isn't a real virtual
				// directory, it has no content, but it has its own children,
				// so treat is separately for now
				if (d is VirtualDirectory && !(d is VirtualPackageDirectory))
				{
					var vd = d as VirtualDirectory;
					if (vd.dirs_ != null)
						Merge(vd.dirs_, map);
				}
				else
				{
					IFilesystemContainer c;
					if (map.TryGetValue(d.Name, out c))
					{
						if (c is VirtualDirectory)
						{
							(c as VirtualDirectory).Add(d);
						}
						else
						{
							var vd = new VirtualDirectory(fs_, this, d.Name);
							vd.Add(d);
							vd.Add(c);

							map.Remove(d.Name);
							map.Add(d.Name, vd);
						}
					}
					else if (!d.UnderlyingCanChange)
					{
						map.Add(d.Name, d);
					}
					else
					{
						var vd = new VirtualDirectory(fs_, this, d.Name);
						vd.Add(d);
						map.Add(d.Name, vd);
					}
				}
			}
		}

		private bool MergePackages(Context cx)
		{
			bool changed = false;
			var list = new HashSet<IFilesystemContainer>();

			Instrumentation.Start(I.MergePackages);
			{
				string rootName, path;

				Instrumentation.Start(I.MergePackagesStart);
				{
					rootName = fs_.GetRoot().Name + "/";

					path = VirtualPath;
					if (path.StartsWith(rootName))
						path = path.Substring(rootName.Length);
				}
				Instrumentation.End();

				List<IFilesystemContainer> packages;

				Instrumentation.Start(I.MergePackagesGetPackages);
				{
					packages = fs_.GetPackagesRoot().GetPackages(cx);
				}
				Instrumentation.End();

				foreach (var p in packages)
				{
					string rpath = p.Name + ":/" + path;
					IFilesystemObject o;

					Instrumentation.Start(I.MergePackagesResolve);
					{
						o = p.Resolve(cx, rpath, Filesystem.ResolveDirsOnly);
					}
					Instrumentation.End();

					Instrumentation.Start(I.MergePackagesAdd);
					{
						var d = o as IFilesystemContainer;
						if (d != null)
							list.Add(d);
					}
					Instrumentation.End();
				}
			}
			Instrumentation.End();

			MergeAdd(list);
			merged_ = true;

			return changed;
		}

		private void MergeAdd(HashSet<IFilesystemContainer> list)
		{
			foreach (var c in list)
			{
				if (c is VirtualDirectory)
				{
					var vd = c as VirtualDirectory;
					if (vd.dirs_ != null)
						MergeAdd(vd.dirs_);
				}
				else
				{
					Add(c);
				}
			}
		}

		private bool HasRealDir()
		{
			if (dirs_ != null)
			{
				foreach (var d in dirs_)
				{
					if (d.ParentPackage == null)
						return true;
				}
			}

			return false;
		}
	}


	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		private DateTime dateCreated_ = DateTime.MaxValue;
		private DateTime dateModified_ = DateTime.MaxValue;

		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		public override string ToString()
		{
			return $"FSDirectory({VirtualPath})";
		}

		public override DateTime DateCreated
		{
			get
			{
				if (dateCreated_ == DateTime.MaxValue)
					dateCreated_ = Sys.DirectoryCreationTime(this, MakeRealPath());

				return DateTime.MaxValue;
			}
		}

		public override DateTime DateModified
		{
			get
			{
				if (dateModified_ == DateTime.MaxValue)
					dateModified_ = Sys.DirectoryLastWriteTime(this, MakeRealPath());

				return DateTime.MaxValue;
			}
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Directory); }
		}

		public override bool CanPin
		{
			get { return true; }
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool IsInternal
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var list = new List<IFilesystemContainer>();

			Instrumentation.Start(I.BasicDoGetDirectories);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					var dirs = Sys.GetDirectories(this, path);

					foreach (var dirPath in dirs)
					{
						var vd = new VirtualDirectory(fs_, this, Path.Filename(dirPath));

						vd.Add(new FSDirectory(fs_, this, Path.Filename(dirPath)));

						list.Add(vd);
					}
				}
			}
			Instrumentation.End();

			return list;
		}
	}
}
