using System;
using System.Collections.Generic;

namespace AUI
{
	public interface IAtomUIInfo
	{
		Atom Atom { get; }
		bool Enable();
		void Disable();
	}

	public abstract class BasicAtomUIInfo : IAtomUIInfo
	{
		private readonly Atom atom_;
		private readonly Logger log_;

		protected BasicAtomUIInfo(Atom a, string logPrefix)
		{
			atom_ = a;
			log_ = new Logger(logPrefix + "." + Atom.uid);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public abstract bool Enable();
		public abstract void Disable();
		public abstract bool IsLike(BasicAtomUIInfo other);

		public virtual void Update(float s)
		{
			// no-op
		}
	}

	abstract class AtomUIModifier
	{
		private const float DeferredCheckInterval = 1;

		private readonly Logger log_;
		private readonly List<BasicAtomUIInfo> deferred_ = new List<BasicAtomUIInfo>();
		private readonly List<BasicAtomUIInfo> atoms_ = new List<BasicAtomUIInfo>();
		private float deferredElapsed_ = 0;


		public AtomUIModifier(string logPrefix)
		{
			log_ = new Logger(logPrefix + ".uiMod");
		}

		public Logger Log
		{
			get { return log_; }
		}

		public List<BasicAtomUIInfo> Atoms
		{
			get { return atoms_; }
		}

		public virtual void Update(float s)
		{
			CheckDeferred(s);

			for (int i = 0; i < atoms_.Count; ++i)
				atoms_[i].Update(s);
		}

		public void Enable()
		{
			SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;

			CheckScene();
		}

		public void Disable()
		{
			SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;

			foreach (var a in atoms_)
				a.Disable();

			atoms_.Clear();
			deferred_.Clear();
		}

		protected abstract bool ValidAtom(Atom a);
		protected abstract BasicAtomUIInfo CreateAtomInfo(Atom a);

		private void CheckDeferred(float s)
		{
			if (deferred_.Count == 0)
				return;

			deferredElapsed_ += s;
			if (deferredElapsed_ >= DeferredCheckInterval)
			{
				deferredElapsed_ = 0;

				bool okay = true;

				for (int i = 0; i < deferred_.Count; ++i)
				{
					if (deferred_[i] == null)
						continue;

					if (deferred_[i].Enable())
					{
						Log.Verbose($"deferred atom {deferred_[i]} now okay");
						atoms_.Add(deferred_[i]);
						deferred_[i] = null;
					}
					else
					{
						okay = false;
					}
				}

				if (okay)
				{
					Log.Verbose($"all deferred atoms okay");
					deferred_.Clear();
				}
			}
		}

		private List<Atom> GetAtoms()
		{
			var list = new List<Atom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (ValidAtom(a))
					list.Add(a);
			}

			return list;
		}

		private void CheckScene()
		{
			atoms_.Clear();
			deferred_.Clear();

			foreach (var a in GetAtoms())
				TryAddAtom(a);
		}

		private void TryAddAtom(Atom a)
		{
			var i = CreateAtomInfo(a);

			if (i.Enable())
				atoms_.Add(i);
			else
				deferred_.Add(i);
		}

		private void OnAtomAdded(Atom a)
		{
			try
			{
				if (ValidAtom(a))
					TryAddAtom(a);
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomAdded for {a}:");
				Log.Error(e.ToString());
			}
		}

		private void OnAtomRemoved(Atom a)
		{
			try
			{
				for (int i = 0; i < atoms_.Count; ++i)
				{
					if (atoms_[i].Atom == a)
					{
						atoms_[i].Disable();
						atoms_.RemoveAt(i);
						break;
					}
				}
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomRemoved for {a}:");
				Log.Error(e.ToString());
			}
		}

		private void OnSceneLoaded()
		{
			try
			{
				CheckScene();
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnSceneLoaded:");
				Log.Error(e.ToString());
			}
		}
	}
}
