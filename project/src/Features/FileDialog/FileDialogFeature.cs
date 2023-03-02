using SimpleJSON;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	class FileDialogFeature : BasicFeature
	{
		private FileDialog fd_ = new FileDialog();

		public static string DefaultSceneExtension = ".json";
		public static FS.Filesystem.Extension[] SceneExtensions = new FS.Filesystem.Extension[]
		{
			new FS.Filesystem.Extension("Scenes", ".json"),
			new FS.Filesystem.Extension("VAC files", ".vac"),
			new FS.Filesystem.Extension("Zip files", ".zip"),
		};

		public static string DefaultCUAExtension = ".assetbundle";
		public static FS.Filesystem.Extension[] CUAExtensions = new FS.Filesystem.Extension[]
		{
			new FS.Filesystem.Extension("Asset bundles", ".assetbundle")
		};

		public static string DefaultPluginExtension = ".cslist";
		public static FS.Filesystem.Extension[] PluginExtensions = new FS.Filesystem.Extension[]
		{
			new FS.Filesystem.Extension("cslist files", ".cslist"),
			new FS.Filesystem.Extension("C# files", ".cs"),
			new FS.Filesystem.Extension("DLL files", ".dll"),
		};


		public FileDialogFeature()
			: base("fileDialog", "File dialog", false)
		{
		}

		public static FileDialog.ExtensionItem[] GetSceneExtensions(bool includeAll)
		{
			return GetExtensions(SceneExtensions, "All scene files", includeAll);
		}

		public static FileDialog.ExtensionItem[] GetCUAExtensions(bool includeAll)
		{
			return GetExtensions(CUAExtensions, "All CUA files", includeAll);
		}

		public static FileDialog.ExtensionItem[] GetPluginExtensions(bool includeAll)
		{
			return GetExtensions(PluginExtensions, "All plugin files", includeAll);
		}

		public static FileDialog.ExtensionItem[] GetExtensions(
			FS.Filesystem.Extension[] exts, string allText, bool includeAll)
		{
			var list = new List<FileDialog.ExtensionItem>();

			if (includeAll && exts.Length > 1)
			{
				string all = "";
				var allExts = new List<string>();

				foreach (var e in exts)
				{
					if (all != "")
						all += "; ";

					all += "*" + e.ext;
					allExts.Add(e.ext);
				}

				all = $"{allText} ({all})";
				list.Add(new FileDialog.ExtensionItem(all, allExts.ToArray()));
			}

			foreach (var e in exts)
				list.Add(new FileDialog.ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			if (includeAll)
			{
				list.Add(new FileDialog.ExtensionItem("All files (*.*)", new string[] { "*.*" }));
			}

			return list.ToArray();
		}

		public override string Description
		{
			get { return "Replaces the file dialogs."; }
		}

		protected override void DoEnable()
		{
			var root = SuperController.singleton.transform;

			fd_.Enable();

			//fd_.Show(Modes.OpenCUA());
			//fd_.SetCurrentDirectory("VaM/Saves/scene", true);

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool += (fb, cb, cd) =>
			{
				if (fb == SuperController.singleton.fileBrowserUI)
				{
					var t = fb.titleText?.text ?? "";

					if (t == "Select Scene For Merge" ||
						t == "Select Scene For Edit" ||
						t == "Select Scene To Load")
					{
						fd_.Show(Modes.OpenScene(), (path) => cb?.Invoke(path));
						return;
					}
					else if (t == "Select Save File")
					{
						fd_.Show(Modes.SaveScene(), (path) => cb?.Invoke(path));
						return;
					}
				}

				Log.Error($"unknown show filebrowser request");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat}");

				Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool", () =>
				{
					fb.Show(cb, cd);
				});
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool += (fb, cb, cd) =>
			{
				if (fb == SuperController.singleton.mediaFileBrowserUI)
				{
					var t = fb.titleText?.text ?? "";

					if (t == "Select File")
					{
						if (fb.fileFormat == "assetbundle|scene")
						{
							fd_.Show(Modes.OpenCUA(), (path) => cb?.Invoke(path, true));
							return;
						}
						else if (fb.fileFormat == "cs|cslist|dll")
						{
							fd_.Show(Modes.OpenPlugin(), (path) => cb?.Invoke(path, true));
							return;
						}
					}
				}

				Log.Error($"unknown show filebrowser request (full)");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat}");

				Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool", () =>
				{
					fb.Show(cb, cd);
				});
			};
		}

		protected override void DoDisable()
		{
			fd_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			fd_.Update(s);
		}

		protected override void DoLoadOptions(JSONClass o)
		{
			fd_.LoadOptions(o);
		}

		protected override void DoSaveOptions(JSONClass o)
		{
			fd_.SaveOptions(o);
		}
	}
}
