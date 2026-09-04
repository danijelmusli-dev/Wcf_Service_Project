using Contracts.Models;
using System.Collections.Generic;
using System.ServiceModel;

namespace Contracts.Services
{
    [ServiceContract]
    public interface IPpgService
    {
        [OperationContract]
        void StartSession(Meta metaData);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        void PushSample(PpgSample sample);

        [OperationContract]
        List<PpgSampleResult> PushSamples(List<PpgSample> samples);

        [OperationContract(IsOneWay = true)]
        void EndSession();
    }
}
