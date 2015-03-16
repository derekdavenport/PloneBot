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

		static PloneUtils()
		{
			fiddler.BypassProxyOnLocal = false;
		}

		public static CookieContainer Cookies
		{
			get { return cookies; }
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
