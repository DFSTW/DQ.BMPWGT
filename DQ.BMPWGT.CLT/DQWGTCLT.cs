using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thyt.TiPLM.Common.Interface.Addin;
using Thyt.TiPLM.Common.Interface.Admin.BPM;

namespace DQ.BMPWGT.CLT
{
    public class DQWGTCLT : IAddinClientEntry, IOperationClientDefinition
    {

        public bool ConfigOperation(Thyt.TiPLM.DEL.Admin.BPM.DELDefinitionSpecificArgs dsa, Thyt.TiPLM.DEL.Admin.BPM.DELOperationDefinitionArgs oda)
        {
            return true;
        }
    }
}
