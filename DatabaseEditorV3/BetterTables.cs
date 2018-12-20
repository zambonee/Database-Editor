using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace DatabaseEditorV3
{
    /// <summary>
    /// An ObservableCollection of DynamicObjects that mimics a DataTable for better WPF bindings.
    /// </summary>
    public class BetterDataTable : ObservableCollection<DataRowItem>
    {
        public long ObjectID;

        public string TableName { get; } = string.Empty;

        public bool IsSQLView = false;

        public string DisplayName { get; } = string.Empty;

        public string Description { get; } = string.Empty;

        public List<ColumnInfo> Columns { get; } = new List<ColumnInfo>();

        /// <summary>
        /// Gets an array of columns that are the primary keys. If this is a SQL View, which cannot have a primary key, this gets all columns instead.
        /// </summary>
        public ColumnInfo[] PrimaryKeys
        {
            get
            {
                if (IsSQLView)
                    return Columns.ToArray();
                else
                    return Columns.Where(x => x.IsPrimaryKey).ToArray();
            }
        }

        /// <summary>
        /// Gets an array of columns that are identity columns. Will not allow users to set identity_insert on.
        /// </summary>
        public ColumnInfo[] Identities
        {
            get { return Columns.Where(x => x.IsIdentity).ToArray(); }
        }

        /// <summary>
        /// Database tables that are linked to this table by a Foreign Key.
        /// </summary>
        public ObservableCollection<BetterDataTable> ForeignTables
        {
            get { return _foreignTables; }
            set
            {
                _foreignTables = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ForeignTables"));
                foreach (BetterDataTable table in ForeignTables)
                {
                    table.DataChanged += OnDataChanged;
                }
            }
        }

        private ObservableCollection<BetterDataTable> _foreignTables = new ObservableCollection<BetterDataTable>();

        /// <summary>
        /// Only the foreign keys between this table and the table that has this in its ForeignTables. Key is this table's column and Value is the "parent" table column.
        /// </summary>
        public Dictionary<ColumnInfo, ColumnInfo> RelevantForeignKeys = new Dictionary<ColumnInfo, ColumnInfo>();

        /// <summary>
        /// Cannot add a single item to a CompositeCollection or CollectionContainer in XAML, so have to get the collection of this and all foreign tables in the code behind.
        /// </summary>
        public ObservableCollection<BetterDataTable> AllTableCollection
        {
            get
            {
                ObservableCollection<BetterDataTable> result = new ObservableCollection<BetterDataTable>();
                result.Add(this);
                foreach (BetterDataTable table in ForeignTables)
                {
                    result.Add(table);
                }
                return result;
            }
        }
        
        public SQLManager manager;

        /// <summary>
        /// When Updating, Inserting, or Deleting with a command, don't create a new command. 
        /// Using a global variable rather than a method parameter so I can override Insert and RemoveItem.
        /// </summary>
        public bool skipCommandBuilder = false;
        
        /// <summary>
        /// Constructor. Automatically fills all of the columns, tables with dependencies to it, and metadata.
        /// </summary>
        public BetterDataTable(SQLManager manager, long objectID, bool isMainTable = true)
        {
            this.manager = manager;
            ObjectID = objectID;
            IsSQLView = (bool)manager.GetDatabaseScalar(
                "SELECT CONVERT(bit, 0) FROM sys.tables WHERE object_id = {0} " +
                "UNION SELECT CONVERT(bit, 1) FROM sys.views WHERE object_id = {0}",
                objectID);
            TableName = (string)manager.GetDatabaseScalar(
                "SELECT CONCAT('[', schemas.name, '].[', tables.name, ']') " +
                "FROM sys.schemas " +
                "INNER JOIN (SELECT name, schema_id, object_id FROM sys.tables UNION SELECT name, schema_id, object_id FROM sys.views) AS tables " +
                "ON schemas.schema_id = tables.schema_id " +
                "WHERE object_id = {0}", 
                objectID);
            DisplayName = (string)manager.GetDatabaseScalar(
                "SELECT COALESCE(extended_properties.value, schemas.name + '.' + tables.name) " +
                "FROM (SELECT name, schema_id, object_id FROM sys.tables UNION SELECT name, schema_id, object_id FROM sys.views) AS tables " +
                "LEFT JOIN sys.extended_properties " +
                "    ON tables.object_id = extended_properties.major_id " +
                "    AND minor_id = 0  AND extended_properties.name = 'View_DisplayName' " +
                "LEFT JOIN sys.schemas " +
                "    ON tables.schema_id = schemas.schema_id " +
                "WHERE tables.object_id = {0} ",
                objectID);
            Description = (string)manager.GetDatabaseScalar(
                "SELECT COALESCE(extended_properties.value, '') " +
                "FROM(SELECT name, schema_id, object_id FROM sys.tables UNION SELECT name, schema_id, object_id FROM sys.views) AS tables " +
                "LEFT JOIN sys.extended_properties " +
                "    ON tables.object_id = extended_properties.major_id " +
                "    AND minor_id = 0  AND extended_properties.name = 'MS_Description' " +
                "LEFT JOIN sys.schemas " +
                "    ON tables.schema_id = schemas.schema_id " +
                "WHERE tables.object_id = {0} ",
                objectID);
            // Reads the following Extended Property names: DisplayName, DisplayOrder, Deprecated, MS_Description
            DataTable dtColumns = manager.GetDatabaseTable(
                @"SELECT 
	                Name = columns.name,
	                DisplayName = COALESCE(DisplayName.value, columns.name),
	                Description = COALESCE(Description.value, ''),
					PrimaryKey = CONVERT(bit, COALESCE(is_primary_key, 0)),
					IsIdentity = CONVERT(bit, COALESCE(is_identity, 0)),
                    DataType = types.name
                FROM sys.columns
				INNER JOIN sys.types
					ON columns.user_type_id = types.user_type_id
                LEFT JOIN sys.extended_properties AS DisplayName
	                ON columns.object_id = DisplayName.major_id
	                AND columns.column_id = DisplayName.minor_id
	                AND DisplayName.name = 'DisplayName'
                LEFT JOIN sys.extended_properties AS DisplayOrder
	                ON columns.object_id = DisplayOrder.major_id
	                AND columns.column_id = DisplayOrder.minor_id
	                AND DisplayOrder.name = 'DisplayOrder'
                LEFT JOIN sys.extended_properties AS Deprecated
	                ON columns.object_id = Deprecated.major_id
	                AND columns.column_id = Deprecated.minor_id
	                AND Deprecated.name = 'Deprecated'
                LEFT JOIN sys.extended_properties AS Description
	                ON columns.object_id = Description.major_id
	                AND columns.column_id = Description.minor_id
	                AND Description.name = 'MS_Description'
				LEFT JOIN sys.index_columns ON columns.object_id = index_columns.object_id AND columns.column_id = index_columns.column_id
				LEFT JOIN sys.indexes ON indexes.object_id = index_columns.object_id AND indexes.index_id = index_columns.index_id
                WHERE columns.object_id = {0} 
                    AND (COALESCE(Deprecated.value, 0) != 1 OR is_primary_key = 1)
                ORDER BY CASE WHEN ISNUMERIC(CONVERT(nvarchar, DisplayOrder.value)) = 1 THEN CONVERT(int, DisplayOrder.value) END, columns.column_id",
                objectID);
            // Selected in the order of DisplayOrder extended property, then column_id.
            foreach (DataRow row in dtColumns.Rows)
            {
                string name = row.Field<string>("Name");
                string display = row.Field<string>("DisplayName");
                string description = row.Field<string>("Description");
                bool isPK = row.Field<bool>("PrimaryKey");
                bool isID = row.Field<bool>("IsIdentity");
                string dataType = row.Field<string>("DataType");
                ColumnInfo info = new ColumnInfo(name, display, dataType, description, isPK, isID);
                Columns.Add(info);
            }
            // Only go down one level of dependencies. Otherwise, it will be too hard for the user to follow in the UI.
            if (isMainTable)
            {
                // Do not include dependencies of SQL views: they should handle dependencies, plus it would be difficult to show all of the rows for complex calculated fields.
                if (!IsSQLView)
                {
                    DataTable dtForeignTables = manager.GetDatabaseTable(
                    "SELECT tables.object_id FROM sys.tables " +
                    "   INNER JOIN sys.schemas " +
                    "       ON tables.schema_id = schemas.schema_id " +
                    "   INNER JOIN sys.foreign_keys " +
                    "       ON tables.object_id = foreign_keys.parent_object_id " +
                    "   WHERE referenced_object_id = {0} " +
                    "UNION SELECT referenced_id FROM sys.sql_expression_dependencies " +
                    "   INNER JOIN sys.views " +
                    "      ON sql_expression_dependencies.referencing_id = views.object_id " +
                    "   WHERE referencing_id = {0}",
                    objectID);
                    foreach (DataRow row in dtForeignTables.Rows)
                    {
                        BetterDataTable table = new BetterDataTable(manager, (int)row[0], false);
                        ForeignTables.Add(table);
                        DataTable fks = manager.GetDatabaseTable(
                            "SELECT " +
                            "   parent = parent.name, " +
                            "   reference = ref.name " +
                            "FROM sys.foreign_key_columns as fk " +
                            "INNER JOIN sys.columns AS parent " +
                            "ON fk.parent_object_id = parent.object_id AND fk.parent_column_id = parent.column_id " +
                            "INNER JOIN sys.columns AS ref " +
                            "ON fk.referenced_object_id = ref.object_id AND fk.referenced_column_id = ref.column_id " +
                            "WHERE fk.parent_object_id = {0} AND fk.referenced_object_id = {1}",
                            table.ObjectID, ObjectID);
                        foreach (DataRow r in fks.Rows)
                        {
                            ColumnInfo parent = table.Columns.Where(x => x.ColumnName == r.Field<string>("parent")).First();
                            ColumnInfo reference = Columns.Where(x => x.ColumnName == r.Field<string>("reference")).First();
                            table.RelevantForeignKeys[parent] = reference;
                        }
                    }
                }
                FillTable();
            }
            else
            {
                FillTable("1 = 2");
            }
        }

        /// <summary>
        /// Fill the "Table" with "Rows".
        /// </summary>
        public void FillTable()
        {
            FillTable("");
        }

        /// <summary>
        /// Fill the "Table" with "Rows". The WHERE clause is appended to the select statement that gets the data table.
        /// </summary>
        /// <param name="where">The WHERE clause, but don't include "WHERE" in the string.</param>
        /// <param name="args">Use args like in string.Format(). Matches values with parameters denoted in the query string with {#}.</param>
        public void FillTable(string where, params object[] args)
        {
            where = !string.IsNullOrWhiteSpace(where) ? "WHERE " + where : string.Empty;
            DataTable dt = manager.GetDatabaseTable($"SELECT {string.Join(",", Columns.Select(x => $"[{x.ColumnName}]"))} FROM {TableName} {where}", args);
            Clear();
            foreach (DataRow row in dt.Rows)
            {
                // Do NOT use table.Items.Add()! This causes binding issues.
                Add(new DataRowItem(this, row));
            }
        }

        /// <summary>
        /// Fill out the foreign tables with rows that are linked to the collection of DataRowItems.
        /// </summary>
        /// <param name="items">Parent table items used to select the foreign table rows based on the tables' foreign keys.</param>
        public void FillForeignTables(DataRowItem[] items)
        {
            if (ForeignTables.Count < 1)
                return;
            List<string> wheres = new List<string>();
            List<object> parameters = new List<object>();
            foreach (DataRowItem row in items)
            {
                List<string> l = new List<string>();
                foreach (ColumnInfo pk in PrimaryKeys)
                {
                    l.Add($"{TableName}.{pk.ColumnName} = {{{parameters.Count}}}");
                    parameters.Add(row[pk.ColumnName]);
                }
                wheres.Add($"({string.Join(" AND ", l.ToArray())})");
            }
            if (wheres.Count == 0)
                wheres.Add("1 = 2");
            foreach (BetterDataTable table in ForeignTables)
            {
                List<string> joins = new List<string>();
                foreach (KeyValuePair<ColumnInfo, ColumnInfo> pair in table.RelevantForeignKeys)
                {
                    joins.Add($"{TableName}.{pair.Value.ColumnName} = {table.TableName}.{pair.Key.ColumnName}");
                }
                string query = $"SELECT {table.TableName}.* FROM {TableName} INNER JOIN {table.TableName} ON {string.Join(" AND ", joins.ToArray())} WHERE {string.Join(" OR ", wheres.ToArray())}";
                DataTable dt = manager.GetDatabaseTable(query, parameters.ToArray());
                table.ClearItems();
                foreach (DataRow row in dt.Rows)
                {
                    // Do NOT use table.Items.Add()! This causes binding issues.
                    table.Add(new DataRowItem(table, row));
                }
            }
        }
        
        /// <summary>
        /// Set the new row's parent table to this.
        /// Does not fire on Items.Add().
        /// </summary>
        protected override void InsertItem(int index, DataRowItem item)
        {
            item.Table = this;
            base.InsertItem(index, item);
        }

        /// <summary>
        /// Delete a row from the database. This can be inside an override method just because the user is not submitting data- just have to remove this.
        /// </summary>
        /// <param name="index"></param>
        protected override void RemoveItem(int index)
        {
            DataRowItem row = Items[index];
            bool result = manager.ExecuteNonQuery(
                $"DELETE FROM { TableName } WHERE { string.Join(" AND ", PrimaryKeys.Select((x, i) => $"[{x.ColumnName}] = {{{ i }}}")) }",
                PrimaryKeys.Select(x => row[x.ColumnName]).ToArray());
            if (result)
            {
                if (!skipCommandBuilder)
                    OnDataChanged(new DeleteCommand(Items[index]));
                base.RemoveItem(index);
                // identity column values may have changed...
                row.RaisePropertyChanged("ID");
            }            
        }

        /// <summary>
        /// Insert a DataRowItem at index, and treat it as a new row (item.IsNewRow = true, item.DirtyColumns is all columns).
        /// </summary>
        public void InsertNewRow(int index, DataRowItem item)
        {
            item.IsNewRow = true;
            item.MakeDirty();
            InsertItem(index, item);
        }

        /// <summary>
        /// Adds a DataRowItem to the end of the Items collection and treat it as a new row.
        /// </summary>
        public void InsertNewRow(DataRowItem item)
        {
            item.Table = this;
            item.IsNewRow = true;
            item.MakeDirty();
            Add(item);
        }
        
        /// <summary>
        /// Sort the collection items outside of the DataGrid CollectionView so that they stay in that order after the DataGrid sorts are cleared.
        /// </summary>
        public void Sort(SortDescriptionCollection sort)
        {
            if (sort.Count == 0)
                return;
            List<DataRowItem> sorted = this.Select(x => x).ToList();
            // go backwards through the sorts.
            for (int i = sort.Count - 1; i >= 0; i--)
            {
                SortDescription s = sort[i];
                if (s.Direction == ListSortDirection.Ascending)
                    sorted = sorted.OrderBy(x => x[s.PropertyName]).ToList();
                else
                    sorted = sorted.OrderByDescending(x => x[s.PropertyName]).ToList();
            }
            for (int i = 0; i < sorted.Count; i++)
                Move(IndexOf(sorted[i]), i);
        }

        /// <summary>
        /// Bring the changes to the Model layer so that user has intuitive access to all changes he/she has made.
        /// </summary>
        public delegate void DataChangedEventHandler(Command command);

        /// <summary>
        /// The database data has been updated, inserted, or deleted with the command.
        /// </summary>
        public event DataChangedEventHandler DataChanged;
        public void OnDataChanged(Command command)
        {
            if (DataChanged != null)
                DataChanged(command);
        }
    }

    /// <summary>
    /// An object that acts like a DataRow.
    /// User changes are kept track in _dirtyValues and cannot be accessed until _columnValues is updated.
    /// When updating or inserting a DataRowItem into the database, use DirtyValues for column-values, and then call Fill() to clear DirtyValues and synchronize item properties with the database, including DB triggers.
    /// </summary>
    public class DataRowItem : DynamicObject, INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _columnValues = new Dictionary<string, object>();

        /// <summary>
        /// Keep track of what cells have been changed before saving to the database. Clears when calling Fill().
        /// </summary>
        public readonly Dictionary<string, object> DirtyValues = new Dictionary<string, object>();

        /// <summary>
        /// When user creates a new row, this is true until it is saved to the database.
        /// </summary>
        public bool IsNewRow = false;

        /// <summary>
        /// The BetterDataTable hosting this.
        /// </summary>
        public BetterDataTable Table;

        /// <summary>
        /// Set to true when the DataGridRow.Commit() throws an error.
        /// </summary>
        public bool HasError
        {
            get { return _hasError; }
            set
            {
                _hasError = value;
                RaisePropertyChanged();
            }
        }
        private bool _hasError = false;
        
        /// <summary>
        /// Have to have an initializer without parameters to make the table in the visual layer happy. Set the Table value in Collection.Add override. Sets IsNewRow = true.
        /// </summary>
        public DataRowItem()
        {
            IsNewRow = true;
        }

        /// <summary>
        /// Initialize a new "row" belonging to a BetterDataTable and set all property names and values to a DataRow column names and values. Not marked as a new row (IsNewRow = false).
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        public DataRowItem(BetterDataTable table, DataRow row)
        {
            IsNewRow = false;
            Table = table;
            foreach (ColumnInfo column in table.Columns)
            {
                object value = null;
                if (row.Table.Columns.Contains(column.ColumnName))
                    value = row[column.ColumnName];
                _columnValues[column.ColumnName] = value;
            }
        }

        /// <summary>
        /// Fill out a list of fields from a DataRow without triggering the events that come with this[key], TrySetMember[], etc. Also clears DirtCells. Leave the columns parameter empty to fill all columns.
        /// </summary>
        /// <param name="row">DataRow with the values.</param>
        /// <param name="columns">Column names to update. If empty, this updates all columns.</param>
        public void Fill(DataRow row, params string[] columns)
        {
            if (columns.Length > 0)
            {
                foreach (string c in columns)
                {
                    _columnValues[c] = row[c];
                    RaisePropertyChanged(c);
                }
            }
            else
            {
                foreach (DataColumn column in row.Table.Columns)
                {
                    _columnValues[column.ColumnName] = row[column];
                    RaisePropertyChanged(column.ColumnName);
                }
            }
            DirtyValues.Clear();
        }
        
        /// <summary>
        /// Allows references to values to be in the format DataRowItem[ColumnName].
        /// </summary>
        public object this[string key]
        {
            get
            {
                object value = null;
                _columnValues.TryGetValue(key, out value);
                return value;
            }
            set
            {
                DirtyValues[key] = value;
                RaisePropertyChanged(key);
            }
        }

        /// <summary>
        /// Override the other ways the dynamic properties may be accessed.
        /// Returns the dirty value when one exists.
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (DirtyValues.ContainsKey(binder.Name))
                return DirtyValues.TryGetValue(binder.Name, out result);
            else
                return _columnValues.TryGetValue(binder.Name, out result);
        }

        /// <summary>
        /// Override the other ways the dynamic properties may be set.
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            DirtyValues[binder.Name] = value;
            RaisePropertyChanged(binder.Name);
            return true;
        }
        
        /// <summary>
        /// Returns all field names.
        /// </summary>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _columnValues.Keys;
        }

        /// <summary>
        /// When removing the column name, try updating the value to NULL and then just remove it from the dictionary. User can then add the field again through the BetterDataTable.
        /// </summary>
        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            DirtyValues[binder.Name] = null;
            return true;
        }
        
        /// <summary>
        /// Update a row to database. If there is no primary key, try inserting it instead. 
        /// Use the primary keys if a table, or all rows if it is a view. The latter assumes that the row is unique in the view, which may cause issues.
        /// Do not insert on override InsertItem because it is triggered before the user can enter any data.
        /// Have to set this through UI behavior rather than inside InsertItem() or row[value], etc overrides; otherwise this will just insert blank rows or update one column at a time. I feel like this has better control over behavior than overriding even more DataGrid methods.
        /// Keep this at both the row and table levels for options.
        /// </summary>
        /// <returns>True when successfully saved to the DB with a returned DataTable of the inserted virtual table.</returns>
        public bool Commit()
        {
            if (DirtyValues.Count == 0)
                return true;
            string query;
            List<object> parameters = new List<object>();
            List<string> columns = new List<string>();
            List<string> values = new List<string>();
            List<Command> commands = new List<Command>();
            if (IsNewRow)
            {
                // Avoid unnecessary iterations with multiple LINQs by using a single loop.
                foreach (KeyValuePair<string, object> pair in DirtyValues)
                {
                    columns.Add($"[{pair.Key}]");
                    values.Add($"{{{parameters.Count}}}");
                    parameters.Add(pair.Value);
                }
                commands.Add(new InsertCommand(this));
                query = $"INSERT INTO {Table.TableName} ({ string.Join(",", columns) }) OUTPUT inserted.* VALUES ({ string.Join(",", values) })";
            }
            else
            {
                // Don't use LINQ to avoid repeated iterations over the same objects.
                foreach (ColumnInfo info in Table.PrimaryKeys)
                {
                    columns.Add($"[{info.ColumnName}] = {{{parameters.Count}}}");
                    parameters.Add(this[info.ColumnName]);
                }
                foreach (KeyValuePair<string, object> pair in DirtyValues)
                {
                    values.Add($"[{pair.Key}] = {{{parameters.Count}}}");
                    parameters.Add(pair.Value);
                    commands.Add(new UpdateCommand(this, pair.Key, pair.Value));
                }
                // Just in case there are duplicate rows, only update the TOP 1.
                query =
                    $";WITH cte AS (SELECT TOP 1 * FROM { Table.TableName } WHERE { string.Join(" AND ", columns) }) " +
                    $"UPDATE cte SET { string.Join(", ", values) } OUTPUT inserted.*";
            }
            // The rest of the method is the same whether it is an insert or update.
            DataTable table = Table.manager.GetDatabaseTable(query, parameters.ToArray());
            if (table.Rows.Count > 0)
            {
                Fill(table.Rows[0]);
                if (!Table.skipCommandBuilder)
                    // Go backwards through the commands so that UpdateCommands are in the right order in the CommandsList
                    for (int i = commands.Count; i > 0; i--)
                        Table.OnDataChanged(commands[i - 1]);
                IsNewRow = false;
                // identity column values may have changed...
                RaisePropertyChanged("ID");
                return true;
            }
            return false;
        }

        /// <summary>
        /// When using the undo or redo commands, have to insert an old row sometimes. Make all non-identity columns dirty.
        /// </summary>
        public void MakeDirty()
        {
            foreach (ColumnInfo info in Table.Columns)
            {
                if (!info.IsIdentity && !DirtyValues.ContainsKey(info.ColumnName))
                {
                    DirtyValues[info.ColumnName] = this[info.ColumnName];
                }
            }
        }

        /// <summary>
        /// Clears the "dirty" values and notifies the view.
        /// </summary>
        public void MakeClean()
        {
            HasError = false;
            string[] columns = DirtyValues.Keys.ToArray();
            DirtyValues.Clear();
            foreach (string s in columns)
            {
                RaisePropertyChanged(s);
            }
        }

        /// <summary>
        /// Get the PK value for this item.
        /// </summary>
        public string ID
        {
            get { return string.Join(", ", Table.PrimaryKeys.Select(x => this[x.ColumnName]).ToArray()); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));                
        }
    }

    public class ColumnInfo
    {
        public string ColumnName { get; } = string.Empty;
        public string DisplayName { get; } = string.Empty;
        public string Description = string.Empty;
        public string DataType = "nvarchar";
        public bool IsPrimaryKey = false;
        public bool IsIdentity = false;

        public ColumnInfo (string columnName, string displayName, string dataType)
        {
            ColumnName = columnName;
            DisplayName = displayName;
            DataType = dataType;
        }

        public ColumnInfo (string columnName, string displayName, string dataType, string description, bool isPrimaryKey, bool isIdentity)
        {
            ColumnName = columnName;
            DisplayName = displayName;
            DataType = dataType;
            Description = description;
            IsPrimaryKey = isPrimaryKey;
            IsIdentity = isIdentity;
        }
    }

    public class ColumnValuePair
    {
        public string Column { get; } = string.Empty;
        public object Value { get; } = null;
        public ColumnValuePair(string column, object value)
        {
            Column = column;
            Value = value;
        }
    }

    public abstract class Command : INotifyPropertyChanged
    {
        /// <summary>
        /// The DataRowItem that is changed. Made this a property that raises a NotifyPropertyChanged event to bind with a FrameworkElement so the user can browse changes that updates its identity column values.
        /// </summary>
        public DataRowItem Item
        {
            get { return _item; }
            set
            {
                _item = value;
                RaisePropertyChanged();
            }
        }
        private DataRowItem _item;

        public Command(DataRowItem row)
        {
            Item = row;
        }

        public bool IsUndone
        {
            get { return _isUndone; }
            set
            {
                _isUndone = value;
                RaisePropertyChanged();
            }
        }
        private bool _isUndone = false;

        /// <summary>
        /// RunCommand() when IsUndone = false. Otherwise, UnRunCommand().
        /// </summary>
        public void Run()
        {
            Item.Table.skipCommandBuilder = true;
            if (IsUndone)
                RunCommand();
            else
                UnRunCommand();
            Item.Table.skipCommandBuilder = false;
            IsUndone = !IsUndone;
        }

        /// <summary>
        /// Redo the change.
        /// </summary>
        protected abstract void RunCommand();

        /// <summary>
        /// Undo the change.
        /// </summary>
        protected abstract void UnRunCommand();

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Update SQL command for a single value. UnExecute() reverts that value.
    /// </summary>
    public class UpdateCommand : Command
    {
        public ColumnValuePair newValue;
        public ColumnValuePair oldValue;

        public UpdateCommand(DataRowItem row, string column, object value) : base(row)
        {
            newValue = new ColumnValuePair(column, value);
            oldValue = new ColumnValuePair(column, row[column]);
        }
        
        protected override void RunCommand()
        {
            Item[newValue.Column] = newValue.Value;
            Item.Commit();
        }

        protected override void UnRunCommand()
        {
            Item[oldValue.Column] = oldValue.Value;
            Item.Commit();
        }
    }

    /// <summary>
    /// Insert SQL command for a single row. UnExecute() deletes that row but still holds it in memory.
    /// </summary>
    public class InsertCommand : Command
    {
        public InsertCommand(DataRowItem row) : base(row)
        {

        }

        protected override void RunCommand()
        {
            Item.Table.InsertNewRow(Item);
            Item.Commit();
        }

        protected override void UnRunCommand()
        {
            Item.Table.Remove(Item);
        }
    }

    /// <summary>
    /// Delete SQL command for a single row but still holds it in memory. UnExecute() inserts that row.
    /// </summary>
    public class DeleteCommand : Command
    {
        public DeleteCommand(DataRowItem row) : base(row)
        {

        }

        protected override void RunCommand()
        {
            Item.Table.Remove(Item);
        }

        protected override void UnRunCommand()
        {
            Item.Table.InsertNewRow(Item);
            Item.Commit();
        }
    }
}
