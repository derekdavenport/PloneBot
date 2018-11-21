using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	class Page : PloneType
	{

		public Page(string id) : base(id)
		{

		}

		public Page(string id, Folder parent) : base(id, parent)
		{
			Parent = parent;
		}

		public Page(Uri uri) : base(uri)
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
					Parent.pages.Remove(id);
				}
				parent = value;
				Parent.pages[id] = this;
			}
		}

		public override Folder GetPortletsFolder()
		{
			return Parent.GetPortletsFolder().GetOrCreateChild<Folder>(id);
		}
	}

}
