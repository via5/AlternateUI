using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	struct StringView
	{
		private string s_;
		private int begin_, end_;

		public StringView(string s)
		{
			s_ = s;
			begin_ = 0;
			end_ = s_.Length;
		}

		public StringView(string s, int start, int count = -1)
		{
			s_ = s;
			begin_ = start;

			if (count < 0)
				end_ = s.Length;
			else
				end_ = Math.Min(start + count, s.Length);
		}

		public int Length
		{
			get { return end_ - begin_; }
		}

		public bool StartsWith(string with)
		{
			if (Length < with.Length)
				return false;

			int r = string.CompareOrdinal(
				s_, begin_,
				with, 0,
				with.Length);

			return (r == 0);
		}

		public bool EndsWith(string with)
		{
			if (Length < with.Length)
				return false;

			int r = string.CompareOrdinal(
				s_, Length - with.Length,
				with, 0,
				with.Length);

			return (r == 0);
		}

		public int LastIndexOf(char c)
		{
			if (Length == 0)
				return -1;

			int r = s_.LastIndexOf(c, end_ - 1, Length);
			if (r == -1)
				return -1;

			return (r - begin_);
		}

		public int LastIndexOfAny(char[] anyOf)
		{
			if (Length == 0)
				return -1;

			int r = s_.LastIndexOfAny(anyOf, end_ - 1, Length);
			if (r == -1)
				return -1;

			return (r - begin_);
		}

		public StringView Substring(int start, int count = -1)
		{
			return new StringView(s_, begin_ + start, count);
		}

		public static implicit operator string(StringView v)
		{
			return v.ToString();
		}

		public override string ToString()
		{
			return s_.Substring(begin_, end_ - begin_);
		}

		public int Compare(string other, bool ignoreCase = false)
		{
			return Compare(new StringView(other), ignoreCase);
		}

		public int Compare(StringView other, bool ignoreCase = false)
		{
			if (Length < other.Length)
				return -1;
			else if (Length > other.Length)
				return 1;

			return string.Compare(
				s_, begin_,
				other.s_, other.begin_,
				Length, ignoreCase);
		}

		public static bool operator ==(StringView a, StringView b)
		{
			if (a.Length != b.Length)
				return false;

			int r = string.CompareOrdinal(
				a.s_, a.begin_,
				b.s_, b.begin_,
				a.Length);

			return (r == 0);
		}

		public static bool operator !=(StringView a, StringView b)
		{
			return !(a == b);
		}

		public static bool operator ==(StringView a, string b)
		{
			if (a.Length != b.Length)
				return false;

			int r = string.CompareOrdinal(
				a.s_, a.begin_,
				b, 0,
				a.Length);

			return (r == 0);
		}

		public static bool operator !=(StringView a, string b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(s_, begin_, end_);
		}

		public override bool Equals(object obj)
		{
			if (obj is StringView)
				return (this == (StringView)obj);
			else
				return false;
		}
	}


	static class Path
	{
		public static StringView Filename(string f)
		{
			return Filename(new StringView(f));
		}

		public static StringView Filename(StringView f)
		{
			var slash = f.LastIndexOfAny(new char[] { '/', '\\' });
			if (slash == -1)
				return new StringView(f);

			return new StringView(f, slash + 1);
		}

		public static StringView Parent(string f)
		{
			return Parent(new StringView(f));
		}

		public static StringView Parent(StringView f)
		{
			var slash = f.LastIndexOfAny(new char[] { '/', '\\' });
			if (slash == -1)
				return new StringView("");

			return new StringView(f, 0, slash);
		}

		public static StringView Stem(string f)
		{
			return Stem(new StringView(f));
		}

		public static StringView Stem(StringView f)
		{
			var n = Filename(f);

			var dot = n.LastIndexOf('.');
			if (dot == -1)
				return n;

			return n.Substring(0, dot);
		}

		public static StringView Extension(string f)
		{
			return Extension(new StringView(f));
		}

		public static StringView Extension(StringView f)
		{
			var dot = f.LastIndexOf('.');
			if (dot == -1)
				return new StringView("");

			return new StringView(f, dot);
		}

		public static string Normalize(string originalPath)
		{
			string path = Sys.NormalizePath(originalPath);

			path = path.Replace("\\", "/");

			return path;
		}

		public static string MakeFSPath(string originalPath)
		{
			string path = Normalize(originalPath);

			path = Join("VaM", path);

			path = path.Replace("/AddonPackages/", "/Packages/");
			path = path.Replace(".var:", "");
			path = path.Replace(".var", "");

			return path;
		}

		// a short path is displayed in labels next to the Select buttons; for
		// vars, it's "author.package.1:/path/to/file"
		//
		// this restores the missing parts and calls MakeFSPath() with it
		//
		public static string MakeFSPathFromShort(string shortPath)
		{
			string path = shortPath;

			int pos = path.IndexOf(":/");
			if (pos != -1)
			{
				path =
					"/AddonPackages/" +
					path.Substring(0, pos) +
					".var:/" +
					path.Substring(pos + 2);
			}

			return MakeFSPath(path);
		}

		public static string Join(string a)
		{
			return a;
		}

		public static string Join(string a, string b)
		{
			return Join(new string[] { a, b });
		}

		public static string Join(string a, string b, string c)
		{
			return Join(new string[] { a, b, c });
		}

		public static string Join(string a, string b, string c, string d)
		{
			return Join(new string[] { a, b, c, d });
		}

		public static string Join(string[] cs)
		{
			string path = "";

			foreach (var sec in cs)
			{
				string s = sec.Replace('\\', '/');

				if (path.EndsWith("/") && s.StartsWith("/"))
					path += s.Substring(1);
				else if (path.EndsWith("/") || s.StartsWith("/"))
					path += s;
				else if (path != "")
					path += "/" + s;
				else
					path = s;
			}

			return path;
		}
	}


	public struct Extension
	{
		private readonly string name_;
		private readonly string[] exts_;

		public Extension(string name, string ext)
			: this(name, new string[] { ext })
		{
		}

		public Extension(string name, string[] exts)
		{
			name_ = name;
			exts_ = exts;
		}

		public string Name
		{
			get { return name_; }
		}

		public string[] Extensions
		{
			get { return exts_; }
		}

		public string ExtensionString
		{
			get
			{
				string s = "";

				for (int i = 0; i < exts_.Length; ++i)
				{
					if (s != "")
						s += "; ";

					s += "*" + exts_[i];
				}

				return s;
			}
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
		public const int ResolveNoCache = 0x04;

		private static Filesystem instance_;

		private Logger log_;
		private RootDirectory root_;
		private PackageRootDirectory packagesRoot_;
		private int cacheToken_ = 1;

		private bool usePackageTime_ = true;


		public Filesystem()
		{
		}

		public static void Init()
		{
			if (instance_ == null)
			{
				instance_ = new Filesystem();
				instance_.DoInit();
			}
		}

		private void DoInit()
		{
			log_ = new Logger("fs");
			root_ = new RootDirectory(this);
			packagesRoot_ = new PackageRootDirectory(this, root_);

			LoadOptions();
		}

		private string GetConfigFile()
		{
			return AlternateUI.Instance.GetConfigFilePath("aui.fs.json");
		}

		private void LoadOptions()
		{
			var file = GetConfigFile();
			if (!FileManagerSecure.FileExists(file))
				return;

			var j = SuperController.singleton.LoadJSON(GetConfigFile())?.AsObject;
			if (j == null)
				return;

			if (j.HasKey("usePackageTime"))
				UsePackageTime = j["usePackageTime"].AsBool;

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

					root_.PinnedRoot.Pin(path, display);
				}
			}
		}

		public void SaveOptions()
		{
			var j = new JSONClass();

			j["usePackageTime"] = new JSONData(UsePackageTime);

			Instrumentation.Start(I.PinSave);
			{
				var pins = new JSONArray();

				foreach (var p in root_.PinnedRoot.Pinned)
				{
					var po = new JSONClass();

					po.Add("path", p.VirtualPath);

					if (p.HasCustomDisplayName)
						po.Add("display", p.DisplayName);

					pins.Add(po);
				}

				j["pins"] = pins;
			}
			Instrumentation.End();

			SuperController.singleton.SaveJSON(j, GetConfigFile());
		}

		public static Filesystem Instance
		{
			get { return instance_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public bool UsePackageTime
		{
			get
			{
				return usePackageTime_;
			}

			set
			{
				if (usePackageTime_ != value)
				{
					usePackageTime_ = value;
					SaveOptions();
				}
			}
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

		public bool HasPinnedParent(IFilesystemContainer o)
		{
			return root_.PinnedRoot.HasPinnedParent(o);
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

		public override string DebugInfo()
		{
			return "RootDirectory";
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

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.Directory);
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

		protected override bool DoIncludeDirectory(Context cx, IFilesystemContainer o)
		{
			if (cx.ShowHiddenFolders)
				return true;

			return
				o.IsSameObject(allFlat_) ||
				o.IsSameObject(packagesFlat_) ||
				o.IsSameObject(pinnedFlat_) ||
				o.IsSameObject(pinned_) ||
				o.IsSameObject(fs_.GetPackagesRoot());
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return true;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			// the virtual directories need to be recreated every time or
			// they'll cache the old packages

			if (cx.PackagesSearch.Empty)
			{
				return new List<IFilesystemContainer>()
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
				return new List<IFilesystemContainer>()
				{
					fs_.GetPackagesRoot()
				};
			}
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			return new List<IFilesystemObject>();
		}

		protected override void DoGetFilesRecursive(
			Context cx, Listing<IFilesystemObject> listing,
			List<IFilesystemContainer> dirs)
		{
			foreach (var sd in GetDirectoriesForFlatten(cx))
			{
				if (sd.IsFlattened || sd.IsRedundant)
					continue;

				sd.GetFilesRecursiveInternal(cx, listing);
			}
		}
	}


	class SavesDirectory : FSDirectory
	{
		public SavesDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Saves")
		{
		}
	}



	class NullDirectory : IDirectory
	{
		private readonly List<IFilesystemObject> emptyFiles_ = new List<IFilesystemObject>();
		private readonly List<IFilesystemContainer> emptyDirs_ = new List<IFilesystemContainer>();

		public Logger Log { get { return AlternateUI.Instance.Log; } }
		public string Name { get { return ""; } }
		public string VirtualPath { get { return ""; } }
		public string RelativeVirtualPath { get { return VirtualPath; } }
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
		public bool IsFile { get { return false; } }

		public string DebugInfo()
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
			get { return Icons.GetIcon(Icons.Null); }
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
