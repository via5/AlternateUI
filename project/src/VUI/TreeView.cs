using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	class ScrollBarHandle : Button
	{
		public delegate void Handler();
		public event Handler Moved;

		private Point dragStart_;
		private Rectangle initialBounds_;
		private bool dragging_ = false;

		public ScrollBarHandle()
		{
			Events.DragStart += OnDragStart;
			Events.Drag += OnDrag;
			Events.DragEnd += OnDragEnd;

			Events.PointerUp += (d) => { Glue.LogInfo("up"); return false; };
		}

		public bool OnDragStart(DragEvent e)
		{
			Glue.LogInfo("drag start");
			dragging_ = true;
			dragStart_ = e.Mouse;
			initialBounds_ = AbsoluteClientBounds;
			return false;
		}

		public bool OnDrag(DragEvent e)
		{
			Glue.LogInfo("drag");

			if (!dragging_)
				return false;

			var p = e.Mouse;
			var delta = p - dragStart_;

			var r = Rectangle.FromSize(
				initialBounds_.Left,
				initialBounds_.Top + (delta.Y),
				initialBounds_.Width,
				initialBounds_.Height);

			var box = Parent.AbsoluteClientBounds;

			if (r.Top < box.Top)
				r.MoveTo(r.Left, box.Top);

			if (r.Bottom > box.Bottom)
				r.MoveTo(r.Left, box.Bottom - r.Height);

			SetBounds(r);
			UpdateBounds();

			Moved?.Invoke();

			return false;
		}

		public bool OnDragEnd(DragEvent e)
		{
			Glue.LogInfo("drag end");
			dragging_ = false;
			return false;
		}
	}


	class ScrollBar : Panel
	{
		public delegate void ValueHandler(float v);
		public event ValueHandler ValueChanged;

		private ScrollBarHandle handle_ = new ScrollBarHandle();
		private float range_ = 0;
		private float value_ = 0;

		public ScrollBar()
		{
			Borders = new Insets(1, 0, 0, 0);
			Layout = new AbsoluteLayout();
			Clickthrough = false;
			Add(handle_);

			Events.PointerDown += OnPointerDown;
			handle_.Moved += OnHandleMoved;
		}

		public float Range
		{
			get { return range_; }
			set { range_ = value; }
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			var h = r.Height - range_;

			var cb = ClientBounds;
			var avh = cb.Height - handle_.ClientBounds.Height;
			var p = range_ == 0 ? 0 : (value_ / range_);
			r.Top += Borders.Top + p * avh;
			r.Bottom = r.Top + h;

			handle_.SetBounds(r);
			DoLayout();

			base.UpdateBounds();
		}

		private void OnHandleMoved()
		{
			var r = ClientBounds;
			var hr = handle_.RelativeBounds;
			var top = hr.Top - Borders.Top;
			var h = r.Height - hr.Height;
			var p = (top / h);
			value_ = p * range_;
			ValueChanged?.Invoke(value_);
		}

		private bool OnPointerDown(PointerEvent e)
		{
			Glue.LogInfo("pointer down");

			var r = AbsoluteClientBounds;
			var p = e.Mouse - r.TopLeft;
			var y = ClientBounds.Top + p.Y - handle_.ClientBounds.Height / 2;

			if (y < 0)
				y = 0;
			else if (y + handle_.ClientBounds.Height > ClientBounds.Height)
				y = ClientBounds.Height - handle_.ClientBounds.Height;

			var cb = handle_.AbsoluteClientBounds;
			var h = cb.Height;
			cb.Top = y;
			cb.Bottom = y + h;

			handle_.SetBounds(cb);
			DoLayout();
			base.UpdateBounds();

			OnHandleMoved();

			var d = e.EventData as PointerEventData;
			SuperController.singleton.StartCoroutine(StartDrag(d));

			return false;
		}

		private IEnumerator StartDrag(PointerEventData d)
		{
			yield return new WaitForEndOfFrame();

			var o = handle_.WidgetObject.gameObject;

			d.pointerPress = o;
			d.pointerDrag = o;
			d.rawPointerPress = o;
			d.pointerEnter = o;
			d.selectedObject = o;
			d.hovered.Clear();

			List<RaycastResult> rc = new List<RaycastResult>();
			EventSystem.current.RaycastAll(d, rc);

			foreach (var r in rc)
			{
				d.hovered.Add(r.gameObject);

				if (r.gameObject == o)
				{
					d.pointerCurrentRaycast = r;
					d.pointerPressRaycast = r;
					break;
				}
			}

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerEnterHandler);

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerDownHandler);
		}
	}



	class TreeView : Panel
	{
		public override string TypeName { get { return "TreeView"; } }

		public class Item
		{
			private Item parent_ = null;
			private string text_;
			private List<Item> children_ = null;
			private bool expanded_ = false;

			public Item(string text)
			{
				text_ = text;
			}

			public virtual TreeView TreeView
			{
				get
				{
					if (parent_ == null)
						return null;
					else
						return parent_.TreeView;
				}
			}

			public Item Parent
			{
				get { return parent_; }
				set { parent_ = value; }
			}

			public string Text
			{
				get { return text_; }
				set { text_ = value; }
			}

			public List<Item> Children
			{
				get{ return children_; }
			}

			public bool Expanded
			{
				get
				{
					return expanded_;
				}

				set
				{
					expanded_ = value;
					TreeView?.ItemExpanded(this);
				}
			}

			public void Toggle()
			{
				Expanded = !Expanded;
			}

			public virtual bool HasChildren
			{
				get { return children_ != null && children_.Count > 0; }
			}

			public void Add(Item child)
			{
				if (children_ == null)
					children_ = new List<Item>();

				children_.Add(child);
				child.Parent = this;
			}
		}


		class InternalRootItem : Item
		{
			private readonly TreeView tree_;

			public InternalRootItem(TreeView tree)
				: base("root")
			{
				tree_ = tree;
			}

			public override TreeView TreeView
			{
				get { return tree_; }
			}
		}


		class Node
		{
			private readonly TreeView tree_;
			private Item item_ = null;
			private ToolButton toggle_ = null;
			private Label label_ = null;

			public Node(TreeView t)
			{
				tree_ = t;
			}

			public void Clear()
			{
				Set(null, Rectangle.Zero);
			}

			public void Set(Item i, Rectangle r)
			{
				item_ = i;

				if (item_ == null)
				{
					if (toggle_ != null)
						toggle_.Render = false;

					if (label_ != null)
						label_.Render = false;
				}
				else
				{
					if (item_.HasChildren)
					{
						if (toggle_ == null)
						{
							toggle_ = new ToolButton("", OnToggle);
							tree_.Add(toggle_);
							toggle_.Create();
						}

						if (item_.Expanded)
							toggle_.Text = "-";
						else
							toggle_.Text = "+";

						var tr = r;
						tr.Width = 30;
						toggle_.SetBounds(tr);
						toggle_.Render = true;
					}
					else
					{
						if (toggle_ != null)
							toggle_.Render = false;
					}


					if (label_ == null)
					{
						label_ = new Label();
						tree_.Add(label_);
						label_.Create();
					}

					var lr = r;
					lr.Left += 35;
					label_.Text = item_.Text;
					label_.SetBounds(lr);
					label_.Render = true;
				}
			}

			private void OnToggle()
			{
				if (item_ != null)
					item_.Toggle();
			}
		}


		class NodeContext
		{
			public Rectangle av;
			public int nodeIndex;
			public int itemIndex;
			public float x, y;
			public int indent;
		}


		private const int InternalPadding = 5;
		private const int ItemHeight = 35;
		private const int ItemPadding = 2;
		private const int IndentSize = 50;
		private const int ScrollBarWidth = 40;

		private readonly InternalRootItem root_;
		private List<Node> nodes_ = new List<Node>();
		private ScrollBar vsb_ = new ScrollBar();
		private int topItemIndex_ = 0;
		private int itemCount_ = 0;
		private int visibleCount_ = 0;
		private IgnoreFlag ignoreVScroll_ = new IgnoreFlag();

		public TreeView()
		{
			root_ = new InternalRootItem(this);

			Borders = new Insets(1);
			Layout = new AbsoluteLayout();
			Clickthrough = false;

			Add(vsb_);

			Events.Wheel += OnWheel;
			vsb_.ValueChanged += OnVerticalScroll;
		}

		private void OnVerticalScroll(float v)
		{
			if (ignoreVScroll_) return;
			SetTopItem((int)(v / (ItemHeight + ItemPadding)), false);
		}

		private void SetTopItem(int index, bool updateSb)
		{
			topItemIndex_ = Utilities.Clamp(index, 0, itemCount_ - visibleCount_);

			if (updateSb)
			{
				float v = topItemIndex_ * (ItemHeight + ItemPadding);
				if (v + (ItemHeight + ItemPadding) > vsb_.Range)
					v = vsb_.Range;

				vsb_.Value = v;
			}

			UpdateNodes();
			base.UpdateBounds();
		}

		public Item RootItem
		{
			get{ return root_; }
		}

		public void ItemExpanded(Item i)
		{
			UpdateNodes();
			base.UpdateBounds();
		}

		private void UpdateNodes()
		{
			var cx = new NodeContext();
			cx.av = AbsoluteClientBounds;
			cx.nodeIndex = 0;
			cx.itemIndex = 0;
			cx.x = cx.av.Left + InternalPadding;
			cx.y = cx.av.Top + InternalPadding;
			cx.indent = 0;

			int nodeCount = (int)(cx.av.Height / (ItemHeight + ItemPadding));

			while (nodes_.Count < nodeCount)
				nodes_.Add(new Node(this));

			while (nodes_.Count > nodeCount)
				nodes_.RemoveAt(nodes_.Count - 1);


			UpdateNode(root_, cx);

			for (int i = cx.nodeIndex; i < nodes_.Count; ++i)
				nodes_[i].Clear();

			DoLayout();

			itemCount_ = cx.itemIndex;
			visibleCount_ = (int)(cx.av.Height / (ItemHeight + ItemPadding));

			float requiredHeight = (itemCount_ + 1) * (ItemHeight + ItemPadding);
			float missingHeight = requiredHeight - cx.av.Height;

			if (missingHeight <= 0)
			{
				vsb_.Visible = false;
			}
			else
			{
				vsb_.Visible = true;
				vsb_.Range = missingHeight;
			}
		}

		private void UpdateNode(Item item, NodeContext cx)
		{
			for (int i = 0; i < item.Children.Count; ++i)
			{
				var child = item.Children[i];

				if (cx.itemIndex >= topItemIndex_)
				{
					if (cx.nodeIndex < nodes_.Count)
					{
						var node = nodes_[cx.nodeIndex];

						var x = cx.x + cx.indent * IndentSize;
						var y = cx.y;

						var r = Rectangle.FromPoints(
							x, y,
							cx.av.Right - InternalPadding - ScrollBarWidth,
							y + ItemHeight);

						node.Set(child, r);

						cx.y += ItemHeight + ItemPadding;
						++cx.nodeIndex;
					}
				}

				++cx.itemIndex;

				if (child.Expanded)
				{
					++cx.indent;
					UpdateNode(child, cx);
					--cx.indent;
				}
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			//Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			r.Left = r.Right - ScrollBarWidth;
			vsb_.SetBounds(r);
			//hsb_.Set(0, 0, 10);

			UpdateNodes();

			base.UpdateBounds();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return base.DoGetPreferredSize(maxWidth, maxHeight);
		}

		protected override Size DoGetMinimumSize()
		{
			return base.DoGetMinimumSize();
		}

		protected override void DoSetRender(bool b)
		{
			base.DoSetRender(b);
		}

		private bool OnWheel(WheelEvent e)
		{
			ignoreVScroll_.Do(() =>
			{
				SetTopItem(topItemIndex_ + (int)-e.Delta.Y, true);
			});

			return false;
		}

		public override string DebugLine
		{
			get { return base.DebugLine; }
		}
	}
}
