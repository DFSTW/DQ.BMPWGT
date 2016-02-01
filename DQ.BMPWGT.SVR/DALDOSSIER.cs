
using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Thyt.TiPLM.DAL.Common;

namespace DQ.BMPWGT.SVR
{
    public class DALDOSSIER : DABase
    {
        public DALDOSSIER(DBParameter dbParam) : base(dbParam) { }
        public Guid GetMasterId(string id)
        {
            
            OracleCommand command = new OracleCommand();
            StringBuilder builder = new StringBuilder();
            command.Connection = (OracleConnection)this.dbParam.Connection;
            builder.Append(@"select m.plm_m_oid
  from plm_psm_itemmaster_revision m,
       (select plm_name as name, plm_label
          from PLM_SYS_METACLASS
        connect by prior plm_oid = plm_parent
         start with plm_name = 'DOC') n
 where m.plm_m_id = :id
   and m.plm_m_lastrevision = m.plm_r_revision
   and m.plm_m_class = n.name
");

            Guid uid = Guid.Empty;
            command.CommandType = CommandType.Text;
            command.CommandText = builder.ToString();
            command.Parameters.Add(":id", OracleDbType.NVarchar2);
            command.Parameters[":id"].Direction = ParameterDirection.Input;
            command.Parameters[":id"].Value = id;

            var dr = command.ExecuteReader();
            while (dr.Read())
            {
                uid = new Guid(dr.GetOracleBinary(0).Value);
            }
            dr.Close();
            return uid;
        }
    }
}
