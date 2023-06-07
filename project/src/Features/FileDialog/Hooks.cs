using uFileBrowser;

namespace AUI.FileDialog
{
	interface IFileDialogHook
	{
		void Enable();
		void Disable();
	}


	class VamosHook : IFileDialogHook
	{
		private readonly FileDialogFeature fd_;

		public VamosHook(FileDialogFeature fd)
		{
			fd_ = fd;
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public void Enable()
		{
			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool += (fb, cb, cd) =>
			{
				if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
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
				if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
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
				if (fd_.IgnoreHandler() || !fd_.GotoDirectoryHandler(fb, path, pkgFilter, flatten, includeRegularDirs))
				{
					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool", () =>
					{
						fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
					});
				}
			};
		}

		public void Disable()
		{
		}
	}


#if VAM_GT_1_22
	class VamHook : IFileDialogHook
	{
		private readonly FileDialogFeature fd_;
		private readonly FileBrowser[] browsers_;

		public VamHook(FileDialogFeature fd)
		{
			fd_ = fd;

			browsers_ = new FileBrowser[]
			{
				SuperController.singleton.fileBrowserUI,
				SuperController.singleton.fileBrowserWorldUI,
				SuperController.singleton.templatesFileBrowserWorldUI,
				SuperController.singleton.mediaFileBrowserUI,
				SuperController.singleton.directoryBrowserUI,
			};
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public void Enable()
		{
			foreach (var b in browsers_)
				SetHandlers(b, false);
		}

		public void Disable()
		{
			foreach (var b in browsers_)
				SetHandlers(b, true);
		}

		private void SetHandlers(FileBrowser fb, bool clear)
		{
			SetShowHandler(fb, clear);
			SetShowFullHandler(fb, clear);
			SetGotoDirectoryHandler(fb, clear);
		}

		private void SetShowHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.showHandler = null;
			else
				fb.showHandler = (cb, cd) => ShowHandler(fb, cb, cd);
		}

		private void SetShowFullHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.showFullHandler = null;
			else
				fb.showFullHandler = (cb, cd) => ShowFullHandler(fb, cb, cd);
		}

		private void SetGotoDirectoryHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.gotoDirectoryHandler = null;
			else
				fb.gotoDirectoryHandler = (p, pk, f, i) => GotoDirectoryHandler(fb, p, pk, f, i);
		}

		private void ShowHandler(FileBrowser fb, FileBrowserCallback cb, bool cd)
		{
			if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
			{
				Log.Error($"unknown show filebrowser request");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				try
				{
					SetShowHandler(fb, true);

					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				}
				finally
				{
					SetShowHandler(fb, false);
				}
			}
		}

		private void ShowFullHandler(FileBrowser fb, FileBrowserFullCallback cb, bool cd)
		{
			if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
			{
				Log.Error($"unknown show filebrowser request (full)");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				try
				{
					SetShowFullHandler(fb, true);

					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				}
				finally
				{
					SetShowFullHandler(fb, false);
				}
			}
		}

		private void GotoDirectoryHandler(FileBrowser fb, string path, string pkgFilter, bool flatten, bool includeRegularDirs)
		{
			if (fd_.IgnoreHandler() || !fd_.GotoDirectoryHandler(fb, path, pkgFilter, flatten, includeRegularDirs))
			{
				try
				{
					SetGotoDirectoryHandler(fb, true);
					fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
				}
				finally
				{
					SetGotoDirectoryHandler(fb, false);
				}
			}
		}
	}
#endif  // VAM_GT_21
}
