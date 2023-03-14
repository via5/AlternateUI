using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class PackageRootDirectory : BasicFilesystemContainer
	{
		class PackagesInfo
		{
			public PackageRootDirectory pr;
			public readonly string root;
			public List<IFilesystemContainer> packages = null;
			public Dictionary<string, ShortCut> shortCuts = null;

			public PackagesInfo(PackageRootDirectory pr, string root)
			{
				this.pr = pr;
				this.root = root;
			}

			public Logger Log
			{
				get { return pr.Log; }
			}

			public void Refresh(bool showHiddenFolders)
			{
				if (packages == null)
					packages = new List<IFilesystemContainer>();
				else
					packages.Clear();

				if (shortCuts == null)
					shortCuts = new Dictionary<string, ShortCut>();
				else
					shortCuts.Clear();

				foreach (var sc in FMS.GetShortCutsForDirectory(root))
				{
					if (string.IsNullOrEmpty(sc.package))
						continue;

					if (!string.IsNullOrEmpty(sc.packageFilter))
						continue;

					if (sc.path == "AddonPackages")
						continue;

					packages.Add(new Package(pr.fs_, pr, sc.package, root, showHiddenFolders));
					shortCuts.Add(sc.package, sc);
				}
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

		public override string MakeRealPath()
		{
			return "";
		}

		public List<IFilesystemContainer> GetPackages(Context cx)
		{
			var pi = GetPackagesInfo(cx.PackagesRoot, cx.ShowHiddenFolders);
			return pi?.packages;
		}

		public ShortCut GetShortCut(
			string name, string packagesRoot, bool showHiddenFolders)
		{
			var pi = GetPackagesInfo(packagesRoot, showHiddenFolders);
			if (pi == null)
				return null;

			ShortCut sc;
			pi.shortCuts.TryGetValue(name, out sc);

			return sc;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return GetPackages(cx);
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
			get { return FMS.FileCreationTime(ShortCut.path); }
		}

		public override DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(ShortCut.path); }
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
			if (debug.Enabled)
				debug.Info(this, $"resolveinternal cs={cs} flags={flags}");

			Get();

			if (cs.Done)
			{
				debug.Info(this, $"done, cs={cs}");
				return ResolveResult.NotFound();
			}

			var sc = ShortCut;
			if (sc == null)
			{
				Log.Error($"{this} ResolveInternal: null shortcut");
				return ResolveResult.NotFound();
			}

			var col = sc.path.IndexOf(":");
			if (col == -1)
			{
				Log.Error(
					$"{this} ResolveInternal: shortcut path has no " +
					$"colon '{sc.path}'");

				return ResolveResult.NotFound();
			}

			var pn = cs.Current;
			if (pn.EndsWith(":"))
				pn = pn.Substring(0, pn.Length - 1);

			if (pn != sc.package)
			{
				if (debug.Enabled)
					debug.Info(this, $"resolve bad, {pn}!={sc.package}");

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
				return base.DoGetDirectories(cx);
			else
				return new List<IFilesystemContainer> { rootDir_ };
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
