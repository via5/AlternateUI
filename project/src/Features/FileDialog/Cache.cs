using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AUI.FileDialog
{
	static class Cache
	{
		class CacheInfo
		{
			private readonly string parent_;
			private readonly string normalizedParent_;
			private readonly string[] exts_;

			public CacheInfo(string parent, string[] exts)
			{
				parent_ = parent;
				normalizedParent_ = parent.Replace('\\', '/');
				exts_ = exts;
			}

			public override int GetHashCode()
			{
				return HashHelper.Combine(
					normalizedParent_.GetHashCode(),
					HashHelper.HashArray(exts_));
			}

			public override bool Equals(object o)
			{
				return Equals(o as CacheInfo);
			}

			public bool Equals(CacheInfo o)
			{
				return
					normalizedParent_ == o.normalizedParent_ &&
					ArraysEqual(exts_, o.exts_);
			}

			private bool ArraysEqual<T>(T[] a, T[] b)
			{
				if (a == b)
					return true;

				if (a == null || b == null)
					return false;

				return Enumerable.SequenceEqual(a, b);
			}
		}


		public class Filter
		{
			private readonly string search_;
			private readonly string searchLc_;
			private readonly string[] exts_;
			private readonly Regex searchRe_;

			public Filter(string search, string[] extensions)
			{
				search_ = search;
				exts_ = extensions;

				if (VUI.Utilities.IsRegex(search_))
				{
					searchLc_ = null;
					searchRe_ = VUI.Utilities.CreateRegex(search_);
				}
				else
				{
					searchLc_ = search.ToLower();
					searchRe_ = null;
				}
			}

			public string Search
			{
				get { return search_; }
			}

			public string[] Extensions
			{
				get { return exts_; }
			}

			public bool ExtensionMatches(string path)
			{
				if (exts_ != null)
				{
					foreach (var e in exts_)
					{
						if (path.EndsWith(e))
							return true;
					}

					return false;
				}

				return true;
			}

			public bool SearchMatches(File f)
			{
				if (searchLc_ != null)
				{
					if (f.Filename.ToLower().IndexOf(searchLc_) == -1)
						return false;
				}
				else if (searchRe_ != null)
				{
					if (!searchRe_.IsMatch(f.Filename))
						return false;
				}

				return true;
			}
		}


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
		public static string ScenesRoot = "Saves/scene";

		public static string DefaultSceneExtension = ".json";

		public static Extension[] SceneExtensions = new Extension[]
		{
			new Extension("Scenes", ".json"),
			new Extension("VAC files", ".vac"),
			new Extension("Zip files", ".zip"),
		};


		private static readonly Dictionary<CacheInfo, List<File>> files_ =
			new Dictionary<CacheInfo, List<File>>();

		private static readonly Dictionary<CacheInfo, List<File>> dirs_ =
			new Dictionary<CacheInfo, List<File>>();

		private static readonly Dictionary<CacheInfo, List<ShortCut>> packages_ =
			new Dictionary<CacheInfo, List<ShortCut>>();

		private static List<File> packagesFlat_ = null;


		public static List<File> GetDirectories(string parent, Filter filter)
		{
			List<File> all;
			var ci = new CacheInfo(parent, null);

			if (!dirs_.TryGetValue(ci, out all))
			{
				all = new List<File>();
				foreach (var d in FileManagerSecure.GetDirectories(parent))
				{
					if (filter == null || filter.ExtensionMatches(d))
						all.Add(new File(d));
				}

				U.NatSort(all);
				dirs_.Add(ci, all);
			}

			List<File> list;

			if (filter == null || filter.Search == "")
			{
				list = all;
			}
			else
			{
				list = new List<File>();

				foreach (var f in all)
				{
					if (filter.SearchMatches(f))
						list.Add(f);
				}
			}

			return list;
		}

		public static bool HasDirectories(string parent, Filter filter)
		{
			var list = GetDirectories(parent, filter);
			return (list.Count > 0);
		}

		public static List<File> GetFiles(string parent, Filter filter)
		{
			List<File> all = null;
			var ci = new CacheInfo(parent, filter.Extensions);

			if (!files_.TryGetValue(ci, out all))
			{
				all = new List<File>();

				foreach (var f in FileManagerSecure.GetFiles(parent))
				{
					if (filter == null || filter.ExtensionMatches(f))
						all.Add(new File(f));
				}

				U.NatSort(all);
				files_.Add(ci, all);
			}

			List<File> list;

			if (filter == null || filter.Search == "")
			{
				list = all;
			}
			else
			{
				list = new List<File>();

				foreach (var f in all)
				{
					if (filter.SearchMatches(f))
						list.Add(f);
				}
			}

			return list;
		}

		public static List<File> GetFilesRecursive(string parent, Filter filter)
		{
			var list = new List<File>();
			GetFilesRecursive(parent, filter, list);
			return list;
		}

		private static void GetFilesRecursive(string parent, Filter filter, List<File> list)
		{
			list.AddRange(GetFiles(parent, filter));

			foreach (var d in Cache.GetDirectories(parent, null))
				GetFilesRecursive(d.Path, filter, list);
		}

		public static bool HasPackages(string parent)
		{
			var scs = FileManagerSecure.GetShortCutsForDirectory(parent);
			return (scs != null && scs.Count > 0);
		}

		public static bool DirectoryInPackage(string dir)
		{
			return FileManagerSecure.IsDirectoryInPackage(dir);
		}

		public static List<File> GetPackagesFlat(string parent, Filter filter)
		{
			if (packagesFlat_ == null)
			{
				packagesFlat_ = new List<File>();

				foreach (var p in FileManagerSecure.GetShortCutsForDirectory(parent))
				{
					if (string.IsNullOrEmpty(p.package))
						continue;

					if (!string.IsNullOrEmpty(p.packageFilter))
						continue;

					GetFilesRecursive(p.path, filter, packagesFlat_);
				}
			}

			return packagesFlat_;
		}

		public static List<ShortCut> GetPackages(string parent)
		{
			var ci = new CacheInfo(parent, null);
			List<ShortCut> list;

			if (!packages_.TryGetValue(ci, out list))
			{
				list = FileManagerSecure.GetShortCutsForDirectory(parent);
				packages_.Add(ci, list);
			}

			return list;
		}

		public static ShortCut GetPackage(string path)
		{
			foreach (var p in GetPackages(ScenesRoot))
			{
				if (p.path == path)
					return p;
			}

			return null;
		}
	}

	public static class Icons
	{
		class Icon
		{
			public Texture texture = null;
			public readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();

			public Icon(string path)
				: this(path, 0, 0)
			{
			}

			public Icon(string path, int w, int h)
			{
				ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
				q.imgPath = path;

				q.callback = (tt) =>
				{
					var tex = tt.tex;

					if (w != 0 && h != 0)
						tex = ScaleTexture(tex, w, h);

					tex.wrapMode = TextureWrapMode.Clamp;

					texture = tex;

					foreach (var f in callbacks_)
						f(texture);

					callbacks_.Clear();
				};

				//ImageLoaderThreaded.singleton.ClearCacheThumbnail(q.imgPath);
				ImageLoaderThreaded.singleton.QueueThumbnail(q);
			}

			public void Get(Action<Texture> f)
			{
				if (texture != null)
					f(texture);
				else
					callbacks_.Add(f);
			}

			private static Texture2D ScaleTexture(Texture src, int width, int height)
			{
				RenderTexture rt = RenderTexture.GetTemporary(width, height);
				Graphics.Blit(src, rt);

				RenderTexture currentActiveRT = RenderTexture.active;
				RenderTexture.active = rt;
				Texture2D tex = new Texture2D(rt.width, rt.height);

				tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
				tex.Apply();

				RenderTexture.ReleaseTemporary(rt);
				RenderTexture.active = currentActiveRT;

				return tex;
			}
		}

		private static Icon package_ = null;
		private static Icon pinned_ = null;
		private static Icon resizeWE_ = null;

		public static void LoadAll()
		{
			package_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/package.png");
			pinned_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/pinned.png");
			resizeWE_ = new Icon(AlternateUI.Instance.PluginPath + "/res/cursors/resize_w_e.png", 40, 40);
		}

		public static void GetPackageIcon(Action<Texture> f)
		{
			package_.Get(f);
		}

		public static void GetPinnedIcon(Action<Texture> f)
		{
			pinned_.Get(f);
		}

		public static void GetResizeWE(Action<Texture> f)
		{
			resizeWE_.Get(f);
		}

		public static void GetDirectoryIcon(Action<Texture> f)
		{
			f(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}

		public static Texture GetFileIconFromCache(string path)
		{
			var imgPath = GetThumbnailPath(path);

			if (imgPath == null)
				return SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
			else
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(imgPath);
		}

		public static void GetFileIcon(string path, Action<Texture> f)
		{
			var t = GetFileIconFromCache(path);
			if (t != null)
			{
				f(t);
				return;
			}

			var q = new ImageLoaderThreaded.QueuedImage
			{
				imgPath = GetThumbnailPath(path),
				callback = tt => f(tt?.tex)
			};

			ImageLoaderThreaded.singleton.QueueThumbnail(q);
		}

		private static string GetThumbnailPath(string file)
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file) + "\\" + Path.Stem(file) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}
	}
}
