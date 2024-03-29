﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	public class DropShadow
	{
		private Color c_;
		private Vector2 distance_;
		private Shadow shadow_;

		public DropShadow()
		{
		}

		public void Set(GameObject o, Color c, Vector2 distance)
		{
			c_ = c;
			distance_ = distance;

			if (o != null)
				Create(o);
		}

		public void Create(GameObject o)
		{
			if (o == null)
				return;

			if (shadow_ == null)
			{
				var image = o.GetComponent<UnityEngine.UI.Image>();
				if (image == null)
					image = o.AddComponent<UnityEngine.UI.Image>();

				shadow_ = o.AddComponent<Shadow>();
			}

			shadow_.effectColor = c_;
			shadow_.effectDistance = distance_;
			shadow_.useGraphicAlpha = true;
		}
	}


	public abstract class Widget : IDisposable, IWidget
	{
		public virtual string TypeName { get { return "Widget"; } }

		public delegate void Callback();
		public event Callback Created;

		public const float DontCare = -1;

		private Widget parent_ = null;
		private string name_ = "";
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rectangle bounds_ = new Rectangle();
		private Size minSize_ = new Size(DontCare, DontCare);
		private Size maxSize_ = new Size(DontCare, DontCare);
		private bool fixedBounds_ = false;

		private GameObject mainObject_ = null;
		private GameObject widgetObject_ = null;
		private GameObject graphicsObject_ = null;
		private WidgetBorderGraphics borderGraphics_ = null;

		private RectTransform widgetObjectRT_ = null;
		private RectTransform mainObjectRT_ = null;
		private RectTransform borderGraphicsRT_ = null;
		private LayoutElement mainObjectLE_ = null;

		private DropShadow dropShadow_ = null;

		private bool render_ = true;
		private bool visible_ = true;
		private bool enabled_ = true;
		private Insets margins_ = new Insets();
		private Insets borders_ = new Insets();
		private Insets padding_ = new Insets();
		private Color borderColor_ = Style.Theme.BorderColor;
		private Font font_ = null;
		private FontStyle fontStyle_ = FontStyle.Normal;
		private int fontSize_ = -1;
		private Color textColor_ = Style.Theme.TextColor;
		private readonly Tooltip tooltip_;
		private Events events_ = new Events();
		private bool wantsFocus_ = true;
		private bool didLayoutWhileRendered_ = false;
		private bool bringToTop_ = false;

		private bool dirty_ = true;


		public Widget(string name = "")
		{
			name_ = name;
			tooltip_ = new Tooltip();
		}

		public Logger Log
		{
			get
			{
				return GetRoot()?.Log ?? Logger.Global;
			}
		}

		public virtual void Dispose()
		{
			Destroy();
		}

		protected virtual void Destroy()
		{
			foreach (var c in children_)
				c.Destroy();

			if (mainObject_ != null)
			{
				UnityEngine.Object.Destroy(mainObject_);
				mainObject_ = null;
				widgetObject_ = null;
				graphicsObject_ = null;
				borderGraphics_ = null;
			}
		}

		public static string S(string s, params object[] ps)
		{
			return Glue.GetString(s, ps);
		}

		public Layout Layout
		{
			get
			{
				return layout_;
			}

			set
			{
				layout_ = value;

				if (layout_ != null)
					layout_.Parent = this;

				NeedsLayout("layout changed");
			}
		}

		public Font Font
		{
			get
			{
				return font_;
			}

			set
			{
				if (font_ != value)
				{
					font_ = value;
					NeedsLayout("font changed");
				}
			}
		}

		public FontStyle FontStyle
		{
			get
			{
				return fontStyle_;
			}

			set
			{
				if (fontStyle_ != value)
				{
					fontStyle_ = value;
					NeedsLayout("font style changed");
				}
			}
		}

		public int FontSize
		{
			get
			{
				return fontSize_;
			}

			set
			{
				if (fontSize_ != value)
				{
					fontSize_ = value;
					NeedsLayout("font size changed");
				}
			}
		}

		public Color TextColor
		{
			get
			{
				return textColor_;
			}

			set
			{
				if (textColor_ != value)
				{
					textColor_ = value;
					Polish();
				}
			}
		}

		public void SetDropShadow(Color color, Vector2 distance)
		{
			if (dropShadow_ == null)
				dropShadow_ = new DropShadow();

			dropShadow_.Set(MainObject, color, distance);
		}

		public GameObject MainObject
		{
			get { return mainObject_; }
		}

		public GameObject WidgetObject
		{
			get { return widgetObject_; }
		}

		public RectTransform WidgetObjectRT
		{
			get { return widgetObjectRT_; }
		}

		public Events Events
		{
			get { return events_; }
		}

		public bool WantsFocus
		{
			get { return wantsFocus_; }
			set { wantsFocus_ = value; }
		}

		public bool Render
		{
			get
			{
				return render_;
			}

			set
			{
				if (render_ != value)
				{
					render_ = value;
					UpdateRenderState();
				}
			}
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;
					UpdateActiveState();

					if (!visible_)
						NeedsLayout("visibility changed to hidden", true);
				}
			}
		}

		public Widget WidgetAtInternal(Point p)
		{
			if (!IsVisibleOnScreen())
				return null;

			if (BoundsContainPoint(p))
			{
				for (int i = 0; i < children_.Count; ++i)
				{
					var w = children_[i].WidgetAtInternal(p);
					if (w != null)
						return w;
				}

				if (!IsTransparent())
					return this;
			}

			return null;
		}

		protected virtual bool BoundsContainPoint(Point p)
		{
			return AbsoluteClientBounds.Contains(p);
		}

		public bool HasParent(Widget w)
		{
			if (w == null)
				return false;

			if (w == this)
				return true;

			return Parent?.HasParent(w) ?? false;
		}

		protected virtual bool IsTransparent()
		{
			return false;
		}

		protected virtual void UpdateActiveState()
		{
			if (mainObject_ != null)
				mainObject_.SetActive(visible_);

			if (render_ && visible_)
			{
				var dirtyChild = AnyDirtyChild();
				if (dirtyChild != null)
				{
					NeedsLayout(
						"visibility changed, dirty child:\n" +
						dirtyChild.DebugLine);
				}
			}
		}

		private Widget AnyDirtyChild()
		{
			if (!visible_ || !render_)
				return null;

			if (dirty_)
				return this;

			foreach (var c in children_)
			{
				var w = c.AnyDirtyChild();
				if (w != null)
					return w;
			}

			return null;
		}

		public bool IsVisibleOnScreen()
		{
			if (mainObject_ == null)
				return false;
			else
				return mainObject_.activeInHierarchy && RenderInHierarchy;
		}

		private bool RenderInHierarchy
		{
			get
			{
				if (!render_)
					return false;

				if (Parent != null)
					return Parent.RenderInHierarchy;

				return true;
			}
		}

		public bool Enabled
		{
			get
			{
				if (!enabled_)
					return false;

				if (parent_ != null)
					return parent_.Enabled;

				return true;
			}

			set
			{
				if (enabled_ != value)
				{
					enabled_ = value;

					if (widgetObject_ != null)
					{
						DoSetEnabled(enabled_);
						PolishRecursive();
					}
				}
			}
		}

		private void PolishRecursive()
		{
			if (WidgetObject != null)
				Polish();

			foreach (var c in children_)
				c.PolishRecursive();
		}

		public void Polish()
		{
			if (WidgetObject != null)
				DoPolish();
		}

		protected virtual void DoPolish()
		{
			// no-op
		}

		public bool StrictlyEnabled
		{
			get
			{
				return enabled_;
			}
		}

		public Insets Margins
		{
			get
			{
				return margins_;
			}

			set
			{
				if (margins_ != value)
				{
					margins_ = value;
					NeedsLayout("margins changed");
				}
			}
		}

		public Insets Borders
		{
			get
			{
				return borders_;
			}

			set
			{
				if (borders_ != value)
				{
					borders_ = value;

					if (borderGraphics_ != null)
						borderGraphics_.Borders = value;

					NeedsLayout("borders changed");
				}
			}
		}

		public Insets Padding
		{
			get
			{
				return padding_;
			}

			set
			{
				if (padding_ != value)
				{
					padding_ = value;
					NeedsLayout("padding changed");
				}
			}
		}

		public Insets Insets
		{
			get { return margins_ + borders_ + padding_; }
		}

		public Color BorderColor
		{
			get
			{
				return borderColor_;
			}

			set
			{
				if (borderColor_ != value)
				{
					borderColor_ = value;

					if (borderGraphics_ != null)
						borderGraphics_.Color = value;
				}
			}
		}

		public Tooltip Tooltip
		{
			get { return tooltip_; }
		}

		public Rectangle Bounds
		{
			get { return new Rectangle(bounds_); }
		}

		public bool FixedBounds()
		{
			if (fixedBounds_)
				return true;

			return parent_?.FixedBounds() ?? false;
		}

		public bool StrictlyFixedBounds
		{
			get { return fixedBounds_; }
		}

		public Rectangle AbsoluteClientBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle ClientBounds
		{
			get
			{
				var r = new Rectangle(0, 0, Bounds.Size);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle RelativeBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				if (parent_ != null)
					r.Translate(-parent_.Bounds.TopLeft);

				return r;
			}
		}

		public void SetBounds(Rectangle r, bool isFixed = false)
		{
			bounds_ = r;
			fixedBounds_ = isFixed;
		}

		public void SetCapture()
		{
			GetRoot()?.SetCapture(this);
		}

		public void ReleaseCapture()
		{
			GetRoot()?.ReleaseCapture(this);
		}

		public List<Widget> Children
		{
			get { return new List<Widget>(children_); }
		}

		public Widget Parent
		{
			get { return parent_; }
		}

		public Size GetRealPreferredSize(float maxWidth, float maxHeight)
		{
			var s = new Size();

			if (layout_ != null)
				s = layout_.GetPreferredSize(maxWidth, maxHeight);

			s = Size.Max(s, DoGetPreferredSize(maxWidth, maxHeight));
			s = Size.Max(s, GetRealMinimumSize());

			if (maxSize_.Width >= 0)
				s.Width = Math.Min(s.Width, maxSize_.Width);

			if (maxSize_.Height >= 0)
				s.Height = Math.Min(s.Height, maxSize_.Height);

			s += Margins.Size + Borders.Size + Padding.Size;

			if (maxWidth != DontCare)
				s.Width = Math.Min(maxWidth, s.Width);

			if (maxHeight != DontCare)
				s.Height = Math.Min(maxHeight, s.Height);

			return s;
		}

		public Size GetRealMinimumSize()
		{
			var s = Size.Zero;

			if (layout_ != null)
				s = layout_.GetMinimumSize();

			s = Size.Max(s, DoGetMinimumSize());
			s = Size.Max(s, minSize_);

			return s;
		}

		public Size MinimumSize
		{
			get
			{
				return minSize_;
			}

			set
			{
				if (minSize_ != value)
				{
					NeedsLayout("min size changed");
					minSize_ = value;
				}
			}
		}

		public Size MaximumSize
		{
			get
			{
				return maxSize_;
			}

			set
			{
				if (maxSize_ != value)
				{
					maxSize_ = value;
					NeedsLayout("max size changed");
				}
			}
		}

		public string Name
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
			}
		}

		public virtual Root GetRoot()
		{
			if (parent_ != null)
				return parent_.GetRoot();

			return null;
		}

		public void Focus(int focusFlags = 0)
		{
			GetRoot()?.SetFocus(this, focusFlags);
		}

		protected virtual void DoFocus()
		{
			// no-op
		}

		protected virtual void DoBlur()
		{
			// no-op
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(name_))
				return $"{TypeName}";
			else
				return $"{TypeName}.{name_}";
		}

		public virtual string DebugLine
		{
			get
			{
				var list = new List<string>();

				list.Add(TypeName);

				if (name_ != "")
					list.Add(name_);

				list.Add("b=" + Bounds.ToString());
				list.Add("rb=" + RelativeBounds.ToString());
				list.Add("ps=" + GetRealPreferredSize(DontCare, DontCare).ToString());
				list.Add("ly=" + (Layout?.TypeName ?? "none"));
				list.Add("r=" + render_.ToString());
				list.Add("v=" + visible_.ToString());
				list.Add("d=" + dirty_.ToString());

				return string.Join(" ", list.ToArray());
			}
		}


		public void AddGeneric(IWidget w, LayoutData d = null)
		{
			Add((Widget)w, d);
		}

		public T Add<T>(T w, LayoutData d = null)
			where T : Widget
		{
			if (w.parent_ != null)
				Log.WarningST("widget already has a parent");

			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
			NeedsLayout("widget added (" + w.TypeName + ")");
			return w;
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Log.Error(
					"can't remove widget '" + w.Name + "' from " +
					"'" + Name + "', not found");

				return;
			}

			layout_.Remove(w);
			w.parent_ = null;

			NeedsLayout("widget removed (" + w.TypeName + ")");

			w.Destroy();
		}

		public void Remove()
		{
			if (parent_ == null)
			{
				Log.Error("can't remove '" + Name + ", no parent");
				return;
			}

			parent_.Remove(this);
		}

		public void RemoveAllChildren()
		{
			while (children_.Count > 0)
				Remove(children_[0]);
		}

		public void BringToTop()
		{
			bringToTop_ = true;

			if (widgetObject_ != null)
				Utilities.BringToTop(widgetObject_);
		}

		public void DoLayout()
		{
			Create();
			DoLayoutImpl();
			UpdateBounds();
		}

		protected void DoLayoutImpl()
		{
			layout_?.DoLayout();

			foreach (var w in children_)
			{
				if (w.IsVisibleOnScreen())
					w.DoLayoutImpl();
			}

			SetDirty(false);
		}

		public void Create()
		{
			bool created = false;
			Root root = GetRoot();

			if (mainObject_ == null && root != null)
			{
				created = true;

				mainObject_ = new GameObject(ToString());
				mainObjectRT_ = mainObject_.AddComponent<RectTransform>();
				mainObjectLE_ = mainObject_.AddComponent<LayoutElement>();
				mainObject_.AddComponent<MouseCallbacks>().Widget = this;

				if (parent_?.MainObject == null)
					mainObject_.transform.SetParent(root.WidgetParentTransform, false);
				else
					mainObject_.transform.SetParent(parent_.MainObject.transform, false);

				dropShadow_?.Create(MainObject);

				widgetObject_ = CreateGameObject();
				widgetObject_.AddComponent<MouseCallbacks>().Widget = this;
				widgetObject_.transform.SetParent(mainObject_.transform, false);

				DoCreate();
				DoSetEnabled(enabled_);

				widgetObjectRT_ = widgetObject_.GetComponent<RectTransform>();
				if (widgetObjectRT_ == null)
					widgetObjectRT_ = widgetObject_.AddComponent<RectTransform>();

				UpdateActiveState();
			}

			foreach (var w in children_)
			{
				if (w.visible_ && w.RenderInHierarchy)
					w.Create();
			}

			UpdateRenderState();

			if (created)
			{
				Created?.Invoke();

				if (root.Focused == this)
					DoFocus();

				if (bringToTop_)
					Utilities.BringToTop(widgetObject_);
			}

			DoPostCreate();
		}

		private void CreateBorderGraphics()
		{
			if (graphicsObject_ == null)
			{
				graphicsObject_ = new GameObject("WidgetBorders");
				graphicsObject_.transform.SetParent(mainObject_.transform, false);

				borderGraphics_ = graphicsObject_.AddComponent<WidgetBorderGraphics>();
				borderGraphicsRT_ = borderGraphics_.GetComponent<RectTransform>();
				borderGraphics_.Borders = borders_;
				borderGraphics_.Color = borderColor_;

				borderGraphicsRT_.gameObject.SetActive(IsVisibleOnScreen());

				SetBorderBounds();
			}
		}

		protected virtual void DoPostCreate()
		{
			// no-op
		}

		private void UpdateRenderState()
		{
			SetRender(render_);

			if (RenderInHierarchy && !didLayoutWhileRendered_)
			{
				didLayoutWhileRendered_ = true;
				NeedsLayout("first render true", true);
			}
		}

		private void SetRender(bool b)
		{
			if (widgetObject_ == null)
				return;

			if (!borders_.Empty)
			{
				CreateBorderGraphics();
				borderGraphics_?.gameObject?.SetActive(b);
			}

			DoSetRender(b);

			foreach (var c in children_)
				c.SetRender(b && c.render_);
		}

		protected virtual void DoSetRender(bool b)
		{
		}

		private void SetMainObjectBounds()
		{
			var r = RelativeBounds;

			Utilities.SetRectTransform(mainObjectRT_, r);
			Utilities.SetLayoutElement(mainObjectLE_, r.Size);
		}

		private void SetBorderBounds()
		{
			var r = new Rectangle(0, 0, Bounds.Size);
			r.Deflate(Margins);

			if (borderGraphicsRT_ != null)
				Utilities.SetRectTransform(borderGraphicsRT_, r);
		}

		protected virtual void SetWidgetObjectBounds()
		{
			Utilities.SetRectTransform(widgetObjectRT_, ClientBounds);
		}

		public void UpdateBounds()
		{
			BeforeUpdateBounds();

			SetMainObjectBounds();
			SetBorderBounds();
			SetWidgetObjectBounds();

			foreach (var w in children_)
			{
				if (w.MainObject != null)
					w.UpdateBounds();
			}

			UpdateActiveState();

			AfterUpdateBounds();
		}

		protected virtual void BeforeUpdateBounds()
		{
			// no-op
		}

		protected virtual void AfterUpdateBounds()
		{
			// no-op
		}

		public void NeedsLayout(string why, bool force = false)
		{
			if (parent_ != null && parent_.Layout is AbsoluteLayout)
				return;

			if (force || IsVisibleOnScreen())
				NeedsLayoutImpl(TypeName + ": " + why);
			else
				SetDirty(true, TypeName + ": " + why);
		}

		protected virtual void SetDirty(bool b, string why = "")
		{
			dirty_ = b;

			if (why != "")
				Log.Verbose("SetDirty: " + why);
		}

		protected virtual void NeedsLayoutImpl(string why)
		{
			if (parent_ != null)
				parent_.NeedsLayoutImpl(why);
		}

		public void Dump()
		{
			Log.Verbose(DumpString());
		}

		public string DumpString()
		{
			var lines = new List<string>();

			var p = Parent;
			while (p != null)
			{
				lines.Insert(0, p.DebugLine);
				p = p.Parent;
			}

			for (int i = 0; i < lines.Count; ++i)
				lines[i] = new string(' ', i * 2) + lines[i];

			int indent = lines.Count;

			lines.Add(new string(' ', indent * 2)  + DebugLine + "    *** <-");

			DumpChildren(lines, indent + 1);

			return string.Join("\n", lines.ToArray());
		}

		private void DumpChildren(List<string> lines, int indent)
		{
			foreach (var w in children_)
			{
				lines.Add(new string(' ', indent * 2) + w.DebugLine);
				w.DumpChildren(lines, indent + 1);
			}
		}


		public float TextLength(string s)
		{
			return Root.TextLength(Font, FontSize, FontStyle, s);
		}

		public Size TextSize(string s)
		{
			return Root.TextSize(Font, FontSize, FontStyle, s);
		}

		public Size TextSize(string s, Size maxSize, bool vertOverflow = false)
		{
			return Root.TextSize(Font, FontSize, FontStyle, s, maxSize, vertOverflow);
		}

		public Size FitText(string s, Size maxSize, bool vertOverflow = false)
		{
			return Root.FitText(
				Font, FontSize, FontStyle, TextAnchor.UpperLeft,
				s, maxSize, vertOverflow);
		}


		protected virtual GameObject CreateGameObject()
		{
			return new GameObject("Widget");
		}

		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual void DoSetEnabled(bool b)
		{
			// no-op
		}

		protected virtual Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(DontCare, DontCare);
		}

		protected virtual Size DoGetMinimumSize()
		{
			return new Size(DontCare, DontCare);
		}


		public void OnFocusInternal(Widget w)
		{
			if (WidgetObject!=null)
				DoFocus();

			events_.FireFocus(w);
		}

		public void OnBlurInternal(Widget w)
		{
			if (WidgetObject != null)
				DoBlur();

			events_.FireBlur(w);
		}

		public void OnPointerEnterInternal(PointerEventData d, bool manualBubble = false)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
			{
				var r = GetRoot();
				var c = r?.Captured;

				if (c == null || c == this)
				{
					GetRoot()?.WidgetEntered(this);
					bubble = events_.FirePointerEnter(this, d, manualBubble);
				}
			}

			if (manualBubble && bubble && parent_ != null)
				parent_.OnPointerEnterInternal(d, manualBubble);
		}

		public void OnPointerEnterInternalSynth()
		{
			var d = new PointerEventData(EventSystem.current);
			OnPointerEnterInternal(d, true);
		}

		public void OnPointerExitInternal(PointerEventData d)
		{
			if (IsVisibleOnScreen())
			{
				var r = GetRoot();

				if (r?.Captured != this)
				{
					r?.WidgetExited(this);
					events_.FirePointerExit(this, d);
				}
			}
		}

		public void OnPointerExitInternalSynth()
		{
			var d = new PointerEventData(EventSystem.current);
			OnPointerExitInternal(d);
		}

		public virtual void OnPointerDownInternal(PointerEventData d, bool setFocus=true)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
			{
				if (setFocus)
				{
					GetRoot()?.PointerDown(this);

					if (WantsFocus)
					{
						Focus();
						setFocus = false;
					}
				}

				bubble = events_.FirePointerDown(this, d);
			}


			if (bubble && parent_ != null)
				parent_.OnPointerDownInternal(d, setFocus);
		}

		public void OnPointerUpInternal(PointerEventData d)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
				bubble = events_.FirePointerUp(this, d);

			if (bubble && parent_ != null)
				parent_.OnPointerUpInternal(d);
		}

		public void OnPointerClickInternal(PointerEventData d)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
				bubble = events_.FirePointerClick(this, d);

			if (bubble && parent_ != null)
				parent_.OnPointerClickInternal(d);
		}

		public void OnPointerDoubleClickInternal(PointerEventData d)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
				bubble = events_.FirePointerDoubleClick(this, d);

			if (bubble && parent_ != null)
				parent_.OnPointerDoubleClickInternal(d);
		}

		public void OnPointerMoveInternal()
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
				bubble = events_.FirePointerMove(this, null);

			if (bubble && parent_ != null)
				parent_.OnPointerMoveInternal();
		}

		public void OnBeginDragInternal(PointerEventData d)
		{
			events_.FireDragStart(this, d);
		}

		public void OnDragInternal(PointerEventData d)
		{
			events_.FireDrag(this, d);
		}

		public void OnEndDragInternal(PointerEventData d)
		{
			events_.FireDragEnd(this, d);
		}

		public void OnWheelInternal(PointerEventData d)
		{
			bool bubble = true;

			if (IsVisibleOnScreen())
				bubble = events_.FireWheel(this, d);

			if (bubble && parent_ != null)
				parent_.OnWheelInternal(d);
		}
	}
}
