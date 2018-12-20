using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Data;
using System.Linq;
using SharedLibrary;
using System.Windows.Media;
using System.Windows.Documents;

namespace DatabaseEditorV3
{
    /// <summary>
    /// I was not happy with a lot of the DataGrid behaviors, so I fixed them.
    /// Users can drag down values from a cell.
    /// Allows copy and paste
    /// Better navigation key behaviors
    /// Assigns column headers to DataColumn.Caption when autogenerating columns
    /// Single-click sorting
    /// Binding to BetterTable class
    /// Undo/Redo inserts, updates, and deletes.
    /// Data in ForeignTables linked to the items containing SelectedCells.
    /// Option to hide seconds in time values, and can enter time as 4-digit value.
    /// </summary>
    public class BetterDataGrid : DataGrid
    {
        /// <summary>
        /// Formats the time without seconds and allows the user to enter time values as 4-digits without a colon character.
        /// </summary>
        public static readonly DependencyProperty FormatShortTimeProperty = DependencyProperty.Register("FormatShortTime", typeof(bool), typeof(BetterDataGrid), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(UpdateTimeFormat)));
        private static void UpdateTimeFormat(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BetterDataTable source = (sender as BetterDataGrid).ItemsSource as BetterDataTable;
            if (source == null)
                return;
            foreach (DataGridColumn column in (sender as BetterDataGrid).Columns)
            {
                if (column is DataGridBoundColumn c)
                {
                    string s = (c.Binding as Binding).Path.Path;
                    string t = source.Columns.Where(x => x.ColumnName == s).Select(x => x.DataType).FirstOrDefault();
                    if (t == "time")
                    {
                        Binding b = new Binding(s);
                        if ((bool)e.NewValue)
                        {
                            b.Converter = new LazyTimeConverter();
                            b.StringFormat = @"hh\:mm";
                        }
                        c.Binding = b;
                    }
                }
            }
        }
        public bool FormatShortTime
        {
            get { return (bool)GetValue(FormatShortTimeProperty); }
            set
            {
                SetValue(FormatShortTimeProperty, value);
            }
        }

