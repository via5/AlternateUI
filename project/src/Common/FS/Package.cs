﻿using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class PackageRootDirectory : BasicFilesystemContainer
	{
		class PackagesInfo
		{
			private PackageRootDirectory pr_;
			private readonly string root_;
			private List<IFilesystemContainer> packages_ = null;
			private Dictionary<string, ShortCut> shortCuts_ = null;

			private string search_ = null;
			private List<IFilesystemContainer> searchedPackages_ = null;

			public PackagesInfo(PackageRootDirectory pr, string root)
			{
				pr_ = pr;
				root_ = root;
			}

			public Logger Log
			{
				get { return pr_.Log; }
			}

			public List<IFilesystemContainer> GetPackages(Context cx)
			{
				if (cx.PackagesSearch.Empty || packages_ == null)
					return packages_;

				if (search_ != cx.PackagesSearch.String)
				{
					search_ = cx.PackagesSearch.String;

					if (searchedPackages_ == null)
						searchedPackages_ = new List<IFilesystemContainer>();
					else
						searchedPackages_.Clear();

					foreach (var p in packages_)
					{
						if (cx.PackagesSearch.Matches(p.DisplayName))
							searchedPackages_.Add(p);
					}
				}

				return searchedPackages_;
			}

			public ShortCut GetShortcut(string name)
			{
				ShortCut sc = null;
				shortCuts_.TryGetValue(name, out sc);

				return sc;
			}

			public void Refresh(bool showHiddenFolders)
			{
				Instrumentation.Start(I.RefreshPackages);
				{
					if (packages_ == null)
						packages_ = new List<IFilesystemContainer>();
					else
						packages_.Clear();

					if (shortCuts_ == null)
						shortCuts_ = new Dictionary<string, ShortCut>();
					else
						shortCuts_.Clear();

					var scs = Sys.GetShortCutsForDirectory(pr_, root_);

					foreach (var sc in scs)
					{
						if (string.IsNullOrEmpty(sc.package))
							continue;

						if (!string.IsNullOrEmpty(sc.packageFilter))
							continue;

						if (sc.path == "AddonPackages")
							continue;

						packages_.Add(new Package(pr_.fs_, pr_, sc.package, root_, showHiddenFolders));
						shortCuts_.Add(sc.package, sc);
					}
				}
				Instrumentation.End();
			}
		}

		private readonly Dictionary<string, PackagesInfo> packages_ =
			new Dictionary<string, PackagesInfo>();


		public PackageRootDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages")
		{
		}

		public override string ToString()
		{
			return $"PackageRootDirectory";
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Package); }
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

		public override string MakeRealPath()
		{
			return "";
		}

		public List<IFilesystemContainer> GetPackages(Context cx)
		{
			var pi = GetPackagesInfo(cx.PackagesRoot, cx.ShowHiddenFolders);
			return pi?.GetPackages(cx);
		}

		public ShortCut GetShortCut(
			string name, string packagesRoot, bool showHiddenFolders)
		{
			var pi = GetPackagesInfo(packagesRoot, showHiddenFolders);
			if (pi == null)
				return null;

			return pi.GetShortcut(name);
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

		private PackagesInfo GetPackagesInfo(
			string packagesRoot, bool showHiddenFolders)
		{
			PackagesInfo pi;

			if (packages_.TryGetValue(packagesRoot, out pi))
				return pi;

			pi = new PackagesInfo(this, packagesRoot);
			packages_.Add(packagesRoot, pi);

			pi.Refresh(showHiddenFolders);

			return pi;
		}
	}


	class VirtualPackageDirectory : VirtualDirectory
	{
		private readonly Package p_;
		private List<IFilesystemContainer> children_ = null;

		public VirtualPackageDirectory(Filesystem fs, Package p, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
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

		public override string ToString()
		{
			return $"VirtualPackageDirectory({p_?.ShortCut?.package}:{Name})";
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

		public override string ToString()
		{
			return $"RealPackageDirectory({p_?.ShortCut?.path})";
		}
	}


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly string name_;
		private ShortCut sc_ = null;
		private string packagesRoot_;
		private bool showHiddenFolders_ = false;
		private VirtualPackageDirectory rootDir_ = null;

		public Package(
			Filesystem fs, IFilesystemContainer parent, string name,
			string packagesRoot, bool showHiddenFolders)
			: base(fs, parent, name)
		{
			name_ = name;
			packagesRoot_ = packagesRoot;
			showHiddenFolders_ = showHiddenFolders;
		}

		public override string ToString()
		{
			return $"Package({ShortCut.package})";
		}

		public override IPackage ParentPackage
		{
			get { return this; }
		}

		public ShortCut ShortCut
		{
			get
			{
				Get();
				return sc_;
			}
		}

		private void Get()
		{
			if (sc_ == null)
			{
				sc_ = fs_.GetPackagesRoot()
					.GetShortCut(name_, packagesRoot_, showHiddenFolders_);

				CreatePackageDirectories();
			}
		}

		private void CreatePackageDirectories()
		{
			rootDir_ = null;

			string path = sc_.path;
			var col = path.IndexOf(":");
			if (col != -1)
				path = path.Substring(col + 1);

			path = path.Replace('\\', '/');
			if (path.StartsWith("/"))
				path = path.Substring(1);

			var cs = path.Split('/');
			if (cs == null || cs.Length == 0 || (cs.Length == 1 && cs[0] == ""))
				return;

			rootDir_ = new VirtualPackageDirectory(fs_, this, this, cs[0]);

			VirtualPackageDirectory parent = rootDir_;
			for (int i = 1; i < cs.Length - 1; ++i)
			{
				var d = new VirtualPackageDirectory(fs_, this, parent, cs[i]);
				parent.AddChild(d);
				parent = d;
			}

			{
				var d = new RealPackageDirectory(fs_, this, parent, cs[cs.Length - 1]);
				parent.AddChild(d);
			}
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

		protected override string GetDisplayName()
		{
			return ShortCut.package;
		}

		public override DateTime DateCreated
		{
			get { return Sys.FileCreationTime(this, ShortCut.path); }
		}

		public override DateTime DateModified
		{
			get { return Sys.FileLastWriteTime(this, ShortCut.path); }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Package); }
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

			Get();

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

			var pn = cs.Current;
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
			if (showHiddenFolders_ != cx.ShowHiddenFolders)
			{
				sc_ = null;
				showHiddenFolders_ = cx.ShowHiddenFolders;
			}

			Get();

			if (rootDir_ == null)
				return new List<IFilesystemContainer>();
			else
				return new List<IFilesystemContainer> { rootDir_ };
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return (rootDir_ != null);
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			Get();

			if (showHiddenFolders_ != cx.ShowHiddenFolders)
			{
				sc_ = null;
				showHiddenFolders_ = cx.ShowHiddenFolders;
			}

			return base.DoGetFiles(cx);
		}

		protected override bool IncludeFile(Context cx, IFilesystemObject o)
		{
			if (cx.ShowHiddenFiles)
				return true;

			return (o.Name != "meta.json");
		}
	}
}
