using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Thyt.TiPLM.BRL.Admin.NewResponsibility;
using Thyt.TiPLM.BRL.Common;
using Thyt.TiPLM.BRL.Product;
using Thyt.TiPLM.DAL.Common;
using Thyt.TiPLM.DEL.Product;

namespace DQ.BMPWGT.SVR
{
    class WGT2DOSSIOR : PRBase
    {
        Guid userOid;
        string usernmame;
        DALDOSSIER DWGMaster;
        Dictionary<string, string> userPrint = new Dictionary<string, string>();
        public WGT2DOSSIOR(Guid user, DBParameter dbParam)
        {
            userOid = user;
            var userss = new BRUser(userOid).GetUserByOid(userOid);
            usernmame = userss.Name;
            DWGMaster = new DALDOSSIER(dbParam);
            ReadUser();
        }

        private void ReadUser()
        {
            var lines = File.ReadAllLines("userMap.txt");
            foreach (var line in lines)
            {
                var tmp = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp != null && tmp.Count() > 1 &&
                    !String.IsNullOrWhiteSpace(tmp[0]) && !String.IsNullOrWhiteSpace(tmp[0]))
                    userPrint.Add(tmp[0].Trim(), tmp[1].Trim());
            }
        }
        public void Run(string box, List<string> draws, int number, string comment, string order)
        {
            //if (!base.isInTrans) base.dbParam.Open();
            QRItem qritem = new QRItem(base.dbParam);
            PRItem pritem = new PRItem(base.dbParam);
            var meifen = 0;
            DEItemMaster2 dossmasterIterOid;
            dossmasterIterOid = qritem.GetItemMaster(box, "DQDOSSIERPRINT", userOid);
            DEBusinessItem dossmaster;
            if (dossmasterIterOid == null)
                dossmaster = CreateDossier(Guid.NewGuid(), box, comment, order);
            else
            {
                if (dossmasterIterOid.State == ItemState.Release)
                {
                    pritem.NewRelease(dossmasterIterOid.Oid, "DQDOSSIERPRINT", null, userOid, true);

                }
                else if (dossmasterIterOid.State == ItemState.CheckIn)
                {
                    pritem.CheckOut(dossmasterIterOid.Oid, "DQDOSSIERPRINT", userOid, true);
                }
                dossmaster = qritem.GetBizItem(dossmasterIterOid.Oid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
                dossmaster = qritem.GetBizItem(dossmasterIterOid.Oid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
                dossmaster.Iteration.SetAttrValue("TSSTATUS", "未发打印");
                dossmaster.Iteration.SetAttrValue("TWDMC", "外供图");
                dossmaster.Iteration.SetAttrValue("WKFLINFO", comment + "(" + order + ")");
                dossmaster.Iteration.SetAttrValue("DOCCODE", box);
                dossmaster.Iteration.SetAttrValue("YCT", "其它");
                dossmaster.Iteration.SetAttrValue("NAME", "外供图：" + box);
                dossmaster.Iteration.SetAttrValue("ZDHQMXBYDOC", false);
                dossmaster.Iteration.SetAttrValue("ZDHQMX", false);
                dossmaster.Iteration.SetAttrValue("CJLXMX", false);
            }
            dossmasterIterOid = qritem.GetItemMaster(box, "DQDOSSIERPRINT", userOid);
            var masteritem = qritem.GetBizItem(dossmasterIterOid.Oid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
            var itpLinkList = qritem.GetLinkRelations(masteritem.IterOid, "DQDOSSIERPRINT", "DOSSIRPRINTTODOC", userOid);
            foreach (DERelation2 r in itpLinkList)
            {
                itpLinkList.DeleteLinkRelation(r.RightObj);
            }
            foreach (var draw in draws)
            {
                var drawid = DWGMaster.GetMasterId(draw);
                if (drawid == Guid.Empty) continue;
                IBizItem itemalq = qritem.GetBizItem(drawid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem);
                var mtzs = Convert.ToInt32(itemalq.GetAttrValue("I", "GZ"));
                if (mtzs < 1) mtzs = 1;
                meifen += mtzs;

                //var itpPartRel = itpLinkList.GetLinkRelation(draw);
                var rel = itpLinkList.GetLinkRelation(itemalq.MasterOid);
                if (rel != null)
                {
                    itpLinkList.DeleteLinkRelation(itemalq.MasterOid);
                }

                var relation = new DERelation2(masteritem.IterOid, "DQDOSSIERPRINT", drawid, itemalq.ClassName, 0,
                              "system", Guid.Empty, "DOSSIRPRINTTODOC");
                relation.SetAttrValue("DOCCODE", draw);
                relation.SetAttrValue("DOCNAME", itemalq.Name);
                relation.SetAttrValue("FS", number);
                relation.SetAttrValue("JSDW", "21储运中心(" + number + ")");
                relation.SetAttrValue("MTZS", mtzs);
                relation.SetAttrValue("ZS", number * mtzs);
                relation.SetAttrValue("DOCREV", itemalq.RevLabel);
                //relation.SetAttrValue("", "");
                itpLinkList.AddLinkRelation(relation
                         );
            }
            pritem.UpdateLinkRelationsDirectly(masteritem.MasterOid, itpLinkList, userOid, null);
            dossmaster.Iteration.SetAttrValue("MTZS", meifen);
            dossmaster.Iteration.SetAttrValue("FS", number);
            dossmaster.Iteration.SetAttrValue("ZS", meifen * number);
            pritem.UpdateItemIteration(dossmaster.Iteration, userOid, null);
            //if (!base.isInTrans) dbParam.Commit();
            //if (!base.isInTrans) dbParam.Close();
        }

        private DEBusinessItem CreateDossier(Guid dossId, string ID, string comment, string order)
        {

            var className = "DQDOSSIERPRINT";
            DEBusinessItem doc = new DEBusinessItem()
            {
                Master = new DEItemMaster2(),
                Iteration = new DEItemIteration2(),
                Revision = new DEItemRevision2()
            };

            doc.Master.Oid = dossId;
            doc.Master.Status = 'O';
            doc.Master.LastRevision = 1;
            doc.Master.Id = ID;
            doc.Master.ClassName = className;
            doc.Master.Holder = userOid;
            doc.Master.Phase = Guid.Empty;
            doc.Master.Susbended = false;
            doc.Master.SecurityLevel = 1;
            doc.Master.StartView = Guid.Empty;
            doc.Master.ViewModel = Guid.Empty;
            doc.Master.Group = Guid.Empty;

            doc.Revision.ClassName = className;
            doc.Revision.Oid = Guid.NewGuid();
            doc.Revision.MasterOid = doc.Master.Oid;
            doc.Revision.Revision = 1;
            doc.Revision.LastIteration = 1;
            doc.Revision.Creator = userOid;
            doc.Revision.CreateTime = DateTime.Now;
            doc.Revision.Releaser = userOid;
            doc.Revision.ReleaseTime = DateTime.Now;
            doc.Revision.ReleaseDesc = string.Empty;
            //doc.Revision.RevLabel = ;
            doc.Revision.State = 'A';

            doc.Iteration.ClassName = className;
            doc.Iteration.Oid = Guid.NewGuid();
            doc.Iteration.RevisionOid = doc.Revision.Oid;
            //doc.Iteration.Name = ITP;
            doc.Iteration.Iteration = 1;
            doc.Iteration.Creator = userOid;
            doc.Iteration.CreateTime = DateTime.Now;
            doc.Iteration.CheckInTime = DateTime.Now;
            doc.Iteration.CheckInDesc = string.Empty;
            doc.Iteration.LatestUpdator = userOid;
            doc.Iteration.LatestUpdateTime = DateTime.Now;
            doc.Iteration.SetAttrValue("FTLX", "全套产品图");
            doc.Iteration.SetAttrValue("TSSTATUS", "未发打印");
            doc.Iteration.SetAttrValue("TSTYPE", "随机");
            //doc.Iteration.SetAttrValue("ORGPRINTER", "谢晓霞");
            //doc.Iteration.SetAttrValue("ORGPRINTER", "曾志筠");

            if (userPrint.ContainsKey(usernmame))
                doc.Iteration.SetAttrValue("ORGPRINTER", userPrint[usernmame]);
            else
                doc.Iteration.SetAttrValue("ORGPRINTER", "邓文斌");

            doc.Iteration.SetAttrValue("TWDMC", "外供图");
            doc.Iteration.SetAttrValue("WKFLINFO", comment + "(" + order + ")");
            doc.Iteration.SetAttrValue("DOCCODE", ID);
            doc.Iteration.SetAttrValue("YCT", "其它");
            //doc.Iteration.SetAttrValue("NAME", "外供图：" + comment + "(" + order + ")");
            doc.Iteration.SetAttrValue("NAME", "外供图：" + ID);

            doc.Iteration.SetAttrValue("ZDHQMXBYDOC", false);
            doc.Iteration.SetAttrValue("ZDHQMX", false);
            doc.Iteration.SetAttrValue("CJLXMX", false);

            PRItem item = new PRItem(base.dbParam);
            var re = item.CreateItem(doc, string.Empty, userOid);
            //设置文档名称
            //re.Iteration.SetAttrValue("", "");
            //item.UpdateItemIteration(re
            //item.CheckIn(dossId, className, userOid, string.Empty);
            //item.Release(itpId, className, userOid, string.Empty, true);
            return re;
        }
        internal void RunHezi(List<string> heziList)
        {
            if (!base.isInTrans) base.dbParam.Open();
            QRItem qritem = new QRItem(base.dbParam);
            foreach (var hezi in heziList)
            {
                var heziMaster = qritem.GetItemMaster(hezi, "HEZI", userOid);
                var heziItem = qritem.GetBizItem(heziMaster.Oid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
                var content = heziItem.GetAttrValue("I", "WGTQD") as byte[];
                var fenceList = qritem.GetLinkedRelationItems(heziItem.MasterOid, 0, 0, "PARTTOPART", userOid, null);
                foreach (DERelation2 fencerel in fenceList)
                {
                    var order = fencerel.GetAttrValue("ORDER");
                    var fenceItem = qritem.GetItemIteration(fencerel.LeftObj, "FENCE", false, userOid);
                    var number = fenceItem.GetAttrValue("TZZS");
                    var comment = fenceItem.GetAttrValue("PROJECTNAME");

                    //draws.AddRange(PassWGTQD(Encoding.UTF8.GetString(content)));
                    var draws = PassWGTQD(Encoding.UTF8.GetString(content));
                    Run(heziItem.Id, draws, Convert.ToInt32(number), (string)comment, Convert.ToInt32(order).ToString());
                    //break;
                }
            }

            if (!base.isInTrans) dbParam.Commit();
            if (!base.isInTrans) dbParam.Close();
        }
        internal void RunFence(string fence)
        {
            if (!base.isInTrans) base.dbParam.Open();
            QRItem qritem = new QRItem(base.dbParam);
            var all = new List<string>();
            var fenceMaster = qritem.GetItemMaster(fence, "FENCE", userOid);
            var fenceItem = qritem.GetBizItem(fenceMaster.Oid, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
            var number = fenceItem.GetAttrValue("I", "TZZS");
            var comment = fenceItem.GetAttrValue("I", "PROJECTNAME");

            var heziList = qritem.GetLinkRelations(fenceItem.IterOid, "FENCE", "PARTTOPART", userOid);
            foreach (DERelation2 hezirel in heziList)
            {
                var order = hezirel.GetAttrValue("ORDER");
                var heziItem = qritem.GetBizItem(hezirel.RightObj, 0, 0, Guid.Empty, userOid, BizItemMode.BizItem) as DEBusinessItem;
                var content = heziItem.GetAttrValue("I", "WGTQD") as byte[];

                //draws.AddRange(PassWGTQD(Encoding.UTF8.GetString(content)));
                var draws = PassWGTQD(Encoding.UTF8.GetString(content));
                all.AddRange(draws);
                Run(heziItem.Id, draws, Convert.ToInt32(number), (string)comment, Convert.ToInt32(order).ToString());
                //break;
            }
            Run(fenceItem.Id + "(PLT)", all, Convert.ToInt32(number), (string)comment, "PLT");

            if (!base.isInTrans) dbParam.Commit();
            if (!base.isInTrans) dbParam.Close();
        }
        private List<string> PassWGTQD(string content)
        {
            List<string> draws = new List<string>();
            Regex regex = new Regex(@"<图号>(.*)</图号>");
            var matches = regex.Matches(content);
            foreach (Match m in matches)
            {
                draws.Add(m.Groups[1].Value.Trim());
            }
            return draws;
        }

        internal void Run(List<string> heziList)
        {
            //foreach (var fence in heziList)
            //RunFence(fence);
            RunHezi(heziList);
        }
    }
}
