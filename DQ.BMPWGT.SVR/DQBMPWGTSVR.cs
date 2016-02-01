
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using Thyt.TiPLM.BRL.Admin.BPM;
using Thyt.TiPLM.BRL.Common;
using Thyt.TiPLM.Common;
using Thyt.TiPLM.Common.Interface.Addin;
using Thyt.TiPLM.Common.Interface.Admin.BPM;
using Thyt.TiPLM.DEL.Admin.BPM;

namespace DQ.BMPWGT.SVR
{
    public class DQBMPWGTSVR : IAddinServiceEntry, IOperationServerInstancePersistence, IOperationServerExecution, IOperationServerDefinitionPersistence
    {
        public WellKnownServiceTypeEntry[] RemoteTypes
        {
            get
            {
                return new WellKnownServiceTypeEntry[]{  new WellKnownServiceTypeEntry(
                    typeof(DQBMPWGTSVR),
                     "DQ/Common/WGT.rem",
                    WellKnownObjectMode.SingleCall)};
            }
        }

        public void CancelInstance(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public void CompleteInstance(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public void CreateInstanceDataByDefinition(DELProcessDefinition processDef, DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public void DeleteInstance(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public bool NeedServerManagedInsPersistence() { return true; }

        public void SaveInstanceData(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public void ExceuteOperation(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea)
        {
            Guid processDefinitionID = isa.ProcessInstance.ProcessDefinitionID;
            Guid activityDefinitionID = isa.ActivityInstance.ActivityDefinitionID;
            Guid operationID = isa.OperationRelationship.OperationID;
            Guid processInstanceID = isa.OperationRelationship.ProcessInstanceID;
            int executeOrder = isa.OperationRelationship.ExecuteOrder;
            
            try
            {
                List<string> fenceList = new List<string>();
                List<string> heziList = new List<string>();
                QRBPMProcessor qritem = new QRBPMProcessor();
                DELBPMEntityList list2 = new DELBPMEntityList();

                var s = qritem.GetProcessDataInstanceList(sea.Operator, processInstanceID, out list2, true, true);

                foreach (DELProcessDataInstance d in list2)
                {
                    if (d.DataType == 9)
                    {
                        foreach (DELRGroupDataIns seww in d.ObjectOrVarList)
                        {
                            if (seww.BOClassName == "FENCE")
                                fenceList.Add(seww.BOID);
                            if (seww.BOClassName == "HEZI")
                                heziList.Add(seww.BOID);
                        }
                    }
                }
                new WGT2DOSSIOR(sea.Operator, sea.DBParam).Run(heziList);
                //DEPSOption userGlobalOption = new DAOption2(sea.DBParam).GetUserGlobalOption(sea.Operator);
                //new ServerSplitTask(sea.DBParam).Split(processInstanceID, sea.Operator, userGlobalOption);
            }
            catch (Exception exception)
            {
                PLMEventLog.WriteExceptionLog(exception);
            }
        }

        public void TerminateOperation(DELInstanceSpecificArgs isa, DELInstanceGeneralArgs iga, PLMServerEventArgs sea) { }

        public bool NeedServerManagedDefPersistence() { return true; }

        public void SaveOperationDefinitionData(DELProcessDefProperty processDef, DELActivityDefinitionProperty activityDef, DELOperationDefinitionArgs args, PLMServerEventArgs sea) { }


    }
}
