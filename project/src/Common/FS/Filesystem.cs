using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class Filesystem
	{
		public delegate void ObjectHandler(IFilesystemObject o);
		public event ObjectHandler ObjectChanged;

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


		public static string SavesRoot = "Saves";

		public static string DefaultSceneExtension = ".json";
		public static Extension[] SceneExtensions = new Extension[]
		{
			new Extension("Scenes", ".json"),
			new Extension("VAC files", ".vac"),
			new Extension("Zip files", ".zip"),
		};


		public const int ResolveDefault = 0x00;
		public const int ResolveDirsOnly = 0x01;

		private static readonly Filesystem instance_ = new Filesystem();

		private readonly Dictionary<string, IFilesystemContainer> dirs_ =
			new Dictionary<string, IFilesystemContainer>();

		private readonly RootDirectory root_;
		private readonly PackageRootDirectory packagesRoot_;


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

		public IFilesystemContainer GetRootDirectory()
		{
			return root_;
		}

		public IFilesystemContainer GetPackagesRootDirectory()
		{
			return packagesRoot_;
		}

		public void FireObjectChanged(IFilesystemObject o)
		{
			ObjectChanged?.Invoke(o);
		}

		public bool IsSameObject(IFilesystemObject a, IFilesystemObject b)
		{
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


	static class FS
	{
	}


	class RootDirectory : BasicFilesystemContainer
	{
		private readonly FSDirectory saves_;
		private readonly PinnedRoot pinned_;
		private readonly List<IFilesystemObject> empty_ = new List<IFilesystemObject>();

		public RootDirectory(Filesystem fs)
			: base(fs, null, "VaM")
		{
			saves_ = new FSDirectory(fs_, this, "Saves");
			pinned_ = new PinnedRoot(fs_, this);
		}

		public override string ToString()
		{
			return "RootDirectory";
		}

		public FSDirectory Saves
		{
			get { return saves_; }
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
			var list = new List<IFilesystemContainer>();
			list.Add(pinned_);
			list.Add(saves_);
			list.Add(fs_.GetPackagesRootDirectory());
			return list;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return empty_;
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.Files.entries);
		}
	}


	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		public override string ToString()
		{
			return $"FSDirectory({VirtualPath})";
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
			get { return true; }
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return DoGetSubDirectories(c_, filter);
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return DoGetFiles(c, filter);
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.Files.entries);
		}
	}


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


	class File : IFile
	{
		private readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private readonly string name_;
		private string displayName_ = null;

		public File(Filesystem fs, IFilesystemContainer parent, string name)
		{
			fs_ = fs;
			parent_ = parent;
			name_ = name;
		}

		public override string ToString()
		{
			return $"File({VirtualPath})";
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public string VirtualPath
		{
			get
			{
				string s = name_;

				var parent = Parent;
				while (parent != null)
				{
					s = parent.Name + "/" + s;
					parent = parent.Parent;
				}

				return s;
			}
		}

		public virtual string DisplayName
		{
			get
			{
				return displayName_ ?? Name;
			}

			set
			{
				displayName_ = value;
			}
		}

		public bool HasCustomDisplayName
		{
			get { return (displayName_ != null); }
		}

		public DateTime DateCreated
		{
			get { return FMS.FileCreationTime(VirtualPath); }
		}

		public DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(VirtualPath); }
		}

		public bool CanPin
		{
			get { return false; }
		}

		public bool Virtual
		{
			get { return false; }
		}

		public bool IsFlattened
		{
			get { return false; }
		}

		public IPackage ParentPackage
		{
			get
			{
				IFilesystemObject o = this;

				while (o != null)
				{
					if (o is IPackage)
						return o as IPackage;

					o = o.Parent;
				}

				return null;
			}
		}

		public Icon Icon
		{
			get { return Icons.File(MakeRealPath()); }
		}

		public string MakeRealPath()
		{
			string s = Name;

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		public bool IsSameObject(IFilesystemObject o)
		{
			return fs_.IsSameObject(this, o);
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

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.Files.entries);
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

		public Icon Icon
		{
			get { return Icons.Null; }
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

		public bool IsSameObject(IFilesystemObject o)
		{
			return (o is NullDirectory);
		}
	}


	class PinnedObject : IFilesystemContainer
	{
		private readonly PinnedRoot parent_;
		private readonly IFilesystemContainer c_;
		private string displayName_ = null;

		public PinnedObject(PinnedRoot parent, IFilesystemContainer c, string displayName = null)
		{
			parent_ = parent;
			c_ = c;
			displayName_ = displayName;
		}

		public IFilesystemContainer Parent { get { return parent_; } }
		public string Name { get { return c_.Name; } }
		public string VirtualPath { get { return c_.VirtualPath; } }

		public string DisplayName
		{
			get
			{
				return displayName_ ?? c_.DisplayName;
			}

			set
			{
				displayName_ = value;
				parent_.Save();
			}
		}

		public bool HasCustomDisplayName
		{
			get { return (displayName_ != null); }
		}

		public DateTime DateCreated { get { return c_.DateCreated; } }
		public DateTime DateModified { get { return c_.DateModified; } }
		public Icon Icon { get { return c_.Icon; } }
		public bool CanPin { get { return false; } }
		public bool Virtual { get { return c_.Virtual; } }
		public bool IsFlattened { get { return c_.IsFlattened; } }
		public IPackage ParentPackage { get { return c_.ParentPackage; } }

		public List<IFilesystemObject> GetFiles(Filter filter)
		{
			return c_.GetFiles(filter);
		}

		public List<IFilesystemObject> GetFilesRecursive(Filter filter)
		{
			return c_.GetFilesRecursive(filter);
		}

		public void GetFilesRecursiveUnfiltered(List<IFilesystemObject> list)
		{
			c_.GetFilesRecursiveUnfiltered(list);
		}

		public List<IFilesystemContainer> GetSubDirectories(Filter filter)
		{
			return c_.GetSubDirectories(filter);
		}

		public bool HasSubDirectories(Filter filter)
		{
			return c_.HasSubDirectories(filter);
		}

		public string MakeRealPath()
		{
			return c_.MakeRealPath();
		}

		public bool IsSameObject(IFilesystemObject o)
		{
			return c_.IsSameObject(o);
		}
	}


	class PinnedRoot : BasicFilesystemContainer
	{
		private readonly List<IFilesystemContainer> pinned_ = new List<IFilesystemContainer>();
		private readonly List<IFilesystemObject> emptyFiles_ = new List<IFilesystemObject>();

		public PinnedRoot(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned")
		{
		}

		public override string ToString()
		{
			return $"PinnedRoot";
		}

		private string GetConfigFile()
		{
			return AlternateUI.Instance.GetConfigFilePath("aui.fs.pinned.json");
		}

		public void Load()
		{
			if (FMS.FileExists(GetConfigFile()))
			{
				var j = SuperController.singleton.LoadJSON(GetConfigFile())?.AsObject;
				var pins = j?["pins"]?.AsArray;

				if (pins != null)
				{
					foreach (JSONNode p in pins)
					{
						string path = p?["path"]?.Value;
						string display = p?["display"]?.Value?.Trim();

						if (string.IsNullOrEmpty(path))
						{
							AlternateUI.Instance.Log.Error("bad pin");
							continue;
						}

						if (display == "")
							display = null;

						Pin(path, display);
					}
				}
			}
		}

		public void Save()
		{
			var j = new JSONClass();

			var pins = new JSONArray();

			foreach (var p in pinned_)
			{
				var po = new JSONClass();

				po.Add("path", p.VirtualPath);

				if (p.HasCustomDisplayName)
					po.Add("display", p.DisplayName);

				pins.Add(po);
			}

			j["pins"] = pins;

			SuperController.singleton.SaveJSON(j, GetConfigFile());
		}

		public void Pin(string s, string display = null)
		{
			var o = fs_.Resolve(s, Filesystem.ResolveDirsOnly) as IFilesystemContainer;
			if (o == null)
			{
				AlternateUI.Instance.Log.Error($"cannot resolve pinned item '{s}'");
				return;
			}

			Pin(o, display);
		}

		public void Pin(IFilesystemContainer o, string display = null)
		{
			if (!IsPinned(o))
			{
				pinned_.Add(new PinnedObject(this, o, display));
				Changed();
			}
		}

		public void Unpin(IFilesystemContainer c)
		{
			for (int i = 0; i < pinned_.Count; ++i)
			{
				if (pinned_[i].IsSameObject(c) || pinned_[i] == c)
				{
					pinned_.RemoveAt(i);
					Changed();
					break;
				}
			}
		}

		public bool IsPinned(IFilesystemContainer o)
		{
			foreach (var p in pinned_)
			{
				if (o.IsSameObject(p) || p == o)
					return true;
			}

			return false;
		}

		private void Changed()
		{
			Save();
			ClearCaches();
			fs_.FireObjectChanged(this);
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override Icon Icon
		{
			get { return Icons.Pinned; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return pinned_;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return emptyFiles_;
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.Files.entries);
		}
	}
}
