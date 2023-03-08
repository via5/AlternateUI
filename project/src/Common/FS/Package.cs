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
