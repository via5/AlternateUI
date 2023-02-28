using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class AllFlatDirectory : BasicFilesystemContainer
	{
		private List<IFilesystemContainer> dirs_ = null;

		public AllFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "All flattened")
		{
		}

		public override string ToString()
		{
			return "AllFlatDirectory";
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
			get { return Icons.Directory; }
		}

		public override bool CanPin
		{
			get { return false; }
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
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		public override List<IFilesystemObject> GetFiles(Filter filter)
		{
			// always recursive
			return GetFilesRecursive(filter);
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			if (dirs_ == null)
				dirs_ = fs_.GetRootDirectory().GetRealDirectories();

			return dirs_;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			// no-op
			if (c.Files.entries == null)
				c.Files.entries = new List<IFilesystemObject>();

			return c.Files.entries;
		}
	}


	class PackagesFlatDirectory : BasicFilesystemObject, IFilesystemContainer
	{
		public PackagesFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages flattened")
		{
		}

		public override string Name
		{
			get { return "Packages flattened"; }
		}

		public override string ToString()
		{
			return "PackagesFlatDirectory";
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
			get { return false; }
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
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		public List<IFilesystemObject> GetFiles(Filter filter)
		{
			// always recursive
			return GetFilesRecursive(filter);
		}

		public List<IFilesystemObject> GetFilesRecursive(Filter filter)
		{
			return fs_.GetPackagesRootDirectory().GetFilesRecursive(filter);
		}

		public void GetFilesRecursiveUnfiltered(List<IFilesystemObject> list)
		{
			fs_.GetPackagesRootDirectory().GetFilesRecursiveUnfiltered(list);
		}

		public List<IFilesystemContainer> GetSubDirectories(Filter filter)
		{
			return fs_.GetPackagesRootDirectory().GetSubDirectories(filter);
		}

		public bool HasSubDirectories(Filter filter)
		{
			return fs_.GetPackagesRootDirectory().HasSubDirectories(filter);
		}
	}


	class PinnedFlatDirectory : BasicFilesystemContainer
	{
		public PinnedFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned flattened")
		{
			fs_.PinsChanged += ClearCaches;
		}

		public override string ToString()
		{
			return "PinnedFlatDirectory";
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
			get { return Icons.Directory; }
		}

		public override bool CanPin
		{
			get { return false; }
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
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		public override List<IFilesystemObject> GetFiles(Filter filter)
		{
			// always recursive
			return GetFilesRecursive(filter);
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return fs_.GetRootDirectory().PinnedRoot.Pinned;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			// no-op
			if (c.Files.entries == null)
				c.Files.entries = new List<IFilesystemObject>();

			return c.Files.entries;
		}
	}
}
