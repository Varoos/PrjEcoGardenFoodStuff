using Focus.Transactions.DataStructs;
using Focus.TranSettings.DataStructs;
using Focus.Common.DataStructs;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Focus.Masters.DataStructs;
using Focus.Conn;
using System.Diagnostics;
using System.Collections;
using AlBaraa_AutoPosting_Services.Classes;
using System.Xml.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml;
using AlBaraa_AutoPosting_Services.Models;
using Newtonsoft.Json.Linq;
using static AlBaraa_AutoPosting_Services.Models.APIResponse;

namespace AlBaraa_AutoPosting_Services
{
    public class Trigger
    {
        Log_lib _log = new Log_lib();
        DB_lib _db = new DB_lib();
        
        public bool After_Save_Trigger(Transaction objTrans, VWVoucherSettings objSettings, int iField, String strFieldName, int iRowindex)
        {
            bool rtnFlag = true;
            bool PostedFlag = true;
            try
            {

                _log.EventLog("entered After_Save_Trigger ");
                int Date = Convert.ToInt32(objTrans.Header.Date);
                _log.EventLog("Date "+Date.ToString());
                int DocNo = Convert.ToInt32(objTrans.Header.DocNo);
                _log.EventLog("DocNo " + DocNo.ToString());
                string VoucherType = Convert.ToString(objTrans.Header.VoucherType);
                _log.EventLog("VoucherTyepe " + VoucherType);
                string HeaderId = Convert.ToString(objTrans.Header.HeaderId);
                _log.EventLog("HeaderId " + HeaderId);
                int CompId = Focus.Conn.GlobalPref.CompId;
                _log.EventLog("CompanyId " + CompId.ToString());

                DataSet ds = _db.getPaymentEntry(DocNo,CompId);
                if (ds.Tables.Count > 0)
                {
                    _log.EventLog("Dataset Count " + ds.Tables.Count);
                    if(ds.Tables[0].Rows.Count>0)
                    {
                        _log.EventLog("Dataset Count " + ds.Tables[0].Rows.Count);
                        string strServer = getServiceLink();
                        _log.EventLog("getServiceLink " + strServer);
                        string baseUrl = "http://"+ strServer + "/focus8API";
                        _log.EventLog("baseUrl " + baseUrl);
                        string sessionId = Focus.Conn.GlobalPref.SessionId;
                        _log.EventLog("sessionid " + sessionId);
                        List<Hashtable> listBodyJV = new List<Hashtable>();
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            if (Convert.ToInt32(dr["IntercompanyAccount"]) == -1)
                            {
                                _log.EventLog("Intercompany Account is not mapped");
                                MessageBox.Show("Intercompany Account is not mapped");
                                throw new Exception("Intercompany Account is not mapped");
                            }
                            else if (Convert.ToInt32(dr["IntercompanyAccount"]) == Convert.ToInt32(dr["Credit_Account_Paid_By_Company"]))
                            {
                                _log.EventLog("IntercompanyAccount and Credit_Account_Paid_By_Company are same");
                                MessageBox.Show("Credit and Debit Account Should not be Same");
                                throw new Exception("IntercompanyAccount and Credit_Account_Paid_By_Company are same");
                            }
                            if (Convert.ToInt32(dr["creditactype"]) == 7 || Convert.ToInt32(dr["debitactype"]) == 7)
                            {
                                _log.EventLog("Debit or Credit Account should not be Customer / Vendor Account");
                                MessageBox.Show("Debit or Credit Account should not be Customer / Vendor Account");
                                throw new Exception("Debit or Credit Account should not be Customer / Vendor Account");
                            }
                            if (Convert.ToInt32(dr["Paid_By"]) == -1)
                            {
                                _log.EventLog("Paid By is NONE");
                                //MessageBox.Show("Paid By should not be NONE");
                                throw new Exception("Paid By is NONE");
                            }
                            Hashtable headerJV = new Hashtable();
                            Hashtable objJVBody = new Hashtable();
                            
                            DataSet dsExists = _db.getIfExists(dr["sAbbr"].ToString() + "-" + dr["sVoucherNo"].ToString());
                            string NewDocNo = "";
                            if (dsExists.Tables.Count > 0)
                            {
                                if (dsExists.Tables[0].Rows.Count > 0)
                                {
                                    NewDocNo = dsExists.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                    _log.EventLog("NewDocNo " + NewDocNo);
                                    string delerr = "";
                                    string delurl = baseUrl + "/Transactions/Journal Intercompany/" +NewDocNo;
                                    _log.EventLog("delurl " + delurl);
                                    var delres = Focus8API.Delete(delurl, sessionId, ref delerr);
                                    _log.EventLog("delres " + delres);

                                    if (delres != null)
                                    {
                                        _log.EventLog("delete Response" + delres.ToString());
                                        var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(delres);
                                        if (responseData1.result == -1)
                                        {
                                            _log.EventLog("Deletion failed " + responseData1.message.ToString());
                                            _log.ErrLog(delres + "\n " + "Journal Intercompany Deletion Failed  \n Error Message : " + responseData1.message + "\n " + delerr);
                                            MessageBox.Show("Journal Intercompany Updation Failed");
                                            throw new Exception("Journal Intercompany Updation Failed");
                                        }
                                        else
                                        {
                                            _log.EventLog("Journal Intercompany Entry Deleted Successfully");
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("Delete response = null" + delerr);
                                        throw new Exception("Delete response = null" + delerr);
                                    }


                                }
                            }
                            headerJV.Add("DocNo", NewDocNo);
                            headerJV.Add("Date", Date);
                            headerJV.Add("Currency__Id", Convert.ToInt32(dr["iCurrencyId"]));
                            headerJV.Add("ExchangeRate", Convert.ToDecimal(dr["exrate"]));
                            headerJV.Add("Company Name__Id", Convert.ToInt32(dr["Paid_By"]));
                            headerJV.Add("Source_Document_Number", dr["sAbbr"].ToString()+"-" +dr["sVoucherNo"].ToString());
                            headerJV.Add("sNarration", Convert.ToString(dr["sNarration"]));
                            objJVBody.Add("Deal No__Id", Convert.ToInt32(dr["dealno"]));
                            objJVBody.Add("Vendor Group __Id", Convert.ToInt32(dr["vendorgrp"]));
                            objJVBody.Add("Vessel__Id", Convert.ToInt32(dr["vessel"]));
                            objJVBody.Add("Cost Center__Id", Convert.ToInt32(dr["costcenter"]));
                            objJVBody.Add("DrAccount__Id", Convert.ToInt32(dr["IntercompanyAccount"]));
                            objJVBody.Add("CrAccount__Id", Convert.ToInt32(dr["Credit_Account_Paid_By_Company"]));
                            objJVBody.Add("Amount", Convert.ToDecimal(dr["mAmount2"]));
                            objJVBody.Add("sRemarks", Convert.ToString(dr["sRemarks"]));
                            Hashtable billRef = new Hashtable();
                            List<Hashtable> listbillRef = new List<Hashtable>();
                            billRef.Add("Reference", "New Reference");
                            listbillRef.Add(billRef);
                            objJVBody.Add("Reference", listbillRef);
                            listBodyJV.Add(objJVBody);
                            

                            var postingData1 = new PostingData();
                            postingData1.data.Add(new Hashtable { { "Header", headerJV }, { "Body", listBodyJV } });
                            string sContent1 = JsonConvert.SerializeObject(postingData1);
                            _log.EventLog("content " + sContent1);
                            string err1 = "";
                            string Url1 = baseUrl + "/Transactions/Vouchers/Journal Intercompany";
                            //string sessionID = GetSessionId(CompId);
                            _log.EventLog("Url "+ Url1);
                            var response1 = Focus8API.Post(Url1, sContent1, sessionId, ref err1);
                            if (response1 != null)
                            {
                                _log.EventLog("posting Response" + response1.ToString());
                                var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                if (responseData1.result == -1)
                                {
                                    _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                    _log.ErrLog(response1 + "\n " + "Journal Intercompany Entry Posted Failed  \n Error Message : " + responseData1.message + "\n " + err1);
                                    PostedFlag = false;
                                }
                                else
                                {
                                    //Message = "JV WIP Reversal Entry Posted Successfully" + "\n";
                                    _log.EventLog("Journal Intercompany Entry Posted Success");
                                    //MessageBox.Show("Journal Intercompany Posted Successfully");
                                }
                            }
                            else
                            {
                                PostedFlag = false;
                                _log.EventLog("Posting response = null" + err1);
                            }
                        }
                        if (PostedFlag)
                        {
                            MessageBox.Show("Journal Intercompany Posted Successfully");
                        }
                    }
                }
                else
                {
                    _log.EventLog("Get data count is 0");
                }

            }
            catch (Exception ex)
            {
                _log.ErrLog(ex.Message + "Trigger.After_Save_Trigger()");
                rtnFlag = false;
                
            }

            return rtnFlag;
        }
        //public string GetSessionId(int CompId)
        //{
        //    string sSessionId = "";
        //    try
        //    {
        //        string strServer = getServiceLink();
        //        _log.EventLog("strServer " + strServer);
        //        int ccode = CompId;
        //        string User_Name = BL_Configdata.UserName;
        //        string Password = BL_Configdata.Password;


