using DQ.BMPWGT.SVR;
using Oracle.DataAccess.Client;
using PLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thyt.TiPLM.BRL.Common;
using Thyt.TiPLM.DAL.Common;
using Thyt.TiPLM.DEL.Admin.BPM;

namespace DQ.BMPWGT.TEST
{
    class Program : DABase
    {
        private DBParameter dparam;

        public Program(DBParameter dparam):base(dparam)
        {
            // TODO: Complete member initialization
            
        }
        static void Main(string[] args)
        {
            string[] guids = new string[]{
                                    "117DD617FA2943449AE19EC80A4065DE",};
            int i = 0;
            foreach (var guid in guids)
            {
                var dparam = DBUtil.GetDbParameter(false);
                dparam.Open();
                Console.WriteLine(++i);
                Console.WriteLine(guid);
                var program = new Program(dparam);
                program.Run(guid);
                dparam.Close();
            }
        }
        public void Run(string Guids)
        {


            // 1.流程定义oid  M-工艺路线审签流程 89A31E422C14C54FB971AD5186744187
            var processDefinitionID = Guid.Empty; //= new Guid(PLMCommon.OracleToDotNet("89A31E422C14C54FB971AD5186744187"));
            // 2.活动节点定义oid  审核 E1B31DB3F2251C46A8DA6385BB84FFD2
            var activityDefinitionID = Guid.Empty; // = new Guid(PLMCommon.OracleToDotNet("13E9286506432949A290DE203C0F911E"));
            // 3. 操作oid plm_bpm_oprcfg_ins  C85970E6C5AD47428E44FBA806F2174A
            var operationID = Guid.Empty; //= new Guid(PLMCommon.OracleToDotNet("BDF4C1163979274E97492B05E62FC6E6"));
            // 4. 流程实例oid  CE9B9A2097934C40A7DB77A3E4E872D7
            var ProcessInstanceID = new Guid(PLMCommon.OracleToDotNet(Guids));
            // 5. 活动实例oid 440BF07771325445A7A4F42D29C461BE
            var ActivityInstanceID = Guid.Empty; //= new Guid(PLMCommon.OracleToDotNet("A27F8320C5B1154BA024E3658FE38F93"));
            // 6. workItem oid 80883CE28E7696409E2B10E92371922F  取审核节点的workitem
            var WorkItemID = Guid.Empty; //= new Guid(PLMCommon.OracleToDotNet("724FD7AD5AB9EA41A4402A6449D3C0D3"));


            OracleCommand cmd = new OracleCommand();

            cmd.Connection = this.dbParam.Connection as OracleConnection;
            cmd.CommandText =
                @"select i.PLM_PROCESSDEFINITIONID,
       a.PLM_ACTIVITYDEFINITIONID,
        h.plm_oid                  operationid,
       i.PLM_OID,
       a.PLM_OID                  activityid
  from plm_bpmv_process_instance    i,
       plm_bpmv_acitivity_instance  a,
       plm_bpm_r_activityins_oper_h h
 where i.PLM_OID = a.PLM_PROCESSINSTANCEID
   and a.PLM_OID = h.plm_activityinstanceid
   and h.plm_operationname = '外供图自动托晒'
   and i.PLM_OID = :poid";
            cmd.Parameters.Add(":poid", ProcessInstanceID.ToByteArray());
            //            cmd.CommandText = @"select i.plm_oid from  plm_bpm_process_instance i, plm_cus_c_taskroute_prc c where i.plm_oid = c.plm_process_oid
            //and c.plm_status <> 'P' and i.plm_state = 'Completed'";   
            OracleDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                processDefinitionID = new Guid(r.GetOracleBinary(0).Value);
                activityDefinitionID = new Guid(r.GetOracleBinary(1).Value);
                operationID = new Guid(r.GetOracleBinary(2).Value);
                ProcessInstanceID = new Guid(r.GetOracleBinary(3).Value);
                ActivityInstanceID = new Guid(r.GetOracleBinary(4).Value);
            }
            WorkItemID = GetWorkItemID(ProcessInstanceID);

            var proc = new DELProcessInsProperty();
            proc.ProcessDefinitionID = processDefinitionID;
            var activity = new DELActivityInstance();
            activity.ActivityDefinitionID = activityDefinitionID;
            var relation = new DELRActivityInsOperation();
            relation.OperationID = operationID;
            relation.ProcessInstanceID = ProcessInstanceID;
            var isa = new DELInstanceSpecificArgs(proc, activity, relation);
            isa.ProcessInstance.ID = ProcessInstanceID;
            isa.ActivityInstance.ID = ActivityInstanceID;
            isa.OperationRelationship.ID = operationID;
            isa.WorkItem = new DELWorkItem();
            isa.WorkItem.ID = WorkItemID;
            var iga = new DELInstanceGeneralArgs();
            var sea = new PLMServerEventArgs(this.dbParam, null);
            sea.Operator = PLMCommon.Sysadmin;
            new DQBMPWGTSVR().ExceuteOperation(isa, iga, sea);

        }
        public Guid GetWorkItemID(Guid poid)
        {
            Guid WorkItemID = Guid.Empty;
            OracleCommand cmd = new OracleCommand();

            cmd.Connection = this.dbParam.Connection as OracleConnection;
            cmd.CommandText =
                @"select w.PLM_OID
  from plm_bpmv_process_instance   i,
       plm_bpmv_acitivity_instance a,
       plm_bpmv_workitem           w
 where i.PLM_OID = a.PLM_PROCESSINSTANCEID
   and w.PLM_ACTIVITYINSTANCEID = a.PLM_OID
   and w.PLM_LATESTDONEOPERATION >= 0
   and a.PLM_NAME = '外供电子版'
   and i.PLM_OID = :poid";
            cmd.Parameters.Add(":poid", poid.ToByteArray());
            //            cmd.CommandText = @"select i.plm_oid from  plm_bpm_process_instance i, plm_cus_c_taskroute_prc c where i.plm_oid = c.plm_process_oid
            //and c.plm_status <> 'P' and i.plm_state = 'Completed'";
            OracleDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                WorkItemID = new Guid(r.GetOracleBinary(0).Value);
            }
            return WorkItemID;
        }
    }
}
