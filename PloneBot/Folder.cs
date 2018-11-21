using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	class Folder : PloneType
	{
		internal Dictionary<string, Folder> folders = new Dictionary<string, Folder>();
		internal Dictionary<string, Page>   pages   = new Dictionary<string, Page>();
		// internal List<File> files = new List<File>();

		public Folder(string id) : base(id)
		{

		}

		public Folder(string id, Folder parent) : base(id, parent)
		{

		}

		public Folder(Uri uri) : base(uri)
		{

		}

		public override Folder Parent
		{
			get
			{
				return base.Parent;
			}
			set
			{
				if (parent != null)
				{
					Parent.folders.Remove(id);
				}
				parent = value;
				Parent.folders[id] = this;
			}
		}

		public PT GetOrCreateChild<PT>(string id) where PT : PloneType
		{
			PloneType child;
			if (typeof(PT) == typeof(Folder))
			{
				child = folders[id];
				if(child == null)
				{
					child = folders[id] = new Folder(id, this);
				}
			}
			else if (typeof(PT) == typeof(Page))
			{
				child = pages[id];
				if (child == null)
				{
					child = pages[id] = new Page(id, this);
				}
			}
			else
			{
				throw new ArgumentException("unknown type");
			}
			return (PT)child;
		}

		public override Folder GetPortletsFolder()
		{
			return GetOrCreateChild<Folder>("portlets");
		}

		public void getDefaultView()
		{
			//getDefaultPage
		}
	}
}
