using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PloneBot
{
	abstract class PloneType : PloneObject
	{
		protected readonly Dictionary<Portlet.Zones, PortletManager> portletManagers;

		public PloneType(string id) : base(id)
		{
			portletManagers = new Dictionary<Portlet.Zones, PortletManager>
			{
				{ Portlet.Zones.Left,   new PortletManager(Portlet.Zones.Left, this) },
				{ Portlet.Zones.Right,  new PortletManager(Portlet.Zones.Right, this) },
				{ Portlet.Zones.Bottom, new PortletManager(Portlet.Zones.Bottom, this) }
			};
		}

		public PloneType(string id, PloneObject parent) : base(id, parent)
		{
			portletManagers = new Dictionary<Portlet.Zones, PortletManager>
			{
				{ Portlet.Zones.Left,   new PortletManager(Portlet.Zones.Left, this) },
				{ Portlet.Zones.Right,  new PortletManager(Portlet.Zones.Right, this) },
				{ Portlet.Zones.Bottom, new PortletManager(Portlet.Zones.Bottom, this) }
			};
		}

		public PloneType(Uri uri) : base(uri)
		{
			portletManagers = new Dictionary<Portlet.Zones, PortletManager>
			{
				{ Portlet.Zones.Left,   new PortletManager(Portlet.Zones.Left, this) },
				{ Portlet.Zones.Right,  new PortletManager(Portlet.Zones.Right, this) },
				{ Portlet.Zones.Bottom, new PortletManager(Portlet.Zones.Bottom, this) }
			};
		}


		public virtual Folder Parent
		{
			get { return (Folder)parent; }
			set { }
		}

		public override void Download()
		{
			Uri externalEditUri = new Uri(uri, "external_edit");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(externalEditUri);
			request.CookieContainer = PloneUtils.Cookies;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var reader = new StreamReader(response.GetResponseStream());
					string line, key = "";

					//ignore first batch, next batch is attributes, next batch is content
					var mode = Modes.Ignore;
					var attributeValueBuffer = new StringBuilder();
					//bool ignoreExtraLine = false;
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

						if (mode == Modes.Attr)
						{
							// a continuation line
							if (line.StartsWith("  "))
							{
								// Plone inserts 2 space lines between every line of a multiline attribute
								if (line.Length == 2)
								{
									/*
									ignoreExtraLine = !ignoreExtraLine; // remember to set false after adding the attribute to dict
									if (ignoreExtraLine)
									{
										continue;
									}
									 * */
									// Upon further inspection, this is likely Plone turning \r and \n into two returns. Things not set by the user don't have the extra space. So for now I've decided to just ignore empty lines
									continue;
								}
								attributeValueBuffer.Append("\n" + line.Substring(2));
							}
							// a new line
							else
							{
								int colonIndex = line.IndexOf(": ");
								// a new entry
								if (colonIndex >= 1)
								{
									// add finished attr
									if (key.Length > 0)
									{
										attributes.Add(key, attributeValueBuffer.ToString());
										//ignoreExtraLine = false;
									}
									// make new attr
									key = line.Substring(0, colonIndex);
									attributeValueBuffer = new StringBuilder(line.Substring(colonIndex + 2));
								}
								else
								{
									// should never get here
									Console.Error.WriteLine("error, not a continue or a new. what is this: " + line);
								}
							}
						}
						// page contents
						else if (mode == Modes.Content)
						{
							// we're done
							contents = reader.ReadToEnd();
							break;
						}
					}
				}
			}
		}

		public abstract Folder GetPortletsFolder();

		public void SetupPortletsFolder()
		{
			Folder portletsFolder = GetPortletsFolder();
			Folder leftFolder = portletsFolder.GetOrCreateChild<Folder>("left");
			foreach (var portlet in portletManagers[Portlet.Zones.Left].portlets)
			{
				Page portletPage = leftFolder.GetOrCreateChild<Page>(portlet.Id);
				portletPage.title    = portlet.title;
				portletPage.contents = portlet.contents;
			}
		}

		public void GetPortlets(bool forParents = true)
		{
			Uri managePortletsUri = new Uri(uri, "@@manage-portlets");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(managePortletsUri);
			request.CookieContainer = PloneUtils.Cookies;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			{
				HtmlDocument document = new HtmlDocument();
				document.Load(response.GetResponseStream());

				// left portlet links
				HtmlNodeCollection portletmanagerDivs = document.DocumentNode.SelectNodes("//div[@class='portlets-manager']");

				foreach (HtmlNode portletmanagerDiv in portletmanagerDivs)
				{
					Portlet.Zones zone;
					if (Portlet.idToZone.TryGetValue(portletmanagerDiv.GetAttributeValue("id", ""), out zone))
					{
						// the edit link doesn't have a class, all other links do
						foreach (HtmlNode portletEditLink in portletmanagerDiv.SelectNodes(".//*[@class='portletHeader']//a[not(@class)]"))
						{
							Uri editLink = new Uri(portletEditLink.GetAttributeValue("href", ""));
							string editPath = editLink.AbsolutePath.Remove(editLink.AbsolutePath.LastIndexOf("/edit"));
							string urlPart = Portlet.zoneToPath[zone];
							int chop = editLink.AbsolutePath.IndexOf(urlPart);
							string where = editPath.Substring(0, chop); //.Trim(slash);
							string portletId = editPath.Substring(chop + urlPart.Length + 1);

							// will not have first slash but will have last
							string goUp = uri.AbsolutePath.Substring(chop).Trim(slash);

							// belongs here
							if (goUp.Length == 0)
							{
								portletManagers[zone].portlets.Add(new Portlet(portletId, portletManagers[zone]));
							}
							// belongs to some parent
							else if (forParents)
							{
								// have to use PloneType even though this will ultimately be a folder because we start with this. 
								PloneType portletParent = this;
								foreach (var ancestorId in goUp.Split(slash).Reverse())
								{
									if (portletParent.Parent == null)
									{
										portletParent.parent = new Folder(ancestorId);
									}
									portletParent = portletParent.Parent;
								}
								portletParent.portletManagers[zone].portlets.Add(new Portlet(portletId, portletParent.portletManagers[zone]));
							}

							// TODO: determine portlet type? static text?
						}
					}
				}
			}
		}
	}
}
