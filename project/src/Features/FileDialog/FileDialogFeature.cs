using SimpleJSON;
using System.Collections.Generic;
using uFileBrowser;

namespace AUI.FileDialog
{
	class FileDialogFeature : UIReplacementFeature
	{
		private FileDialog fd_ = new FileDialog();

		public static string DefaultSceneExtension = ".json";
		public static FS.Extension[] SceneExtensions = new FS.Extension[]
		{
			new FS.Extension("JSON files", ".json"),
			new FS.Extension("VAC files", ".vac"),
			new FS.Extension("Zip files", ".zip"),
		};

		public static string DefaultCUAExtension = ".assetbundle";
		public static FS.Extension[] CUAExtensions = new FS.Extension[]
		{
			new FS.Extension("Asset bundles", ".assetbundle")
		};

		public static string DefaultPluginExtension = ".cslist";
		public static FS.Extension[] PluginExtensions = new FS.Extension[]
		{
			new FS.Extension("cslist files", ".cslist"),
			new FS.Extension("C# files", ".cs"),
			new FS.Extension("DLL files", ".dll"),
		};

		public static string DefaultPresetExtension = ".vap";
		public static FS.Extension[] PresetExtensions = new FS.Extension[]
		{
			new FS.Extension("VAP files", ".vap")
		};

		public static string DefaultLegacyPresetExtension = ".json";
		public static FS.Extension[] LegacyPresetExtensions = new FS.Extension[]
		{
			new FS.Extension("JSON files", ".json"),
			new FS.Extension("VAC files", ".vac"),
			new FS.Extension("Zip files", ".zip")
		};


		public FileDialogFeature()
			: base("fileDialog", "File dialog", false)
		{
		}

		public static ExtensionItem[] GetSceneExtensions(bool includeAll)
		{
			return GetExtensions(SceneExtensions, "All scene files", includeAll);
		}

		public static ExtensionItem[] GetCUAExtensions(bool includeAll)
		{
			return GetExtensions(CUAExtensions, "All CUA files", includeAll);
		}

		public static ExtensionItem[] GetPluginExtensions(bool includeAll)
		{
			return GetExtensions(PluginExtensions, "All plugin files", includeAll);
		}

		public static ExtensionItem[] GetPresetExtensions(bool includeAll)
		{
			return GetExtensions(PresetExtensions, "All preset files", includeAll);
		}

		public static ExtensionItem[] GetLegacyPresetExtensions(bool includeAll)
		{
			return GetExtensions(LegacyPresetExtensions, "All preset files", includeAll);
		}

		public static ExtensionItem[] GetExtensions(
			FS.Extension[] exts, string allText, bool includeAll)
		{
			var list = new List<ExtensionItem>();

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
				list.Add(new ExtensionItem(all, allExts.ToArray()));
			}

			foreach (var e in exts)
				list.Add(new ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			if (includeAll)
			{
				list.Add(new ExtensionItem("All files (*.*)", new string[] { "*.*" }));
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

			//fd_.Show(Modes.OpenScene(), null, "VaM/Packages");

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool += (fb, cb, cd) =>
			{
				Log.Info($"show filebrowser request fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				if (fb == SuperController.singleton.fileBrowserUI)
				{
					var t = fb.titleText?.text ?? "";

					if (t == "Select Scene For Merge" ||
						t == "Select Scene For Edit" ||
						t == "Select Scene To Load")
					{
						Show(Modes.OpenScene(), cb);
						return;
					}
					else if (t == "Select Save File")
					{
						Show(Modes.SaveScene(), cb);
						return;
					}
					else if (t == "Select Preset File")
					{
						if (fb.fileFormat == "json|vac|zip")
						{
							Show(Modes.OpenPreset(fb.defaultPath), cb, null);
							return;
						}
					}
					else if (t == "Select Save Preset File")
					{
						if (fb.fileFormat == "json|vac|zip")
						{
							Show(Modes.SavePreset(fb.defaultPath), cb, null);
							return;
						}
					}
				}

				Log.Error($"unknown show filebrowser request");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool", () =>
				{
					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				});
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool += (fb, cb, cd) =>
			{
				Log.Info($"show filebrowser request (full) fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				if (fb == SuperController.singleton.mediaFileBrowserUI)
				{
					var t = fb.titleText?.text ?? "";

					if (t == "Select File")
					{
						if (fb.fileFormat == "assetbundle|scene")
						{
							Show(Modes.OpenCUA(), cb);
							return;
						}
						else if (fb.fileFormat == "cs|cslist|dll")
						{
							Show(Modes.OpenPlugin(), cb);
							return;
						}
						else if (fb.fileFormat == "vap")
						{
							Show(Modes.OpenPreset(fb.defaultPath), cb, null);
							return;
						}
					}
				}

				Log.Error($"unknown show filebrowser request (full)");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat}");

				Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool", () =>
				{
					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				});
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool += (fb, path, pkgFilter, flatten, includeRegularDirs) =>
			{
				if (!fd_.Visible)
				{
					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool", () =>
					{
						fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
					});

					return;
				}

				path = path.Replace("\\", "/");
				path = "VaM/" + path;

				path = path.Replace("/AddonPackages/", "/Packages/");
				path = path.Replace(".var", "");

				fd_.SelectDirectory(
					path, FileDialog.SelectDirectoryNoFlags,
					VUI.TreeView.ScrollToCenter);
			};
		}

		private void Show(IFileDialogMode mode, FileBrowserCallback cb, string cwd = null)
		{
			fd_.Show(mode, (path) => cb?.Invoke(path), cwd);
		}

		private void Show(IFileDialogMode mode, FileBrowserFullCallback cb, string cwd = null)
		{
			fd_.Show(mode, (path) => cb?.Invoke(path, true), cwd);
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
			base.DoLoadOptions(o);
			fd_.LoadOptions(o);
		}

		protected override void DoSaveOptions(JSONClass o)
		{
			base.DoSaveOptions(o);
			fd_.SaveOptions(o);
		}
	}
}
