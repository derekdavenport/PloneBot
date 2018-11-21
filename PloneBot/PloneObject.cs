using HtmlAgilityPack;
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
	class PloneObject
	{
		protected PloneObject parent;
		protected Uri uri;
		protected NameValueCollection attributes;
		internal string id, title, description, contents, authenticator;
		
		protected enum Modes { Ignore, Attr, Content };

		protected readonly static char[] slash = new char[] { '/' };

		public PloneObject(string id)
		{
			attributes = new NameValueCollection();
			this.id = id;
			parent = null;
		}

		public PloneObject(string id, PloneObject parent) : this(id)
		{
			this.parent = parent;
			this.uri = new Uri(parent.Uri, id + '/');
		}

		public PloneObject(Uri uri) : this((uri.AbsolutePath.Split(slash, StringSplitOptions.RemoveEmptyEntries).Last()))
		{
			this.uri = uri;
			// TODO: lookup parent by Uri
		}

		public PloneObject(PloneUtils.Message message) : this(message.uri)
		{

		}

		public string Id
		{
			get { return id; }
		}

		public Uri Uri
		{
			get { return uri; }
			set { uri = value; }
		}


		public virtual void Download() { }
	}
}