        //        var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + strServer + "/focus8api/Login");
        //        httpWebRequest.ContentType = "application/json";
        //        httpWebRequest.Method = "POST";

        //        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        //        {
        //            string json = "{" + "\"data\": [{" + "\"Username\":\"" + User_Name + "\"," + "\"password\":\"" + Password + "\"," + "\"CompanyId\":\"" + ccode + "\"}]}";
        //            streamWriter.Write(json);
        //            _log.EventLog("json " + json);
        //        }

        //        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        //        StreamReader Updatereader = new StreamReader(httpResponse.GetResponseStream());
        //        string Udtcontent = Updatereader.ReadToEnd();

        //        JObject odtbj = JObject.Parse(Udtcontent);
        //        Temperatures Updtresult = JsonConvert.DeserializeObject<Temperatures>(Udtcontent);
        //        _log.EventLog("updateresult " + Updtresult.Result.ToString());
        //        if (Updtresult.Result == 1)
        //        {
        //            sSessionId = Updtresult.Data[0].FSessionId;
        //        }


        //        return sSessionId;
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.ErrLog(ex.ToString());
        //    }
        //    return sSessionId;
        //}
        public string getServiceLink()
        {
            XmlDocument xmlDoc = new XmlDocument();
            string strFileName = "";
            
            string PrgmFilesPath = "C:\\Program Files (x86)";
            if (!Directory.Exists(PrgmFilesPath))
            {
                PrgmFilesPath = "C:\\Program Files";
            }
            string sAppPath = PrgmFilesPath+"\\Focus Softnet\\Focus8";
            strFileName = sAppPath + "\\ERPXML\\ServerSettings.xml";
            xmlDoc.Load(strFileName);
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/ServSetting/MasterServer/ServerName");
            string strValue;
            XmlNode node = nodeList[0];
            if (node != null)
                strValue = node.InnerText;
            else
                strValue = "";
            return strValue;
        }

