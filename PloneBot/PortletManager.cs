using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	class PortletManager : PloneObject
	{
		readonly Portlet.Zones zone;
		public List<Portlet> portlets = new List<Portlet>();

		public PortletManager(Portlet.Zones zone, PloneType parent) : base(Portlet.zoneToPath[zone], parent)
		{
			this.zone = zone;
			this.uri = new Uri(parent.Uri, Portlet.zoneToPath[zone] + '/');
		}
	}
}
