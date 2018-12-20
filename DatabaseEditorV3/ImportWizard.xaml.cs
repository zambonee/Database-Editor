using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Dynamic;
using System.Threading.Tasks;
using System.Text;

namespace DatabaseEditorV3
{
    /// <summary>
    /// Interaction logic for ImportWizard.xaml
    /// </summary>
    public partial class ImportWizard : Window
    {
        private ImportWizardContext context;

        public ImportWizard(SQLManager manager)
        {
            context = new ImportWizardContext(manager);
            DataContext = context;
            InitializeComponent();
        }

        public ImportWizard(SQLManager manager, string fileName, long objectid)
        {
            context = new ImportWizardContext(manager, fileName, objectid);
            DataContext = context;
            InitializeComponent();
        }

        private void ButtonPreview_Click(object sender, RoutedEventArgs e)
        {
            context.Preview();

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonFindFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Find the File to Import";
            dialog.Filter = "Excel|*.XLSX;*.XLS|Access|*.MDB;*.ACCDB|XML|*.xml|Text-delimited|*.CSV;*.TXT|All Files|*.*";
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == true)
            {
                context.FileName = dialog.FileName;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(context.FileName))
            {
                ButtonFindFile_Click(null, null);
            }
        }

        private void ComboBox_PreviewGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox cb = sender as System.Windows.Controls.ComboBox;
            object selection = cb.SelectedItem;
            foreach (object o in cb.Items)
            {
                System.Windows.Controls.ComboBoxItem item = cb.ItemContainerGenerator.ContainerFromItem(o) as System.Windows.Controls.ComboBoxItem;
                item.IsEnabled = true;
                foreach (MatchedColumn column in context.CollectionMatches)
                {
                    if (selection == column.DatabaseColumn)
                    {
                        continue;
                    }
                    if (o == column.DatabaseColumn)
                    {
                        item.IsEnabled = false;
                        continue;
                    }
                }
            }
        }

        private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is MatchedColumn match)
            {
                match.FindInForeign = false;
                match.DatabaseColumn = null;
            }
        }
    }

    public class ImportWizardContext : INotifyPropertyChanged
    {
        /// <summary>
        /// Do not yet know the file name or db table.
        /// </summary>
        public ImportWizardContext(SQLManager manager)
        {
            Initialize(manager);
        }

        /// <summary>
        /// When a file is dragged into a table, already know the file name and db table.
        /// </summary>
        public ImportWizardContext(SQLManager manager, string fileName, long objectid)
        {
            Initialize(manager, objectid);
            FileName = fileName;
        }

        /// <summary>
        /// Shared method for all ImportWizardContext() initializers.
        /// </summary>
        private void Initialize(SQLManager manager, long objectid = 0)
        {
            string selectedSchema = string.Empty;
            TableStructure selectedTable = null;
            this.manager = manager;
            DataTable dtSchemas = manager.GetDatabaseTable("SELECT name, schema_id FROM sys.schemas");
            foreach (DataRow rowSchema in dtSchemas.Rows)
            {
                DataTable dtTables = manager.GetDatabaseTable("SELECT object_id FROM sys.tables WHERE schema_id = {0}", rowSchema["schema_id"]);
                List<TableStructure> listTables = new List<TableStructure>(dtTables.Rows.Count);
                foreach (DataRow rowTable in dtTables.Rows)
                {
                    long id = rowTable.Field<int>("object_id");
                    TableStructure table = new TableStructure(id, manager);
                    listTables.Add(table);
                    if (id == objectid)
                    {
                        selectedSchema = rowSchema.Field<string>("name");
                        selectedTable = table;
                    }
                }
                if (listTables.Count > 0)
                    DatabaseStructure[rowSchema.Field<string>("name")] = listTables.ToArray();
            }
            if (DatabaseStructure.ContainsKey(selectedSchema))
            {
                CollectionSchemaTables = DatabaseStructure[selectedSchema];
                SelectedTable = selectedTable;
            }
            CollectionPreview.CollectionChanged += CollectionPreview_CollectionChanged;
        }

        private void CollectionPreview_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PreviewRow row in e.NewItems)
                {
                    row.CreateColumns(CollectionMatches.ToArray(), SelectedTable);
                }
            }
        }

        /// <summary>
        /// Extract the OleDB tables and views (or Excel Worksheets).
        /// </summary>
        private void UpdateWorksheet()
        {
            ListWorksheets.Clear();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                {
                    conn.Open();
                    DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["TABLE_TYPE"].Equals("VIEW") || row["TABLE_TYPE"].Equals("TABLE"))
                        {
                            ListWorksheets.Add(row.Field<string>("TABLE_NAME"));
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Extract the columns in the OleDB table. If the selected file is a flat file, then treat the file as the OleDB table, and run this as soon as the file is chosen.
        /// Table name is a parameter rather than getting it from the Worksheet property because flat files do not have tables like other file types.
        /// </summary>
        private void UpdateColumns()
        {
            CollectionMatches.Clear();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                {
                    conn.Open();
                    OleDbCommand command = conn.CreateCommand();
                    // Get an empty data set just for the columns.
                    command.CommandText = $"SELECT * FROM [{OLEDBTableName}] WHERE 1=2";
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataColumn column in dt.Columns)
                    {
                        CollectionMatches.Add(new MatchedColumn(column.ColumnName));
                    }
                }
            }
            catch (Exception ex)
            {
                // Show the OleDB connection string for editing.
                MessageBoxResult result = MessageBox.Show($"Cannot connect to the file. Error message: {ex.Message} Do you want to edit the connection string?", "Error", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    ConnectionStringVisible = true;
                }
            }
            AutoMatch();
        }

        /// <summary>
        /// Match file columns to a database column of the same name. If a column of the same name does not exist, look through the referenced tables columns.
        /// </summary>
        private void AutoMatch()
        {
            if (SelectedTable == null)
                return;
            foreach (MatchedColumn match in CollectionMatches)
            {
                //if (match.DatabaseColumn != null)
                //    continue;
                string fileColumn = System.Text.RegularExpressions.Regex.Replace(match.FileColumn, @"\W|_", "").ToUpper();
                foreach (DisplayValuePair column in SelectedTable.Columns)
                {
                    string display = System.Text.RegularExpressions.Regex.Replace(column.Display, @"\W|_", "").ToUpper();
                    string value = System.Text.RegularExpressions.Regex.Replace(column.Value.ToString(), @"\W|_", "").ToUpper();
                    if (fileColumn == display || fileColumn == value)
                    {
                        match.FindInForeign = false;
                        match.DatabaseTable = SelectedTable;
                        match.DatabaseColumn = column;
                        break;
                    }
                }
                if (match.DatabaseColumn != null)
                    continue;
                foreach (TableStructure parent in SelectedTable.ParentTables)
                {
                    foreach (DisplayValuePair column in parent.Columns)
                    {
                        string display = System.Text.RegularExpressions.Regex.Replace(column.Display, @"\W|_", "").ToUpper();
                        string value = System.Text.RegularExpressions.Regex.Replace(column.Value.ToString(), @"\W|_", "").ToUpper();
                        if (fileColumn == display || fileColumn == value)
                        {
                            match.FindInForeign = true;
                            match.DatabaseTable = parent;
                            match.DatabaseColumn = column;
                            break;
                        }
                    }
                    if (match.DatabaseColumn != null)
                        break;
                }
            }
        }

        public void Preview()
        {
            // Verify missing matches.
            string[] missing = CollectionMatches.Where(x => x.DatabaseColumn == null).Select(x => x.FileColumn).ToArray();
            if (missing.Length > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"You did not match the following columns to a database column:" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"\t{string.Join(Environment.NewLine + "\t", missing)}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"Continue?",
                    "Missing Matches", MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }
            // Order the select columns just like CollectionMatches. Cannot alias column names with periods!
            List<MatchedColumn> matchedColumns = CollectionMatches.Where(x => x.DatabaseColumn != null).ToList();
            // Clean up the matches: assign SelectedTable to the database table value where it is null.
            for (int i = 0; i < matchedColumns.Count; i++)
            {
                if (matchedColumns[i] == null || !matchedColumns[i].FindInForeign)
                {
                    matchedColumns[i].DatabaseTable = SelectedTable;
                }
            }
            string[] columns = matchedColumns.Select(x => $"[{x.FileColumn}]").ToArray();
            // Create table of data source values.
            DataTable dtImported = new DataTable();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                {
                    conn.Open();
                    OleDbCommand command = conn.CreateCommand();
                    command.CommandText = $"SELECT {string.Join(",", columns)} FROM [{OLEDBTableName}]";
                    if (!string.IsNullOrWhiteSpace(Filter))
                    {
                        command.CommandText += $" WHERE {Filter}";
                    }
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    adapter.Fill(dtImported);
                }
            }
            catch (Exception ex)
            {
                // Show the OleDB connection string for editing.
                MessageBoxResult result = MessageBox.Show($"Cannot connect to the file. Error message: {ex.Message} Do you want to edit the connection string?", "Error", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    ConnectionStringVisible = true;
                }
                return;
            }
            //// Add FK columns.
            var groups =
                matchedColumns
                .Where(x => x.DatabaseTable != null && x.DatabaseTable != SelectedTable)
                .GroupBy(x => x.DatabaseTable);
            foreach (var g in groups)
            {
                foreach (KeyValuePair<string, DisplayValuePair> kvp in g.Key.Dependencies)
                {
                    MatchedColumn newColumn = new MatchedColumn(kvp.Value.Display) { DatabaseColumn = kvp.Value, FindInForeign = true, ReferencedTable = g.Key };
                    newColumn.DatabaseTable = SelectedTable;
                    matchedColumns.Add(newColumn);
                }
            }
            // Create a bindable object for the preview window.
            CollectionPreview.Clear();
            foreach (DataRow row in dtImported.Rows)
            {
                PreviewRow newRow = new PreviewRow();
                for (int i = 0; i < matchedColumns.Count; i++)
                {
                    if (i < dtImported.Columns.Count)
                    {
                        newRow[matchedColumns[i]] = row[i];
                    }
                    else
                    {
                        newRow[matchedColumns[i]] = null;
                    }
                }
                // Get FK values                
                foreach (var g in groups)
                {
                    TableStructure table = g.Key;
                    string[] where = g.Select((x, i) => $"{x.DatabaseColumn.Value} = {{{i}}}").ToArray();
                    object[] parameters = g.Select(x => newRow[x]).ToArray();
                    string query = $"SELECT {string.Join(", ", table.Dependencies.Keys)} FROM {table.Schema}.{table.TableName} WHERE {string.Join(" AND ", where)}";
                    DataTable dtFKValues;
                    Exception exception;
                    if (manager.TryGetDatabaseTable(query, out dtFKValues, out exception, parameters))
                    {
                        if (dtFKValues.Rows.Count == 1)
                        {
                            foreach (var d in table.Dependencies)
                            {
                                newRow[(string)d.Value.Value] = dtFKValues.Rows[0][d.Key];
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Failed to locate potential foreign key values. Error message: {exception}");
                        return;
                    }
                    
                }
                CollectionPreview.Add(newRow);
            }
            // Show preview window. Handle the Save button in the dialog window.
            if (groups.Count() > 0)
            {
                HasReferencedTable = true;
            }
            else
            {
                HasReferencedTable = false;
            }
            bool _hasPrimaryKey = false;
            foreach (var pk in SelectedTable.PrimaryKeys)
            {
                if (matchedColumns.Where(x => x.DatabaseTable == SelectedTable && x.DatabaseColumn.Value == pk.Value).Count() > 0)
                {
                    _hasPrimaryKey = true;
                    break;
                }
            }
            HasPrimaryKey = _hasPrimaryKey;
            PreviewImportWizard newPreviewWindow = new PreviewImportWizard(matchedColumns.ToArray(), this);
            newPreviewWindow.ShowDialog();
        }
                
        /// <summary>
        /// Try making the inserts to the selected database table, and returns true if there were no errors. 
        /// When InsertOnNotExists is true and no foreign key can be found based on other referenced table columns, inserts a new record and fills the foreign key value.
        /// When UpdateOnExists is true and the primary key is included, updates the other columns when a record exists with that primary key. Otherwise, inserts a new record with all column values including the primary key (will throw an error if it is identity and identity_insert is off).
        /// </summary>
        /// <param name="isTest">When true, all changes sent to the database are rolled back.</param>
        /// <param name="progress">Reports the progress of the async.</param>
        /// <returns>True when there were no errors, otherwise returns false.</returns>
        public bool MakeDatabaseChanges(bool isTest)
        {
            //RUN THIS AS ISTEST FIRST NO MATTER WHAT BECAUSE THIS DOES NOT INSERT ALL RECORDS AS A SINGLE TRANSACTION. EACH ROW IS ITS OWN TRANSACTION TO AVOID HITTING THE PARAMETER COUNT LIMIT!
            if (CollectionPreview.Count < 1)
            {
                return true;
            }
            bool hasError = false;
            int rowCount = -1;
            // Insert one row at a time so it can use parameters with very little possibility to exceed the maximum of 2100 parameters.
            // Caveat #1 a reference has to return exactly 1 record (after it is inserted if InsertOnNotExists).
            // This is OK because if there are multiple referenced table records matching the given columns, I want to throw an error instead of making a new record if InsertOnNotExists or returning just the top 1.
            // Caveat #2 an error does not rollback previously successful rows.
            // IMPORTANT!!!!! Run this with isTest first before committing so that successful rows are rollbacked when there is an error.
            foreach (PreviewRow row in CollectionPreview)
            {
                rowCount++;
                // All of the column-value pairs.
                Dictionary<MatchedColumn, object> columns = row.Values;
                // containing columns
                IEnumerable<KeyValuePair<MatchedColumn, object>> containingColumns = columns.Where(x => x.Key.DatabaseTable == SelectedTable && x.Key.ReferencedTable == null);
                // Foreign table columns
                IEnumerable<IGrouping<TableStructure, KeyValuePair<MatchedColumn, object>>> foreignColumns = columns.GroupBy(x => x.Key.DatabaseTable).Where(x => x.Key != SelectedTable && x.Key != null);
                // Build the query that handles everything
                StringBuilder query = new StringBuilder();
                query.AppendLine("DECLARE @InsertOnNotExists bit = {0}");
                query.AppendLine("DECLARE @UpdateOnExists bit = {1}");
                query.AppendLine("DECLARE @IsTest bit = {2}");
                // Collect the parameters in a list, starting with the 3 parameter-style variables
                List<object> parameters = new List<object>() { InsertOnNotExists, UpdateOnExists, isTest };
                // Run this as a transaction to rollback if there is an error.
                query.AppendLine("BEGIN TRANSACTION");
                // Fill this in the next foreach because order matters and can re-user parameters.
                List<string> cteWhereClause = new List<string>();
                // Find the referenced column values based on the foreign table column values with the same referenced table.
                foreach (IGrouping<TableStructure, KeyValuePair<MatchedColumn, object>> table in foreignColumns)
                {
                    row.ErrorMessage = null;
                    // Parameterized list for the referenced table columns and values
                    // Do this in a loop rather than LINQ because order matters, and it avoids duplicate iterations through LINQ for every reference to the KeyValuePairs.
                    List<string> listValues = new List<string>();
                    List<string> listColumns = new List<string>();
                    List<string> listMatches = new List<string>();
                    foreach (KeyValuePair<MatchedColumn, object> pair in table)
                    {
                        cteWhereClause.Add($"({pair.Key.DatabaseColumn.OtherValue} = {{{parameters.Count}}} OR ({pair.Key.DatabaseColumn.OtherValue} IS NULL AND {{{parameters.Count}}} IS NULL))");
                        listColumns.Add($"[{pair.Key.DatabaseColumn.Value}]");
                        listMatches.Add($"(target.[{pair.Key.DatabaseColumn.Value}] = source.[{pair.Key.DatabaseColumn.Value}] OR (target.[{pair.Key.DatabaseColumn.Value}] IS NULL AND source.[{pair.Key.DatabaseColumn.Value}] IS NULL))");
                        listValues.Add($"{{{parameters.Count}}}");
                        parameters.Add(pair.Value);
                    }
                    string stringValues = string.Join(",", listValues);
                    string stringColumns = string.Join(",", listColumns);
                    string stringMatches = string.Join(" AND ", listMatches);
                    // Columns that reference this table
                    IEnumerable<KeyValuePair<MatchedColumn, object>> referencedColumns = columns.Where(x => x.Key.DatabaseTable == SelectedTable && x.Key.ReferencedTable == table.Key);
                    query.AppendLine($"MERGE [{table.Key.Schema}].[{table.Key.TableName}] AS target");
                    query.AppendLine($"USING (SELECT {stringValues}) AS source ({stringColumns})");
                    query.AppendLine($"ON ({stringMatches})");
                    query.AppendLine($"WHEN NOT MATCHED AND @InsertOnNotExists = 1 THEN");
                    query.AppendLine($"INSERT ({stringColumns})");
                    query.AppendLine($"VALUES ({stringValues})");
                    query.AppendLine(";"); // Merge statements have to be terminated with a semicolon.
                }
                // List of values to insert into the main table, and the columns to select from referenced tables to insert.
                List<string> listContainingColumnValues = new List<string>();
                List<string> listContainingColumns = new List<string>();
                List<string> listSetClause = new List<string>();
                // List the primary key columns included in the matches.
                List<string> listNotMatchedClause = new List<string>() { "(SELECT COUNT(*) FROM cte) = 1" };
                List<string> listPrimaryKeyMatches = new List<string>();
                // Values + alias
                foreach (KeyValuePair<MatchedColumn, object> pair in containingColumns)
                {
                    listContainingColumnValues.Add($"{{{parameters.Count}}} AS [{pair.Key.DatabaseColumn.Value}]");
                    listContainingColumns.Add($"[{pair.Key.DatabaseColumn.Value}]");
                    parameters.Add(pair.Value);
                    // Is this pair a primary key?
                    if (SelectedTable.PrimaryKeys.Contains(pair.Key.DatabaseColumn))
                    {
                        listNotMatchedClause.Add($"source.[{pair.Key.DatabaseColumn.Value}] IS NULL");
                        // Primary keys that are NULL should be ignored anyways, so don't include OR (column IS NULL AND column IS NULL).
                        listPrimaryKeyMatches.Add($"target.[{pair.Key.DatabaseColumn.Value}] = source.[{pair.Key.DatabaseColumn.Value}]");
                    }
                    else
                    {
                        listSetClause.Add($"target.[{pair.Key.DatabaseColumn.Value}] = source.[{pair.Key.DatabaseColumn.Value}]");
                    }
                }
                // Columns to select from referenced tables. Order does not matter so much.
                IEnumerable<KeyValuePair<MatchedColumn, object>> fks = columns.Where(x => x.Key.DatabaseTable == SelectedTable && x.Key.ReferencedTable != null);
                listContainingColumnValues.AddRange(fks.Select(x => (string)x.Key.DatabaseColumn.OtherValue));
                listContainingColumns.AddRange(fks.Select(x => $"[{x.Key.DatabaseColumn.Value}]"));
                listSetClause.AddRange(columns.Where(x => x.Key.DatabaseTable == SelectedTable && x.Key.ReferencedTable != null).Select(x => $"target.[{x.Key.DatabaseColumn.Value}] = source.[{x.Key.DatabaseColumn.Value}]"));
                // Use a Common Table Expression to join the new values with the returned reference table values.
                query.AppendLine("WITH cte AS (");
                query.AppendLine($"SELECT {string.Join(",", listContainingColumnValues)}");
                if (foreignColumns.Count() > 0)
                {
                    query.AppendLine($"FROM {string.Join(" CROSS JOIN ", foreignColumns.Select(x => $"[{x.Key.Schema}].[{x.Key.TableName}]"))}");
                    query.AppendLine($"WHERE {string.Join(" AND ", cteWhereClause)}");
                }
                query.AppendLine(")");
                query.AppendLine($"MERGE [{SelectedTable.Schema}].[{SelectedTable.TableName}] AS target");
                query.AppendLine($"USING (SELECT * FROM cte) AS source");
                query.AppendLine($"ON {(listPrimaryKeyMatches.Count > 0 ? string.Join(" AND ", listPrimaryKeyMatches) : "1 = 0")}");
                query.AppendLine($"WHEN NOT MATCHED AND {string.Join(" AND ", listNotMatchedClause)} THEN");
                query.AppendLine($"INSERT ({string.Join(",", listContainingColumns)})");
                query.AppendLine($"VALUES ({string.Join(",", listContainingColumns)})");
                query.AppendLine("WHEN MATCHED AND @UpdateOnExists = 1 AND (SELECT COUNT(*) FROM cte) = 1 THEN");
                query.AppendLine($"UPDATE SET {string.Join(",", listSetClause)}");
                query.AppendLine("OUTPUT inserted.*");
                query.AppendLine(";");
                query.AppendLine("IF @IsTest = 1 ROLLBACK TRANSACTION");
                query.AppendLine("ELSE COMMIT TRANSACTION");
                DataTable dtResults;
                Exception ex;
                if (manager.TryGetDatabaseTable(query.ToString(), out dtResults, out ex, parameters.ToArray()))
                {
                    if (dtResults.Rows.Count == 1)
                    {
                        // Update row information for testing.
                    }
                    else
                    {
                        row.ErrorMessage = "Could not find a single unique value for the foreign key column.";
                        hasError = true;
                    }
                }
                else if (ex != null)
                {
                    row.ErrorMessage = ex.Message;
                    hasError = true;
                }
            }
            return !hasError;
        }
        
        private SQLManager manager;

        /// <summary>
        /// Accepts Excel, Access, XML, csv, and tab-delimited (default) file types.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                RaisePropertyChanged();
                string extension = Path.GetExtension(FileName).ToUpper();
                switch (extension)
                {
                    case ".XLSX":
                    case ".XLS":
                        ConnectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Mode=Read;Data Source={FileName};Extended Properties='Excel 12.0;HDR=YES'";
                        break;
                    case ".MDB":
                    case ".ACCDB":
                        ConnectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Mode=Read;Data Source={FileName}";
                        break;
                    // OleDB for flat files is really wierd! The file directory is the source, and the file is the table.
                    case ".XML":
                        ConnectionString = $"Provider=MSDAOSP; Data Source={FileName}";
                        OLEDBTableName = Path.GetFileName(FileName);
                        UpdateColumns();
                        break;
                    case ".CSV":
                        ConnectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Path.GetDirectoryName(FileName)};Extended Properties='Text;HDR=Yes;Delimited(,)'";
                        OLEDBTableName = Path.GetFileName(FileName);
                        UpdateColumns();
                        break;
                    default:
                        ConnectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Path.GetDirectoryName(FileName)};Extended Properties='Text;HDR=Yes;Delimited(\\t)'";
                        OLEDBTableName = Path.GetFileName(FileName);
                        UpdateColumns();
                        break;
                }
            }
        }

        public ObservableCollection<string> ListWorksheets { get; } = new ObservableCollection<string>();

        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                RaisePropertyChanged();
                UpdateWorksheet();
            }
        }

        public string Worksheet
        {
            get { return _worksheet; }
            set
            {
                _worksheet = value;
                RaisePropertyChanged();
                if (!string.IsNullOrWhiteSpace(Worksheet))
                {
                    OLEDBTableName = Worksheet;
                    UpdateColumns();
                }
            }
        }

        /// <summary>
        /// Sometimes the OLEDB table name is the worksheet. Sometimes it is the file name.
        /// </summary>
        private string OLEDBTableName = string.Empty;

        public bool ConnectionStringVisible
        {
            get { return _connectionStringVisible; }
            set
            {
                _connectionStringVisible = value;
                RaisePropertyChanged();
            }
        }

        public string Filter { get; set; } = string.Empty;

        public Dictionary<string, TableStructure[]> DatabaseStructure { get; } = new Dictionary<string, TableStructure[]>();

        public TableStructure[] CollectionSchemaTables
        {
            get { return _collectionSchemaTables; }
            set
            {
                _collectionSchemaTables = value;
                RaisePropertyChanged();
            }
        }

        public TableStructure SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                _selectedTable = value;
                RaisePropertyChanged();
                AutoMatch();
            }
        }

        public ObservableCollection<MatchedColumn> CollectionMatches { get; } = new ObservableCollection<MatchedColumn>();

        public ObservableCollection<PreviewRow> CollectionPreview { get; } = new ObservableCollection<PreviewRow>();

        public bool UpdateOnExists { get; set; } = false;
        public bool InsertOnNotExists { get; set; } = false;

        public bool HasReferencedTable { get; set; } = false;
        public bool HasPrimaryKey { get; set; } = false;

        private string _fileName = string.Empty;
        private string _worksheet = string.Empty;
        private string _connectionString = string.Empty;
        private bool _connectionStringVisible = false;
        private TableStructure[] _collectionSchemaTables = new TableStructure[0];
        private TableStructure _selectedTable;

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MatchedColumn : INotifyPropertyChanged
    {
        public string FileColumn { get; set; } = string.Empty;
        public bool FindInForeign
        {
            get { return _findInForeign; }
            set
            {
                _findInForeign = value;
                RaisePropertyChanged();
                DatabaseTable = null;
            }
        }
        public TableStructure DatabaseTable
        {
            get { return _databaseTable; }
            set
            {
                _databaseTable = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Table to get the foreign key from. Keep null when the FileColumn is not a foreign key to locate.
        /// </summary>
        public TableStructure ReferencedTable { get; set; }
        public DisplayValuePair DatabaseColumn
        {
            get { return _databaseColumn; }
            set
            {
                _databaseColumn = value;
                RaisePropertyChanged();
            }
        }
        public string ColumnName
        {
            get
            {
                if (FindInForeign)
                {
                    return DatabaseColumn.Display;
                }
                else
                {
                    return $"{DatabaseTable.DisplayName}.{DatabaseColumn.Display}";
                }
            }
        }

        private bool _findInForeign = false;
        private TableStructure _databaseTable;
        private DisplayValuePair _databaseColumn;

        public MatchedColumn(string FileColumn)
        {
            this.FileColumn = FileColumn;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TableStructure
    {
        public string Schema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<DisplayValuePair> Columns { get; set; } = new List<DisplayValuePair>();
        public List<TableStructure> ParentTables { get; set; } = new List<TableStructure>();

        /// <summary>
        /// Key is parent column, Value is referenced column name and display name.
        /// </summary>
        public Dictionary<string, DisplayValuePair> Dependencies = new Dictionary<string, DisplayValuePair>();

        /// <summary>
        /// The primary keys for this table.
        /// </summary>
        public List<DisplayValuePair> PrimaryKeys = new List<DisplayValuePair>();

        /// <summary>
        /// The identity columns for this table.
        /// </summary>
        public List<DisplayValuePair> Identities = new List<DisplayValuePair>();

        public TableStructure(long objectid, SQLManager manager, long parentObjectID = 0)
        {
            DataTable dtTable = manager.GetDatabaseTable(
                "SELECT " +
                "   schemas.name AS [schema], " +
                "   objects.name AS [table], " +
                "   COALESCE(extended_properties.value, objects.name) AS display " +
                "FROM sys.objects " +
                "INNER JOIN sys.schemas " +
                "ON objects.schema_id = schemas.schema_id " +
                "LEFT JOIN sys.extended_properties " +
                "ON extended_properties.major_id = objects.object_id " +
                "AND extended_properties.minor_id = 0 " +
                "AND extended_properties.name = 'DisplayName' " +
                "WHERE objects.object_id = {0}",
                objectid);
            if (dtTable.Rows.Count != 1)
                return;
            Schema = dtTable.Rows[0].Field<string>("schema");
            TableName = dtTable.Rows[0].Field<string>("table");
            DisplayName = dtTable.Rows[0].Field<string>("display");
            DataTable dtColumns = manager.GetDatabaseTable(
                "SELECT " +
                "   columns.name, " +
                "   COALESCE(extended_properties.value, columns.name) AS display, " +
                "   CONVERT(bit, COALESCE(is_identity, 0)) AS is_identity, " +
                "   CONVERT(bit, COALESCE(is_primary_key, 0)) AS is_primary_key " +
                "FROM sys.columns " +
                "LEFT JOIN sys.extended_properties " +
                "    ON columns.object_id = extended_properties.major_id " +
                "    AND columns.column_id = extended_properties.minor_id " +
                "    AND extended_properties.name = 'DisplayName' " +
                "LEFT JOIN sys.index_columns " +
                "    ON columns.object_id = index_columns.object_id " +
                "    AND columns.column_id = index_columns.column_id " +
                "LEFT JOIN sys.indexes " +
                "    ON index_columns.object_id = indexes.object_id " +
                "    AND index_columns.index_id = indexes.index_id " +
                "WHERE columns.object_id = {0}",
                objectid);
            foreach (DataRow rowColumn in dtColumns.Rows)
            {
                string other = $"[{TableName}].[{rowColumn.Field<string>("name")}]";
                string columnName = rowColumn.Field<string>("name");
                string display =
                    parentObjectID == 0 ?
                    rowColumn.Field<string>("display") :
                    $"{rowColumn.Field<string>("display")} ({DisplayName})";
                DisplayValuePair pair = new DisplayValuePair(display, columnName) { OtherValue = other };
                Columns.Add(pair);
                if (rowColumn.Field<bool>("is_primary_key"))
                {
                    PrimaryKeys.Add(pair);
                }
                if (rowColumn.Field<bool>("is_identity"))
                {
                    Identities.Add(pair);
                }
            }
            // Set the parentObjectID of the primary table to 0, and this will take care of finding all of its dependencies for only one level up.
            if (parentObjectID == 0)
            {
                DataTable dtForeignTable = manager.GetDatabaseTable(
                    "SELECT " +
                    "   tables.object_id " +
                    "FROM sys.tables " +
                    "INNER JOIN sys.foreign_keys " +
                    "   ON tables.object_id = foreign_keys.referenced_object_id " +
                    "WHERE foreign_keys.parent_object_id = {0} " +
                    "UNION SELECT " +
                    "   referencing_id " +
                    "FROM sys.sql_expression_dependencies " +
                    "INNER JOIN sys.views " +
                    "   ON sql_expression_dependencies.referenced_id = views.object_id " +
                    "WHERE referenced_id = {0}",
                    objectid);
                foreach (DataRow rowForeignTable in dtForeignTable.Rows)
                {
                    ParentTables.Add(new TableStructure(rowForeignTable.Field<int>("object_id"), manager, objectid));
                }
            }
            else
            {
                DataTable dtDependencies = manager.GetDatabaseTable(
                    "SELECT " +
                    "   COALESCE(extended_properties.value, parentColumns.name) AS display, " +
                    "   parentColumns.name AS parent, " +
                    "   referencedColumns.name AS referenced " +
                    "FROM sys.foreign_key_columns " +
                    "INNER JOIN sys.columns AS parentColumns " +
                    "   ON foreign_key_columns.parent_object_id = parentColumns.object_id " +
                    "   AND foreign_key_columns.parent_column_id = parentColumns.column_id " +
                    "INNER JOIN sys.columns AS referencedColumns " +
                    "   ON foreign_key_columns.referenced_object_id = referencedColumns.object_id " +
                    "   AND foreign_key_columns.referenced_column_id = referencedColumns.column_id " +
                    "LEFT JOIN sys.extended_properties " +
                    "   ON extended_properties.name = 'DisplayName' " +
                    "   AND extended_properties.major_id = parentColumns.object_id " +
                    "   AND extended_properties.minor_id = parentColumns.column_id " +
                    "WHERE foreign_key_columns.parent_object_id = {0} " +
                    "AND foreign_key_columns.referenced_object_id = {1}",
                    parentObjectID, objectid);
                foreach (DataRow row in dtDependencies.Rows)
                {
                    string display = $"{row.Field<string>("display")} (from {DisplayName})";
                    // Value is the parent column name. Other is the full referenced column name with table name.
                    string value = row.Field<string>("parent");
                    string other = $"[{TableName}].[{row.Field<string>("referenced")}]";

                    Dependencies[row.Field<string>("referenced")] = new DisplayValuePair(display, row.Field<string>("parent")) { OtherValue = other };
                }
            }
        }
    }

    public class PreviewRow : DynamicObject, INotifyPropertyChanged
    {
        private readonly Dictionary<MatchedColumn, object> _dictionary = new Dictionary<MatchedColumn, object>();

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                RaisePropertyChanged();
            }
        }
        private string _errorMessage;
        
        public object this[string key]
        {
            get
            {
                return _dictionary.Where(x => x.Key.DatabaseColumn.Value.Equals(key)).Select(x => x.Value).FirstOrDefault();
            }
            set
            {
                MatchedColumn m = _dictionary.Keys.Where(x => x.DatabaseColumn.Value.Equals(key)).FirstOrDefault();
                if (m != null)
                {
                    _dictionary[m] = value;
                    RaisePropertyChanged(key);
                }
            }
        }

        public object this[MatchedColumn key]
        {
            get { return _dictionary[key]; }
            set { _dictionary[key] = value; }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            IEnumerable<object> selects = _dictionary.Where(x => x.Key.DatabaseColumn.Value.Equals(binder.Name)).Select(x => x.Value);
            if (selects.Count() == 1)
            {
                result = selects.First();
                return true;
            }
            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            MatchedColumn m = _dictionary.Keys.Where(x => x.DatabaseColumn.Value.Equals(binder.Name)).FirstOrDefault();
            if (m != null)
            {
                _dictionary[m] = value;
                RaisePropertyChanged(binder.Name);
                return true;
            }
            return false;
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            IEnumerable<KeyValuePair<MatchedColumn, object>> items = _dictionary.Where(x => x.Key.DatabaseColumn.Value.Equals(binder.Name));
            if (items.Count() == 1)
            {
                _dictionary[items.First().Key] = null;
                RaisePropertyChanged(binder.Name);
                return true;
            }
            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _dictionary.Select(x => x.Key.DatabaseColumn.Value.ToString());
        }

        public PreviewRow()
        {

        }

        public void CreateColumns(MatchedColumn[] columns, TableStructure selectedTable)
        {
            if (_dictionary.Count > 0)
            {
                return;
            }
            columns = columns.Where(x => x.DatabaseColumn != null).ToArray();
            _dictionary.Clear();
            foreach (MatchedColumn c in columns)
            {
                _dictionary[c] = null;
            }
            // Add FK columns.
            var groups =
                columns
                .Where(x => x.DatabaseTable != selectedTable)
                .GroupBy(x => x.DatabaseTable);
            foreach (var g in groups)
            {
                TableStructure table = g.Key;
                foreach (KeyValuePair<string, DisplayValuePair> kvp in table.Dependencies)
                {
                    MatchedColumn newColumn = new MatchedColumn(kvp.Value.Display) { DatabaseTable = selectedTable, DatabaseColumn = kvp.Value, FindInForeign = true, ReferencedTable = table };
                    _dictionary[newColumn] = null;
                }
            }
        }

        public Dictionary<MatchedColumn, object> Values
        {
            get { return _dictionary; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// A dictionary with a default value if the key does not exist.
    /// </summary>
    public class DictionaryWithDefault<TKey, TValue> : Dictionary<TKey, TValue>
    {
        
        public DictionaryWithDefault(TValue defaultValue) : base()
        {
            DefaultValue = defaultValue;
        }

        public TValue DefaultValue { get; set; }

        public new TValue this[TKey key]
        {
            get
            {
                TValue t;
                return TryGetValue(key, out t) ? t : DefaultValue;
            }
            set { base[key] = value; }
        }
    }
}
