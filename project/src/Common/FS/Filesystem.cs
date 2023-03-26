using System;
using System.Collections.Generic;

namespace AUI.FS
{
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
		public const int ResolveDebug = 0x02;

		private static readonly Filesystem instance_ = new Filesystem();

		private readonly Logger log_;
		private readonly RootDirectory root_;
		private readonly PackageRootDirectory packagesRoot_;
		private int cacheToken_ = 1;


		public Filesystem()
		{
			log_ = new Logger("fs");
			root_ = new RootDirectory(this);
			packagesRoot_ = new PackageRootDirectory(this, root_);

			root_.PinnedRoot.Load();
		}

		public static Filesystem Instance
		{
			get { return instance_; }
		}

		public Logger Log
		{
			get { return log_; }
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

		public bool DirectoryInPackage(string path)
		{
			return Sys.IsDirectoryInPackage(null, path);
		}

		public RootDirectory GetRoot()
		{
			return root_;
		}

		public PackageRootDirectory GetPackagesRoot()
		{
			return packagesRoot_;
		}

		public PinnedRoot GetPinnedRoot()
		{
			return root_.PinnedRoot;
		}

		public void FireObjectChanged(IFilesystemObject o)
		{
			Instrumentation.Start(I.FireObjectChanged);
			{
				ObjectChanged?.Invoke(o);
			}
			Instrumentation.End();
		}

		public void FirePinsChanged()
		{
			Instrumentation.Start(I.FirePinsChanged);
			{
				PinsChanged?.Invoke();
			}
			Instrumentation.End();
		}

		public bool IsSameObject(IFilesystemObject a, IFilesystemObject b)
		{
			if (a == b)
				return true;

			string pa = a.VirtualPath.Replace('\\', '/');
			string pb = b.VirtualPath.Replace('\\', '/');

			return (pa == pb);
		}

		public IFilesystemObject Resolve(
			Context cx, string path, int flags = ResolveDefault)
		{
			return Resolve<IFilesystemObject>(cx, path, flags);
		}

		public T Resolve<T>(
			Context cx, string path, int flags = ResolveDefault)
				where T : class, IFilesystemObject
		{
			T t;

			Instrumentation.Start(I.Resolve);
			{
				t = root_.Resolve(cx, path, flags) as T;
			}
			Instrumentation.End();

			return t;
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

		public override void ClearCache()
		{
			base.ClearCache();
			dirs_ = null;
		}

		public AllFlatDirectory AllFlat
		{
			get { return allFlat_; }
		}

		public PackagesFlatDirectory PackagesFlat
		{
			get { return packagesFlat_; }
		}

		public PinnedFlatDirectory PinnedFlat
		{
			get { return pinnedFlat_; }
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

		public override bool AlreadySorted
		{
			get { return true; }
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

		public override bool IsInternal
		{
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		public List<IFilesystemContainer> GetDirectoriesForFlatten(Context cx)
		{
			if (cx.PackagesRoot == "Saves/scene")
			{
				return new List<IFilesystemContainer>
				{
					new VirtualDirectory(fs_, this, saves_),
					new VirtualDirectory(fs_, this, fs_.GetPackagesRoot())
				};
			}
			else if (cx.PackagesRoot == "Custom/Scripts")
			{
				return new List<IFilesystemContainer>
				{
					new VirtualDirectory(fs_, this, custom_),
					new VirtualDirectory(fs_, this, fs_.GetPackagesRoot())
				};
			}
			else
			{
				return new List<IFilesystemContainer>
				{
					new VirtualDirectory(fs_, this, saves_),
					new VirtualDirectory(fs_, this, custom_),
					new VirtualDirectory(fs_, this, fs_.GetPackagesRoot())
				};
			}
		}

		protected override bool IncludeDirectory(Context cx, IFilesystemContainer o)
		{
			if (cx.ShowHiddenFolders)
				return true;

			// todo
			if (cx.PackagesRoot == "Saves/scene")
				return !o.IsSameObject(custom_);
			else if (cx.PackagesRoot == "Custom/Scripts")
				return !o.IsSameObject(saves_);
			else
				return true;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			if (dirs_ == null)
			{
				if (cx.PackagesSearch.Empty)
				{
					dirs_ = new List<IFilesystemContainer>()
					{
						new VirtualDirectory(fs_, this, allFlat_),
						new VirtualDirectory(fs_, this, packagesFlat_),
						pinnedFlat_,
						pinned_,
						new VirtualDirectory(fs_, this, saves_),
						new VirtualDirectory(fs_, this, custom_),
						fs_.GetPackagesRoot()
					};
				}
				else
				{
					dirs_ = new List<IFilesystemContainer>()
					{
						fs_.GetPackagesRoot()
					};
				}
			}

			return dirs_;
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return true;
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

		protected override bool IncludeDirectory(Context cx, IFilesystemContainer o)
		{
			if (cx.ShowHiddenFolders)
				return true;

			for (int i = 0; i < whitelist_.Length; ++i)
			{
				if (whitelist_[i] == o.Name)
					return true;
			}

			return false;
		}
	}



	class NullDirectory : IDirectory
	{
		private readonly List<IFilesystemObject> emptyFiles_ = new List<IFilesystemObject>();
		private readonly List<IFilesystemContainer> emptyDirs_ = new List<IFilesystemContainer>();

		public Logger Log { get { return AlternateUI.Instance.Log; } }
		public string Name { get { return ""; } }
		public string VirtualPath { get { return ""; } }
		public string DisplayName { get { return ""; } set { } }
		public string Tooltip { get { return ""; } }
		public bool HasCustomDisplayName { get { return false; } }
		public DateTime DateCreated { get { return DateTime.MaxValue; } }
		public DateTime DateModified { get { return DateTime.MaxValue; } }
		public bool CanPin { get { return false; } }
		public bool Virtual { get { return true; } }
		public bool ChildrenVirtual { get { return true; } }
		public bool IsFlattened { get { return false; } }
		public bool IsRedundant { get { return false; } }
		public IPackage ParentPackage { get { return null; } }
		public bool AlreadySorted { get { return false; } }
		public bool UnderlyingCanChange { get { return false; } }
		public bool IsInternal { get { return true; } }

		public override string ToString()
		{
			return $"NullDirectory";
		}

		public IFilesystemContainer Parent
		{
			get { return null; }
		}

		public bool HasDirectories(Context cx)
		{
			return false;
		}

		public List<IFilesystemContainer> GetDirectories(Context cx)
		{
			return emptyDirs_;
		}

		public List<IFilesystemObject> GetFiles(Context cx)
		{
			return emptyFiles_;
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			// no-op
		}

		public void GetFilesRecursiveUnfilteredInternal(
			Context cx, List<IFilesystemObject> list)
		{
			// no-op
		}

		public VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Null); }
		}

		public string MakeRealPath()
		{
			return "";
		}

		public string DeVirtualize()
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

		public IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			return null;
		}

		public ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			return ResolveResult.NotFound();
		}
	}
}
