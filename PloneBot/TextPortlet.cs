using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	class TextPortlet : Portlet
	{
		protected string footer, link;

		public TextPortlet(string id) : base(id)
		{

		}

		public TextPortlet(string id, PortletManager parent) : base(id, parent)
		{

		}

		public new void Upload()
		{

			NameValueCollection data = new NameValueCollection()
			{
				{ "form.header",       title },
				{ "form.text",         contents },
				{ "form.footer",       footer},
				{ "form.more_url",     link },
				{ "_authenticator",    authenticator }, //23be92da9d04d62b4af6f6952ac9f93affa2e2ac
				{ "form.actions.save", "Save" }
			};
			using (HttpWebResponse editResponse = PloneUtils.post(new Uri(uri, "edit"), data))
			{
				// TODO: check success
			}
		}
	}
}
