﻿using MVR.FileManagementSecure;
using SimpleJSON;
using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;

namespace AUI.FileDialog
{
	using FMS = FileManagerSecure;

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

		public static FS.Extension[] TextureExtensions = new FS.Extension[]
		{
			new FS.Extension("JPG files", new string[]{ ".jpg", ".jpeg" }),
			new FS.Extension("PNG files", ".png"),
			new FS.Extension("TIF files", new string[]{ ".tif", ".tiff" })
		};

		public static FS.Extension[] SoundExtensions = new FS.Extension[]
		{
			new FS.Extension("MP3 files", ".mp3"),
			new FS.Extension("OGG files", ".ogg"),
			new FS.Extension("WAV files", ".wav")
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

		public static ExtensionItem[] GetTextureExtensions(bool includeAll)
		{
			return GetExtensions(TextureExtensions, "All texture files", includeAll);
		}

		public static ExtensionItem[] GetSoundExtensions(bool includeAll)
		{
			return GetExtensions(SoundExtensions, "All sound files", includeAll);
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

					all += e.ExtensionString;

					foreach (var ee in e.Extensions)
						allExts.Add(ee);
				}

				all = $"{allText} ({all})";
				list.Add(new ExtensionItem(all, allExts.ToArray()));
			}

			foreach (var e in exts)
				list.Add(new ExtensionItem(e.Name + " (" + e.ExtensionString + ")", e.Extensions));

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
			FS.Filesystem.Init();

			var root = SuperController.singleton.transform;

			fd_.Enable();

