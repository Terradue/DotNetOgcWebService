using System;
using Terradue.ServiceModel.Ogc.Gml321;
using Terradue.ServiceModel.Ogc.Om20;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// Default implementation to format an observation.
    /// </summary>
    public class OmDefaultObservationFormatter : BaseObservationFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OmDefaultObservationFormatter"/> class.
        /// </summary>
        /// <param name="operation">The operation.</param>
        public OmDefaultObservationFormatter(BaseSosOperation operation)
            : base(operation)
        {
        }

        /// <summary>
        /// Gets the observation feature of interest.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public override FeaturePropertyType GetObservationFeatureOfInterest(OM_ObservationType observation)
        {
            return observation.featureOfInterest;
        }

        /// <summary>
        /// Gets the observation property.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public override ReferenceType GetObservationProperty(OM_ObservationType observation)
        {
            return observation.observedProperty;
        }

        /// <summary>
        /// Gets the observation procedure.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public override OM_ProcessPropertyType GetObservationProcedure(OM_ObservationType observation)
        {
            return observation.procedure;
        }

        /// <summary>
        /// Gets the observation result.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <param name="responseMode">The response mode.</param>
        /// <param name="samplingBeginDate">The sampling begin date.</param>
        /// <param name="samplingEndDate">The sampling end date.</param>
        /// <returns></returns>
        public override OM_ObservationType GetObservationResult(OM_ObservationType observation, ServiceModel.Ogc.Sos10.ResponseModeType responseMode, DateTime samplingBeginDate, DateTime samplingEndDate)
        {
            throw new NotImplementedException();
        }



        /// <summary>
        /// Gets the output format used by this formatter.
        /// </summary>
        /// <value>The output format.</value>
        public override OutputFormat OutputFormat
        {
            get { return OutputFormat.ApplicationXml; }
        }
    }
}
