﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AUI.FS
{
	class VirtualDirectory : BasicFilesystemContainer, IDirectory
	{
		private const int MaxTooltip = 20;

		private HashSet<IFilesystemContainer> dirs_ = null;
		private HashSet<IFilesystemContainer> mergedDirs_ = null;
		private List<IFilesystemContainer> sortedDirs_ = null;
		private List<string> tooltip_ = null;
		private Context lastContext_ = null;
		private bool merged_ = false;
		private int mergedToken_ = -1;

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent,
			IFilesystemContainer content, Context lastContext = null)
				: this(fs, parent, content.Name, lastContext)
		{
			if (content != null)
				Add(content);
		}

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent, string name,
			Context lastContext = null)
				: base(fs, parent, name)
		{
			lastContext_ = lastContext;
		}

		protected override string DoGetDebugName()
		{
			return "VirtualDirectory";
		}

		private IFilesystemContainer SingleContent
		{
			get
			{
				if (ContainedDirectories == null || ContainedDirectories.Count != 1)
					return null;
				else
					return ContainedDirectories.First();
			}
		}

		public HashSet<IFilesystemContainer> ContainedDirectories
		{
			get { return mergedDirs_ ?? dirs_; }
		}

		public override void ClearCache()
		{
			base.ClearCache();

			if (ContainedDirectories != null)
			{
				foreach (var d in ContainedDirectories)
					d.ClearCache();
			}

			sortedDirs_ = null;
			SetStaleMerged();
		}

		private void SetStaleMerged()
		{
			merged_ = false;
			mergedDirs_ = null;
			mergedToken_ = -1;
		}

		private void SetMerged()
		{
			merged_ = true;
			mergedToken_ = fs_.CacheToken;
		}

		protected override string DoGetDisplayName(Context cx)
		{
			return SingleContent?.Name ?? Name;
		}

		public override string Tooltip
		{
			get { return MakeTooltip(); }
		}

		private bool RequireAllDirectories(Context cx)
		{
			if (merged_ && mergedToken_ == fs_.CacheToken)
				return false;

			if (cx == null)
			{
				Log.ErrorST($"{this}: trying to merge, but no context");
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

			if (sortedDirs_ == null && ContainedDirectories != null)
			{
				sortedDirs_ = new List<IFilesystemContainer>(ContainedDirectories);
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

		private string MakeTooltip()
		{
			RequireAllDirectories(lastContext_);

			var s = base.Tooltip;

			if (ContainedDirectories != null && ContainedDirectories.Count > 0)
			{
				if (tooltip_ == null)
					tooltip_ = new List<string>();
				else
					tooltip_.Clear();

				int count = MakeTooltipInternal(
					lastContext_, tooltip_, MaxTooltip, U.DevMode);

				string dirSources = "";

				for (int i = 0; i < Math.Min(tooltip_.Count, MaxTooltip); ++i)
					dirSources += $"\n  - {tooltip_[i]}";

				if (count > MaxTooltip)
					dirSources += $"\n  +{count - MaxTooltip} more";

				s += $"\nSources:{dirSources}";
			}

			return s;
		}

		private int MakeTooltipInternal(
			Context cx, List<string> outList, int max, bool devMode)
		{
			if (ContainedDirectories == null)
				return 0;

			int count = 0;

			if (outList.Count < int.MaxValue)
			{
				foreach (var d in SortedDirs(cx))
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
							s += p.GetDisplayName(cx);
					}

					if (tooltip_.Count < max)
						tooltip_.Add(s);

					++count;

					if (d is VirtualDirectory)
						count += (d as VirtualDirectory).MakeTooltipInternal(cx, outList, max, devMode);
				}
			}
			else
			{
				foreach (var d in ContainedDirectories)
				{
					++count;

					if (d is VirtualDirectory)
						count += (d as VirtualDirectory).MakeTooltipInternal(cx, outList, max, devMode);
				}
			}

			return count;
		}

		public void Add(IFilesystemContainer c)
		{
			if (dirs_ == null)
				dirs_ = new HashSet<IFilesystemContainer>();

			if (!dirs_.Contains(c))
			{
				dirs_.Add(c);
				SetStaleMerged();
				sortedDirs_ = null;
			}
		}

		public void AddRange(IEnumerable<IFilesystemContainer> c)
		{
			foreach (var cc in c)
				Add(cc);

			SetStaleMerged();
			sortedDirs_ = null;
		}

		protected override DateTime GetDateCreated()
		{
			var c = SingleContent;
			if (c == null)
				return Sys.BadDateTime;
			else
				return c.DateCreated;
		}

		protected override DateTime GetDateModified()
		{
			var c = SingleContent;
			if (c == null)
				return Sys.BadDateTime;
			else
				return c.DateModified;
		}

		protected override VUI.Icon GetIcon()
		{
			if (ContainsWritableDirectory())
				return Icons.GetIcon(Icons.Directory);
			else
				return Icons.GetIcon(Icons.PackageDark);
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
				if (ContainedDirectories != null)
				{
					foreach (var d in ContainedDirectories)
					{
						if (!d.IsInternal)
							return false;
					}
				}

				return true;
			}
		}

		public override bool IsWritable
		{
			get { return ContainsWritableDirectory(); }
		}


		public override string MakeRealPath()
		{
			return "";
		}

		public override string DeVirtualize()
		{
			if (ContainedDirectories != null)
			{
				foreach (var d in ContainedDirectories)
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

			if (ContainedDirectories != null)
			{
				// see DoGetFiles() below
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "", "",
					cx.Whitelist);

				foreach (var d in ContainedDirectories)
				{
					var ds = d.GetDirectories(cx2);
					if (ds != null)
						list.UnionWith(ds);
				}
			}

			var map = new Dictionary<string, IFilesystemContainer>();
			UntangleVDs(list, map);

			var list2 = new List<IFilesystemContainer>();
			foreach (var ss in map)
			{
				if (ss.Value is VirtualDirectory)
				{
					var ssvd = ss.Value as VirtualDirectory;
					ssvd.SetMerged();
				}

				list2.Add(ss.Value);
			}

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
			if (ContainedDirectories == null)
				return false;

			// see DoGetFiles() below
			var cx2 = new Context(
				"", null, cx.PackagesRoot,
				Context.NoSort, Context.NoSortDirection, cx.Flags, "", "",
				cx.Whitelist);

			foreach (var d in ContainedDirectories)
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

			if (ContainedDirectories != null)
			{
				// this needs to get the raw files, not filtered, so get a new
				// context with the same flags only
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "", "",
					cx.Whitelist);

				foreach (var d in ContainedDirectories)
				{
					var fs = d.GetFiles(cx2);
					if (fs != null)
						list.AddRange(fs);
				}
			}

			return list;
		}

		private void UntangleVDs(
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
						UntangleVDs(vd.dirs_, map);
				}
				else
				{
					IFilesystemContainer c;
					if (map.TryGetValue(d.Name, out c))
					{
						if (c is VirtualDirectory && !(c is VirtualPackageDirectory))
						{
							var vd = (c as VirtualDirectory);
							AddUntangled(vd, d);
						}
						else
						{
							var vd = new VirtualDirectory(fs_, this, d.Name);

							AddUntangled(vd, d);
							AddUntangled(vd, c);

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
						AddUntangled(vd, d);
						map.Add(d.Name, vd);
					}
				}
			}
		}

		private void AddUntangled(VirtualDirectory vd, IFilesystemContainer d)
		{
			if (d.ParentPackage == null)
			{
				if (vd.dirs_ == null)
					vd.dirs_ = new HashSet<IFilesystemContainer>();

				if (!vd.dirs_.Contains(d))
					vd.dirs_.Add(d);

			}

			if (vd.mergedDirs_ == null)
				vd.mergedDirs_ = new HashSet<IFilesystemContainer>();

			if (!vd.mergedDirs_.Contains(d))
				vd.mergedDirs_.Add(d);
		}

		private bool MergePackages(Context cx)
		{
			if (mergedDirs_ != null)
				mergedDirs_.Clear();

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

			changed = MergeAdd(list);

			if (dirs_ != null && dirs_.Count > 0)
			{
				if (mergedDirs_ == null)
					mergedDirs_ = new HashSet<IFilesystemContainer>();

				foreach (var d in dirs_)
					mergedDirs_.Add(d);
			}

			SetMerged();

			return changed;
		}

		private bool MergeAdd(HashSet<IFilesystemContainer> list)
		{
			bool changed = false;

			foreach (var c in list)
			{
				// todo; a VirtualPackageDirectory isn't a real virtual
				// directory, it has no content, but it has its own children,
				// so treat is separately for now
				if (c is VirtualDirectory && !(c is VirtualPackageDirectory))
				{
					var vd = c as VirtualDirectory;
					if (vd.ContainedDirectories != null)
					{
						if (MergeAdd(vd.ContainedDirectories))
							changed = true;
					}
				}
				else
				{
					if (mergedDirs_ == null)
						mergedDirs_ = new HashSet<IFilesystemContainer>();

					mergedDirs_.Add(c);
					changed = true;
				}
			}

			return changed;
		}

		public bool ContainsWritableDirectory()
		{
			if (ContainedDirectories != null)
			{
				foreach (var d in ContainedDirectories)
				{
					if (d.IsWritable)
						return true;
				}
			}

			return false;
		}
	}

}
