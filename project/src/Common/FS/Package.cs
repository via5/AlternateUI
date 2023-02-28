using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class PackageRootDirectory : BasicFilesystemContainer
	{
		private List<ShortCut> shortCuts_ = null;

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

		public override Icon Icon
		{
			get { return Icons.Package; }
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

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			if (c_.Directories.entries == null)
			{
				c_.Directories.entries = new List<IFilesystemContainer>();

				foreach (var s in GetShortCuts())
					c_.Directories.entries.Add(new Package(fs_, fs_.GetPackagesRootDirectory(), s));
			}

			// todo filter

			return c_.Directories.entries;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			// no-op
			if (c.Files.entries == null)
				c.Files.entries = new List<IFilesystemObject>();

			return c.Files.entries;
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			foreach (var d in GetSubDirectories(filter))
				(d as BasicFilesystemContainer).DoGetFilesRecursive(rc_.Files.entries);
		}

		private List<ShortCut> GetShortCuts()
		{
			if (shortCuts_ == null)
			{
				shortCuts_ = new List<ShortCut>();

				foreach (var sc in FMS.GetShortCutsForDirectory("Saves/scene"))
				{
					if (string.IsNullOrEmpty(sc.package))
						continue;

					if (!string.IsNullOrEmpty(sc.packageFilter))
						continue;

					shortCuts_.Add(sc);
				}
			}

			return shortCuts_;
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

		public override Icon Icon
		{
			get { return Icons.Package; }
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

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return DoGetSubDirectories(c_, filter);
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return DoGetFiles(c, filter);
		}
	}
}
