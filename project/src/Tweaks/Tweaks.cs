using UnityEngine;
using UnityEngine.UI;

namespace AUI.Tweaks
{
	class Tweaks : IAlternateUI
	{
		public Tweaks()
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			OnSceneLoaded();
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		private void OnSceneLoaded()
		{
			SuperController.singleton.gameMode = SuperController.GameMode.Edit;
			SuperController.singleton.ShowMainHUD();

			//SuperController.LogError("scene loaded");

			var atoms = SuperController.singleton.GetAtoms();
			//SuperController.LogError($"{atoms.Count} atoms");

			foreach (var a in atoms)
			{
				if (a.type != "Person")
					continue;

				foreach (var fc in a.freeControllers)
				{
					if (fc.name == "headControl")
					{
						//SuperController.LogError($"focusing head of {a.uid}");
						SuperController.singleton.FocusOnController(fc);
						return;
					}
				}
			}

			//SuperController.LogError($"nothing to focus");
		}
	}


	class DisableLoadPosition : IAlternateUI
	{
		public DisableLoadPosition()
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			OnSceneLoaded();
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		private void OnSceneLoaded()
		{
			SuperController.singleton.useSceneLoadPosition = false;
		}
	}


	class MoveNewLight: IAlternateUI
	{
		public MoveNewLight()
		{
			SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		private void OnAtomAdded(Atom a)
		{
			if (a.type == "InvisibleLight")
			{
				a.mainController.MoveControl(new Vector3(
					a.mainController.transform.position.x,
					a.mainController.transform.position.y,
					0.6f));
			}
		}
	}
}
