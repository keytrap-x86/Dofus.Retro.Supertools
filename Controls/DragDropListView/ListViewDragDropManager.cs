﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Dofus.Retro.Supertools.Controls.DragDropListView
{
	#region ListViewDragDropManager

	/// <summary>
	///     Manages the dragging and dropping of ListViewItems in a ListView.
	///     The ItemType type parameter indicates the type of the objects in
	///     the ListView's items source.  The ListView's ItemsSource must be
	///     set to an instance of ObservableCollection of ItemType, or an
	///     Exception will be thrown.
	/// </summary>
	/// <typeparam name="TItemType">The type of the ListView's items.</typeparam>
	public class ListViewDragDropManager<TItemType> where TItemType : class
	{
		#region Data

		private bool canInitiateDrag;
		private DragAdorner dragAdorner;
		private double dragAdornerOpacity;
		private int indexToSelect;
		private TItemType itemUnderDragCursor;
		private ListView listView;
		private Point ptMouseDown;
		private bool showDragAdorner;

		#endregion // Data

		#region Constructors

		/// <summary>
		///     Initializes a new instance of ListViewDragManager.
		/// </summary>
		public ListViewDragDropManager()
		{
			canInitiateDrag = false;
			dragAdornerOpacity = 0.7;
			indexToSelect = -1;
			showDragAdorner = false;
		}

		/// <summary>
		///     Initializes a new instance of ListViewDragManager.
		/// </summary>
		/// <param name="listView"></param>
		public ListViewDragDropManager(ListView listView)
			: this()
		{
			ListView = listView;
		}

		/// <summary>
		///     Initializes a new instance of ListViewDragManager.
		/// </summary>
		/// <param name="listView"></param>
		/// <param name="dragAdornerOpacity"></param>
		public ListViewDragDropManager(ListView listView, double dragAdornerOpacity)
			: this(listView)
		{
			DragAdornerOpacity = dragAdornerOpacity;
		}

		/// <summary>
		///     Initializes a new instance of ListViewDragManager.
		/// </summary>
		/// <param name="listView"></param>
		/// <param name="showDragAdorner"></param>
		public ListViewDragDropManager(ListView listView, bool showDragAdorner)
			: this(listView)
		{
			ShowDragAdorner = showDragAdorner;
		}

		#endregion // Constructors

		#region Public Interface

		#region DragAdornerOpacity

		/// <summary>
		///     Gets/sets the opacity of the drag adorner.  This property has no
		///     effect if ShowDragAdorner is false. The default value is 0.7
		/// </summary>
		public double DragAdornerOpacity
		{
			get => dragAdornerOpacity;
			set
			{
				if (IsDragInProgress)
					throw new InvalidOperationException(
						"Cannot set the DragAdornerOpacity property during a drag operation.");

				if (value < 0.0 || value > 1.0)
					throw new ArgumentOutOfRangeException("DragAdornerOpacity", value, "Must be between 0 and 1.");

				dragAdornerOpacity = value;
			}
		}

		#endregion // DragAdornerOpacity

		#region IsDragInProgress

		/// <summary>
		///     Returns true if there is currently a drag operation being managed.
		/// </summary>
		public bool IsDragInProgress { get; private set; }

		#endregion // IsDragInProgress

		#region ListView

		/// <summary>
		///     Gets/sets the ListView whose dragging is managed.  This property
		///     can be set to null, to prevent drag management from occuring.  If
		///     the ListView's AllowDrop property is false, it will be set to true.
		/// </summary>
		public ListView ListView
		{
			get { return listView; }
			set
			{
				if (IsDragInProgress)
					throw new InvalidOperationException("Cannot set the ListView property during a drag operation.");

				if (listView != null)
				{
					#region Unhook Events

					listView.PreviewMouseLeftButtonDown -= listView_PreviewMouseLeftButtonDown;
					listView.PreviewMouseMove -= listView_PreviewMouseMove;
					listView.DragOver -= listView_DragOver;
					listView.DragLeave -= listView_DragLeave;
					listView.DragEnter -= listView_DragEnter;
					listView.Drop -= listView_Drop;

					#endregion // Unhook Events
				}

				listView = value;

				if (listView != null)
				{
					if (!listView.AllowDrop)
						listView.AllowDrop = true;

					#region Hook Events

					listView.PreviewMouseLeftButtonDown += listView_PreviewMouseLeftButtonDown;
					listView.PreviewMouseMove += listView_PreviewMouseMove;
					listView.DragOver += listView_DragOver;
					listView.DragLeave += listView_DragLeave;
					listView.DragEnter += listView_DragEnter;
					listView.Drop += listView_Drop;

					#endregion // Hook Events
				}
			}
		}

		#endregion // ListView

		#region ProcessDrop [event]

		/// <summary>
		///     Raised when a drop occurs.  By default the dropped item will be moved
		///     to the target index.  Handle this event if relocating the dropped item
		///     requires custom behavior.  Note, if this event is handled the default
		///     item dropping logic will not occur.
		/// </summary>
		public event EventHandler<ProcessDropEventArgs<TItemType>> ProcessDrop;

		#endregion // ProcessDrop [event]

		#region ShowDragAdorner

		/// <summary>
		///     Gets/sets whether a visual representation of the ListViewItem being dragged
		///     follows the mouse cursor during a drag operation.  The default value is true.
		/// </summary>
		public bool ShowDragAdorner
		{
			get => showDragAdorner;
			set
			{
				if (IsDragInProgress)
					throw new InvalidOperationException(
						"Cannot set the ShowDragAdorner property during a drag operation.");

				showDragAdorner = value;
			}
		}

		#endregion // ShowDragAdorner

		#endregion // Public Interface

		#region Event Handling Methods

		#region listView_PreviewMouseLeftButtonDown

		private void listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (IsMouseOverScrollbar)
			{
				// 4/13/2007 - Set the flag to false when cursor is over scrollbar.
				canInitiateDrag = false;
				return;
			}

			var index = IndexUnderDragCursor;
			canInitiateDrag = index > -1;

			if (canInitiateDrag)
			{
				// Remember the location and index of the ListViewItem the user clicked on for later.
				ptMouseDown = Core.Helpers.MouseHelper.GetMousePosition(listView);
				indexToSelect = index;
			}
			else
			{
				ptMouseDown = new Point(-10000, -10000);
				indexToSelect = -1;
			}
		}

		#endregion // listView_PreviewMouseLeftButtonDown

		#region listView_PreviewMouseMove

		private void listView_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (!CanStartDragOperation)
				return;

			// Select the item the user clicked on.
			if (listView.SelectedIndex != indexToSelect)
				listView.SelectedIndex = indexToSelect;

			// If the item at the selected index is null, there's nothing
			// we can do, so just return;
			if (listView.SelectedItem == null)
				return;

			var itemToDrag = GetListViewItem(listView.SelectedIndex);
			if (itemToDrag == null)
				return;

			var adornerLayer = ShowDragAdornerResolved ? InitializeAdornerLayer(itemToDrag) : null;

			InitializeDragOperation(itemToDrag);
			PerformDragOperation();
			FinishDragOperation(itemToDrag, adornerLayer);
		}

		#endregion // listView_PreviewMouseMove

		#region listView_DragOver

		private void listView_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.Move;

			if (ShowDragAdornerResolved)
				UpdateDragAdornerLocation();

			// Update the item which is known to be currently under the drag cursor.
			var index = IndexUnderDragCursor;
			ItemUnderDragCursor = index < 0 ? null : ListView.Items[index] as TItemType;
		}

		#endregion // listView_DragOver

		#region listView_DragLeave

		private void listView_DragLeave(object sender, DragEventArgs e)
		{
			if (!IsMouseOver(listView))
			{
				if (ItemUnderDragCursor != null)
					ItemUnderDragCursor = null;

				if (dragAdorner != null)
					dragAdorner.Visibility = Visibility.Collapsed;
			}
		}

		#endregion // listView_DragLeave

		#region listView_DragEnter

		private void listView_DragEnter(object sender, DragEventArgs e)
		{
			if (dragAdorner != null && dragAdorner.Visibility != Visibility.Visible)
			{
				// Update the location of the adorner and then show it.				
				UpdateDragAdornerLocation();
				dragAdorner.Visibility = Visibility.Visible;
			}
		}

		#endregion // listView_DragEnter

		#region listView_Drop

		private void listView_Drop(object sender, DragEventArgs e)
		{
			if (ItemUnderDragCursor != null)
				ItemUnderDragCursor = null;

			e.Effects = DragDropEffects.None;

			if (!e.Data.GetDataPresent(typeof(TItemType)))
				return;

			// Get the data object which was dropped.
			var data = e.Data.GetData(typeof(TItemType)) as TItemType;
			if (data == null)
				return;

			// Get the ObservableCollection<ItemType> which contains the dropped data object.
			var itemsSource = listView.ItemsSource as ObservableCollection<TItemType>;
			if (itemsSource == null)
				throw new Exception(
					"A ListView managed by ListViewDragManager must have its ItemsSource set to an ObservableCollection<ItemType>.");

			var oldIndex = itemsSource.IndexOf(data);
			var newIndex = IndexUnderDragCursor;

			if (newIndex < 0)
			{
				// The drag started somewhere else, and our ListView is empty
				// so make the new item the first in the list.
				if (itemsSource.Count == 0)
					newIndex = 0;

				// The drag started somewhere else, but our ListView has items
				// so make the new item the last in the list.
				else if (oldIndex < 0)
					newIndex = itemsSource.Count;

				// The user is trying to drop an item from our ListView into
				// our ListView, but the mouse is not over an item, so don't
				// let them drop it.
				else
					return;
			}

			// Dropping an item back onto itself is not considered an actual 'drop'.
			if (oldIndex == newIndex)
				return;

			if (ProcessDrop != null)
			{
				// Let the client code process the drop.
				var args = new ProcessDropEventArgs<TItemType>(itemsSource, data, oldIndex, newIndex, e.AllowedEffects);
				ProcessDrop(this, args);
				e.Effects = args.Effects;
			}
			else
			{
				// Move the dragged data object from it's original index to the
				// new index (according to where the mouse cursor is).  If it was
				// not previously in the ListBox, then insert the item.
				if (oldIndex > -1)
					itemsSource.Move(oldIndex, newIndex);
				else
					itemsSource.Insert(newIndex, data);

				// Set the Effects property so that the call to DoDragDrop will return 'Move'.
				e.Effects = DragDropEffects.Move;
			}
		}

		#endregion // listView_Drop

		#endregion // Event Handling Methods

		#region Private Helpers

		#region CanStartDragOperation

		private bool CanStartDragOperation
		{
			get
			{
				if (Mouse.LeftButton != MouseButtonState.Pressed)
					return false;

				if (!canInitiateDrag)
					return false;

				if (indexToSelect == -1)
					return false;

				if (!HasCursorLeftDragThreshold)
					return false;

				return true;
			}
		}

		#endregion // CanStartDragOperation

		#region FinishDragOperation

		private void FinishDragOperation(ListViewItem draggedItem, AdornerLayer adornerLayer)
		{
			// Let the ListViewItem know that it is not being dragged anymore.
			ListViewItemDragState.SetIsBeingDragged(draggedItem, false);

			IsDragInProgress = false;

			if (ItemUnderDragCursor != null)
				ItemUnderDragCursor = null;

			// Remove the drag adorner from the adorner layer.
			if (adornerLayer != null)
			{
				adornerLayer.Remove(dragAdorner);
				dragAdorner = null;
			}
		}

		#endregion // FinishDragOperation

		#region GetListViewItem

		private ListViewItem GetListViewItem(int index)
		{
			if (listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
				return null;

			return listView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
		}

		private ListViewItem GetListViewItem(TItemType dataItem)
		{
			if (listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
				return null;

			return listView.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
		}

		#endregion // GetListViewItem

		#region HasCursorLeftDragThreshold

		private bool HasCursorLeftDragThreshold
		{
			get
			{
				if (indexToSelect < 0)
					return false;

				var item = GetListViewItem(indexToSelect);
				var bounds = VisualTreeHelper.GetDescendantBounds(item);
				var ptInItem = listView.TranslatePoint(ptMouseDown, item);

				// In case the cursor is at the very top or bottom of the ListViewItem
				// we want to make the vertical threshold very small so that dragging
				// over an adjacent item does not select it.
				var topOffset = Math.Abs(ptInItem.Y);
				var btmOffset = Math.Abs(bounds.Height - ptInItem.Y);
				var vertOffset = Math.Min(topOffset, btmOffset);

				var width = SystemParameters.MinimumHorizontalDragDistance * 2;
				var height = Math.Min(SystemParameters.MinimumVerticalDragDistance, vertOffset) * 2;
				var szThreshold = new Size(width, height);

				var rect = new Rect(ptMouseDown, szThreshold);
				rect.Offset(szThreshold.Width / -2, szThreshold.Height / -2);
				var ptInListView = Core.Helpers.MouseHelper.GetMousePosition(listView);
				return !rect.Contains(ptInListView);
			}
		}

		#endregion // HasCursorLeftDragThreshold

		#region IndexUnderDragCursor

		/// <summary>
		///     Returns the index of the ListViewItem underneath the
		///     drag cursor, or -1 if the cursor is not over an item.
		/// </summary>
		private int IndexUnderDragCursor
		{
			get
			{
				var index = -1;
				for (var i = 0; i < listView.Items.Count; ++i)
				{
					var item = GetListViewItem(i);
					if (IsMouseOver(item))
					{
						index = i;
						break;
					}
				}

				return index;
			}
		}

		#endregion // IndexUnderDragCursor

		#region InitializeAdornerLayer

		private AdornerLayer InitializeAdornerLayer(ListViewItem itemToDrag)
		{
			// Create a brush which will paint the ListViewItem onto
			// a visual in the adorner layer.
			var brush = new VisualBrush(itemToDrag);

			// Create an element which displays the source item while it is dragged.
			dragAdorner = new DragAdorner(listView, itemToDrag.RenderSize, brush);

			// Set the drag adorner's opacity.		
			dragAdorner.Opacity = DragAdornerOpacity;

			var layer = AdornerLayer.GetAdornerLayer(listView);
			layer.Add(dragAdorner);

			// Save the location of the cursor when the left mouse button was pressed.
			ptMouseDown = Core.Helpers.MouseHelper.GetMousePosition(listView);

			return layer;
		}

		#endregion // InitializeAdornerLayer

		#region InitializeDragOperation

		private void InitializeDragOperation(ListViewItem itemToDrag)
		{
			// Set some flags used during the drag operation.
			IsDragInProgress = true;
			canInitiateDrag = false;

			// Let the ListViewItem know that it is being dragged.
			ListViewItemDragState.SetIsBeingDragged(itemToDrag, true);
		}

		#endregion // InitializeDragOperation

		#region IsMouseOver

		private bool IsMouseOver(Visual target)
		{
			// We need to use MouseUtilities to figure out the cursor
			// coordinates because, during a drag-drop operation, the WPF
			// mechanisms for getting the coordinates behave strangely.

			var bounds = VisualTreeHelper.GetDescendantBounds(target);
			var mousePos = Core.Helpers.MouseHelper.GetMousePosition(target);
			return bounds.Contains(mousePos);
		}

		#endregion // IsMouseOver

		#region IsMouseOverScrollbar

		/// <summary>
		///     Returns true if the mouse cursor is over a scrollbar in the ListView.
		/// </summary>
		private bool IsMouseOverScrollbar
		{
			get
			{
				var ptMouse = Core.Helpers.MouseHelper.GetMousePosition(listView);
				var res = VisualTreeHelper.HitTest(listView, ptMouse);
				if (res == null)
					return false;

				var depObj = res.VisualHit;
				while (depObj != null)
				{
					if (depObj is ScrollBar)
						return true;

					// VisualTreeHelper works with objects of type Visual or Visual3D.
					// If the current object is not derived from Visual or Visual3D,
					// then use the LogicalTreeHelper to find the parent element.
					if (depObj is Visual || depObj is Visual3D)
						depObj = VisualTreeHelper.GetParent(depObj);
					else
						depObj = LogicalTreeHelper.GetParent(depObj);
				}

				return false;
			}
		}

		#endregion // IsMouseOverScrollbar

		#region ItemUnderDragCursor

		private TItemType ItemUnderDragCursor
		{
			get => itemUnderDragCursor;
			set
			{
				if (itemUnderDragCursor == value)
					return;

				// The first pass handles the previous item under the cursor.
				// The second pass handles the new one.
				for (var i = 0; i < 2; ++i)
				{
					if (i == 1)
						itemUnderDragCursor = value;

					if (itemUnderDragCursor != null)
					{
						var listViewItem = GetListViewItem(itemUnderDragCursor);
						if (listViewItem != null)
							ListViewItemDragState.SetIsUnderDragCursor(listViewItem, i == 1);
					}
				}
			}
		}

		#endregion // ItemUnderDragCursor

		#region PerformDragOperation

		private void PerformDragOperation()
		{
			var selectedItem = listView.SelectedItem as TItemType;
			var allowedEffects = DragDropEffects.Move | DragDropEffects.Move | DragDropEffects.Link;
			if (DragDrop.DoDragDrop(listView, selectedItem, allowedEffects) != DragDropEffects.None)
				// The item was dropped into a new location,
				// so make it the new selected item.
				listView.SelectedItem = selectedItem;
		}

		#endregion // PerformDragOperation

		#region ShowDragAdornerResolved

		private bool ShowDragAdornerResolved => ShowDragAdorner && DragAdornerOpacity > 0.0;

		#endregion // ShowDragAdornerResolved

		#region UpdateDragAdornerLocation

		private void UpdateDragAdornerLocation()
		{
			if (dragAdorner != null)
			{
				var ptCursor = Core.Helpers.MouseHelper.GetMousePosition(ListView);

				var left = ptCursor.X - ptMouseDown.X;

				// 4/13/2007 - Made the top offset relative to the item being dragged.
				var itemBeingDragged = GetListViewItem(indexToSelect);
				var itemLoc = itemBeingDragged.TranslatePoint(new Point(0, 0), ListView);
				var top = itemLoc.Y + ptCursor.Y - ptMouseDown.Y;

				dragAdorner.SetOffsets(left, top);
			}
		}

		#endregion // UpdateDragAdornerLocation

		#endregion // Private Helpers
	}

	#endregion // ListViewDragDropManager

	#region ListViewItemDragState

	/// <summary>
	///     Exposes attached properties used in conjunction with the ListViewDragDropManager class.
	///     Those properties can be used to allow triggers to modify the appearance of ListViewItems
	///     in a ListView during a drag-drop operation.
	/// </summary>
	public static class ListViewItemDragState
	{
		#region IsBeingDragged

		/// <summary>
		///     Identifies the ListViewItemDragState's IsBeingDragged attached property.
		///     This field is read-only.
		/// </summary>
		public static readonly DependencyProperty IsBeingDraggedProperty =
			DependencyProperty.RegisterAttached(
				"IsBeingDragged",
				typeof(bool),
				typeof(ListViewItemDragState),
				new UIPropertyMetadata(false));

		/// <summary>
		///     Returns true if the specified ListViewItem is being dragged, else false.
		/// </summary>
		/// <param name="item">The ListViewItem to check.</param>
		public static bool GetIsBeingDragged(ListViewItem item)
		{
			return (bool)item.GetValue(IsBeingDraggedProperty);
		}

		/// <summary>
		///     Sets the IsBeingDragged attached property for the specified ListViewItem.
		/// </summary>
		/// <param name="item">The ListViewItem to set the property on.</param>
		/// <param name="value">Pass true if the element is being dragged, else false.</param>
		internal static void SetIsBeingDragged(ListViewItem item, bool value)
		{
			item.SetValue(IsBeingDraggedProperty, value);
		}

		#endregion // IsBeingDragged

		#region IsUnderDragCursor

		/// <summary>
		///     Identifies the ListViewItemDragState's IsUnderDragCursor attached property.
		///     This field is read-only.
		/// </summary>
		public static readonly DependencyProperty IsUnderDragCursorProperty =
			DependencyProperty.RegisterAttached(
				"IsUnderDragCursor",
				typeof(bool),
				typeof(ListViewItemDragState),
				new UIPropertyMetadata(false));

		/// <summary>
		///     Returns true if the specified ListViewItem is currently underneath the cursor
		///     during a drag-drop operation, else false.
		/// </summary>
		/// <param name="item">The ListViewItem to check.</param>
		public static bool GetIsUnderDragCursor(ListViewItem item)
		{
			return (bool)item.GetValue(IsUnderDragCursorProperty);
		}

		/// <summary>
		///     Sets the IsUnderDragCursor attached property for the specified ListViewItem.
		/// </summary>
		/// <param name="item">The ListViewItem to set the property on.</param>
		/// <param name="value">Pass true if the element is underneath the drag cursor, else false.</param>
		internal static void SetIsUnderDragCursor(ListViewItem item, bool value)
		{
			item.SetValue(IsUnderDragCursorProperty, value);
		}

		#endregion // IsUnderDragCursor
	}

	#endregion // ListViewItemDragState

	#region ProcessDropEventArgs

	/// <summary>
	///     Event arguments used by the ListViewDragDropManager.ProcessDrop event.
	/// </summary>
	/// <typeparam name="ItemType">The type of data object being dropped.</typeparam>
	public class ProcessDropEventArgs<ItemType> : EventArgs where ItemType : class
	{
		#region Constructor

		internal ProcessDropEventArgs(
			ObservableCollection<ItemType> itemsSource,
			ItemType dataItem,
			int oldIndex,
			int newIndex,
			DragDropEffects allowedEffects)
		{
			ItemsSource = itemsSource;
			DataItem = dataItem;
			OldIndex = oldIndex;
			NewIndex = newIndex;
			AllowedEffects = allowedEffects;
		}

		#endregion // Constructor

		#region Data

		#endregion // Data

		#region Public Properties

		/// <summary>
		///     The items source of the ListView where the drop occurred.
		/// </summary>
		public ObservableCollection<ItemType> ItemsSource { get; }

		/// <summary>
		///     The data object which was dropped.
		/// </summary>
		public ItemType DataItem { get; }

		/// <summary>
		///     The current index of the data item being dropped, in the ItemsSource collection.
		/// </summary>
		public int OldIndex { get; }

		/// <summary>
		///     The target index of the data item being dropped, in the ItemsSource collection.
		/// </summary>
		public int NewIndex { get; }

		/// <summary>
		///     The drag drop effects allowed to be performed.
		/// </summary>
		public DragDropEffects AllowedEffects { get; } = DragDropEffects.None;

		/// <summary>
		///     The drag drop effect(s) performed on the dropped item.
		/// </summary>
		public DragDropEffects Effects { get; set; } = DragDropEffects.None;

		#endregion // Public Properties
	}

	#endregion // ProcessDropEventArgs
}
