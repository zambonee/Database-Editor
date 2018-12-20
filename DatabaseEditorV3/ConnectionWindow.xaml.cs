using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseEditorV3
{
    /// <summary>
    /// User inputs a DataSource(ServerName), TrustedConnection- Yes/No, UserID (UserName), and Password. Then, ConnectionWindow tries to connect to the DataSource to get a list of Catalogs (Databases) that the connection can access.
    /// Once the user selects a Database given the other connection parameters, the MainWindow can now create a connection string with an InitialCatalog.
    /// Build and test the connection string separate from the MainWindow's connection just in case the user cancels out of this window.
    /// </summary>
    public partial class ConnectionWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Server name or DataSource.
        /// </summary>
        public string ServerName
        {
            get { return _serverName; }
            set
            {
                _serverName = value;
                UpdateListDatabase();
            }
        }

        /// <summary>
        /// SQL Server integrated security. Do not use SQLStringBuilder.IntegratedSecurity- it is more convoluted than builder.TrustedConnection.
        /// </summary>
        public bool TrustedConnection
        {
            get { return _trustedConnection; }
            set
            {
                _trustedConnection = value;
                RaisePropertyChanged("NeedPassword");
                UpdateListDatabase();
            }
        }
        
        /// <summary>
        /// The opposite of TrustedConnection to enable/disable the username and password entry.
        /// </summary>
        public bool NeedPassword
        {
            get { return !TrustedConnection; }
        }
        
        /// <summary>
        /// Do not need a UserName when TrustedConnection == true.
        /// </summary>
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                UpdateListDatabase();
            }
        }
        
        /// <summary>
        /// Use SecureString for security. Although, if the system memory is compromised, we probably have bigger problems than unauthorized SQL access.
        /// </summary>
        public System.Security.SecureString Password = new System.Security.SecureString();
        
        /// <summary>
        /// List of databases that can be connected to given the current SQL connection parameters.
        /// </summary>
        public ObservableCollection<string> ListDatabase { get; } = new ObservableCollection<string>();
        
        /// <summary>
        /// The selected database name (or Initial Catalog). User can only save the connection string when this is not empty.
        /// </summary>
        public string Database
        {
            get { return _database; }
            set
            {
                _database = value;
                RaisePropertyChanged();
                if (string.IsNullOrWhiteSpace(Database))
                    IsValid = false;
                else
                    IsValid = true;
            }
        }
        
        /// <summary>
        /// True when the user can save the connection parameters. Sets when Database changes because when the user selects a database name, we know that the connection string is valid and the Initial Catalog has a value for the connection string.
        /// </summary>
        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                _isValid = value;
                RaisePropertyChanged();
            }
        }

        private string _serverName = string.Empty;
        private bool _trustedConnection = true;
        private string _userName = string.Empty;
        private string _database = string.Empty;
        private SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
        private bool _isValid = false;

        public ConnectionWindow()
        {
            DataContext = this;
            InitializeComponent();
            // Nothing has focus, so move it to the first focusable element on load.
            Loaded += (sender, e) => MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
        }

        /// <summary>
        /// Set ServerName, TrustedConnection, UserName, and Database to an existing connection string.
        /// </summary>
        public ConnectionWindow(SqlConnectionStringBuilder builder, SqlCredential credential)
        {
            ServerName = builder.DataSource;
            TrustedConnection = builder.IntegratedSecurity;
            if (credential != null)
            {
                UserName = credential.UserId;
                Password = credential.Password;
            }
            Database = builder.InitialCatalog;
            DataContext = this;
            InitializeComponent();
            // Nothing has focus, so move it to the first focusable element on load.
            Loaded += (sender, e) => MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
        }

        /// <summary>
        /// Dispose of password SecureString for security.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        { 
            Password.Dispose();
        }

        /// <summary>
        /// Cannot bind a PasswordBox.Password or .SecurePassword. Rather than inheriting a PasswordBox to modify its behavior, might as well roll with it out of the box.
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = (sender as PasswordBox).SecurePassword;
            UpdateListDatabase();
        }

        /// <summary>
        /// Setting the IsDefault in XAML does not set the DialogResult- have to do that here.
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Clear and fill the list of database names given the server name, authentication, user name, and password. 
        /// </summary>
        private void UpdateListDatabase()
        {
            Database = string.Empty;
            ListDatabase.Clear();
            builder.DataSource = ServerName;
            SqlConnection connection;
            if (TrustedConnection)
            {
                builder.IntegratedSecurity = true;
                connection = new SqlConnection(builder.ConnectionString);
            }
            else
            {
                builder.IntegratedSecurity = false;
                // Cannot pass Password to SqlCredential unless it is readonly.
                Password.MakeReadOnly();
                connection = new SqlConnection(builder.ConnectionString, new SqlCredential(UserName, Password));
            }
            try
            {
                connection.Open();
                // Get the database names that user has access; database_id <= 4 are SQL management tables.
                SqlCommand command = new SqlCommand(
                    "SET NOCOUNT ON " +
                    "DECLARE @tables TABLE(TableName nvarchar(MAX)) " +
                    "INSERT INTO @tables(TableName) " +
                    "   EXEC sp_msforeachdb 'SELECT DISTINCT ''?'' FROM [?].sys.tables WHERE EXISTS(SELECT 1 FROM sys.databases WHERE name = ''?'' AND database_id > 4)' " +
                    "SET NOCOUNT OFF " +
                    "SELECT * FROM @tables "
                    , connection);
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(command))
                {
                    da.Fill(dt);
                }
                foreach (DataRow row in dt.Rows)
                {
                    ListDatabase.Add(row.Field<string>(0));
                }
            }
            catch { }
            finally { connection.Close(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
