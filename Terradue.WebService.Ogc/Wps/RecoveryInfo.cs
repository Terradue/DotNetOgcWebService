using System;
namespace Terradue.WebService.Ogc.Wps {
    public class RecoveryInfo {

        public string wpsProcessIdentifier { get; set; }

        public string identifier { get; set; }
        
        public int retry { get; set; }

    }
}
