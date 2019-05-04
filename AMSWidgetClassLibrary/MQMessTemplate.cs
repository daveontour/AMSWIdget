using System;

namespace AMSWidgetClassLibrary {
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
                <amsx-messages:GateUpdateRequest>
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

        public static String updateStand = @"<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.12"">
            <amsx-messages:Content>
                <amsx-messages:StandUpdateRequest>
                    <amsx-datatypes:Token>{0}</amsx-datatypes:Token>
                    <amsx-messages:StandId>
                        <amsx-datatypes:ExternalName>{1}</amsx-datatypes:ExternalName>
                        <amsx-datatypes:AirportCode codeContext=""IATA"">{2}</amsx-datatypes:AirportCode>
                    </amsx-messages:StandId>
                    <amsx-messages:StandUpdates>
                        <amsx-messages:Update propertyName=""Name"">{1}</amsx-messages:Update>
                        <amsx-messages:Update propertyName=""S---_Status"">{3}</amsx-messages:Update>
                    </amsx-messages:StandUpdates>
                </amsx-messages:StandUpdateRequest>
            </amsx-messages:Content>
        </amsx-messages:Envelope>";

        public static String updateChute = @"<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.12"">
            <amsx-messages:Content>
                <amsx-messages:ChuteUpdateRequest>
                    <amsx-datatypes:Token>{0}</amsx-datatypes:Token>
                    <amsx-messages:ChuteId>
                        <amsx-datatypes:ExternalName>{1}</amsx-datatypes:ExternalName>
                        <amsx-datatypes:AirportCode codeContext=""IATA"">{2}</amsx-datatypes:AirportCode>
                    </amsx-messages:ChuteId>
                    <amsx-messages:ChuteUpdates>
                        <amsx-messages:Update propertyName=""Name"">{1}</amsx-messages:Update>
                        <amsx-messages:Update propertyName=""S---_Status"">{3}</amsx-messages:Update>
                    </amsx-messages:ChuteUpdates>
                </amsx-messages:ChuteUpdateRequest>
            </amsx-messages:Content>
        </amsx-messages:Envelope>";

        public static String updateCarousel = @"<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.12"">
            <amsx-messages:Content>
                <amsx-messages:CarouselUpdateRequest>
                    <amsx-datatypes:Token>{0}</amsx-datatypes:Token>
                    <amsx-messages:CarouselId>
                        <amsx-datatypes:ExternalName>{1}</amsx-datatypes:ExternalName>
                        <amsx-datatypes:AirportCode codeContext=""IATA"">{2}</amsx-datatypes:AirportCode>
                    </amsx-messages:CarouselId>
                    <amsx-messages:CarouselUpdates>
                        <amsx-messages:Update propertyName=""Name"">{1}</amsx-messages:Update>
                        <amsx-messages:Update propertyName=""S---_Status"">{3}</amsx-messages:Update>
                    </amsx-messages:CarouselUpdates>
                </amsx-messages:CarouselUpdateRequest>
            </amsx-messages:Content>
        </amsx-messages:Envelope>";
        public static String GetMQMessTemplate(String type) {

            switch (type) {
                case "CheckIn":
                    return updateDesk;
                case "Gate":
                    return updateGate;
                case "Stand":
                    return updateStand;
                case "Chute":
                    return updateChute;
                case "Carousel":
                    return updateCarousel;
                default:
                    return updateDesk;
            }
        }
    }

}
