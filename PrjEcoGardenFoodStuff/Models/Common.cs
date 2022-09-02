using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Xml;

namespace PrjEcoGardenFoodStuff.Models
{
    public class Common
    {
        public string serverip = WebConfigurationManager.AppSettings["Server_Ip"];
        public string username = WebConfigurationManager.AppSettings["User_Name"];
        public string Password = WebConfigurationManager.AppSettings["Password"];
        public string getDBServer_Details(string tagname)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string strFileName = "";
            string PrgmFilesPath = AppDomain.CurrentDomain.BaseDirectory;

            strFileName = PrgmFilesPath + "\\XMLFiles\\DBConfig.xml";
            xmlDoc.Load(strFileName);
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/DatabaseConfig/Database/" + tagname + "");
            string strValue;
            XmlNode node = nodeList[0];
            if (node != null)
                strValue = node.InnerText;
            else
                strValue = "";
            return strValue;
        }

        public DataSet GetData(string strSelQry, int CompId, ref string error)
        {
            try
            {
                string FSQLPWD = getDBServer_Details("Password");
                string FSQLUID = getDBServer_Details("User_Id");
                string FSerName = getDBServer_Details("Data_Source");
                string FDB = "Focus8Erp";
                string Fconnection = $"Server={FSerName};Database={FDB};User Id={FSQLUID};Password={FSQLPWD};";
                EventLog("Fconnection " + Fconnection);
                SqlConnection con = new SqlConnection(Fconnection);
                con.Open();
                EventLog("sql con opened ");
                SqlCommand cmd = new SqlCommand(strSelQry, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                EventLog("sql cmd executed ");
                DataSet ds = new DataSet();
                da.Fill(ds);
                con.Close();
                return ds;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
        public DataSet GetData2(string strSelQry, int CompId, string CCode, ref string error)
        {
            try
            {
                string FSQLPWD = getDBServer_Details("Password");
                string FSQLUID = getDBServer_Details("User_Id");
                string FSerName = getDBServer_Details("Data_Source");
                string FDB = "Focus8" + CCode;
                string Fconnection = $"Server={FSerName};Database={FDB};User Id={FSQLUID};Password={FSQLPWD};";
                EventLog("Fconnection " + Fconnection);
                SqlConnection con = new SqlConnection(Fconnection);
                con.Open();
                EventLog("sql con opened ");
                SqlCommand cmd = new SqlCommand(strSelQry, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                EventLog("sql cmd executed ");
                DataSet ds = new DataSet();
                da.Fill(ds);
                con.Close();
                return ds;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
        public int SetData(string strSelQry, int CompId, string CCode, ref string error)
        {
            try
            {
                string FSQLPWD = getDBServer_Details("Password");
                string FSQLUID = getDBServer_Details("User_Id");
                string FSerName = getDBServer_Details("Data_Source");
                string FDB = "Focus8" + CCode;
                string Fconnection = $"Server={FSerName};Database={FDB};User Id={FSQLUID};Password={FSQLPWD};";
                EventLog("Fconnection " + Fconnection);
                SqlConnection con = new SqlConnection(Fconnection);
                con.Open();
                EventLog("sql con opened ");
                SqlCommand cmd = new SqlCommand(strSelQry, con);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                error = e.Message;
                return -1;
            }
        }
        public void EventLog(string content)
        {
            try
            {
                string folderName = AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\Logs";
                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                string str = "Logs/" + "EventLog-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                FileStream stream = new FileStream(AppDomain.CurrentDomain.BaseDirectory.ToString() + str, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(stream);
                writer.BaseStream.Seek(0L, SeekOrigin.End);
                writer.WriteLine(DateTime.Now.ToString() + " - " + content);
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
        }
        public void ErrLog(string content)
        {
            try
            {
                string folderName = AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\Logs";
                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                string str = "Logs/" + "ErrorLog-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                FileStream stream = new FileStream(AppDomain.CurrentDomain.BaseDirectory.ToString() + str, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(stream);
                writer.BaseStream.Seek(0L, SeekOrigin.End);
                writer.WriteLine(DateTime.Now.ToString() + " - " + content);
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
        }
        public string GetCCode(int CompId)
        {
            string error = "";
            try
            {
                string getCompCode = "select sCompanyCode from tCore_Company_Details where iCompanyId = " + CompId;
                EventLog("GetCCode getCompCode = " + getCompCode);
                DataSet dsCo = GetData(getCompCode, CompId, ref error);
                string CompCode = dsCo.Tables[0].Rows[0][0].ToString();
                EventLog("GetCCode CompCode = " + CompCode);
                return CompCode;
            }
            catch (Exception ex)
            {
                ErrLog(ex.Message);
                return ex.Message;
            }
        }
        public string getsessionid(string usrename, string password, string companycode)
        {
            string sid = "";
            ClsProperties.Datum datanum = new ClsProperties.Datum();
            datanum.CompanyCode = companycode;
            datanum.Username = usrename;
            datanum.password = password;
            List<ClsProperties.Datum> lstd = new List<ClsProperties.Datum>();
            lstd.Add(datanum);
            ClsProperties.Lolgin lngdata = new ClsProperties.Lolgin();
            lngdata.data = lstd;
            string sContent = JsonConvert.SerializeObject(lngdata);
            EventLog(sContent);
            EventLog("http://" + serverip + "/focus8API/Login");
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                var arrResponse = client.UploadString("http://" + serverip + "/focus8API/Login", sContent);
                ClsProperties.Resultlogin lng = JsonConvert.DeserializeObject<ClsProperties.Resultlogin>(arrResponse);
                sid = lng.data[0].fSessionId;
                EventLog(sid);
            }

            return sid;
        }
        public static string Post(string url, string data, string sessionId, ref string err)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("fSessionId", sessionId);
                    client.Headers.Add("Content-Type", "application/json");
                    var response = client.UploadString(url, data);

                    return response;
                }
            }
            catch (Exception e)
            {

                err = e.Message;
                return null;
            }

        }
    }
}