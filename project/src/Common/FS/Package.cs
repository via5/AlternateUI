﻿using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	public class PackageRootDirectory : BasicFilesystemContainer
	{
		class ShortCutsCache
		{
			private readonly PackageRootDirectory pr_;
			private readonly string root_;
			private List<ShortCut> list_ = null;
			private Dictionary<string, ShortCut> map_ = null;
			private bool latestOnly_ = false;
			private int cacheToken_ = -1;

			public ShortCutsCache(PackageRootDirectory pr, string root)
			{
				pr_ = pr;
				root_ = root;
			}

			public List<ShortCut> GetShortCuts()
			{
				return list_;
			}

			public bool Stale(Context cx)
			{
				if (latestOnly_ != cx.LatestPackagesOnly)
					return true;

				return (cacheToken_ != pr_.fs_.CacheToken);
			}

			public ShortCut GetShortcut(string name)
			{
				ShortCut sc = null;
				map_?.TryGetValue(name, out sc);

				return sc;
			}

			public void Refresh(Context cx)
			{
				cacheToken_ = pr_.fs_.CacheToken;
				latestOnly_ = cx.LatestPackagesOnly;

				Instrumentation.Start(I.RefreshShortCuts);
				{
					if (list_ == null)
						list_ = new List<ShortCut>();
					else
						list_.Clear();

					if (map_ == null)
						map_ = new Dictionary<string, ShortCut>();
					else
						map_.Clear();

					var scs = SysWrappers.GetShortCutsForDirectory(pr_, root_);

					foreach (var sc in scs)
					{
						if (string.IsNullOrEmpty(sc.package))
							continue;

						if (!string.IsNullOrEmpty(sc.packageFilter))
							continue;

						if (sc.path == "AddonPackages")
							continue;

						if (cx.LatestPackagesOnly && !sc.isLatest)
							continue;

						list_.Add(sc);
						map_.Add(sc.package, sc);
					}
				}
				Instrumentation.End();
			}
		}


		class PackagesCache
		{
			private readonly PackageRootDirectory pr_;
			private List<IFilesystemContainer> packages_ = null;

			private string search_ = null;
			private List<IFilesystemContainer> searchedPackages_ = null;
			private int cacheToken_ = -1;

			public PackagesCache(PackageRootDirectory pr)
			{
				pr_ = pr;
			}

			public Logger Log
			{
				get { return pr_.Log; }
			}

			public bool Stale(Context cx)
			{
				return (cacheToken_ != pr_.fs_.CacheToken);
			}

			public List<IFilesystemContainer> GetPackages(Context cx)
			{
				if (cx.PackagesSearch.Empty || packages_ == null)
					return packages_;

				if (search_ != cx.PackagesSearch.String || searchedPackages_ == null)
				{
					search_ = cx.PackagesSearch.String;

					if (searchedPackages_ == null)
						searchedPackages_ = new List<IFilesystemContainer>();
					else
						searchedPackages_.Clear();

					foreach (var p in packages_)
					{
						if (cx.PackagesSearch.Matches(cx, p))
							searchedPackages_.Add(p);
					}
				}

				return searchedPackages_;
			}

			public void Refresh(Context cx, ShortCutsCache scs)
			{
				cacheToken_ = pr_.fs_.CacheToken;
				searchedPackages_ = null;

				Instrumentation.Start(I.RefreshPackages);
				{
					if (packages_ == null)
						packages_ = new List<IFilesystemContainer>();
					else
						packages_.Clear();

					foreach (var sc in scs.GetShortCuts())
						packages_.Add(new Package(pr_.fs_, pr_, sc));
				}
				Instrumentation.End();
			}
		}


		private readonly Dictionary<string, ShortCutsCache> shortcuts_ =
			new Dictionary<string, ShortCutsCache>();

		private readonly Dictionary<string, PackagesCache> packages_ =
			new Dictionary<string, PackagesCache>();


		public PackageRootDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages")
		{
		}

		public override void ClearCache()
		{
			base.ClearCache();
			shortcuts_.Clear();
			packages_.Clear();
		}

		public void ClearPackagesCache()
		{
			base.ClearCache();
			packages_.Clear();
		}

		protected override string DoGetDebugName()
		{
			return "PackageRootDirectory";
		}

		protected override VUI.Icon GetIcon()
		{
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
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool IsInternal
		{
			get { return true; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		public List<IFilesystemContainer> GetPackages(Context cx)
		{
			var pi = GetPackagesInfo(cx);
			return pi?.GetPackages(cx);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return GetPackages(cx);
		}

		protected override bool DoHasDirectories(Context cx)
		{
			var ps = GetPackages(cx);
			return (ps != null && ps.Count > 0);
		}

		protected override bool DoIncludeDirectory(Context cx, IFilesystemContainer o)
		{
			return true;
		}

		private PackagesCache GetPackagesInfo(Context cx)
		{
			ShortCutsCache scc;

			if (shortcuts_.TryGetValue(cx.PackagesRoot, out scc))
			{
				if (scc.Stale(cx))
					scc.Refresh(cx);
			}
			else
			{
				scc = new ShortCutsCache(this, cx.PackagesRoot);
				scc.Refresh(cx);
				shortcuts_.Add(cx.PackagesRoot, scc);
			}


			PackagesCache pc;

			if (packages_.TryGetValue(cx.PackagesRoot, out pc))
			{
				if (pc.Stale(cx))
					pc.Refresh(cx, scc);

				return pc;
			}
			else
			{
				pc = new PackagesCache(this);
				packages_.Add(cx.PackagesRoot, pc);
				pc.Refresh(cx, scc);
			}

			return pc;
		}
	}


	class VirtualPackageDirectory : VirtualDirectory
	{
		private readonly Package p_;
		public List<IFilesystemContainer> children_ = null;

		public VirtualPackageDirectory(
			Filesystem fs, Package p, IFilesystemContainer parent,
			string name, Context cx)
				: base(fs, parent, name, cx)
		{
			p_ = p;
		}

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override string DoGetDebugName()
		{
			return "VirtualPackageDirectory";
		}

		protected override string DoGetDebugInfo()
		{
			return $"{p_?.ShortCut?.package}:{Name}";
		}

		public void AddChild(IFilesystemContainer c)
		{
			if (children_ == null)
				children_ = new List<IFilesystemContainer>();

			children_.Add(c);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return children_;
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return (children_ != null && children_.Count > 0);
		}
	}


	class RealPackageDirectory : FSDirectory
	{
		private readonly Package p_;

		public RealPackageDirectory(Filesystem fs, Package p, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
			p_ = p;
		}

		public string ShortCutPath
		{
			get { return p_?.ShortCut?.path; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		protected override string DoGetDebugName()
		{
			return "RealPackageDirectory";
		}

		protected override string DoGetDebugInfo()
		{
			return $"{ShortCutPath}, p={p_}";
		}
	}


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly ShortCut sc_ = null;
		private VirtualPackageDirectory rootDir_ = null;


		public Package(Filesystem fs, IFilesystemContainer parent, ShortCut sc)
			: base(fs, parent, sc.package)
		{
			sc_ = sc;
		}

		public override void ClearCache()
		{
			base.ClearCache();
		}

		protected override string DoGetDebugName()
		{
			return "Package({ShortCut.Package})";
		}

		public override IPackage ParentPackage
		{
			get { return this; }
		}

		public ShortCut ShortCut
		{
			get { return sc_; }
		}

		public override string Tooltip
		{
			get
			{
				var sc = ShortCut;
				var tt = base.Tooltip;

				tt +=
					$"\npackage={sc.package}" +
					$"\nfilter={sc.packageFilter}" +
					$"\nflatten={sc.flatten}" +
					$"\nhidden={sc.isHidden}" +
					$"\npath={sc.path}";

				return tt;
			}
		}

		protected override string DoGetDisplayName(Context cx)
		{
			return ShortCut.package;
		}

		protected override DateTime GetDateCreated()
		{
			return SysWrappers.FileCreationTime(this, GetFilePath());
		}

		protected override DateTime GetDateModified()
		{
			return SysWrappers.FileLastWriteTime(this, GetFilePath());
		}

		public virtual string GetRelativeVirtualPath(IFilesystemObject o)
		{
			var thisVP = VirtualPath;
			var oVP = o.VirtualPath;

			if (!thisVP.EndsWith("/"))
				thisVP += "/";

			if (!o.IsFile && !oVP.EndsWith("/"))
				oVP += "/";

			if (oVP.StartsWith(thisVP))
				return fs_.GetRoot().Name + "/" + oVP.Substring(thisVP.Length);
			else
				return o.VirtualPath;
		}

		protected override VUI.Icon GetIcon()
		{
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
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool IsInternal
		{
			get { return false; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			var path = ShortCut.path;

			int col = path.IndexOf(':');
			if (col == -1)
				return path;
			else
				return path.Substring(0, col) + ":/";
		}

		public override ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			ResolveResult rr;

			Instrumentation.Start(I.PackageResolveInternal);
			{
				rr = DoResolveInternal(cx, cs, flags, debug);
			}
			Instrumentation.End();

			return rr;
		}

		private ResolveResult DoResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			if (debug.Enabled)
				debug.Info(this, $"resolveinternal cs={cs} flags={flags}");

			if (cs.Done)
			{
				if (debug.Enabled)
					debug.Info(this, $"done, cs={cs}");

				return ResolveResult.NotFound();
			}

			if (sc_ == null)
			{
				Log.Error($"{this} ResolveInternal: null shortcut");
				return ResolveResult.NotFound();
			}


			{
				var pn = new StringView(cs.Current);

				if (pn.EndsWith(":"))
					pn = pn.Substring(0, pn.Length - 1);

				if (pn.EndsWith(".var"))
					pn = pn.Substring(0, pn.Length - 4);

				if (pn != sc_.package)
				{
					if (debug.Enabled)
						debug.Info(this, $"resolve bad, {pn}!={sc_.package}");

					return ResolveResult.NotFound();
				}

			}

			if (debug.Enabled)
				debug.Info(this, $"resolved to this cs={cs}");

			cs.Next();

			while (!cs.Done && cs.Current == "")
				cs.Next();

			if (cs.Done)
				return ResolveResult.Found(this);

			return base.ResolveInternal2(cx, cs, flags, debug);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var rd = GetRootDirectory(cx);

			if (rd == null)
				return new List<IFilesystemContainer>();
			else
				return new List<IFilesystemContainer> { rd };
		}

		protected override bool DoHasDirectories(Context cx)
		{
			var rd = GetRootDirectory(cx);
			return (rd != null);
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			return base.DoGetFiles(cx);
		}

		protected override bool DoIncludeFile(Context cx, IFilesystemObject o)
		{
			if (cx.ShowHiddenFiles)
				return true;

			return (o.Name != "meta.json");
		}

		private string GetFilePath()
		{
			string path = sc_.path;
			var col = path.IndexOf(":");

			if (col == -1)
				return path;
			else
				return path.Substring(0, col);
		}

		private IFilesystemContainer GetRootDirectory(Context cx)
		{
			if (rootDir_ != null)
				return rootDir_;

			string path = sc_.path;
			var col = path.IndexOf(":");
			if (col != -1)
				path = path.Substring(col + 1);

			path = path.Replace('\\', '/');
			if (path.StartsWith("/"))
				path = path.Substring(1);

			var cs = path.Split('/');
			if (cs == null || cs.Length == 0 || (cs.Length == 1 && cs[0] == ""))
				return null;

			rootDir_ = new VirtualPackageDirectory(fs_, this, this, cs[0], cx);

			VirtualPackageDirectory parent = rootDir_;
			for (int i = 1; i < cs.Length - 1; ++i)
			{
				var d = new VirtualPackageDirectory(fs_, this, parent, cs[i], cx);
				parent.AddChild(d);
				parent = d;
			}

			{
				var d = new RealPackageDirectory(fs_, this, parent, cs[cs.Length - 1]);
				parent.AddChild(d);
			}

			return rootDir_;
		}
	}
}
