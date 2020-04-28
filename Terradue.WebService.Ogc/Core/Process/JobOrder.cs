using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Terradue.ServiceModel.Ogc.Wps10;
using Terradue.WebService.Ogc.Wps;

namespace Terradue.WebService.Ogc.Core
{
	public class JobOrder
	{

        public DateTime CreationTime { get; set; }
        public Execute ExecuteRequest { get; set; }
		public ExecuteResponse ExecuteResponse { get; set; }

		public JobProgress JobProgress { get; set; }
        public string Uid { get; set; }

        public JobCacheHandler JobCache { get; set; }

        private RecoveryInfo _recoveryInfo;
        public RecoveryInfo RecoveryInfo {
            get {
                if (_recoveryInfo == null && !string.IsNullOrEmpty(this.Uid))
                    _recoveryInfo = ReadRecoveryInfo(this.Uid);
                return _recoveryInfo;
            }
            set {
                _recoveryInfo = value;
            }
        }

        public JobOrder() {
        }
        
        public void Report(StatusType status) {
            var response = new ExecuteResponse();
            response.Status = status;
            Report(response);
        }

        public void SetRecoveryInfo(RecoveryInfo recoveryInfo) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"]) && recoveryInfo != null) {
                var filepath = string.Format("{0}/{1}.RecoveryInfo.json", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], this.Uid);
                if (recoveryInfo != null && !File.Exists(filepath)) {
                    var json = JsonSerializer.Serialize<RecoveryInfo>(recoveryInfo);
                    System.IO.File.WriteAllText(filepath, json);
                }
            }
        }

        public static RecoveryInfo ReadRecoveryInfo(string uid) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"])) {
                try {
                    var filepath = string.Format("{0}/{1}.RecoveryInfo.json", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], uid);
                    var jsonString = File.ReadAllText(filepath);
                    var recoveryInfo = JsonSerializer.Deserialize<RecoveryInfo>(jsonString);
                    return recoveryInfo;
                }catch(Exception e) {
                    return null;
                }
            }
            return null;
        }

        public static void WriteExecuteRequest(Execute execute, string uid) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"]) && execute != null) {
                var filepath = string.Format("{0}/{1}.ExecuteRequest.xml", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], uid);
                if (execute != null && !File.Exists(filepath)) {
                    XmlSerializer serializer = new XmlSerializer(typeof(Execute));
                    Stream fs = new FileStream(filepath, FileMode.Create);
                    XmlWriter writer = new XmlTextWriter(fs, Encoding.Unicode);
                    serializer.Serialize(writer, execute);
                    writer.Close();
                }
            }
        }

        public static Execute ReadExecuteRequest(string uid) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"])) {
                try { 
                    var filepath = string.Format("{0}/{1}.ExecuteRequest.xml", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], uid);
                    XmlSerializer serializer = new XmlSerializer(typeof(Execute));
                    Execute execute = (Execute)serializer.Deserialize(File.OpenText(filepath));
                    return execute;
                } catch (Exception e) {
                    return null;
                }
            }
            return null;
        }

        public static void WriteExecuteResponse(ExecuteResponse execute, string uid) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"]) && execute != null) {
                var filepath = string.Format("{0}/{1}.ExecuteResponse.xml", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], uid);
                if (execute != null) {
                    XmlSerializer serializer = new XmlSerializer(typeof(ExecuteResponse));
                    Stream fs = new FileStream(filepath, FileMode.Create);
                    XmlWriter writer = new XmlTextWriter(fs, Encoding.Unicode);
                    serializer.Serialize(writer, execute);
                    writer.Close();
                }
            }
        }

        public static ExecuteResponse ReadExecuteResponse(string uid) {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"])) {
                try { 
                    var filepath = string.Format("{0}/{1}.ExecuteResponse.xml", System.Configuration.ConfigurationManager.AppSettings["app:recoveryfiles_path"], uid);
                    XmlSerializer serializer = new XmlSerializer(typeof(ExecuteResponse));
                    ExecuteResponse execute = (ExecuteResponse)serializer.Deserialize(File.OpenText(filepath));
                    return execute;
                } catch (Exception e) {
                    return null;
                }
            }
            return null;
        }

        public void Report(ExecuteResponse response) {
            this.ExecuteResponse = response;
            JobProgress.Report(response.Status);

            WriteExecuteResponse(response, this.Uid);
        }

    }
}