using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace DatabaseEditorV3
{
    public partial class MainWindow : Window
    {
        MainWindowModel context;

        public MainWindow()
        {
            InitializeComponent();
            context = DataContext as MainWindowModel;
        }

        private void MenuConnect_Click(object sender, RoutedEventArgs e)
        {
            context.Connect();
        }

        private void CommandOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ImportWizard newWizard = new ImportWizard(context.manager);
            newWizard.Show();
        }

        private void CommandFind_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach(Window w in Application.Current.Windows)
            {
                if (w is FilterWindow)
                {
                    w.Activate();
                    return;
                }
            }
            FilterWindow newW = new FilterWindow(context);
            newW.Show();
        }

        /// <summary>
        /// Browse through the Commands for database changes. Make sure there is only one instance of this window.
        /// </summary>
        private void CommandUndo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is UndoChangesWindow)
                {
                    w.Activate();
                    return;
                }
            }
            UndoChangesWindow newW = new UndoChangesWindow(context);
            newW.Show();
        }

        private void CommandUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (context.UndoCommands.Count == 0)
                e.CanExecute = false;
            else
                e.CanExecute = true;
        }

        private void BetterDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                BetterDataTable dt = (sender as BetterDataGrid).ItemsSource as BetterDataTable;
                string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string s in fileNames)
                {
                    ImportWizard newWizard = new ImportWizard(context.manager, s, dt.ObjectID);
                    newWizard.Show();
                }
            }
        }

        /// <summary>
        /// Shutdown the entire App, which does not happen natively when other windows, like UndoChangesWindow, are still open.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            if (context.IsFiltered)
            {
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is FilterWindow)
                    {
                        w.Activate();
                        return;
                    }
                }
                FilterWindow newW = new FilterWindow(context);
                newW.Show();
            }
        }
    }

    public class MainWindowModel : INotifyPropertyChanged
    {
        public SQLManager manager = new SQLManager();

        /// <summary>
        /// The SQL connection parts to display.
        /// </summary>
        public string DisplayedConnection
        {
            get { return _displayedConnection; }
            set
            {
                _displayedConnection = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of Schemas to choose from. Display is schema name and Value is a list of tables and views inside that schema.
        /// </summary>
        public ObservableCollection<DisplayValuePair> ListSchemaObjects { get; } = new ObservableCollection<DisplayValuePair>();

        /// <summary>
        /// The selected item from the Schema ComboBox. Bind the Table ComboBox.ItemsSource to this Value, which is the list of objects belonging to that schema.
        /// I would rather bind TableComboBox.ItemsSource to this instead of SchemaComboBox.SelectedValue.Value just to keep the View as anonymous as possible without named controls.
        /// </summary>
        public DisplayValuePair SelectedSchema
        {
            get { return _selectedSchema; }
            set
            {
                _selectedSchema = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The ItemsSource of the Table ComboBox is bound to SelectedSchema.Value (the Schema ComboBox SelectedValue). This is the table selected from the Table ComboBox.
        /// </summary>
        public DisplayValuePair SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                _selectedTable = value;
                if (value == null)
                    PrimaryTable = null;
                else
                    PrimaryTable = new BetterDataTable(manager, (int)SelectedTable.Value, true);
                RaisePropertyChanged();
            }
        }
        
        /// <summary>
        /// The data from the selected table.
        /// </summary>
        public BetterDataTable PrimaryTable
        {
            get { return _primaryTable; }
            set
            {
                // Avoid memory leaks.
                if (PrimaryTable != null)
                    PrimaryTable.DataChanged -= Table_DataChanged;
                _primaryTable = value;
                RaisePropertyChanged();
                // Add listener for database changes for a consolidated list of changes for the user to browse.
                PrimaryTable.DataChanged += Table_DataChanged;
            }
        }

        /// <summary>
        /// Are DataGrids readonly? Bind to DataGrid.IsReadonly.
        /// </summary>
        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                _isEditable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Collection of commands used to change database data. Keep the changes from all tables in one spot to make this easier on the UI.
        /// </summary>
        public ObservableCollection<Command> UndoCommands { get; } = new ObservableCollection<Command>();

        /// <summary>
        /// Collection of filter conditions to apply to the tables. Filters are performed at the SQL level rather than using a WPF collection filter because the latter can eat up a lot of system memory.
        /// </summary>
        public ObservableCollection<FilterCondition> FilterConditions { get; set; } = new ObservableCollection<FilterCondition>();

        /// <summary>
        /// Give user the option to point and click filters (FilterConditions), or write out their own SQL script here.
        /// </summary>
        public string AdvancedFilterString
        {
            get { return _advancedFilterString; }
            set
            {
                _advancedFilterString = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// True to use the AdvancedFilterString to filter the PrimaryTable. False to use the FilterConditions collection.
        /// </summary>
        public bool UseAdvancedFilter
        {
            get { return _useAdvancedFilter; }
            set
            {
                _useAdvancedFilter = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Calls FilterTable().
        /// </summary>
        public bool IsFiltered
        {
            get { return _isFiltered; }
            set
            {
                _isFiltered = value;
                RaisePropertyChanged();
                FilterTable();
            }
        }

        /// <summary>
        /// Notify user that they have changed the filters without applying them to the tables.
        /// </summary>
        public bool HasUnsavedFilter
        {
            get { return _hasUnsavedFilter; }
            set
            {
                _hasUnsavedFilter = value;
                RaisePropertyChanged();
            }
        }

        public bool LayoutHorizontal
        {
            get { return _layoutHorizontal; }
            set
            {
                _layoutHorizontal = value;
                RaisePropertyChanged();
            }
        }

        public bool FormatShortTime
        {
            get { return _formatShortTime; }
            set
            {
                _formatShortTime = value;
                RaisePropertyChanged();
            }
        }

        #region private variables for the Properties
        private string _displayedConnection = string.Empty;
        private DisplayValuePair _selectedSchema;
        private DisplayValuePair _selectedTable;
        private BetterDataTable _primaryTable;
        private bool _isEditable = false;
        private bool _isFiltered = false;
        private bool _layoutHorizontal = true;
        private bool _formatShortTime = true;
        private string _advancedFilterString = string.Empty;
        private bool _useAdvancedFilter = false;
        private bool _hasUnsavedFilter = false;
        #endregion

        /// <summary>
        /// Create a new connection string in SQLManager manager, and then build the navigation comboboxes.
        /// </summary>
        public void Connect()
        {
            ConnectionWindow newWindow = new ConnectionWindow(manager.builder, manager.credential);
            // No changes when user cancels out of the ConnectionWindow.
            if (newWindow.ShowDialog() != true)
                return;
            // The connection string from the ConnectionWindow should be valid at this point. Otherwise, the user could not view and select an InitialCatalog, and cannot click OK without an InitialCatalog selected.
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder() { DataSource = newWindow.ServerName, InitialCatalog = newWindow.Database };
            if (newWindow.TrustedConnection)
                manager.Connect(newWindow.ServerName, newWindow.Database);
            else
                manager.Connect(newWindow.ServerName, newWindow.Database, newWindow.UserName, newWindow.Password);
            DisplayedConnection = newWindow.Database;
            ListSchemaObjects.Clear();
            // Only get the schemas that the user can read at least one table or triggered view.
            string query =
                "SELECT " +
                "    schemas.schema_id, schemas.name " +
                "FROM sys.schemas " +
                "INNER JOIN sys.tables " +
                "    ON schemas.schema_id = tables.schema_id " +
                "UNION SELECT " +
                "    schemas.schema_id, schemas.name " +
                "FROM sys.schemas " +
                "INNER JOIN sys.views " +
                "    ON schemas.schema_id = views.schema_id " +
                "INNER JOIN sys.triggers " +
                "    ON views.object_id = triggers.parent_id";
            DataTable dtSchemas = manager.GetDatabaseTable(query);
            foreach (DataRow row in dtSchemas.Rows)
            {
                // Get the tables and views with triggers.
                // Includes a blank row between tables with dependencies and those without to demark where a Separator goes.
                // Tables need a primary key. Views with triggers are assumed to contain a primary key, so I can update on all columns matching.
                long schemaID = row.Field<int>("schema_id");
                string name = row.Field<string>("name");
                query =
                    "SELECT name, object_id FROM ( " +
                    "   SELECT " +
                    "       name = COALESCE(extended_properties.value, tables.name), " +
                    "       tables.object_id,  " +
                    "       IsReferenced = CASE WHEN referenced_object_id IS NULL THEN 0 ELSE 1 END " +
                    "   FROM sys.tables " +
                    "   INNER JOIN(SELECT DISTINCT object_id FROM sys.indexes WHERE is_primary_key = 1) AS pks " +
                    "      ON tables.object_id = pks.object_id " +
                    "   LEFT JOIN sys.foreign_keys " +
                    "       ON tables.object_id = foreign_keys.referenced_object_id " +
                    "   LEFT JOIN sys.extended_properties " +
                    "       ON tables.object_id = extended_properties.major_id " +
                    "       AND minor_id = 0  AND extended_properties.name = 'View_DisplayName' " +
                    "   WHERE tables.schema_id = {0} " +
                    "   UNION SELECT " +
                    "       name = COALESCE(extended_properties.value, views.name), " +
                    "       views.object_id,  " +
                    "       IsReferenced = 1 " +
                    "   FROM sys.views " +
                    "   LEFT JOIN sys.extended_properties " +
                    "       ON views.object_id = extended_properties.major_id " +
                    "       AND minor_id = 0  AND extended_properties.name = 'View_DisplayName' " +
                    "   INNER JOIN sys.triggers " +
                    "       ON views.object_id = triggers.parent_id" +
                    "   WHERE views.schema_id = {0} " +
                    "   UNION SELECT NULL, NULL, 0 " +
                    ") AS A ORDER BY IsReferenced DESC, name ASC";
                ObservableCollection<DisplayValuePair> list = manager.GetObservableCollection("name", "object_id", query, schemaID);
                ListSchemaObjects.Add(new DisplayValuePair(name, list));
            }
        }

        /// <summary>
        /// Add a command representing a database change to a collection in the main DataContext.
        /// </summary>
        private void Table_DataChanged(Command command)
        {
            UndoCommands.Add(command);
        }

        /// <summary>
        /// Filter table through SQL rather than using WPF collection filters because it can be a lot of data to store in memory.
        /// </summary>
        private void FilterTable()
        {
            if (IsFiltered)
            {
                if (UseAdvancedFilter)
                {
                    PrimaryTable.FillTable(AdvancedFilterString);
                }
                else
                {
                    IEnumerable<IGrouping<BetterDataTable, FilterCondition>> groups = FilterConditions.GroupBy(x => x.Table);
                    List<string> parameters = new List<string>();
                    List<string> query = new List<string>();
                    foreach (IGrouping<BetterDataTable, FilterCondition> group in groups)
                    {
                        List<string> tableWheres = new List<string>();
                        foreach (FilterCondition item in group)
                        {
                            List<string> l = new List<string>();
                            // Split values by commas not surrounded by double-quotes. Escape double-quote with 2 double-quotes. Fails with unbalanced quotes (goes to end) and skips values outside quotes but inside commas.
                            // match.Value and match.Groups[0] return the entire match, including the non-capturing groups. Have to use match.Groups[1].Value.
                            MatchCollection match = Regex.Matches(item.Value, "(?:^|,\\s*)((?:\")(?:[^\"]+|\"\")*\"|[^,]*)");
                            if (item.Evaluator == FilterCondition.EvaluatorType.Between)
                            {
                                l.Add($"{item.Table.TableName}.{item.ColumnName} BETWEEN {{{parameters.Count}}} AND {{{parameters.Count + 1}}}");
                                parameters.Add(match[0].Groups[1].Value.Trim());
                                parameters.Add(match[match.Count - 1].Groups[1].Value.Trim());
                            }
                            else
                            {
                                foreach (Match m in match)
                                {
                                    string s = m.Groups[1].Value.Trim();
                                    if (string.IsNullOrWhiteSpace(s))
                                    {
                                        if (item.Evaluator == FilterCondition.EvaluatorType.Equals || item.Evaluator == FilterCondition.EvaluatorType.Like)
                                            l.Add($"{item.Table.TableName}.{item.ColumnName} IS NULL");
                                        else if (item.Evaluator == FilterCondition.EvaluatorType.NotEquals || item.Evaluator == FilterCondition.EvaluatorType.NotLike)
                                            l.Add($"{item.Table.TableName}.{item.ColumnName} IS NOT NULL");
                                    }
                                    else
                                    {
                                        switch (item.Evaluator)
                                        {
                                            case FilterCondition.EvaluatorType.Equals:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} = {{{parameters.Count}}}");
                                                break;
                                            case FilterCondition.EvaluatorType.NotEquals:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} != {{{parameters.Count}}}");
                                                break;
                                            case FilterCondition.EvaluatorType.Like:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} LIKE {{{parameters.Count}}}");
                                                s = $"%{s}%";
                                                break;
                                            case FilterCondition.EvaluatorType.NotLike:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} NOT LIKE {{{parameters.Count}}}");
                                                s = $"%{s}%";
                                                break;
                                            case FilterCondition.EvaluatorType.LT:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} < {{{parameters.Count}}}");
                                                break;
                                            case FilterCondition.EvaluatorType.LTE:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} <= {{{parameters.Count}}}");
                                                break;
                                            case FilterCondition.EvaluatorType.GTE:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} >= {{{parameters.Count}}}");
                                                break;
                                            case FilterCondition.EvaluatorType.GT:
                                                l.Add($"{item.Table.TableName}.{item.ColumnName} > {{{parameters.Count}}}");
                                                break;
                                        }
                                        parameters.Add(s);
                                    }
                                }
                            }
                            string condition = " AND ";
                            if (item.Evaluator == FilterCondition.EvaluatorType.Equals || item.Evaluator == FilterCondition.EvaluatorType.Like)
                                condition = " OR ";
                            tableWheres.Add($"{(tableWheres.Count == 0 ? string.Empty : item.Operator)} ({string.Join(condition, l.ToArray())})");
                        }
                        if (group.Key == PrimaryTable)
                        {
                            query.Insert(0, string.Join(" ", tableWheres.ToArray()));
                        }
                        else
                        {
                            foreach (KeyValuePair<ColumnInfo,ColumnInfo> pair in group.Key.RelevantForeignKeys)
                            {
                                tableWheres.Add($"AND {pair.Key.ColumnName} = {PrimaryTable.TableName}.{pair.Value.ColumnName}");
                            }
                            query.Add($"EXISTS (SELECT 1 FROM {group.Key.TableName} WHERE {string.Join(" ", tableWheres.ToArray())})");
                        }
                    }
                    PrimaryTable.FillTable(string.Join(" AND ", query.ToArray()), parameters.ToArray());
                }
            }
            else
                PrimaryTable.FillTable();
            HasUnsavedFilter = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SQLManager
    {
        private SqlConnection connection = new SqlConnection();

        public SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder() { IntegratedSecurity = true };

        /// <summary>
        /// Keep credentials separate for security.
        /// </summary>
        public SqlCredential credential;

        public SQLManager()
        {
            Connect(string.Empty, string.Empty);
        }
        
        /// <summary>
        /// Connect with Integrated Security.
        /// </summary>
        public void Connect (string dataSource, string initialCatalog)
        {
            builder.DataSource = dataSource;
            builder.InitialCatalog = initialCatalog;
            builder.IntegratedSecurity = true;
            connection = new SqlConnection(builder.ConnectionString);
        }

        /// <summary>
        /// Connect with a SQL login.
        /// </summary>
        public void Connect(string dataSource, string initialCatalog, string userName, System.Security.SecureString password)
        {
            builder.DataSource = dataSource;
            builder.InitialCatalog = initialCatalog;
            builder.IntegratedSecurity = false;
            credential = new SqlCredential(userName, password);
            connection = new SqlConnection(builder.ConnectionString, credential);
        }

        /// <summary>
        /// Submit a SQL script to the database without a return. This returns true when successful.
        /// </summary>
        public bool ExecuteNonQuery(string sql, params object[] args)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i] == null || string.IsNullOrEmpty(args[i].ToString()) ? DBNull.Value : args[i];
                command.Parameters.AddWithValue($"@param{i}", value);
                args[i] = $"@param{i}";
            }
            command.CommandText = string.Format(sql, args);
            connection.Open();
            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not get the database value. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Get a scalar value from the database.
        /// </summary>
        /// <param name="sql">The parameterized sql string. Use {#} for parameters as you would in string.Format().</param>
        /// <param name="args">The objects to send as parameters. The order corresponds to the number assigned in to the parameters inside the SQL string.</param>
        public object GetDatabaseScalar(string sql, params object[] args)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i] == null || string.IsNullOrEmpty(args[i].ToString()) ? DBNull.Value : args[i];
                command.Parameters.AddWithValue($"@param{i}", value);
                args[i] = $"@param{i}";
            }
            command.CommandText = string.Format(sql, args);
            connection.Open();
            try
            {
                return command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not get the database value. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Get a DataTable value from the database.
        /// </summary>
        /// <param name="sql">The parameterized sql string. Use {#} for parameters as you would in string.Format().</param>
        /// <param name="args">The objects to send as parameters. The order corresponds to the number assigned in to the parameters inside the SQL string.</param>
        public DataTable GetDatabaseTable(string sql, params object[] args)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i] == null || string.IsNullOrEmpty(args[i].ToString()) ? DBNull.Value : args[i];
                command.Parameters.AddWithValue($"@param{i}", value);
                args[i] = $"@param{i}";
            }
            command.CommandText = string.Format(sql, args);
            DataTable result = new DataTable();
            connection.Open();
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not access the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Just like GetDatabaseTable, only with a bool result, and a DataTable and an Exception out.
        /// </summary>
        public bool TryGetDatabaseTable(string sql, out DataTable table, out Exception exception, params object[] args)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i] == null || string.IsNullOrEmpty(args[i].ToString()) ? DBNull.Value : args[i];
                command.Parameters.AddWithValue($"@param{i}", value);
                args[i] = $"@param{i}";
            }
            command.CommandText = string.Format(sql, args);
            table = new DataTable();
            exception = null;
            connection.Open();
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(table);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
            finally
            {
                connection.Close();
            }
            return true;
        }

        /// <summary>
        /// Get an ObservableCollection representation of the query results. DisplayValuePair has a Display, Value, and Filter property. Those columns can be identified in the parameters, named as such in the columns, or just default to the first column for each property.
        /// </summary>
        public ObservableCollection<DisplayValuePair> GetObservableCollection(string display, string value, string sql, params object[] args)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            for (int i = 0; i < args.Length; i++)
            {
                object v = args[i] == null || string.IsNullOrEmpty(args[i].ToString()) ? DBNull.Value : args[i];
                command.Parameters.AddWithValue($"@param{i}", v);
                args[i] = $"@param{i}";
            }
            command.CommandText = string.Format(sql, args);
            DataTable dt = new DataTable();
            connection.Open();
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not get the database value. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
            ObservableCollection<DisplayValuePair> result = new ObservableCollection<DisplayValuePair>();
            if (dt.Columns.Count > 0)
            {
                DataColumn colDisplay = dt.Columns.Contains(display) ? dt.Columns[display] : dt.Columns.Contains("Display") ? dt.Columns["Display"] : dt.Columns[0];
                DataColumn colValue = dt.Columns.Contains(value) ? dt.Columns[value] : dt.Columns.Contains("Value") ? dt.Columns["Value"] : dt.Columns[0];
                foreach (DataRow row in dt.Rows)
                {
                    DisplayValuePair pair = new DisplayValuePair(row.Field<string>(colDisplay), row[colValue]);
                    result.Add(pair);
                }
            }
            return result;
        }
        
    }

    public class DisplayValuePair
    {
        public string Display { get; } = string.Empty;
        public object Value { get; }
        public object OtherValue { get; set; }

        public DisplayValuePair(string display)
        {
            Display = display;
            Value = display;
        }
        public DisplayValuePair(string display, object value)
        {
            Display = display;
            Value = value;
        }
    }

    public class FilterCondition : INotifyPropertyChanged
    {
        public enum EvaluatorType { Equals, NotEquals, Like, NotLike, GT, GTE, LT, LTE, Between };

        public string Operator { get; set; } = "and";

        public BetterDataTable Table
        {
            get { return _table; }
            set
            {
                _table = value;
                RaisePropertyChanged();
            }
        }

        private BetterDataTable _table;

        public string ColumnName { get; set; } = string.Empty;

        public EvaluatorType Evaluator { get; set; } = EvaluatorType.Equals;

        public string Value { get; set; } = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
