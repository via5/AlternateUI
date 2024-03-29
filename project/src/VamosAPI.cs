﻿using System;
using System.Collections;
using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;

namespace Vamos
{
	class VamosAPIReceiver : MonoBehaviour
	{
		public delegate void uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool_Handler(FileBrowser self, FileBrowserCallback cb, bool changeDirectory);
		public event uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool_Handler uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool;

		public delegate void uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool_Handler(FileBrowser self, FileBrowserFullCallback cb, bool changeDirectory);
		public event uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool_Handler uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool;

		public delegate void uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool_Handler(FileBrowser self, string path, string pkgFilter, bool flatten, bool includeRegularDirs);
		public event uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool_Handler uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool;

		public void Vamos_uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool(object[] args)
		{
			try
			{
				//SuperController.LogMessage($"VamosAPIReceiver: got uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool {args.Length}");
				uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool?.Invoke(args[0] as FileBrowser, args[1] as FileBrowserCallback, (bool)args[2]);
				//SuperController.LogMessage($"ok");
			}
			catch (Exception e)
			{
				SuperController.LogMessage($"VamosAPIReceiver: exception in uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
				SuperController.LogMessage(e.ToString());
			}
		}

		public void Vamos_uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool(object[] args)
		{
			try
			{
				//SuperController.LogMessage($"VamosAPIReceiver: got uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool {args.Length}");
				uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool?.Invoke(args[0] as FileBrowser, args[1] as FileBrowserFullCallback, (bool)args[2]);
			}
			catch (Exception e)
			{
				SuperController.LogMessage($"VamosAPIReceiver: exception in uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool");
				SuperController.LogMessage(e.ToString());
			}
		}

		public void Vamos_uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool(object[] args)
		{
			try
			{
				//SuperController.LogMessage($"VamosAPIReceiver: got Vamos_uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool {args.Length}");
				uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool?.Invoke(args[0] as FileBrowser, args[1] as string, args[2] as string, (bool)args[3], (bool)args[4]);
			}
			catch (Exception e)
			{
				SuperController.LogMessage($"VamosAPIReceiver: exception in uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool");
				SuperController.LogMessage(e.ToString());
			}
		}

		public void VamosPong(object[] args)
		{
			API.Instance?.Pong(args[0] as string);
		}

		public void VamosInhibitReady(object[] args)
		{
			API.Instance?.InhibitReady(args[0] as string);
		}

		public override string ToString()
		{
			return "VamosAPIReceiver";
		}
	}


	class API : VamosAPIReceiver
	{
		private static API instance_ = null;

		private string name_ = null;
		private readonly List<string> enabledApis_ = new List<string>();
		private bool connected_ = false;
		private bool connecting_ = false;
		private float connectingTime_ = 0;
		private readonly HashSet<string> inhibitReady_ = new HashSet<string>();

		public static API Instance
		{
			get { return instance_; }
		}

		public static void Enable(string name)
		{
			RemoveComponent();
			AddComponent(name);
		}

		public static void Disable()
		{
			if (instance_ != null)
				instance_.DisableAllAPIs();

			RemoveComponent();
		}

		private static void RemoveComponent()
		{
			try
			{
				var sc = SuperController.singleton.gameObject;

				foreach (Component c in sc.GetComponents<Component>())
				{
					if (c.ToString().Contains("VamosAPIReceiver"))
						UnityEngine.Object.Destroy(c);
				}

				instance_ = null;
			}
			catch (Exception)
			{
				// eat it, happens on shutdown
			}
		}

		private static void AddComponent(string name)
		{
			var sc = SuperController.singleton.gameObject;

			instance_ = sc.AddComponent<API>();
			instance_.name_ = name;
			instance_.Connect();
		}

		public void DoUpdate(float s)
		{
			if (connecting_)
			{
				connectingTime_ += s;
				if (connectingTime_ >= 1)
				{
					Log($"connection timeout, retrying");

					connecting_ = false;
					Connect();
				}
			}
		}

		public void EnableAPI(string name)
		{
			if (connected_)
				DoEnableAPI(name);
			else
				Log($"enabled api {name} (deferred)");

			enabledApis_.Add(name);
		}

		public void DisableAPI(string name)
		{
			DoDisableAPI(name);
			enabledApis_.Remove(name);
		}

		public void InhibitNext(string name, Action f)
		{
			SuperController.singleton.StartCoroutine(CoInhibitNext(name, f));
		}

		private IEnumerator CoInhibitNext(string name, Action f)
		{
			name = WrapName(name);
			Log($"inhibiting next {name}, waiting");

			inhibitReady_.Remove(name);
			SuperController.singleton.SendMessage("VamosInhibitNext", new object[] { name });

			for (int i = 0; i < 100; ++i)
			{
				yield return new WaitForSeconds(0.01f);

				if (inhibitReady_.Contains(name))
				{
					Log($"inhibit {name} is ready");
					inhibitReady_.Remove(name);
					f();
					yield break;
				}
			}

			Log($"timeout while waiting for inhibit {name}");
		}

		public void DisableAllAPIs()
		{
			foreach (var e in enabledApis_)
				DoDisableAPI(e);

			enabledApis_.Clear();
		}

		private void Connect()
		{
			if (connecting_)
			{
				Log("already connecting to vamos");
				return;
			}

			Log($"connecting to vamos");

			connecting_ = true;
			connectingTime_ = 0;
			SuperController.singleton.SendMessage("VamosPing", new object[] { name_ });
		}

		public void Pong(string name)
		{
			if (name == name_)
			{
				Log($"got pong, connected");
				connecting_ = false;
				connected_ = true;

				foreach (var e in enabledApis_)
					DoEnableAPI(e);
			}
		}

		public void InhibitReady(string name)
		{
			Log($"got inhibit ready for {name}");
			inhibitReady_.Add(name);
		}

		private void DoEnableAPI(string name)
		{
			name = WrapName(name);
			Log($"enabled api {name}");
			SuperController.singleton.SendMessage("VamosEnableAPI", new object[] { name });
		}

		private void DoDisableAPI(string name)
		{
			name = WrapName(name);
			Log($"disabled api {name}");
			SuperController.singleton.SendMessage("VamosDisableAPI", new object[] { name });
		}

		private string WrapName(string name)
		{
			return "Vamos_" + name;
		}

		private void Log(string s)
		{
			//SuperController.LogMessage($"VamosAPIReceiver.{name}: {s}");
		}
	}
}
