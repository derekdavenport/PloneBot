using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	class Portlet : PloneObject
	{
		public enum Zones { Left, Right, Bottom };

		public static readonly Dictionary<string, Zones> idToZone = new Dictionary<string, Zones>()
		{
			{ "portletmanager-plone-leftcolumn",      Zones.Left },
			{ "portletmanager-plone-rightcolumn",     Zones.Right },
			{ "portletmanager-uofl-prefootermanager", Zones.Bottom }
		};

		public static readonly Dictionary<string, Zones> pathToZone = new Dictionary<string, Zones>()
		{
			{ "++contextportlets++plone.leftcolumn",      Zones.Left },
			{ "++contextportlets++plone.rightcolumn",     Zones.Right },
			{ "++contextportlets++uofl.prefootermanager", Zones.Bottom }
		};

		public static readonly Dictionary<Zones, string> zoneToPath = new Dictionary<Zones, string>()
		{
			{ Zones.Left,   "++contextportlets++plone.leftcolumn"      },
			{ Zones.Right,  "++contextportlets++plone.rightcolumn"     },
			{ Zones.Bottom, "++contextportlets++uofl.prefootermanager" }
		};

		public Portlet(string id) : base(id)
		{
		}

		public Portlet(string id, PortletManager parent) : base(id, parent)
		{
		}

		public PortletManager Parent
		{
			get { return (PortletManager)parent; }
			private set
			{
				if (parent != null)
				{
					Parent.portlets.Remove(this);
				}
				parent = value;
				if (!value.portlets.Contains(this))
				{
					value.portlets.Add(this);
				}
			}
		}

		public new void Download()
		{
			// not implemented. can't external edit portlets
		}

		public void Upload()
		{
		}
	}
}
