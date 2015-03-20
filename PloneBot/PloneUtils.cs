using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	static class PloneUtils
	{
		static CookieContainer cookies = new CookieContainer();

		static WebProxy fiddler = new WebProxy("localhost", 8888);

		
		enum Modes { Ignore, Attr, Content };

		static PloneUtils()
		{
			fiddler.BypassProxyOnLocal = false;
		}

		public static CookieContainer Cookies
		{
			get { return cookies; }
		}

		public static void HandleMessage(string body)
		{
			Console.WriteLine(body);
			StringReader bodyReader = new StringReader(body);
			string derekId = bodyReader.ReadLine();
			string user = bodyReader.ReadLine();
			string page = bodyReader.ReadLine();
			string portlet = bodyReader.ReadLine();
			string title = bodyReader.ReadLine();
			string footer = bodyReader.ReadLine();
			string link = bodyReader.ReadLine();
			if(derekId != "1760332")
			{
				Console.Error.WriteLine("permission denied");
			}
			else
			{
				Uri pageUri = new Uri(page + '/');
				Uri portletUri = new Uri(pageUri, "../../" + portlet + '/');
				Console.WriteLine(portletUri);

				Uri externalEditUri = new Uri(pageUri, "external_edit");
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(externalEditUri);
				request.Proxy = fiddler;
				request.CookieContainer = cookies;

				using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						var reader = new StreamReader(response.GetResponseStream());
						string line;
						//ignore first batch, next batch is attributes, next batch is content
						var mode = Modes.Ignore;
						var contentsBuffer = new StringBuilder();
						while ((line = reader.ReadLine()) != null)
						{
							// empty line signals a change
							if (line.Equals(""))
							{
								if (mode == Modes.Ignore)
								{
									mode = Modes.Attr;
									continue;
								}
								// double return?
								else if (mode == Modes.Attr)
								{
									mode = Modes.Content;
									continue;
								}
							}
							/*
							// read in the attributes
							if(mode == Modes.Attr) {
								// a continuation line
								if(line.StartsWith("  ")) {
									// plone inserts blank lines between each line, ignore them.
									if(!line.Equals("  ")) {
										//valBuf.Append("\n" + line.Substring(2));
									}
								}
								// a new line
								else {
									int colonIndex = line.IndexOf(": ");
									// a new entry
									if(colonIndex >= 1) {
										// add finished attr
										if(key.length() > 0)
										{
											attr(key, valBuf.toString());
										}
										// make new attr
										key = line.substring(0, colonIndex);
										valBuf = new StringBuffer(line.substring(colonIndex + 2, line.length()));
									}
									else {
										// should never get here
										Console.Error.WriteLine("error, not a continue or a new. what is this: " + line);
									}
								}
							}
							// page contents
							else */
							if (mode == Modes.Content)
							{
								contentsBuffer.AppendLine(line);
							}
						}
						string contents = contentsBuffer.ToString();


						// http://stage.louisville.edu/aging/home/++contextportlets++plone.leftcolumn/focal-1/edit
						NameValueCollection data = new NameValueCollection()
						{
							{ "form.header",       title },
							{ "form.text",         contents },
							{ "form.footer",       footer},
							{ "form.more_url",     link },
							{ "_authenticator",    "23be92da9d04d62b4af6f6952ac9f93affa2e2ac" },
							{ "form.actions.save", "Save" }
						};

						using (HttpWebResponse editResponse = post(new Uri(portletUri, "edit"), data))
						{
							
						}
					}
					else
					{
						Console.WriteLine("got response " + response.StatusDescription);
					}
				}
			}
		}

		public static HttpWebResponse post(Uri uri, NameValueCollection data)
		{
			// set up request
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.Proxy = fiddler;
			request.AllowAutoRedirect = false;
			request.CookieContainer = cookies;
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			// prepare data string
			StringBuilder builder = new StringBuilder();
			foreach (var entry in data.AllKeys.SelectMany(data.GetValues, (k, v) => new { key = k, value = v }))
			{
				builder.Append(WebUtility.UrlEncode(entry.key) + "=" + WebUtility.UrlEncode(entry.value != null ? entry.value : "") + "&");
			}
			builder.Length -= 1; // remove final &

			using (Stream dataStream = request.GetRequestStream())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
				dataStream.Write(bytes, 0, bytes.Length);
			}

			return (HttpWebResponse)request.GetResponse();
		}

		public static bool login(Uri siteUri, string username, string password)
		{
			bool success = false;
			NameValueCollection data = new NameValueCollection()
			{
				{ "__ac_name",      username },
				{ "__ac_password",  password },
				{ "form.submitted", "1" }
			};
			
			using(HttpWebResponse response = post(new Uri(siteUri, "login_form"), data))
			{
				success = cookies.GetCookies(siteUri).Count > 0;
				// need to save cookie to custom cookie store based on base path because Plone 4 overwrites with next login
				//WriteNVC(response.Headers);
			}

			return success;
		}

		static void WriteNVC(NameValueCollection nvc)
		{
				int loop1, loop2;

				// Put the names of all keys into a string array.
				String[] arr1 = nvc.AllKeys;
				for (loop1 = 0; loop1 < arr1.Length; loop1++)
				{
					Console.WriteLine(arr1[loop1]);
					// Get all values under this key.
					String[] arr2 = nvc.GetValues(arr1[loop1]);
					for (loop2 = 0; loop2 < arr2.Length; loop2++)
					{
						Console.WriteLine("\t" + arr2[loop2]);
					}
				}
		}
	}
}
