using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	static class Cache
	{
		private static readonly Dictionary<string, List<File>> files_ =
			new Dictionary<string, List<File>>();

		private static readonly Dictionary<string, List<File>> dirs_ =
			new Dictionary<string, List<File>>();

		private static List<File> packagesFlat_ = null;


		public static List<File> GetDirectories(string parent)
		{
			List<File> list = null;
			dirs_.TryGetValue(parent, out list);
			return list;
		}

		public static void AddDirectories(string parent, List<File> list)
		{
			dirs_.Add(parent, list);
		}

		public static List<File> GetFiles(string parent)
		{
			List<File> list = null;
			files_.TryGetValue(parent, out list);
			return list;
		}

		public static void AddFiles(string parent, List<File> list)
		{
			files_.Add(parent, list);
		}

		public static List<File> GetPackagesFlat()
		{
			return packagesFlat_;
		}

		public static void SetPackagesFlat(List<File> list)
		{
			packagesFlat_ = list;
		}
	}


	public static class Icons
	{
		private static Texture packageIcon_ = null;
		private static readonly List<Action<Texture>> packageIconCallbacks_ =
			new List<Action<Texture>>();

		public static void LoadAll()
		{
			ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage();
			queuedImage.imgPath = AlternateUI.Instance.PluginPath + "/res/package.png";
			queuedImage.linear = true;

			queuedImage.callback = (tt) =>
			{
				packageIcon_ = tt.tex;

				foreach (var f in packageIconCallbacks_)
					f(packageIcon_);

				packageIconCallbacks_.Clear();
			};

			//ImageLoaderThreaded.singleton.ClearCacheThumbnail(queuedImage.imgPath);
			ImageLoaderThreaded.singleton.QueueThumbnail(queuedImage);
		}

		public static void GetPackageIcon(Action<Texture> f)
		{
			if (packageIcon_ != null)
			{
				f(packageIcon_);
			}
			else
			{
				packageIconCallbacks_.Add(f);
			}
		}

		public static void GetDirectoryIcon(Action<Texture> f)
		{
			f(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}

		public static Texture GetFileIconFromCache(string path)
		{
			var imgPath = GetThumbnailPath(path);

			if (imgPath == null)
			{
				return SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
			}
			else
			{
				//ImageLoaderThreaded.singleton.ClearCacheThumbnail(imgPath);
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(imgPath);
			}
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
