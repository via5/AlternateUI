using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	static class SysWrappers
	{
		public static string[] GetDirectories(IFilesystemObject from, string path)
		{
			string[] ss;

			Instrumentation.Start(I.FMSGetDirectories);
			{
				try
				{
					ss = AlternateUI.Instance.Sys.GetDirectories(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.GetDirectories exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					ss = new string[0];
				}
			}
			Instrumentation.End();

			return ss;
		}

		public static string[] GetFiles(IFilesystemObject from, string path)
		{
			string[] ss;

			Instrumentation.Start(I.FMSGetFiles);
			{
				try
				{
					ss = AlternateUI.Instance.Sys.GetFiles(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.GetFiles exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					ss = new string[0];
				}
			}
			Instrumentation.End();

			return ss;
		}

		public static List<ISysShortCut> GetShortCutsForDirectory(IFilesystemObject from, string path)
		{
			List<ISysShortCut> ss;

			Instrumentation.Start(I.FMSGetShortCutsForDirectory);
			{
				try
				{
					ss = AlternateUI.Instance.Sys.GetShortCutsForDirectory(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.GetShortCutsForDirectory exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					ss = new List<ISysShortCut>();
				}
			}
			Instrumentation.End();

			return ss;
		}

		public static DateTime FileCreationTime(IFilesystemObject from, string path)
		{
			DateTime dt = Sys.BadDateTime;

			Instrumentation.Start(I.FMSFileCreationTime);
			{
				try
				{
					dt = AlternateUI.Instance.Sys.FileCreationTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.FileCreationTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static DateTime FileLastWriteTime(IFilesystemObject from, string path)
		{
			DateTime dt = Sys.BadDateTime;

			Instrumentation.Start(I.FMSFileLastWriteTime);
			{
				try
				{
					dt = AlternateUI.Instance.Sys.FileLastWriteTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.FileLastWriteTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static DateTime DirectoryCreationTime(IFilesystemObject from, string path)
		{
			DateTime dt = Sys.BadDateTime;

			Instrumentation.Start(I.FMSDirectoryCreationTime);
			{
				try
				{
					dt = AlternateUI.Instance.Sys.DirectoryCreationTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.DirectoryCreationTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static DateTime DirectoryLastWriteTime(IFilesystemObject from, string path)
		{
			DateTime dt = Sys.BadDateTime;

			Instrumentation.Start(I.FMSDirectoryLastWriteTime);
			{
				try
				{
					dt = AlternateUI.Instance.Sys.DirectoryLastWriteTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.DirectoryLastWriteTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static bool IsDirectoryInPackage(IFilesystemObject from, string path)
		{
			bool b;

			Instrumentation.Start(I.FMSIsDirectoryInPackage);
			{
				try
				{
					b = AlternateUI.Instance.Sys.IsDirectoryInPackage(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.IsDirectoryInPackage exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					b = false;
				}
			}
			Instrumentation.End();

			return b;
		}

		public static bool FileExists(IFilesystemObject from, string path)
		{
			bool b;

			Instrumentation.Start(I.FMSFileExists);
			{
				try
				{
					b = AlternateUI.Instance.Sys.FileExists(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: Sys.FileExists exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					b = false;
				}
			}
			Instrumentation.End();

			return b;
		}

		public static string NormalizePath(string path)
		{
			return AlternateUI.Instance.Sys.NormalizePath(path);
		}
	}
}