        public BetterDataGrid() : base()
        {
            AutoGenerateColumns = true;
            SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, CommandCopy_Executed));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, CommandPaste_Executed, CommandPaste_CanExecute));
            timer.Tick += Timer_Tick;
        }

        #region Command behaviors

        /// <summary>
        /// Copy the cell values as tab-delimited, which is what Excel does.
        /// </summary>
        private void CommandCopy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<string> result = new List<string>();
            // Get all distinct selected columns in display order.
            DataGridColumn[] columns = 
                SelectedCells
                .GroupBy(x => x.Column)
                .Select(x => x.First().Column)
                .OrderBy(x => x.DisplayIndex)
                .ToArray();
            // Get the selected items in the visual order.
            object[] items = 
                SelectedCells
                .GroupBy(x => x.Item)
                .Select(x => x.First().Item)
                .OrderBy(x => Items.IndexOf(x))
                .ToArray();
            // Go through each item as a line and each column (from the full range of selected cell columns). If an item column is not selected, it is a null to align what is selected.
            if (ItemsSource is BetterDataTable source)
            {
                foreach (DataRowItem row in items)
                {
                    List<object> l = new List<object>();
                    foreach (DataGridColumn c in columns)
                    {
                        if (SelectedCells.Contains(new DataGridCellInfo(row, c)))
                            l.Add(row[c.SortMemberPath]);
                        else
                            l.Add(string.Empty);
                    }
                    result.Add(string.Join("\t", l.ToArray()));
                }
            }
            Clipboard.SetText(string.Join(Environment.NewLine, result.ToArray()));
        }

        /// <summary>
        /// Can paste when there is text in the Clipboard.
        /// </summary>
        private void CommandPaste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText();
        }

        /// <summary>
        /// Paste has to have cell selection of the same dimensions, or in a new row.
        /// </summary>
        private void CommandPaste_Executed(object sender, ExecutedRoutedEventArgs e)
        {            
            // Convert Clipboard text to array
            string[] lines = Clipboard.GetText().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            List<string[]> values = new List<string[]>(lines.Length);
            foreach (string s in lines)
                values.Add(s.Split(new char[] { '\t' }));
            // Convert SelectedCells to array
            Dictionary<object, DataGridCellInfo[]> rows = SelectedCells.OrderBy(x => x.Column.DisplayIndex).GroupBy(x => x.Item).ToDictionary(x => x.Key, x => x.ToArray());
            bool hit = false;
            // Selected cells are all inside the new row ...
            if (rows.Count() == 1 && !(rows.First().Key is DataRowItem))
            {
                // Cannot reference columns by index because it may differ from display index. So, LINQ it up.
                DataGridColumn[] orderedColumns =
                    Columns
                    .Where(x => x.DisplayIndex >= rows.First().Value[0].Column.DisplayIndex)
                    .OrderBy(x => x.DisplayIndex)
                    .ToArray();
                foreach (string[] arr in values)
                {
                    DataRowItem item = new DataRowItem();
                    (ItemsSource as BetterDataTable).Add(item);
                    List<DataGridCellInfo> l = new List<DataGridCellInfo>();
                    for (int i = 0; i < arr.Length; i++)
                        item[orderedColumns[i].SortMemberPath] = arr[i];
                    item.Commit();
                }
                return;
            }
            // ... or selected cells are in the same dimensions as the pasted tab-delimited string.
            else if (rows.Count() == values.Count)
            {
                int i = 0;
                foreach (KeyValuePair<object, DataGridCellInfo[]> pair in rows)
                {
                    if (pair.Value.Length == values[i].Length)
                    {
                        for (int j = 0; j < pair.Value.Length; j++)
                            (rows.ElementAt(i).Key as DataRowItem)[pair.Value[j].Column.SortMemberPath] = values[i][j];
                    }
                    // wrong dimension
                    else
                    {   
                        hit = true;
                        break;
                    }
                    i++;
                }
            }
            else
            {
                // cannot paste
                hit = true;
            }
            // Consolidate mismatch dialog.
            if (hit)
            {
                MessageBox.Show("You must select a range of cells that matches the pasted cell range.");
            }
            // No dialog to commit because the user already has to select the right cell dimensions, so he/she is already actively aware.
            else
            {
                foreach (DataGridCellInfo info in SelectedCells)
                {
                    (info.Item as DataRowItem).Commit();
                }
                return;
            }
            // Clean up items if not returned by now.
            foreach (DataGridCellInfo info in SelectedCells)
            {
                (info.Item as DataRowItem).MakeClean();
            }
        }
        
        #endregion

        /// <summary>
        /// Cannot get the previous selected cells in OnSelectedCellsChanged(), so keep track of them in a field.
        /// </summary>
        private DataRowItem[] previousItems = new DataRowItem[0];

        /// <summary>
        /// Display the referenced tables rows linked to the selected cell items.
        /// Skip when user is dragging down values, otherwise the drag-copy feature is slow, and filling the foreign table asynchronously is dangerous.
        /// </summary>
        protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
        {
            base.OnSelectedCellsChanged(e);
            if (!isDragging && ItemsSource is BetterDataTable source && source.ForeignTables.Count > 0)
            {
                // For speed, do not continue if the item collection containing the selected cells has not changed.
                DataRowItem[] items = SelectedCells.GroupBy(x => x.Item).Select(x => x.Key as DataRowItem).ToArray();
                if (!items.SequenceEqual(previousItems))
                    source.FillForeignTables(items);
                previousItems = items;
            }
        }

        /// <summary>
        /// Simplifying and extending UX sorting. Single-click add sort columns, clear sort after click on descending, and sort order in order of last clicked.
        /// </summary>
        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            DataGridColumn column = eventArgs.Column;
            // Create a new sort based on the last sort.
            // eventArgs.SortDirection is the last sort direction (before base.OnSorting()). If it is null, make it ascending. If ascending, descending. If descending, remove.
            SortDescription description = Items.SortDescriptions.Where(x => x.PropertyName == column.SortMemberPath).FirstOrDefault();
            switch (column.SortDirection)
            {
                case null:
                    Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));
                    column.SortDirection = ListSortDirection.Ascending;
                    break;
                case ListSortDirection.Ascending:
                    // Cannot change a sort direction- it is sealed.
                    int index = Items.SortDescriptions.IndexOf(description);
                    Items.SortDescriptions.RemoveAt(index);
                    Items.SortDescriptions.Insert(index, new SortDescription(column.SortMemberPath, ListSortDirection.Descending));
                    column.SortDirection = ListSortDirection.Descending;
                    break;
                case ListSortDirection.Descending:
                    Items.SortDescriptions.Remove(description);
                    column.SortDirection = null;
                    break;
            }
            Items.Refresh();
            // Have to manually add current sorts to each column. Otherwise, they all clear.
            foreach (DataGridColumn c in Columns)
            {
                foreach (SortDescription d in Items.SortDescriptions)
                {
                    if (d.PropertyName == c.SortMemberPath)
                    {
                        c.SortDirection = d.Direction;
                        break;
                    }
                }
            }
            // Do not run base.OnSorting(eventArgs);
        }

        /// <summary>
        /// Get confirmation that the user wants to delete the items. This triggers once for multiple rows.
        /// </summary>
        protected override void OnExecutedDelete(ExecutedRoutedEventArgs e)
        {
            var result = MessageBox.Show("Warning: You are about to delete this row. Are you sure you want to continue?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
                base.OnExecutedDelete(e);
        }

        /// <summary>
        /// Commit the row to the database here. Also clears the DataGrid sorting so that the active row does not jump around. The un-sorted ObservableCollection is now in the last sort order.
        /// Override this rather than OnExecutedCommitEdit() because this is cancellable and only triggers once for a row.
        /// </summary>
        protected override void OnRowEditEnding(DataGridRowEditEndingEventArgs e)
        {
            (ItemsSource as BetterDataTable).Sort(Items.SortDescriptions);
            if (e.Row.Item is DataRowItem item)
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    if (!item.Commit())
                    {
                        e.Cancel = true;
                        item.HasError = true;
                    }
                }
                else
                {
                    item.MakeClean();
                }
            }
            base.OnRowEditEnding(e);
            Items.SortDescriptions.Clear();
            foreach (DataGridColumn column in Columns)
            {
                column.SortDirection = null;
            }
        }

        /// <summary>
        /// Remove the dirty cell value from the DataRowItem.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnCellEditEnding(DataGridCellEditEndingEventArgs e)
        {
            // It is easier to set all column UpdateSourceTrigger to PropertyChanged and then remove the DirtyValue here, than set them to Explicit and then update the binding source here.
            if (e.EditAction == DataGridEditAction.Cancel && ItemsSource is BetterDataTable)
            {
                DataRowItem item = e.Row.Item as DataRowItem;
                item.DirtyValues.Remove(((e.Column as DataGridBoundColumn).Binding as Binding).Path.Path);
            }
            base.OnCellEditEnding(e);
        }
        
        /// <summary>
        /// Binding to the BetterTable, an ObservableCollection, does not trigger AutoGeneratingColumn. Have to add columns manually here.
        /// Assigns Converters and StringFormats for bool, date, and time fields. Also sets Header and ToolTip to ColumnInfo Property values.
        /// </summary>
        protected override void OnAutoGeneratedColumns(EventArgs e)
        {
            BetterDataTable source = ItemsSource as BetterDataTable;
            if (ItemsSource is ListCollectionView)
                source = (ItemsSource as ListCollectionView).SourceCollection as BetterDataTable;
            if (source != null)
            {
                Columns.Clear();
                foreach (ColumnInfo info in source.Columns)
                {
                    // Underscores in a Header just underlines the next letter and creates a binding with that letter's corresponding key!
                    string head = info.DisplayName.Replace("_", "__");
                    DataGridTextColumn column = new DataGridTextColumn() { Header = head, IsReadOnly = info.IsIdentity };
                    Binding b = new Binding(info.ColumnName);
                    // DataTypes are string names of SQL Server datatypes.
                    switch (info.DataType)
                    {
                        case "date":
                            b.StringFormat = "MM/dd/yyyy";
                            break;
                        case "time":
                            if (FormatShortTime)
                                b.Converter = new LazyTimeConverter();
                            b.StringFormat = @"hh\:mm";
                            break;
                        case "datetime":
                            b.StringFormat = @"MM/dd/yyyy hh\:mm\:ss";
                            break;
                        case "bit":
                            BoolConverter c = new BoolConverter();
                            b.Converter = c;
                            break;
                    }
                    // By default the bindings only update when row focus is lost after DataGrid.OnRowEditEnding, and OnRowEditEnding only has access to the old data. Change updates to PropertyChanged and clear those new values in CellEditEnding when cancelled.
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    b.TargetNullValue = "";
                    b.NotifyOnSourceUpdated = false;
                    column.Binding = b;
                    column.SortMemberPath = info.ColumnName;
                    // Tooltip is a little convoluted.
                    if (column.HeaderStyle == null)
                        column.HeaderStyle = new Style();
                    column.HeaderStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, info.Description));
                    Columns.Add(column);
                }
            }
            base.OnAutoGeneratedColumns(e);
        }
                
        /// <summary>
        /// Asigns Converters and StringFormats for bool, date, and time fields; sets Header to DataColumn.Caption as it should out of the box but doesn't.
        /// </summary>
        protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridBoundColumn column = e.Column as DataGridBoundColumn;
            DataColumn dc = null;
            if (ItemsSource is DataTable)
            {
                dc = (ItemsSource as DataTable).Columns[e.PropertyName];
            }
            else if (ItemsSource is DataView)
            {
                dc = (ItemsSource as DataView).Table.Columns[e.PropertyName];
            }
            else
            {
                base.OnAutoGeneratingColumn(e);
                return;
            }
            column.Header = dc.Caption;
            column.Binding.TargetNullValue = string.Empty;
            if (e.PropertyType == typeof(TimeSpan) && FormatShortTime)
            {
                (column.Binding as Binding).Converter = new LazyTimeConverter();
                (column.Binding as Binding).StringFormat = @"hh\:mm";
            }
            else if (e.PropertyType == typeof(bool))
            {
                DataGridTextColumn textColumn = new DataGridTextColumn() { Header = column.Header };
                Binding b = new Binding(e.PropertyName);
                BoolConverter converter = new BoolConverter();
                b.Converter = converter;
                textColumn.Binding = b;
                e.Column = textColumn;
            }
            base.OnAutoGeneratingColumn(e);
        }

        /// <summary>
        /// Row numbers in the row header.
        /// </summary>
        protected override void OnLoadingRow(DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            base.OnLoadingRow(e);
        }

        //// User can copy the values from selected cells to other rows.
        //// Can only drag the current cell if it is selected. This creates a simpler UI than giving all selected cells Thumbs.
        //// Move the scroll when the mouse is dragged to the edge or beyond the DataGrid.
        //// Would like to override SelectionChanged so the behavior inherits from native selection behaviors. However, cannot figure out how to drag a cell that is already selected without changing all this behavior anyways.
        #region Drag and drop behavior

        DataGridCellDragAdorner adorner;
            
        /// <summary>
        /// Is the user trying to drag a collection of columns to copy their values into other rows?
        /// </summary>
        public bool isDragging
        {
            get { return _isDragging; }
            set
            {
                if (IsReadOnly)
                    _isDragging = false;
                else
                {
                    _isDragging = value;
                    if (value)
                        Mouse.Capture(this, CaptureMode.SubTree);
                    else
                    {
                        timer.Stop();
                        draggedCells.Clear();
                        ReleaseMouseCapture();
                    }
                }
            }
        }
        bool _isDragging = false;

        /// <summary>
        /// Organize the selected cells to make drag-copy faster and easier.
        /// </summary>
        public readonly Dictionary<object, List<DataGridColumn>> draggedCells = new Dictionary<object, List<DataGridColumn>>();

        /// <summary>
        /// Timer for scrolling through the DataGrid when the mouse is dragged outside of it.
        /// Good intervals are 5e6 - 1e4
        /// </summary>
        public readonly System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer() { Interval = new TimeSpan((int)5e6) };

        /// <summary>
        /// Extend the selection and copy values to the rows under the dragged area starting with the original selection.
        /// If the mouse is outside the DataGrid, start the timer to add a row for each tick. Because the DataGrid captures the mouse while dragging, MouseLeave() never triggers, so have to handle that behavior here.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                DataGridRow row = (e.OriginalSource as DependencyObject).GetParentOfType<DataGridRow>();
                if (row != null)
                {
                    int rowIndex = row.GetIndex();
                    DragCopy(rowIndex);
                    return;
                }
                double positionY = e.GetPosition(this).Y;
                if (positionY < 0 || positionY > ActualHeight)
                {
                    // Faster timer with distance from DataGrid.
                    // Whenever Interval is set, timer re-starts.
                    positionY = positionY > ActualHeight ? positionY - ActualHeight : Math.Abs(positionY);
                    double interval =
                        positionY < 50 ? 5e6 :
                        positionY < 100 ? 1e6 :
                        positionY < 150 ? 1e5 :
                        1e4;
                    timer.Interval = new TimeSpan((int)interval);
                    // Start the timer.
                    if (!timer.IsEnabled)
                        timer.Start();
                }
                else
                    timer.Stop();
            }
        }

        /// <summary>
        /// When dragging cell values, scroll up or down when the mouse is outside the DataGrid at a time interval.
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            IEnumerable<int> selection = SelectedCells.Select(x => Items.IndexOf(x.Item));
            int min = selection.Min();
            int max = selection.Max();
            int rowIndex = max;
            int minStart = Items.IndexOf(draggedCells.First().Key);
            int maxStart = Items.IndexOf(draggedCells.Last().Key);
            double positionY = Mouse.GetPosition(this).Y;
            if (positionY < 0)
            {
                if (max > maxStart)
                    rowIndex = max - 1;
                else
                    rowIndex = min - 1;
            }
            else if (positionY > ActualHeight)
            {
                if (min < minStart)
                    rowIndex = min + 1;
                else
                    rowIndex = max + 1;
            }
            else
                return;
            // At the first item.
            if (rowIndex < 0)
                return;
            DragCopy(rowIndex);
            ScrollIntoView(Items.GetItemAt(rowIndex));
        }

        /// <summary>
        /// If cells are being drag-copied, ask for user confirmation and then commit those values.
        /// Database errors may create a bad user experience here.
        /// </summary>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                MessageBoxResult result = MessageBox.Show("You are about to over-write the existing data. Are you sure you want to continue?", "Confirmation", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    foreach (DataGridCellInfo info in SelectedCells)
                    {
                        DataRowItem item = info.Item as DataRowItem;
                        if (item.DirtyValues.Count > 0)
                        {
                            bool committed = item.Commit();
                            if (!committed)
                            {
                                item.MakeClean();
                            }
                        }
                    }
                }
                else
                {
                    foreach (DataGridCellInfo info in SelectedCells)
                    {
                        DataRowItem item = info.Item as DataRowItem;
                        if (item.DirtyValues.Count > 0)
                            item.MakeClean();
                    }
                }
            }
            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        /// Copy values to the row columns under the dragged area. Add new row items if necessary.
        /// </summary>
        private void DragCopy(int rowIndex)
        {
            if (draggedCells.Count == 0 || IsReadOnly)
                return;
            // Does not re-sort on change- that is a problem with CellEditEnding, so don't have to run BetterDataTable.Sort().
            // Clean up rowIndex and find the range of affected rows.
            // DataGrid.Items with CanUserAddRows == true includes the new row line, where Item = null.
            // The last row in the DataGrid. This is the new row with Item = null when CanUserAddRows == true. Otherwise, it is the last row and should not be null.
            if (rowIndex >= (ItemsSource as BetterDataTable).Count)
                rowIndex = (ItemsSource as BetterDataTable).Count;
            if (rowIndex < 0)
                rowIndex = 0;
            int first = Items.IndexOf(draggedCells.Keys.First());
            int last = Items.IndexOf(draggedCells.Keys.Last());
            int min = Math.Min(first, rowIndex);
            int max = Math.Max(last, rowIndex);
            // Revert the values and unselect from the rows outside of the new dragged cells range.
            for (int i = SelectedCells.Count - 1; i >= 0; i--)
            {
                if (i >= SelectedCells.Count)
                    continue;
                DataGridCellInfo info = SelectedCells[i];
                int index = Items.IndexOf(info.Item);
                if (index > -1 && (index < first || index > last))
                {
                    SelectedCells.Remove(SelectedCells[i]);
                    if (min > index || index > max)
                    {
                        DataRowItem item = info.Item as DataRowItem;
                        if (item.IsNewRow)
                            (ItemsSource as BetterDataTable).Remove(item);
                        else
                            (info.Item as DataRowItem).MakeClean();
                    }
                }
            }
            // Select the cells and change their values within the range.
            for (int i = min; i <= max; i++)
            {
                if (i >= first && i <= last)
                    continue;
                DataRowItem item = new DataRowItem();
                if (i >= (ItemsSource as BetterDataTable).Count)
                    (ItemsSource as BetterDataTable).Add(item);
                else
                    item = Items[i] as DataRowItem; // The Items collection order changes when the user sorts the DataGrid.
                int index =
                    i > last ? (i - last - 1) % (last - first + 1)
                    : (last - first) - (first - i - 1) % (last - first + 1);
                DataRowItem source = draggedCells.Keys.ElementAt(index) as DataRowItem;
                foreach (DataGridColumn column in draggedCells.First().Value)
                {
                    item[column.SortMemberPath] = source[column.SortMemberPath];
                    var info = new DataGridCellInfo(item, column);
                    if (!SelectedCells.Contains(info))
                        SelectedCells.Add(info);
                }
            }
        }

        #endregion

        // Enter keys moves to next row in the field that was last clicked or arrow keyed to.
        #region Key behavior

        /// <summary>
        /// The current column of the next focused row.
        /// </summary>
        DataGridColumn firstColumn;

        /// <summary>
        /// Sets to true when user hits the TAB key so that firstColumn is not set. Othwise, firstColumn is set on currentcellchanged.
        /// </summary>
        bool usedTabKey = false;

        /// <summary>
        /// Sets true when user hits the ENTER key. Then let the DataGrid do its thing, and then move the focus to firstColumn only if ENTER key was used to move focus.
        /// </summary>
        bool usedEnterKey = false;

        /// <summary>
        /// Keep track if ENTER or TAB key was used to navigate, and then handle the behavior changed in OnCurrentCellChanged,
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
                usedTabKey = true;
            else if (e.Key == Key.Enter)
                usedEnterKey = true;
            base.OnPreviewKeyDown(e);
        }

        #endregion

        /// <summary>
        /// If user used the ENTER to change the current cell, move focus to the row's firstColumn. If used anything except TAB, set firstColumn.
        /// Adorn the current cell to allow dragging cells.
        /// </summary>
        protected override void OnCurrentCellChanged(EventArgs e)
        {
            if (usedEnterKey)
                CurrentCell = new DataGridCellInfo(CurrentItem, firstColumn);
            else if (!usedTabKey)
                firstColumn = CurrentColumn;
            usedTabKey = false;
            usedEnterKey = false;
            base.OnCurrentCellChanged(e);
            // Add an adorner for dragging values.
            if (adorner != null)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(adorner.AdornedElement);
                if (layer != null)
                {
                    layer.Remove(adorner);
                }
            }
            if (CurrentCell != null)
            {
                object item = CurrentCell.Item;
                DataGridColumn column = CurrentCell.Column;
                if (item != null && column != null)
                {
                    DataGridCell cell = (DataGridCell)column.GetCellContent(item).Parent;
                    if (cell != null)
                    {
                        adorner = new DataGridCellDragAdorner(cell, this);
                        AdornerLayer al = AdornerLayer.GetAdornerLayer(cell);
                        al.Add(adorner);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Use an Adorner for the control to click + drag to copy values down rows. This sets IsDragging = true, and handle the rest of the behaviors in BetterDataGrid.
    /// </summary>
    public class DataGridCellDragAdorner : Adorner
    {
        private readonly DataGridCell cell;
        private readonly BetterDataGrid dataGrid;
        private Rect box = new Rect(0, 0, 5, 5);
        private readonly Brush brush = new SolidColorBrush(Colors.Blue) { Opacity = 0.2 };
        private readonly Pen pen = new Pen(new SolidColorBrush(Colors.Black), 1);
        
        public DataGridCellDragAdorner(UIElement adornedElement, BetterDataGrid dataGrid) : base(adornedElement)
        {
            cell = (DataGridCell)adornedElement;
            this.dataGrid = dataGrid;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(brush, pen, box);
            base.OnRender(drawingContext);
        }

        /// <summary>
        /// Move with resizing.
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size result = base.MeasureOverride(constraint);
            box.Location = new Point(cell.ActualWidth - box.Width, cell.ActualHeight - box.Height);
            return result;
        }

        /// <summary>
        /// If the user clicks on the Thumb inside a selected cell, handle the MouseDown and keep track of where that cell is dragged and dropped to copy the original cell's values to the newly selected row cells.
        /// Have to do this in the PreviewMouseDown event rather than MouseDown to catch it in the DataGrid rather than in the original source.
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!dataGrid.IsReadOnly)
            {
                // Check that every row with a selected cell have the same fields selected. Also organizes SelectedCells into draggedCells as Dictionary<Item, List<Columns>>.
                Dictionary<object, List<DataGridColumn>> list = new Dictionary<object, List<DataGridColumn>>();
                foreach (DataGridCellInfo selected in dataGrid.SelectedCells)
                {
                    if (!list.ContainsKey(selected.Item))
                    {
                        list.Add(selected.Item, new List<DataGridColumn>());
                    }
                    list[selected.Item].Add(selected.Column);
                }
                bool hit = false;
                foreach (KeyValuePair<object, List<DataGridColumn>> pair0 in list)
                {
                    foreach (KeyValuePair<object, List<DataGridColumn>> pair1 in list)
                    {
                        if (pair0.Key == pair1.Key)
                            break;
                        // Have to check count first, because .All() would return true if pair1.Value contains pair0.Values and then some.
                        if (pair0.Value.Count != pair1.Value.Count || !pair0.Value.All(pair1.Value.Contains))
                        {
                            hit = true;
                            break;
                        }
                    }
                }
                // Allow dragging when the selected cells are reasonable.
                if (!hit)
                {
                    // order the dictionary for later.
                    object[] l = list.Keys.OrderBy(x => dataGrid.Items.IndexOf(x)).ToArray();
                    // One more criteria for dragging- must be neighboring rows.
                    if (l.Length == dataGrid.Items.IndexOf(l.Last()) - dataGrid.Items.IndexOf(l.First()) + 1)
                    {
                        // Set draggedCells in the right order and isDragging = true.
                        foreach (object o in l)
                            dataGrid.draggedCells[o] = list[o];
                        dataGrid.isDragging = true;
                    }
                }
            }
            base.OnPreviewMouseLeftButtonDown(e);
        }

    }

}
