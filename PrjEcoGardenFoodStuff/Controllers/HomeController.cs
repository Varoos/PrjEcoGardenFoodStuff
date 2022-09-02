using Newtonsoft.Json;
using PrjEcoGardenFoodStuff.Models;
using System;
using System.Collections;
using System.Data;
using System.Web.Mvc;

namespace PrjEcoGardenFoodStuff.Controllers
{
    public class HomeController : Controller
    {
        Common c = new Common();
        public JsonResult Index(int companyId, string VoucherNo)
        {
            try
            {
                c.EventLog("Entered Home/Index");
                c.EventLog("companyId = " + companyId.ToString());
                string CCode = c.GetCCode(companyId);
                Hashtable objHash = new Hashtable();
                objHash.Add("sName", VoucherNo);
                objHash.Add("sCode", VoucherNo);
                string errText = "";
                var postingData = new ClsProperties.PostingData();
                postingData.data.Add(objHash);
                string sContent = JsonConvert.SerializeObject(postingData);
                c.EventLog("sContent"+ sContent);
                string baseUrl = "http://" + c.serverip + "/Focus8API/Masters/Core__ConsignmentBatchNo";
                c.EventLog("baseUrl"+ baseUrl);
                string sessionId = c.getsessionid(c.username, c.Password, CCode);
                c.EventLog("sessionId"+ sessionId);
                string response = Common.Post(baseUrl, sContent, sessionId, ref errText);
                c.EventLog("response"+ response);
                c.ErrLog(errText);
                if (response != null)
                {
                    var responseData = JsonConvert.DeserializeObject<ClsProperties.PostResponse>(response);
                    c.EventLog("responseData Message" + responseData.message);
                    if (responseData.result == -1)
                    {
                        c.EventLog("Posting Failed \n" + errText);
                        return Json("Failed. "+ responseData.message, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        c.EventLog("Posted Successfully\n" );
                        string qry = $@"update tCore_Data_Tags_0 
                        set tCore_Data_Tags_0.iTag3013 = c.iMasterId
                        from tCore_Header_0 h
                        join tCore_Data_0 d on h.iHeaderId = d.iHeaderId
                        join tCore_Data_Tags_0 t on d.iBodyId = t.iBodyId
                        join mCore_ConsignmentBatchNo c on c.sCode = h.sVoucherNo
                        join tCore_Batch_0 b on b.iBodyId = d.iBodyId
                        where h.iVoucherType = 1283 and h.sVoucherNo = '{VoucherNo}' and c.iStatus <>5
                        ";
                        //update tCore_Batch_0
                        //set tCore_Batch_0.sBatchNo = h.sVoucherNo
                        //from tCore_Header_0 h
                        //join tCore_Data_0 d on h.iHeaderId = d.iHeaderId
                        //join tCore_Batch_0 b on b.iBodyId = d.iBodyId
                        //where h.iVoucherType = 1283 and h.sVoucherNo = '{VoucherNo}'
                        c.EventLog("qry : "+ qry);
                        int a = c.SetData(qry, companyId, CCode, ref errText);
                        c.EventLog("a : " + a.ToString());
                        if (a > 0)
                        {
                            return Json("Success", JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return Json("Fail", JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                else
                {
                    c.EventLog("Posting Failed \n" + errText + "\n" + response);
                    return Json("Posting Failed \n" + errText + "\n" + response, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                c.ErrLog(ex.Message);
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
