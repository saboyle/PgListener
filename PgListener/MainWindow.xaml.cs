using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using Npgsql;


namespace PgListener
{
    class BridgeEvent
    {
        public string Msg { get; set; }
        public DateTime Created { get; set; }

        public BridgeEvent(string msg)
        {
            this.Msg = msg;
            this.Created = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Created, Msg);
        }
    };
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> ocEvents;
        private string host = "localhost";
        private string port = "5432";
        private string database = "postgres";
        private string username = "postgres";
        private string password = "password";
        private string channel = "fixtures";
        private Thread pgThread;

        public MainWindow()
        {
            Console.WriteLine("Main Window Invoked");
            InitializeComponent();
            ocEvents = new ObservableCollection<string>();
            LstEvents.ItemsSource = ocEvents;
        }

        public void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window Loaded");
            pgThread = new Thread(PgListener);
            pgThread.IsBackground = true;
            pgThread.Start();
        }

        public void PgListener()
        {
            /**************************************************************************************************
             * Setup a listening connection to Postgres and update the listbox once any messages are received *
             **************************************************************************************************/
            Console.WriteLine("Listener Invoked");
            // Pg Endpoint
            string connectionString = String.Format("Host={0}; Port={1}; Database={2}; Username={3}; Password={4};", host, port, database, username, password);
            var pgConn = new NpgsqlConnection(connectionString);
            pgConn.Open();

            // Setup LISTEN to pg emitted events.
            using (var cmd = new NpgsqlCommand(String.Format("LISTEN {0}", channel), pgConn))
            {
                cmd.ExecuteNonQuery();
            }

            // Configure emit to target endpoint
            pgConn.Notification += (o, e) =>
            {
                Console.WriteLine("{0}: {1}", DateTime.Now.ToString(), e.AdditionalInformation);
                this.Dispatcher.Invoke(() => ocEvents.Insert(0, new BridgeEvent(e.AdditionalInformation).ToString()));
            };

            while (true)
            {
                pgConn.Wait();   // Thread will block here
            }
        }
    }
}
