using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Text;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	static class Sys
	{
		public static string[] GetDirectories(IFilesystemObject from, string path)
		{
			string[] ss;

			Instrumentation.Start(I.FMSGetDirectories);
			{
				try
				{
					ss = FMS.GetDirectories(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.GetDirectories exception for '{path}'");
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
					ss = FMS.GetFiles(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.GetFiles exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					ss = new string[0];
				}
			}
			Instrumentation.End();

			return ss;
		}

		public static List<ShortCut> GetShortCutsForDirectory(IFilesystemObject from, string path)
		{
			List<ShortCut> ss;

			Instrumentation.Start(I.FMSGetShortCutsForDirectory);
			{
				try
				{
					ss = FMS.GetShortCutsForDirectory(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.GetShortCutsForDirectory exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					ss = new List<ShortCut>();
				}
			}
			Instrumentation.End();

			return ss;
		}

		public static DateTime FileCreationTime(IFilesystemObject from, string path)
		{
			DateTime dt = DateTime.MaxValue;

			Instrumentation.Start(I.FMSFileCreationTime);
			{
				try
				{
					dt = FMS.FileCreationTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.FileCreationTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static DateTime FileLastWriteTime(IFilesystemObject from, string path)
		{
			DateTime dt = DateTime.MaxValue;

			Instrumentation.Start(I.FMSFileLastWriteTime);
			{
				try
				{
					dt = FMS.FileLastWriteTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.FileLastWriteTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();

			return dt;
		}

		public static DateTime DirectoryCreationTime(IFilesystemObject from, string path)
		{
			DateTime dt = DateTime.MaxValue;

#if VAM_GT_1_22
			Instrumentation.Start(I.FMSDirectoryCreationTime);
			{
				try
				{
					dt = FMS.DirectoryCreationTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.DirectoryCreationTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();
#endif

			return dt;
		}

		public static DateTime DirectoryLastWriteTime(IFilesystemObject from, string path)
		{
			DateTime dt = DateTime.MaxValue;

#if VAM_GT_1_22
			Instrumentation.Start(I.FMSDirectoryLastWriteTime);
			{
				try
				{
					dt = FMS.DirectoryLastWriteTime(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.DirectoryLastWriteTime exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
				}
			}
			Instrumentation.End();
#endif

			return dt;
		}

		public static bool IsDirectoryInPackage(IFilesystemObject from, string path)
		{
			bool b;

			Instrumentation.Start(I.FMSIsDirectoryInPackage);
			{
				try
				{
					b = FMS.IsDirectoryInPackage(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.IsDirectoryInPackage exception for '{path}'");
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
					b = FMS.FileExists(path);
				}
				catch (Exception e)
				{
					from.Log.Error($"{from}: FMS.FileExists exception for '{path}'");
					from.Log.ErrorST($"{e.Message}");
					b = false;
				}
			}
			Instrumentation.End();

			return b;
		}

		public static string NormalizePath(string path)
		{
			return FMS.NormalizePath(path);
		}
	}
}
