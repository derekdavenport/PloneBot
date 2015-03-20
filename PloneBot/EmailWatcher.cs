using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PloneBot
{
	class EmailWatcher
	{
		MainWindow window;

		ImapClient client;
		IMailFolder ploneBotFolder;
		//IList<IMessageSummary> messages;
		CancellationTokenSource done;
		NetworkCredential credentials;

		int messageIndex;

		AutoResetEvent resumeIdle = new AutoResetEvent(true);

		Dictionary<MessageSummary, UniqueId> uidMap;
		Dictionary<MessageSummary, uint> indexMap;

		public EmailWatcher(MainWindow window)
		{
			this.window = window;
		}

		// TODO: handle unwanted disconnects
		void Connect()
		{
			window.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { window.IMAPButton.IsEnabled = false; }));
			client = new ImapClient(new ProtocolLogger(Console.OpenStandardError()));
			//client.Connect("exchange.louisville.edu", 993, true);
			client.Connect(new Uri("imaps://exchange.louisville.edu"));
			client.AuthenticationMechanisms.Remove("XOAUTH2");
			//client.Authenticate("ad.louisville.edu\\" + UsernameBox.Text, PasswordBox.Password);
			client.Authenticate(credentials);
		}

		public void Start(object sender, DoWorkEventArgs e)
		{
			this.credentials = e.Argument as NetworkCredential;
			Connect();

			ploneBotFolder = client.GetFolder(client.PersonalNamespaces[0]).GetSubfolder("Cabinet").GetSubfolder("PloneBot");
			ploneBotFolder.Open(FolderAccess.ReadWrite);

			messageIndex = ploneBotFolder.Count;

			// TODO: do in order. And maybe test to see if later ones overwrite older ones and ignore older ones??
			var uids = ploneBotFolder.Search(SearchQuery.NotSeen);
			if (uids.Count > 0)
			{
				var summaries = ploneBotFolder.Fetch(uids, MessageSummaryItems.UniqueId);
				foreach (var summary in summaries)
				{
					var body = ploneBotFolder.GetMessage(summary.UniqueId.Value).BodyParts.OfType<TextPart>().FirstOrDefault().Text;
					MessageJob(summary.UniqueId.Value, body);

				}
			}

			ploneBotFolder.MessagesArrived += MessagesArrived;

			Console.WriteLine("Found {0} messages", messageIndex);

			Loop();
		}

		void MessagesArrived(object sender, MessagesArrivedEventArgs e)
		{
			// Note: the CountChanged event will fire when new messages arrive in the folder.
			var folder = (ImapFolder)sender;

			// New messages have arrived in the folder.
			Console.WriteLine("{0}: {1} new messages have arrived.", folder, e.Count);

			// stop idling and read email
			done.Cancel();
		}

		// TODO: would be best to also handle deleted messages in case someone is deleting them from another client. It would mess up our index

		void MessageJob(UniqueId uid, string body)
		{
			//var body = message.BodyParts.OfType<TextPart>().FirstOrDefault().ToString();
			Console.WriteLine("Do work on {0}", uid);
			Task task = Task.Factory.StartNew(() => { PloneUtils.HandleMessage(body); });
			task.ContinueWith(e => { JobFinished(uid, e); });
		}

		async void JobFinished(UniqueId uid, Task e)
		{
			switch(e.Status)
			{
				case TaskStatus.RanToCompletion:
					// stop idling to set 
					resumeIdle.Reset();
					if (done != null)
					{
						// TODO: this requests a cancel. It may not complete before the next command
						done.Cancel();
					}
					await ploneBotFolder.SetFlagsAsync(new List<UniqueId> { uid }, MessageFlags.Seen, true);//, done.Token);
					
					Console.WriteLine("Finished {0}", uid);
					resumeIdle.Set();
					break;
				case TaskStatus.Faulted:
					Console.WriteLine(e.Exception.InnerException.Message);
					break;
				default:
					break;
			}
		}

		void Loop()
		{
			try
			{
				while (true)
				{
					/*
					ploneBotFolder.CountChanged += (s, f) =>
					{
						Console.WriteLine(ploneBotFolder.Count);
						ploneBotFolder.Status(StatusItems.Unread);
						var message = ploneBotFolder.GetMessage(0);
						var body = message.BodyParts.OfType<TextPart>().FirstOrDefault();
						Console.WriteLine(body.Text);
					};
					*/

					// Idle until new message signals a cancel
					Idle();

					if (ploneBotFolder.Count > messageIndex)
					{
						Console.WriteLine("The new messages that arrived during IDLE are:");
						//foreach (var message in ploneBotFolder.Fetch(messageIndex, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
						//while (messageIndex < ploneBotFolder.Count )
						{
							var summaries = ploneBotFolder.Fetch(messageIndex, -1, MessageSummaryItems.Body | MessageSummaryItems.UniqueId);
							foreach (var summary in summaries)
							{
								var body = ploneBotFolder.GetMessage(summary.UniqueId.Value).BodyParts.OfType<TextPart>().FirstOrDefault().Text;
								MessageJob(summary.UniqueId.Value, body);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				// TODO: handle disconnect?
			}
		}


		void Idle()
		{
			resumeIdle.WaitOne();
			using (done = new CancellationTokenSource())
			{
				var thread = new Thread(IdleLoop);

				thread.Start(new IdleState(client, done.Token));
				// hopefully this waits until cancel is done
				thread.Join();
				done.Dispose();
				done = null;
			}
		}

		public void Disconnect(object sender, RunWorkerCompletedEventArgs e)
		{
			client.Disconnect(true);
			window.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { window.IMAPButton.IsEnabled = true; }));
		}

		static void IdleLoop(object state)
		{
			var idle = (IdleState)state;

			lock (idle.Client.SyncRoot)
			{
				// Note: since the IMAP server will drop the connection after 30 minutes, we must loop sending IDLE commands that
				// last ~29 minutes or until the user has requested that they do not want to IDLE anymore.
				//
				// For GMail, we use a 9 minute interval because they do not seem to keep the connection alive for more than ~10 minutes.
				while (!idle.IsCancellationRequested)
				{
					// Note: Starting with .NET 4.5, you can make this simpler by using the CancellationTokenSource .ctor that
					// takes a TimeSpan argument, thus eliminating the need to create a timer.
					using (var timeout = new CancellationTokenSource(new TimeSpan(0, 4, 0)))
					{
						try
						{
							// We set the timeout source so that if the idle.DoneToken is cancelled, it can cancel the timeout
							idle.SetTimeoutSource(timeout);

							if (idle.Client.Capabilities.HasFlag(ImapCapabilities.Idle))
							{
								// The Idle() method will not return until the timeout has elapsed or idle.CancellationToken is cancelled
								idle.Client.Idle(timeout.Token, idle.CancellationToken);
							}
							else
							{
								// The IMAP server does not support IDLE, so send a NOOP command instead
								idle.Client.NoOp(idle.CancellationToken);

								// Wait for the timeout to elapse or the cancellation token to be cancelled
								WaitHandle.WaitAny(new[] { timeout.Token.WaitHandle, idle.CancellationToken.WaitHandle });
							}
						}
						catch (OperationCanceledException)
						{
							// This means that idle.CancellationToken was cancelled, not the DoneToken nor the timeout.
							break;
						}
						catch (ImapProtocolException)
						{
							// The IMAP server sent garbage in a response and the ImapClient was unable to deal with it.
							// This should never happen in practice, but it's probably still a good idea to handle it.
							//
							// Note: an ImapProtocolException almost always results in the ImapClient getting disconnected.
							break;
						}
						catch (ImapCommandException)
						{
							// The IMAP server responded with "NO" or "BAD" to either the IDLE command or the NOOP command.
							// This should never happen... but again, we're catching it for the sake of completeness.
							break;
						}
						finally
						{
							// We're about to Dispose() the timeout source, so set it to null.
							idle.SetTimeoutSource(null);
						}
					}
				}
			}
		}

		class IdleState
		{
			readonly object mutex = new object();
			CancellationTokenSource timeout;

			/// <summary>
			/// Gets the cancellation token.
			/// </summary>
			/// <remarks>
			/// <para>The cancellation token is the brute-force approach to cancelling the IDLE and/or NOOP command.</para>
			/// <para>Using the cancellation token will typically drop the connection to the server and so should
			/// not be used unless the client is in the process of shutting down or otherwise needs to
			/// immediately abort communication with the server.</para>
			/// </remarks>
			/// <value>The cancellation token.</value>
			public CancellationToken CancellationToken { get; private set; }

			/// <summary>
			/// Gets the done token.
			/// </summary>
			/// <remarks>
			/// <para>The done token tells the <see cref="Program.IdleLoop"/> that the user has requested to end the loop.</para>
			/// <para>When the done token is cancelled, the <see cref="Program.IdleLoop"/> will gracefully come to an end by
			/// cancelling the timeout and then breaking out of the loop.</para>
			/// </remarks>
			/// <value>The done token.</value>
			public CancellationToken DoneToken { get; private set; }

			/// <summary>
			/// Gets the IMAP client.
			/// </summary>
			/// <value>The IMAP client.</value>
			public ImapClient Client { get; private set; }

			/// <summary>
			/// Checks whether or not either of the CancellationToken's have been cancelled.
			/// </summary>
			/// <value><c>true</c> if cancellation was requested; otherwise, <c>false</c>.</value>
			public bool IsCancellationRequested
			{
				get
				{
					return CancellationToken.IsCancellationRequested || DoneToken.IsCancellationRequested;
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="IdleState"/> class.
			/// </summary>
			/// <param name="client">The IMAP client.</param>
			/// <param name="doneToken">The user-controlled 'done' token.</param>
			/// <param name="cancellationToken">The brute-force cancellation token.</param>
			public IdleState(ImapClient client, CancellationToken doneToken, CancellationToken cancellationToken = default (CancellationToken))
			{
				CancellationToken = cancellationToken;
				DoneToken = doneToken;
				Client = client;

				// When the user hits a key, end the current timeout as well
				doneToken.Register(CancelTimeout);
			}

			/// <summary>
			/// Cancels the timeout token source, forcing ImapClient.Idle() to gracefully exit.
			/// </summary>
			void CancelTimeout()
			{
				lock (mutex)
				{
					if (timeout != null)
						timeout.Cancel();
				}
			}

			/// <summary>
			/// Sets the timeout source.
			/// </summary>
			/// <param name="source">The timeout source.</param>
			public void SetTimeoutSource(CancellationTokenSource source)
			{
				lock (mutex)
				{
					timeout = source;

					if (timeout != null && IsCancellationRequested)
						timeout.Cancel();
				}
			}
		}
	}
}
