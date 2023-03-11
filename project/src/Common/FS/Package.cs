using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class PackageRootDirectory : BasicFilesystemContainer
	{
		private List<IFilesystemContainer> packagesScene_ = null;
		private Dictionary<string, ShortCut> shortCutsScene_ = null;

		private List<IFilesystemContainer> packagesAll_ = null;
		private Dictionary<string, ShortCut> shortCutsAll_ = null;


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
			return RefreshPackages(cx);
		}

		public ShortCut GetShortCut(string name, bool showHiddenFolders)
		{
			ShortCut sc;

			if (showHiddenFolders)
				shortCutsAll_.TryGetValue(name, out sc);
			else
				shortCutsScene_.TryGetValue(name, out sc);

			return sc;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return GetPackages(cx);
		}

		private List<IFilesystemContainer> RefreshPackages(Context cx)
		{
			var ps = (cx.ShowHiddenFolders ? packagesAll_ : packagesScene_);
			if (ps != null)
				return ps;

			ps = new List<IFilesystemContainer>();

			Dictionary<string, ShortCut> map;

			if (cx.ShowHiddenFolders)
			{
				if (shortCutsAll_ == null)
					shortCutsAll_ = new Dictionary<string, ShortCut>();

				map = shortCutsAll_;
			}
			else
			{
				if (shortCutsScene_ == null)
					shortCutsScene_ = new Dictionary<string, ShortCut>();

				map = shortCutsScene_;
			}

			string path = (cx.ShowHiddenFolders ? "" : "Saves/scene");
			map.Clear();

			foreach (var sc in FMS.GetShortCutsForDirectory(path))
			{
				if (string.IsNullOrEmpty(sc.package))
					continue;

				if (!string.IsNullOrEmpty(sc.packageFilter))
					continue;

				if (sc.path == "AddonPackages")
					continue;

				ps.Add(new Package(fs_, this, sc.package, cx.ShowHiddenFolders));
				map.Add(sc.package, sc);
			}

			if (cx.ShowHiddenFolders)
				packagesAll_ = ps;
			else
				packagesScene_ = ps;

			return ps;
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


	class RealPackageDirectory : FSDirectory2
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

		public override string MakeRealPath()
		{
			var sc = p_.ShortCut;
			if (sc == null)
				return "";

			return sc.path + "/";
		}
	}


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly string name_;
		private ShortCut sc_ = null;
		private bool showHiddenFolders_ = false;
		private VirtualPackageDirectory rootDir_ = null;

		public Package(Filesystem fs, IFilesystemContainer parent, string name, bool showHiddenFolders)
			: base(fs, parent, name)
		{
			name_ = name;
			showHiddenFolders_ = showHiddenFolders;
		}

		public override string ToString()
		{
			return $"Package({ShortCut.package})";
		}

		public ShortCut ShortCut
		{
			get
			{
				if (sc_ == null)
				{
					sc_ = fs_.GetPackagesRootDirectory()
						.GetShortCut(name_, showHiddenFolders_);

					CreatePackageDirectories();
				}

				return sc_;
			}
		}

		private void CreatePackageDirectories()
		{
			AlternateUI.Instance.Log.Info($"{this} create package dirs");

			rootDir_ = null;

			string path = sc_.path;
			var col = path.IndexOf(":");
			if (col != -1)
				path = path.Substring(col + 1);

			path = path.Replace('\\', '/');
			if (path.StartsWith("/"))
				path = path.Substring(1);

			AlternateUI.Instance.Log.Info($"  - path is {path}");

			var cs = path.Split('/');
			if (cs == null)
				return;

			rootDir_ = new VirtualPackageDirectory(fs_, this, this, cs[0]);
			AlternateUI.Instance.Log.Info($"  - root {rootDir_}");

			VirtualPackageDirectory parent = rootDir_;
			for (int i = 1; i < cs.Length - 1; ++i)
			{
				var d = new VirtualPackageDirectory(fs_, this, parent, cs[i]);
				//AlternateUI.Instance.Log.Info($"  - dir {d}");
				parent.AddChild(d);
				parent = d;
			}

			parent.AddChild(new RealPackageDirectory(fs_, this, parent, cs[cs.Length - 1]));
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
			return ShortCut.path + "/";
		}

		public override ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			if (debug.Enabled)
				debug.Info(this, $"resolveinternal cs={cs} flags={flags}");

			if (cs.Done)
			{
				debug.Info(this, $"done, cs={cs}");
				return ResolveResult.NotFound();
			}

			var sc = ShortCut;
			if (sc == null)
			{
				AlternateUI.Instance.Log.Error(
					$"{this} ResolveInternal: null shortcut");

				return ResolveResult.NotFound();
			}

			var col = sc.path.IndexOf(":");
			if (col == -1)
			{
				AlternateUI.Instance.Log.Error(
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
			/*
			debug = debug.Inc();

			//var scPath = sc.path.Substring(col + 1);

			if (dirs_.Count > 0)
			{
				//var sccs = new PathComponents(scPath);

				if (debug.Enabled)
					debug.Info(this, $"checking sc path cs={cs} dirs={dirs_}");

				bool found = true;
				int i = 0;

				while (i < dirs_.Count && !cs.Done)
				{
					if (cs.Done)
					{
						if (debug.Enabled)
							debug.Info(this, $"resolve bad, at end");

						found = false;
						break;
						//return ResolveResult.FoundPartial(this);
					}
					else if (cs.Current != dirs_[i].Name)
					{
						if (debug.Enabled)
							debug.Info(this, $"resolve bad cs={cs} dirs={dirs_}");

						found = false;
						break;
						//return ResolveResult.FoundPartial(this);
					}

					cs.Next();
					++i;
				}

				if (found && cs.Done)
				{
					if (debug.Enabled)
						debug.Info(this, $"found {dirs_[i]}");

					return ResolveResult.Found(dirs_[i]);
				}

				if (debug.Enabled)
					debug.Info(this, $"sc path failed, will fwd to base cs={cs}");
			}
			else
			{
				if (debug.Enabled)
					debug.Info(this, $"resolve ok, fwd to base cs={cs}");
			}
			*/
			return base.ResolveInternal2(cx, cs, flags, debug);
		}

		public List<IFilesystemObject> GetFilesForMerge(string path)
		{/*
			var sc = ShortCut;
			path = sc.package + ":/" + path;

			var cx = Context.None;

			var o = Resolve(cx, path, Filesystem.ResolveDirsOnly) as IFilesystemContainer;
			if (o == null)
			{
				//AlternateUI.Instance.Log.Info($"{this}: can't resolve {path}");
				return null;
			}

			return o.GetFiles(cx);*/
			return null;
		}

		public List<IFilesystemContainer> GetDirectoriesForMerge(string path)
		{
			var sc = ShortCut;
			path = sc.package + ":/" + path;

			var cx = Context.None;

			var o = Resolve(cx, path, Filesystem.ResolveDirsOnly) as IFilesystemContainer;
			if (o == null)
			{
				AlternateUI.Instance.Log.Info($"{this}: can't resolve {path}");
				return null;
			}

			return o.GetDirectories(cx);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			if (showHiddenFolders_ != cx.ShowHiddenFolders)
			{
				sc_ = null;
				showHiddenFolders_ = cx.ShowHiddenFolders;
			}

			var sc = ShortCut;

			if (rootDir_ == null)
				return base.DoGetDirectories(cx);
			else
				return new List<IFilesystemContainer> { rootDir_ };
		}
		/*
		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			if (showHiddenFolders_ != cx.ShowHiddenFolders)
			{
				sc_ = null;
				showHiddenFolders_ = cx.ShowHiddenFolders;
			}

			return base.DoGetFiles(cx);
		}*/

		protected override bool IncludeFile(Context cx, IFilesystemObject o)
		{
			if (cx.ShowHiddenFiles)
				return true;

			return (o.Name != "meta.json");
		}
	}
}
