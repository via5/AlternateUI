using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class PackageRootDirectory : BasicFilesystemContainer
	{
		private List<IFilesystemContainer> packages_ = null;

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

		public List<IFilesystemContainer> GetPackages()
		{
			RefreshPackages();
			return packages_;
		}

		protected override List<IFilesystemContainer> GetDirectories()
		{
			RefreshPackages();
			return packages_;
		}

		private void RefreshPackages()
		{
			if (packages_ != null)
				return;

			packages_ = new List<IFilesystemContainer>();

			foreach (var sc in FMS.GetShortCutsForDirectory("Saves/scene"))
			{
				if (string.IsNullOrEmpty(sc.package))
					continue;

				if (!string.IsNullOrEmpty(sc.packageFilter))
					continue;

				if (sc.path == "AddonPackages")
					continue;

				packages_.Add(new Package(fs_, this, sc));
			}
		}
	}


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly ShortCut sc_;

		public Package(Filesystem fs, IFilesystemContainer parent, ShortCut sc)
			: base(fs, parent, sc.package)
		{
			sc_ = sc;
		}

		public override string ToString()
		{
			return $"Package({sc_.package})";
		}

		public ShortCut ShortCut
		{
			get { return sc_; }
		}

		public override string Tooltip
		{
			get
			{
				var tt = base.Tooltip;

				tt +=
					$"\npackage={sc_.package}" +
					$"\nfilter={sc_.packageFilter}" +
					$"\nflatten={sc_.flatten}" +
					$"\nhidden={sc_.isHidden}" +
					$"\npath={sc_.path}";

				return tt;
			}
		}

		protected override string GetDisplayName()
		{
			return sc_.package;
		}

		public override DateTime DateCreated
		{
			get { return FMS.FileCreationTime(sc_.path); }
		}

		public override DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(sc_.path); }
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
			return sc_.path + "/";
		}

		protected override bool IncludeFile(Context cx, IFilesystemObject o)
		{
			return (o.Name != "meta.json");
		}
	}
}
