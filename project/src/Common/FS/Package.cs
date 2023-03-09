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


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly string name_;
		private ShortCut sc_ = null;
		private bool showHiddenFolders_ = false;

		public Package(Filesystem fs, IFilesystemContainer parent, string name, bool showHiddenFolders)
			: base(fs, parent, name)
		{
			name_ = name;
			showHiddenFolders_ = showHiddenFolders;
		}

		public override string ToString()
		{
			return $"Package({ShortCut.path})";
		}

		public ShortCut ShortCut
		{
			get
			{
				if (sc_ == null)
				{
					sc_ = fs_.GetPackagesRootDirectory()
						.GetShortCut(name_, showHiddenFolders_);
				}

				return sc_;
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

			if (cs.Done)
				return ResolveResult.Found(this);

			debug = debug.Inc();

			var scPath = sc.path.Substring(col + 1);

			if (scPath.Length > 0)
			{
				var sccs = new PathComponents(scPath);

				if (debug.Enabled)
					debug.Info(this, $"checking sc path cs={cs} sccs={sccs}");

				bool found = true;

				while (!sccs.Done && !cs.Done)
				{
					if (cs.Done)
					{
						if (debug.Enabled)
							debug.Info(this, $"resolve bad, at end");

						found = false;
						break;
						//return ResolveResult.FoundPartial(this);
					}
					else if (cs.Current != sccs.Current)
					{
						if (debug.Enabled)
							debug.Info(this, $"resolve bad cs={cs} sccs={sccs}");

						found = false;
						break;
						//return ResolveResult.FoundPartial(this);
					}

					cs.Next();
					sccs.Next();
				}

				if (found && cs.Done && sccs.Done)
					return ResolveResult.Found(this);

				if (debug.Enabled)
					debug.Info(this, $"sc path failed, will fwd to base cs={cs}");
			}
			else
			{
				if (debug.Enabled)
					debug.Info(this, $"resolve ok, fwd to base cs={cs}");
			}

			return base.ResolveInternal2(cx, cs, flags, debug);
		}

		public List<IFilesystemObject> GetFilesForMerge(Context cx, string path)
		{
			var sc = ShortCut;
			path = sc.package + ":/" + path;

			var o = Resolve(cx, path, Filesystem.ResolveDirsOnly) as IFilesystemContainer;
			if (o == null)
			{
				//AlternateUI.Instance.Log.Info($"{this}: can't resolve {path}");
				return null;
			}

			return o.GetFiles(cx);
		}

		public List<IFilesystemContainer> GetDirectoriesForMerge(Context cx, string path)
		{
			var sc = ShortCut;
			path = sc.package + ":/" + path;

			var o = Resolve(cx, path, Filesystem.ResolveDirsOnly) as IFilesystemContainer;
			if (o == null)
			{
				//AlternateUI.Instance.Log.Info($"{this}: can't resolve {path}");
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

			return base.DoGetDirectories(cx);
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
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
