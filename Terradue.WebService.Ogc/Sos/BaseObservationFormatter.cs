using System;
using Terradue.ServiceModel.Ogc.Gml321;
using Terradue.ServiceModel.Ogc.Om20;
using Terradue.ServiceModel.Ogc.Sos10;
using Terradue.ServiceModel.Ogc.Swe10;

namespace Terradue.WebService.Ogc.Sos
{
    /// <summary>
    /// Specifies operations required to format observation
    /// </summary>
    public abstract class BaseObservationFormatter
    {
        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <value>The operation.</value>
        public BaseSosOperation Operation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseObservationFormatter"/> class.
        /// </summary>
        /// <param name="operation">The operation.</param>
        protected BaseObservationFormatter(BaseSosOperation operation)
        {
            this.Operation = operation;
        }

        /// <summary>
        /// Gets the observation feature of interest.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public abstract FeaturePropertyType GetObservationFeatureOfInterest(OM_ObservationType observation);

        /// <summary>
        /// Gets the observation property.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public abstract ReferenceType GetObservationProperty(OM_ObservationType observation);

        /// <summary>
        /// Gets the observation procedure.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <returns></returns>
        public abstract OM_ProcessPropertyType GetObservationProcedure(OM_ObservationType observation);

        /// <summary>
        /// Gets the observation result.
        /// </summary>
        /// <param name="observation">The observation.</param>
        /// <param name="responseMode">The response mode.</param>
        /// <param name="samplingBeginDate">The sampling begin date.</param>
        /// <param name="samplingEndDate">The sampling end date.</param>
        /// <returns></returns>
        public abstract OM_ObservationType GetObservationResult(OM_ObservationType observation, ResponseModeType responseMode, DateTime samplingBeginDate, DateTime samplingEndDate);

        /// <summary>
        /// Gets the output format supported by this formatter
        /// </summary>
        /// <value>The output format.</value>
        public abstract OutputFormat OutputFormat { get;  }
    }
}
