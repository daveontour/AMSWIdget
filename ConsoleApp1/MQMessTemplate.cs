using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1 {
    class MQMessTemplate {

        public static String updateDesk = @"<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.12"">
            <amsx-messages:Content>
                <amsx-messages:CheckInUpdateRequest>
                    <amsx-datatypes:Token>{0}</amsx-datatypes:Token>
                    <amsx-messages:CheckInId>
                        <amsx-datatypes:ExternalName>{1}</amsx-datatypes:ExternalName>
                        <amsx-datatypes:AirportCode codeContext=""IATA"">{2}</amsx-datatypes:AirportCode>
                    </amsx-messages:CheckInId>
                    <amsx-messages:CheckInUpdates>
                        <amsx-messages:Update propertyName=""Name"">{1}</amsx-messages:Update>
                        <amsx-messages:Update propertyName=""S---_Status"">{3}</amsx-messages:Update>
                    </amsx-messages:CheckInUpdates>
                </amsx-messages:CheckInUpdateRequest>
            </amsx-messages:Content>
        </amsx-messages:Envelope>";

        public static String updateGate = @"<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.12"">
            <amsx-messages:Content>
                <amsx-messages:Gate`nUpdateRequest>
                    <amsx-datatypes:Token>{0}</amsx-datatypes:Token>
                    <amsx-messages:GateId>
                        <amsx-datatypes:ExternalName>{1}</amsx-datatypes:ExternalName>
                        <amsx-datatypes:AirportCode codeContext=""IATA"">{2}</amsx-datatypes:AirportCode>
                    </amsx-messages:GateId>
                    <amsx-messages:GateUpdates>
                        <amsx-messages:Update propertyName=""Name"">{1}</amsx-messages:Update>
                        <amsx-messages:Update propertyName=""S---_Status"">{3}</amsx-messages:Update>
                    </amsx-messages:GateUpdates>
                </amsx-messages:GateUpdateRequest>
            </amsx-messages:Content>
        </amsx-messages:Envelope>";
        public static String GetMQMessTemplate(String type) {

            switch (type) {
                case "CheckIn":
                    return updateDesk;
                case "Gate":
                    return updateDesk;
                default:
                    return updateDesk;
            }
        }
    }
}
