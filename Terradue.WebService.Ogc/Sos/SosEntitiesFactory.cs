using System;
using System.Collections.Generic;
using Terradue.ServiceModel.Ogc.Gml321;
using Terradue.ServiceModel.Ogc.SensorMl20;
using Terradue.ServiceModel.Ogc.Sos20;

namespace Terradue.WebService.Ogc.Sos
{
    public abstract class SosEntitiesFactory
    {

        public abstract IDictionary<string,ObservationOfferingType> GetOfferings();

        public abstract IDictionary<string,DescribedObjectType> GetSensors();

        public abstract IDictionary<string, ReferenceType> GetObservedProperties();

        public abstract IDictionary<string, FeaturePropertyType> GetFeaturesOfInterest();
   }
}