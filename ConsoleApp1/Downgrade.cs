using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConsoleApp1 {

    class Downgrade {
        public DateTime from;
        public DateTime to;
        public string UniqueID;
        public string resourceType;
        public string reason;
        public List<string> resourceExternalName = new List<String>();

        public Downgrade (Downgrade d) {
            this.from = d.from;
            this.to = d.to;
            this.UniqueID = d.UniqueID;
            this.resourceType = d.resourceType;
            this.reason = d.reason;
            
            foreach( string s in d.resourceExternalName) {
                this.resourceExternalName.Add(s);
            }
        }
        public Downgrade(XElement el, XNamespace ns, String type) {


            this.resourceType = type;

            IEnumerable<XElement> desc = el.Descendants();

            this.UniqueID = (from n in desc
                                      where (n.Name == ns + "DowngradeUniqueId")
                                      select n.Value).First();

            this.reason = (from n in desc
                                       where ((string)n.Attribute("propertyName") == "Reason" && n.Name == ns + "Value")
                                       select n.Value).First();

            IEnumerable<string> fromStr = from n in desc
                                          where ((string)n.Attribute("propertyName") == "StartTime" && n.Name == ns + "Value")
                                          select n.Value;

            IEnumerable<string> toStr = from n in desc
                                        where ((string)n.Attribute("propertyName") == "EndTime" && n.Name == ns + "Value")
                                        select n.Value;

            IEnumerable<string> resStr = from n in desc
                                         where (n.Parent.Name == ns + this.resourceType.Remove(this.resourceType.Length - 9, 9) && (string)n.Attribute("propertyName") == "ExternalName" && n.Name == ns + "Value")
                                         select n.Value;

            this.to = Convert.ToDateTime(toStr.First());
            this.from = Convert.ToDateTime(fromStr.First());

            foreach (String s in resStr) {
                resourceExternalName.Add(s);
            }
        }

        public bool IsActive() {
            if (this.from < DateTime.Now && DateTime.Now < this.to) {
                return true;
            } else {
                return false;
            }
        }

        public override String ToString() {
            return String.Format("{4}  - {0} From: {1}, To: {2} Reason:{3}", this.UniqueID, this.from, this.to, this.reason, this.resourceType);
        }
    }
}