			//fd_.Show(Modes.OpenPreset("Custom/Atom/Person/Plugins"), null, "VaM/Custom/Atom/Person/Plugins");
			//fd_.Show(Modes.OpenScene(), null);

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool += (fb, cb, cd) =>
			{
				if (IgnoreHandler() || !ShowHandler(fb, cb, cd))
				{
					Log.Error($"unknown show filebrowser request");
					Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool", () =>
					{
						fb.gameObject.SetActive(true);
						fb.transform.parent.gameObject.SetActive(true);
						fb.Show(cb, cd);
					});
				}
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool += (fb, cb, cd) =>
			{
				if (IgnoreHandler() || !ShowHandler(fb, cb, cd))
				{
					Log.Error($"unknown show filebrowser request (full)");
					Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool", () =>
					{
						fb.gameObject.SetActive(true);
						fb.transform.parent.gameObject.SetActive(true);
						fb.Show(cb, cd);
					});
				}
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool += (fb, path, pkgFilter, flatten, includeRegularDirs) =>
			{
				if (IgnoreHandler() || !GotoDirectoryHandler(fb, path, pkgFilter, flatten, includeRegularDirs))
				{
					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool", () =>
					{
						fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
					});
				}
			};
		}


		private bool IgnoreHandler()
		{
			return
				UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) ||
				UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);
		}

		private bool ShowHandler(FileBrowser fb, FileBrowserCallback cb, bool cd)
		{
			string path = FS.Path.Normalize(fb.defaultPath);

			Log.Verbose(
				$"show filebrowser request fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={fb.defaultPath} normPath={path}");

			var t = fb.titleText?.text ?? "";

			if (fb == SuperController.singleton.fileBrowserUI)
			{
				if (t == "Select Scene For Merge")
				{
					Show(Modes.OpenScene("Merge scene"), cb);
					return true;
				}
				else if (t == "Select Scene For Edit")
				{
					Show(Modes.OpenScene("Open scene for edit"), cb);
					return true;
				}
				else if (t == "Select Scene To Load")
				{
					Show(Modes.OpenScene(), cb);
					return true;
				}
				else if (t == "Select Save File")
				{
					Show(Modes.SaveScene(), cb);
					return true;
				}
				else if (t == "Select Preset File")
				{
					if (fb.fileFormat == "json|vac|zip")
					{
						Show(Modes.OpenPreset(path), cb, null);
						return true;
					}
				}
				else if (t == "Select Save Preset File")
				{
					if (fb.fileFormat == "json|vac|zip")
					{
						Show(Modes.SavePreset(path), cb, null);
						return true;
					}
				}
			}
			else if (fb == SuperController.singleton.mediaFileBrowserUI)
			{
				if (t == "Select File")
				{
					if (fb.fileFormat == "mp3|ogg|wav")
					{
						Show(Modes.OpenSound(), cb, null);
						return true;
					}
				}
			}

			return false;
		}

		private bool ShowHandler(FileBrowser fb, FileBrowserFullCallback cb, bool cd)
		{
			string path = FS.Path.Normalize(fb.defaultPath);

			Log.Verbose(
				$"show filebrowser request (full) fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={fb.defaultPath} normPath={path}");

			if (fb == SuperController.singleton.mediaFileBrowserUI)
			{
				var t = fb.titleText?.text ?? "";

				if (t == "Select File")
				{
					if (fb.fileFormat == "assetbundle|scene")
					{
						Show(Modes.OpenCUA(), cb);
						return true;
					}
					else if (fb.fileFormat == "cs|cslist|dll")
					{
						Show(Modes.OpenPlugin(), cb);
						return true;
					}
					else if (fb.fileFormat == "vap")
					{
						Show(Modes.OpenPreset(path), cb, null);
						return true;
					}
					else if (fb.fileFormat == "jpg|jpeg|png|tif|tiff")
					{
						Show(Modes.OpenTexture(path), cb, null);
						return true;
					}
				}
			}

			return false;
		}

		private bool GotoDirectoryHandler(
			FileBrowser fb, string originalPath, string pkgFilter,
			bool flatten, bool includeRegularDirs)
		{
			if (!fd_.Visible)
				return false;

			string path = FS.Path.MakeFSPath(originalPath);

			Log.Info(
				$"show filebrowser request (full) fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={originalPath} normPath={path}");

			fd_.SelectDirectory(
				path, FileDialog.SelectDirectoryNoFlags,
				VUI.TreeView.ScrollToCenter);

			return true;
		}


		private void Show(IFileDialogMode mode, FileBrowserCallback cb, string cwd = null)
		{
			var f = GetFilename();

			if (f != null)
			{
				cwd = FS.Path.Parent(f);
				Log.Verbose($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path), cwd, f);
		}

		private void Show(IFileDialogMode mode, FileBrowserFullCallback cb, string cwd = null)
		{
			var f = GetFilename();

			if (f != null)
			{
				cwd = FS.Path.Parent(f);
				Log.Verbose($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path, true), cwd, f);
		}

		private string GetFilename()
		{
			var button = UnityEngine.EventSystems.EventSystem
				.current.currentSelectedGameObject;

			Log.Verbose($"sel={button}");

			if (button == null || button.GetComponent<UnityEngine.UI.Button>() == null)
				return null;

			string f = GetFilenameFromSelect(button);
			if (f != null)
				return f;

			f = GetFilenameForPlugin(button);
			if (f != null)
				return f;

			return null;
		}

		private string GetFilenameFromSelect(GameObject button)
		{
			var parent = button.transform.parent;
			if (parent == null)
				return null;

			foreach (Transform child in parent)
			{
				if (!child.name.Contains("Url"))
					continue;

				var s = child.GetComponent<UnityEngine.UI.Text>()?.text;
				if (string.IsNullOrEmpty(s))
					continue;

				return FS.Path.MakeFSPathFromShort(s);
			}

			return null;
		}

		private string GetFilenameForPlugin(GameObject button)
		{
			var parent = button.transform.parent;
			if (parent == null)
			{
				Log.Verbose($"button {button} has no parent");
				return null;
			}

			var panel = parent.Find("Panel");
			if (panel == null)
			{
				Log.Verbose($"button {button} parent {parent} has no Panel child");
				return null;
			}

			var url = panel.Find("URL");
			if (url == null)
			{
				Log.Verbose($"panel {panel} has no URL child");
				return null;
			}

			var text = url?.GetComponent<UnityEngine.UI.Text>()?.text;
			if (text == null)
			{
				Log.Verbose($"url {url} has text");
				return null;
			}

			return FS.Path.MakeFSPathFromShort(text);
		}

		protected override void DoDisable()
		{
			fd_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			fd_.Update(s);
		}
	}
}
