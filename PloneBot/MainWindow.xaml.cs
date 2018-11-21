using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PloneBot
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//static AutoResetEvent reconnectEvent = new AutoResetEvent(false);

		EmailWatcher emailWatcher;
		private readonly BackgroundWorker emailWorker = new BackgroundWorker();

		public MainWindow()
		{
			InitializeComponent();
			//reconnectEvent = new AutoResetEvent(false);

			emailWatcher = new EmailWatcher(this);
			emailWorker.DoWork += emailWatcher.Start;
			emailWorker.RunWorkerCompleted += emailWatcher.Disconnect;

		}

		private void RunButton_Click(object sender, RoutedEventArgs e)
		{
			String host = "louisville.edu";
			String root = UrlBox.Text;
			Uri siteUri       = new Uri("http://stage."  + host + "/" + root + "/");
			Uri secureSiteUri = new Uri("https://stage." + host + "/" + root + "/");
			Console.WriteLine("login: " + PloneUtils.login(secureSiteUri, UsernameBox.Text, PasswordBox.Password));

			// test login
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(siteUri, "@@manage-portlets"));
			request.CookieContainer = PloneUtils.Cookies;
			using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			{
				Console.WriteLine(response.StatusDescription);
			}

			Folder rootFolder = new Folder(siteUri);
			Page home = new Page("home", rootFolder);
			home.GetPortlets();

			rootFolder.SetupPortletsFolder();
		}

		private void IMAPButton_Click(object sender, RoutedEventArgs e)
		{
			emailWorker.RunWorkerAsync(new NetworkCredential(UsernameBox.Text, PasswordBox.Password));
		}


		private void IMAPButtonIsEnabled(bool isEnabled)
		{
			IMAPButton.IsEnabled = isEnabled;
		}

		/*
		void FolderCheck()
		{
			ploneBotFolder.Open(FolderAccess.ReadOnly);
			var uids = ploneBotFolder.Search(SearchQuery.NotSeen);
			foreach (var uid in uids)
			{
				var message = ploneBotFolder.GetMessage(uid);
				var body = message.BodyParts.OfType<TextPart>().FirstOrDefault();
				Console.WriteLine(body.Text);
			}
			Console.WriteLine("checked");
		}
		 * */


		void NewMessage(object sender, MessagesArrivedEventArgs e)
		{
			var folder = (ImapFolder)sender;
			Console.WriteLine("{0}: {1} new messages have arrived.", folder, e.Count);
		}

		/*
		void IMAP_Connect()
		{
			if (client != null)
				client.Dispose();

			client = new ImapClient("exchange.louisville.edu", 993, "ad.louisville.edu\\" + UsernameBox.Text, PasswordBox.Password, AuthMethod.Login, true);
			//client.DefaultMailbox = "Cabinet/HSC/Medicine/site notifications";
			Console.WriteLine("We are connected!");
			MailboxInfo info = client.GetMailboxInfo();
			Console.WriteLine("Unread: " + info.Unread);

			if (!client.Supports("IDLE"))
				throw new Exception("This server does not support IMAP IDLE");


			client.IdleError += client_IdleError;
			client.NewMessage += new EventHandler<IdleMessageEventArgs>(client_NewMessage);
		}

		void client_IdleError(object sender, IdleErrorEventArgs e)
		{
			Console.Write("An error occurred while idling: ");
			Console.WriteLine(e.Exception.Message);
			// TODO: not thread safe
			IMAP_Connect();
		}

		static void client_NewMessage(object sender, IdleMessageEventArgs e)
		{
			Console.WriteLine("New message!");

			MailMessage m = e.Client.GetMessage(e.MessageUID, FetchOptions.HeadersOnly);
			Console.WriteLine(m.Subject);
		}
		*/
		private void Button_Click(object sender, RoutedEventArgs e)
		{/*
			if(client != null)
			{
				Console.WriteLine(client.Authed);
				MailboxInfo info = client.GetMailboxInfo();
				Console.WriteLine("Unread: " + info.Unread);
			}
		  * */
		}

		private void CheckButton_Click(object sender, RoutedEventArgs e)
		{
			//FolderCheck();
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			// TODO: does this work?
			//emailWorker.CancelAsync();
		}
	}
}
