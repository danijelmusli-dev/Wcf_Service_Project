using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services
{
    [ServiceContract]
    public interface IPpgService
    {
        [OperationContract]
        void StartSession(Meta error);

        [OperationContract]
        bool PushSample(PpgSample sample);

        [OperationContract]
        bool EndSession();
    }
}