        public bool After_Save_Trigger2(Transaction objTrans, VWVoucherSettings objSettings, int iField, String strFieldName, int iRowindex)
        {
            bool rtnFlag = true;
            bool PostedFlag = true;
            try
            {

                _log.EventLog("entered After_Save_Trigger2");
                int Date = Convert.ToInt32(objTrans.Header.Date);
                _log.EventLog("Date " + Date.ToString());
                int DocNo = Convert.ToInt32(objTrans.Header.DocNo);
                _log.EventLog("DocNo " + DocNo.ToString());
                string VoucherType = Convert.ToString(objTrans.Header.VoucherType);
                _log.EventLog("VoucherType " + VoucherType);
                string HeaderId = Convert.ToString(objTrans.Header.HeaderId);
                _log.EventLog("HeaderId " + HeaderId);
                int CompId = Focus.Conn.GlobalPref.CompId;
                _log.EventLog("CompanyId " + CompId.ToString());

                DataSet ds = _db.getReceiptEntry(DocNo, CompId);
                if (ds.Tables.Count > 0)
                {
                    _log.EventLog("Dataset Count " + ds.Tables.Count);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        _log.EventLog("Dataset Count " + ds.Tables[0].Rows.Count);
                        string strServer = getServiceLink();
                        _log.EventLog("getServiceLink " + strServer);
                        string baseUrl = "http://" + strServer + "/focus8API";
                        _log.EventLog("baseUrl " + baseUrl);
                        string sessionId = Focus.Conn.GlobalPref.SessionId;
                        _log.EventLog("sessionid " + sessionId);
                        List<Hashtable> listBodyJV = new List<Hashtable>();
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            if (Convert.ToInt32(dr["IntercompanyAccount"]) == -1)
                            {
                                _log.EventLog("Intercompany Account is not mapped");
                                MessageBox.Show("Intercompany Account is not mapped");
                                throw new Exception("Intercompany Account is not mapped");
                            }
                            else if (Convert.ToInt32(dr["IntercompanyAccount"]) == Convert.ToInt32(dr["Debit_Account_Recd_Company"]))
                            {
                                _log.EventLog("IntercompanyAccount and Debit_Account_Recd_Company are same");
                                MessageBox.Show("Credit and Debit Account Should not be Same");
                                throw new Exception("IntercompanyAccount and Debit_Account_Recd_Company are same");
                            }
                            if (Convert.ToInt32(dr["creditactype"]) == 7 || Convert.ToInt32(dr["debitactype"]) == 7)
                            {
                                _log.EventLog("Debit or Credit Account should not be Customer / Vendor Account");
                                MessageBox.Show("Debit or Credit Account should not be Customer / Vendor Account");
                                throw new Exception("Debit or Credit Account should not be Customer / Vendor Account");
                            }
                            if (Convert.ToInt32(dr["Paid_By"]) == -1)
                            {
                                _log.EventLog("Paid By is NONE");
                                //MessageBox.Show("Paid By should not be NONE");
                                throw new Exception("Paid By is NONE");
                            }
                            Hashtable headerJV = new Hashtable();
                            Hashtable objJVBody = new Hashtable();

                            DataSet dsExists = _db.getIfExists(dr["sAbbr"].ToString() + "-" + dr["sVoucherNo"].ToString());
                            string NewDocNo = "";
                            if (dsExists.Tables.Count > 0)
                            {
                                if (dsExists.Tables[0].Rows.Count > 0)
                                {
                                    NewDocNo = dsExists.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                    _log.EventLog("NewDocNo " + NewDocNo);
                                    string delerr = "";
                                    string delurl = baseUrl + "/Transactions/Journal Intercompany/" + NewDocNo;
                                    _log.EventLog("delurl " + delurl);
                                    var delres = Focus8API.Delete(delurl, sessionId, ref delerr);
                                    _log.EventLog("delres " + delres);

                                    if (delres != null)
                                    {
                                        _log.EventLog("delete Response" + delres.ToString());
                                        var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(delres);
                                        if (responseData1.result == -1)
                                        {
                                            _log.EventLog("Deletion failed " + responseData1.message.ToString());
                                            _log.ErrLog(delres + "\n " + "Journal Intercompany Deletion Failed  \n Error Message : " + responseData1.message + "\n " + delerr);
                                            MessageBox.Show("Journal Intercompany Updation Failed");
                                            throw new Exception("Journal Intercompany Updation Failed");
                                        }
                                        else
                                        {
                                            _log.EventLog("Journal Intercompany Entry Deleted Successfully");
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("Delete response = null" + delerr);
                                        throw new Exception("Delete response = null" + delerr);
                                    }


                                }
                            }
                            headerJV.Add("DocNo", NewDocNo);
                            headerJV.Add("Date", Date);
                            headerJV.Add("Currency__Id", Convert.ToInt32(dr["iCurrencyId"]));
                            headerJV.Add("ExchangeRate", Convert.ToDecimal(dr["exrate"]));
                            headerJV.Add("Company Name__Id", Convert.ToInt32(dr["Paid_By"]));
                            headerJV.Add("Source_Document_Number", dr["sAbbr"].ToString() + "-" + dr["sVoucherNo"].ToString());
                            headerJV.Add("sNarration", Convert.ToString(dr["sNarration"]));
                            objJVBody.Add("Deal No__Id", Convert.ToInt32(dr["dealno"]));
                            objJVBody.Add("Vendor Group __Id", Convert.ToInt32(dr["vendorgrp"]));
                            objJVBody.Add("Vessel__Id", Convert.ToInt32(dr["vessel"]));
                            objJVBody.Add("Cost Center__Id", Convert.ToInt32(dr["costcenter"]));
                            objJVBody.Add("CrAccount__Id", Convert.ToInt32(dr["IntercompanyAccount"]));
                            objJVBody.Add("DrAccount__Id", Convert.ToInt32(dr["Debit_Account_Recd_Company"]));
                            objJVBody.Add("Amount", Convert.ToDecimal(dr["mAmount2"]));
                            objJVBody.Add("sRemarks", Convert.ToString(dr["sRemarks"]));
                            Hashtable billRef = new Hashtable();
                            List<Hashtable> listbillRef = new List<Hashtable>();
                            billRef.Add("Reference", "New Reference");
                            listbillRef.Add(billRef);
                            objJVBody.Add("Reference", listbillRef);
                            listBodyJV.Add(objJVBody);


                            var postingData1 = new PostingData();
                            postingData1.data.Add(new Hashtable { { "Header", headerJV }, { "Body", listBodyJV } });
                            string sContent1 = JsonConvert.SerializeObject(postingData1);
                            _log.EventLog("content " + sContent1);
                            string err1 = "";
                            string Url1 = baseUrl + "/Transactions/Vouchers/Journal Intercompany";
                            //string sessionID = GetSessionId(CompId);
                            _log.EventLog("Url " + Url1);
                            var response1 = Focus8API.Post(Url1, sContent1, sessionId, ref err1);
                            if (response1 != null)
                            {
                                _log.EventLog("posting Response" + response1.ToString());
                                var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                if (responseData1.result == -1)
                                {
                                    _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                    _log.ErrLog(response1 + "\n " + "Journal Intercompany Entry Posted Failed  \n Error Message : " + responseData1.message + "\n " + err1);
                                    PostedFlag = false;
                                }
                                else
                                {
                                    //Message = "JV WIP Reversal Entry Posted Successfully" + "\n";
                                    _log.EventLog("Journal Intercompany Entry Posted Success");
                                    //MessageBox.Show("Journal Intercompany Posted Successfully");
                                }
                            }
                            else
                            {
                                PostedFlag = false;
                                _log.EventLog("Posting response = null" + err1);
                            }
                        }
                        if (PostedFlag)
                        {
                            MessageBox.Show("Journal Intercompany Posted Successfully");
                        }
                    }
                }
                else
                {
                    _log.EventLog("Get data count is 0");
                }

            }
            catch (Exception ex)
            {
                _log.ErrLog(ex.Message + "Trigger.After_Save_Trigger2()");
                rtnFlag = false;

            }

            return rtnFlag;
        }
    }
}
