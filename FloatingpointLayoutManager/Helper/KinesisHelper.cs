using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Kitsune.Models;
using KitsuneLayoutManager.Helper;
using System;
using System.IO;
using System.Text;

public class KinesisHelper
{
    private const string FPWebLogStreamName = "FP-WebLog-Stream";
    private const string kistuneAuditWebLogStreamName = "kitsune-audit-weblog-stream";
    private static AmazonKinesisClient kinesisClient;

    static KinesisHelper()
    {
        InitiateAWSClient();
    }

    internal static void InitiateAWSClient()
    {
        try
        {
            if (kinesisClient == null)
            {
                kinesisClient = new AmazonKinesisClient("[[KIT_CLOUD_AWS_ACCESS_KEY]]", "[[KIT_CLOUD_AWS_SECRET_KEY]]", Amazon.RegionEndpoint.APSouth1);
            }
        }
        catch (Exception ex)
        {
            //EventLogger.Write(ex, "FlowLayoutManager exception occured while processing the request for InitiateAWSClient");
        }
    }

    //FPWebLog
    //This function will push to kinesis with details From Request and Lambda will process accordingly
    public static void LogFPRequestDetailsIntoKinesis(object state)
    {
        try
        {
            var objArray = state as object[];
            var fpCode = objArray[0] as string;
            var ip = objArray[1] as string;
            var agent = objArray[2] as string;
            var url = objArray[3] as string;
            var isCrawler = objArray[4] as string;

            if (!String.IsNullOrEmpty(fpCode))
            {
                if (kinesisClient == null)
                {
                    InitiateAWSClient();
                }

                string record = String.Format("\"FPCode\":\"{0}\", \"IP\":\"{1}\", \"UserAgent\":\"{2}\", \"URL\":\"\", \"IsCrawler\":\"\"", fpCode, ip, agent, url, isCrawler);
                byte[] dataAsBytes = Encoding.UTF8.GetBytes("{" + record + "}");

                string sequenceNumber = string.Empty;
                using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
                {
                    try
                    {
                        PutRecordRequest requestRecord = new PutRecordRequest();
                        requestRecord.StreamName = FPWebLogStreamName;
                        requestRecord.PartitionKey = "FPWebLog-Stream";
                        requestRecord.Data = memoryStream;

                        PutRecordResponse responseRecord = kinesisClient.PutRecord(requestRecord);
                        sequenceNumber = responseRecord.SequenceNumber;
                    }
                    catch (Exception ex)
                    {
                        //EventLogger.Write(ex, "FlowLayoutManager Exception occured while processing the request LogFPRequestDetailsIntoKinesis");
                        throw ex;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            //EventLogger.Write(ex, "FlowLayoutManager Exception occured while processing the request LogFPRequestDetailsIntoKinesis");
        }
    }

    //FPRefererLog
    //This function will push to kinesis with details From Request and Lambda will process accordingly
    public static void LogFPRefererRequestDetailsIntoKinesis(object state)
    {
        try
        {
            var objArray = state as object[];
            var fpCode = objArray[0] as string;
            var referer = objArray[1] as string;
            var ip = objArray[2] as string;

            if (!String.IsNullOrEmpty(fpCode) && !String.IsNullOrEmpty(referer))
            {
                if (kinesisClient == null)
                {
                    InitiateAWSClient();
                }

                string record = String.Format("\"FPCode\":\"{0}\", \"Referer\":\"{1}\", \"IP\":\"{2}\"", fpCode, referer, ip);
                byte[] dataAsBytes = Encoding.UTF8.GetBytes("{" + record + "}");

                string sequenceNumber = string.Empty;
                using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
                {
                    try
                    {
                        PutRecordRequest requestRecord = new PutRecordRequest();
                        requestRecord.StreamName = FPWebLogStreamName;
                        requestRecord.PartitionKey = "FPRefererLog-Stream";
                        requestRecord.Data = memoryStream;

                        PutRecordResponse responseRecord = kinesisClient.PutRecord(requestRecord);
                        sequenceNumber = responseRecord.SequenceNumber;
                    }
                    catch (Exception ex)
                    {
                        //EventLogger.Write(ex, "FlowLayoutManager Exception occured while processing the request LogFPRefererRequestDetailsIntoKinesis");
                        throw ex;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            //EventLogger.Write(ex, "FlowLayoutManager Exception occured while processing the request LogFPRefererRequestDetailsIntoKinesis");
        }
    }

    internal static void LogKLMRequestDetailsIntoKinesis(KLMAuditLogModel model, string url)
    {
        try
        {
            if (!String.IsNullOrEmpty(model.fpTag))
            {
                if (kinesisClient == null)
                {
                    InitiateAWSClient();
                }

                string record = String.Format("\"AuditId\":\"{0}\", \"ThemeId\":\"{1}\", \"ThemeCode\":\"{2}\", \"FunctionLog\":\"{3}\", \"LoadTime\":\"{4}\", \"FPTag\":\"{5}\", \"IP\":\"{6}\", \"FPCode\":\"{7}\", \"URL\":\"{8}\", \"CreatedOn\":\"{9}\"", model._id, model.themeId, model.fpCode, model.functionalLog, model.loadTime, model.fpTag, model.ipAddress, model.fpCode, url, DateTime.Now);
                byte[] dataAsBytes = Encoding.UTF8.GetBytes("{" + record + "}");

                string sequenceNumber = string.Empty;
                using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
                {
                    try
                    {
                        PutRecordRequest requestRecord = new PutRecordRequest();
                        requestRecord.StreamName = kistuneAuditWebLogStreamName;
                        requestRecord.PartitionKey = "audit-log";
                        requestRecord.Data = memoryStream;

                        //PutRecordResponse responseRecord = kinesisClient.PutRecordAsync(requestRecord);
                        kinesisClient.PutRecord(requestRecord);
                        //sequenceNumber = responseRecord.SequenceNumber;
                    }
                    catch (Exception ex)
                    {
                        EventLogger.Write(ex, "Exception occured while processing the request LogKLMRequestDetailsIntoKinesis");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            EventLogger.Write(ex, "Exception occured while processing the request LogKLMRequestDetailsIntoKinesis");
        }
    }
}