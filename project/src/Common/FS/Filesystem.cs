using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	public struct Extension
	{
		public string name;
		public string ext;

		public Extension(string name, string ext)
		{
			this.name = name;
			this.ext = ext;
		}
	}


	class Filesystem
	{
		public delegate void ObjectHandler(IFilesystemObject o);
		public event ObjectHandler ObjectChanged;

		public delegate void Handler();
		public event Handler PinsChanged;

		public static string SavesRoot = "Saves";


		public const int ResolveDefault = 0x00;
		public const int ResolveDirsOnly = 0x01;

		private static readonly Filesystem instance_ = new Filesystem();

		private readonly RootDirectory root_;
		private readonly PackageRootDirectory packagesRoot_;
		private int cacheToken_ = 1;


		public Filesystem()
		{
			root_ = new RootDirectory(this);
			packagesRoot_ = new PackageRootDirectory(this, root_);

			root_.PinnedRoot.Load();
		}

		public static Filesystem Instance
		{
			get { return instance_; }
		}

		public int CacheToken
		{
			get { return cacheToken_; }
		}

		public void ClearCaches()
		{
			++cacheToken_;
		}

		public void Pin(IFilesystemContainer o)
		{
			root_.PinnedRoot.Pin(o);
		}

		public void Unpin(IFilesystemContainer o)
		{
			root_.PinnedRoot.Unpin(o);
		}

		public bool IsPinned(IFilesystemContainer o)
		{
			return root_.PinnedRoot.IsPinned(o);
		}

		public IPackage GetPackage(string name)
		{
			foreach (var f in packagesRoot_.GetSubDirectories(null))
			{
				if (f.Name == name)
					return f as IPackage;
			}

			return null;
		}

		public bool DirectoryInPackage(string path)
		{
			return FMS.IsDirectoryInPackage(path);
		}

		public RootDirectory GetRootDirectory()
		{
			return root_;
		}

		public PackageRootDirectory GetPackagesRootDirectory()
		{
			return packagesRoot_;
		}

		public void FireObjectChanged(IFilesystemObject o)
		{
			ObjectChanged?.Invoke(o);
		}

		public void FirePinsChanged()
		{
			PinsChanged?.Invoke();
		}

		public bool IsSameObject(IFilesystemObject a, IFilesystemObject b)
		{
			if (a == b)
				return true;

			string pa = a.VirtualPath.Replace('\\', '/');
			string pb = b.VirtualPath.Replace('\\', '/');

			return (pa == pb);
		}

		public IFilesystemObject Resolve(string path, int flags = ResolveDefault)
		{
			path = path.Replace('\\', '/');
			return Resolve(root_, path.Split('/'), 0, flags);
		}

		private IFilesystemObject Resolve(IFilesystemContainer o, string[] cs, int csi, int flags)
		{
			if (csi >= cs.Length || o.Name != cs[csi])
				return null;

			if (csi + 1 == cs.Length)
				return o;

			foreach (var d in o.GetSubDirectories(null))
			{
				var r = Resolve(d, cs, csi + 1, flags);
				if (r != null)
					return r;
			}

			if (!Bits.IsSet(flags, ResolveDirsOnly))
			{
				if (csi + 2 < cs.Length)
				{
					// cannot be a file
					return null;
				}

				foreach (var f in o.GetFiles(null))
				{
					if (f.Name == cs[csi + 1])
						return f;
				}
			}

			return null;
		}
	}


	class RootDirectory : BasicFilesystemContainer
	{
		private readonly AllFlatDirectory allFlat_;
		private readonly PackagesFlatDirectory packagesFlat_;
		private readonly PinnedFlatDirectory pinnedFlat_;
		private readonly SavesDirectory saves_;
		private readonly FSDirectory custom_;
		private readonly PinnedRoot pinned_;
		private List<IFilesystemContainer> dirs_ = null;

		public RootDirectory(Filesystem fs)
			: base(fs, null, "VaM")
		{
			allFlat_ = new AllFlatDirectory(fs_, this);
			packagesFlat_ = new PackagesFlatDirectory(fs_, this);
			pinnedFlat_ = new PinnedFlatDirectory(fs, this);
			saves_ = new SavesDirectory(fs_, this);
			custom_ = new FSDirectory(fs_, this, "Custom");
			pinned_ = new PinnedRoot(fs_, this);
		}

		public override string ToString()
		{
			return "RootDirectory";
		}

		public List<IFilesystemContainer> Directories
		{
			get
			{
				MakeDirectories();
				return dirs_;
			}
		}

		public SavesDirectory Saves
		{
			get { return saves_; }
		}

		public FSDirectory Custom
		{
			get { return custom_; }
		}

		public PinnedRoot PinnedRoot
		{
			get { return pinned_; }
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
			get { return Icons.Get(Icons.Directory); }
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
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return Directories;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			// no-op
			if (c.Files.entries == null)
				c.Files.entries = new List<IFilesystemObject>();

			return c.Files.entries;
		}

		public List<IFilesystemContainer> GetRealDirectories()
		{
			return new List<IFilesystemContainer>
			{
				saves_,
				custom_,
				fs_.GetPackagesRootDirectory()
			};
		}

		private void MakeDirectories()
		{
			if (dirs_ == null)
			{
				dirs_ = new List<IFilesystemContainer>
				{
					allFlat_,
					packagesFlat_,
					pinnedFlat_,
					pinned_,
					saves_,
					custom_,
					fs_.GetPackagesRootDirectory()
				};
			}
		}
	}


	class SavesDirectory : FSDirectory
	{
		private readonly string[] whitelist_;

		public SavesDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Saves")
		{
			whitelist_ = new string[]
			{
				"Downloads",
				"scene",
			};
		}

		protected override bool IncludeDirectory(string name)
		{
			//for (int i = 0; i < whitelist_.Length; ++i)
			//{
			//	if (whitelist_[i] == name)
			//		return true;
			//}
			//
			//return false;
			return true;
		}
	}



	class NullDirectory : IDirectory
	{
		private readonly List<IFilesystemObject> emptyFiles_ = new List<IFilesystemObject>();
		private readonly List<IFilesystemContainer> emptyDirs_ = new List<IFilesystemContainer>();

		public string Name { get { return ""; } }
		public string VirtualPath { get { return ""; } }
		public string DisplayName { get { return ""; } set { } }
		public bool HasCustomDisplayName { get { return false; } }
		public DateTime DateCreated { get { return DateTime.MaxValue; } }
		public DateTime DateModified { get { return DateTime.MaxValue; } }
		public bool CanPin { get { return false; } }
		public bool Virtual { get { return true; } }
		public bool ChildrenVirtual { get { return true; } }
		public bool IsFlattened { get { return false; } }
		public IPackage ParentPackage { get { return null; } }

		public override string ToString()
		{
			return $"NullDirectory";
		}

		public IFilesystemContainer Parent
		{
			get { return null; }
		}

		public List<IFilesystemObject> GetFiles(Filter filter)
		{
			return emptyFiles_;
		}

		public List<IFilesystemObject> GetFilesRecursive(Filter filter)
		{
			return emptyFiles_;
		}

		public void GetFilesRecursiveUnfiltered(List<IFilesystemObject> list)
		{
			// no-op
		}

		public VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Null); }
		}

		public List<IFilesystemContainer> GetSubDirectories(Filter filter)
		{
			return emptyDirs_;
		}

		public bool HasSubDirectories(Filter filter)
		{
			return false;
		}

		public string MakeRealPath()
		{
			return "";
		}

		public void ClearCache()
		{
			// no-op
		}

		public bool IsSameObject(IFilesystemObject o)
		{
			return (o is NullDirectory);
		}
	}
}
